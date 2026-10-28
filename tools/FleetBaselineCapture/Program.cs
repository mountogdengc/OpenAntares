using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;

// A one-time capture tool, deliberately outside the game/solution. Never update frozen futures
// with a changed engine: both tracked and untracked product edits are rejected before capture.
if (args.Length == 0 || args[0] != "--capture") throw new ArgumentException("Use --capture and the reviewed explicit options.");
var options = new Dictionary<string, string>(StringComparer.Ordinal);
for (int i = 1; i < args.Length; i += 2)
{
    if (i + 1 == args.Length || !options.TryAdd(args[i], args[i + 1])) throw new ArgumentException("Invalid capture options.");
}
if (options.Keys.Except(new[] { "--root", "--output", "--ui-results", "--turns", "--seed" }).Any()) throw new ArgumentException("Unknown option.");
string root = Path.GetFullPath(options["--root"]);
string output = WithinRoot(options["--output"]);
string uiRoot = WithinRoot(options["--ui-results"]);
int turns = int.Parse(options["--turns"], CultureInfo.InvariantCulture);
ulong seed = ulong.Parse(options["--seed"], CultureInfo.InvariantCulture);
if (turns != 4 || seed != 42 || Rules.CurrentVersion != 7 || SaveGame.SchemaVersion != 8) throw new InvalidOperationException("Capture requires the approved unchanged rules7/writer8 engine, seed42 and four boundaries.");
if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new InvalidOperationException("Refusing to overwrite an existing archive.");
if (!string.IsNullOrEmpty(Git("diff", "7d175ad", "--", "src", "game", "content")) ||
    Git("ls-files", "--others", "--exclude-standard", "src", "game", "content").Length != 0)
    throw new InvalidOperationException("Product changed after the approved pre-capture source. Do not regenerate old futures.");
string sourceCommit = Git("rev-parse", "HEAD");
string staging = WithinRoot(output + ".capture-attempt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture));
Directory.CreateDirectory(staging);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var cases = new List<object>();
var uiSources = new List<object>();
var missingSources = new List<string>();
var coverage = new SortedDictionary<string, bool>(StringComparer.Ordinal);
int generatedCount = 0, uiCaseCount = 0, sequence = 0;
var load = ContentLoader.LoadDirectory(Path.Combine(root, "content/core"));
if (!load.Succeeded) throw new InvalidOperationException(string.Join("; ", load.Errors));
var content = load.Content!;
foreach (string species in content.Species.Keys.OrderBy(x => x, StringComparer.Ordinal))
foreach (string size in content.GalaxySizes.Keys.OrderBy(x => x, StringComparer.Ordinal))
foreach (bool enabled in new[] { false, true })
foreach (int rivals in new[] { 0, 1 })
{
    var game = GalaxyGenerator.CreateNewGame(content, new(size, seed, 7, species, rivals, rivals == 1 ? "peldra" : null, CrisesEnabled: true, AmebasEnabled: enabled));
    if (!game.Succeeded) throw new InvalidOperationException(game.Error);
    string name = $"generated-{species}-{size}-{enabled}-{rivals}";
    string save = "sources/" + name + ".json";
    WriteBytes(save, Encoding.UTF8.GetBytes(SaveGame.Serialize(content, game.State!)));
    Capture(name, save, new(content, game.State!), "Generated normally by unchanged GalaxyGenerator with explicit rules7 parameters; no forced fields.");
    generatedCount++;
}
foreach (string result in Directory.GetFiles(uiRoot, "results.json", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
{
    string scenario = Path.GetFileName(Path.GetDirectoryName(result))!;
    string resultCopy = "source-results/" + scenario + "-results.json";
    WriteBytes(resultCopy, File.ReadAllBytes(result));
    var report = JsonNode.Parse(File.ReadAllText(result))!;
    foreach (var path in report["retainedQaSavePaths"]?.AsArray() ?? new JsonArray())
    {
        string source = path!.GetValue<string>();
        if (!File.Exists(source)) { missingSources.Add(source); continue; }
        string name = $"ui-{scenario}-{sequence++:000}";
        string copy = "sources/" + name + ".json";
        byte[] bytes = File.ReadAllBytes(source);
        WriteBytes(copy, bytes);
        var game = SaveGame.ReadFile(source);
        uiSources.Add(new { source, copy, sha256 = Hash(bytes), sourceResult = resultCopy, valid = game.Succeeded, rulesVersion = game.Game?.State.RulesVersion, errors = game.Errors, provenance = "Actual retained UI save; original scenario result/provenance copied unchanged. Generated versus labelled supplement is determined by that source result, not inferred from rules version." });
        if (!game.Succeeded || game.Game!.State.RulesVersion != 7) continue;
        Capture(name, copy, game.Game, "Retained actual UI save; refer to original scenario provenance in source-results.");
        uiCaseCount++;
    }
}
foreach (string required in new[] { "committedTransit", "warning", "exposed", "governor", "plague", "cargo", "founding" })
    if (!coverage.GetValueOrDefault(required)) throw new InvalidOperationException("Genuine-source coverage missing: " + required + ". Preserve this failed staging capture and add an ordinary-command source on the unchanged engine.");
if (missingSources.Count != 0) throw new InvalidOperationException("QA source files missing; cannot silently claim they were copied: " + string.Join("; ", missingSources));
var manifest = new
{
    rulesVersion = 7, writerVersion = 8, turns, seed, sourceCommit,
    approvedPreProductCommit = "7d175ad07f284a0d07a0f0c0ec21326ddeba9b0c",
    productTree = Git("rev-parse", "HEAD:src/OpenAntares.Simulation"),
    programSha256 = Hash(File.ReadAllBytes(Path.Combine(root, "tools/FleetBaselineCapture/Program.cs"))),
    generatedSources = generatedCount, uiFutureSources = uiCaseCount, uiSources, missingSources,
    coverage, cases
};
WriteJson("manifest.json", manifest);
// Both absolute paths were checked within this repository; destination must not already exist.
if (Directory.Exists(output)) throw new InvalidOperationException("Archive destination appeared during capture; preserve staging and inspect it.");
Directory.Move(staging, output);
Console.WriteLine(JsonSerializer.Serialize(new { passed = true, generatedCount, uiCaseCount, uiSources = uiSources.Count, cases = cases.Count, futureBoundaries = cases.Count * turns, coverage, sourceCommit, output }, jsonOptions));

string WithinRoot(string path)
{
    string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
    if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Capture paths must remain within the repository.");
    return full;
}
string Git(params string[] arguments)
{
    var info = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
    foreach (string arg in arguments) info.ArgumentList.Add(arg);
    using var process = Process.Start(info)!;
    string value = process.StandardOutput.ReadToEnd(); process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException("git failed: " + string.Join(" ", arguments));
    return value.Trim();
}
void WriteBytes(string relative, byte[] bytes)
{
    string path = Path.Combine(staging, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes);
}
void WriteJson(string relative, object data) => WriteBytes(relative, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, jsonOptions)));
string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
string Canonical(GameSession session)
{
    var node = JsonNode.Parse(SaveGame.Serialize(session.Content, session.State))!.AsObject(); node.Remove("schema_version"); return node.ToJsonString();
}
void Observe(GameState state)
{
    void Seen(string name, bool present) => coverage[name] = present || coverage.GetValueOrDefault(name);
    Seen("committedTransit", state.Vessels.Any(v => v.Transit is not null));
    Seen("pendingMove", state.Vessels.Any(v => v.PendingMove is not null));
    Seen("warning", state.Vessels.Any(v => v.AmebaEncounter?.Stage == AmebaStage.Warning));
    Seen("exposed", state.Vessels.Any(v => v.AmebaEncounter?.Stage == AmebaStage.Exposed));
    Seen("governor", state.Colonies.Any(c => c.Development is { } d && (d.WorkforceEnabled || d.ConstructionEnabled || d.Queue.Count > 0)));
    Seen("plague", state.Empires.Any(e => e.Crisis?.Episode is not null) || state.Colonies.Any(c => c.Crisis?.Case is not null));
    Seen("cargo", state.Vessels.Any(v => v.CarriedPopulation > 0));
    Seen("founding", state.Vessels.Any(v => v.PendingFounding is not null) || state.Empires.Any(e => state.Colonies.Count(c => c.EmpireId == e.Id) > 1));
}
void Capture(string name, string save, LoadedGame game, string provenance)
{
    Observe(game.State);
    var session = new GameSession(game.Content, game.State);
    var future = new List<string>(); var commands = new List<List<object>>();
    for (int boundary = 0; boundary < turns; boundary++)
    {
        int from = session.State.Turn; var steps = new List<object>();
        foreach (EmpireId owner in session.State.Empires.Where(e => e.Controller == ControllerKind.Human && !e.Ready).OrderBy(e => e.Id).Select(e => e.Id).ToArray())
        {
            // This is an explicit QA command policy, not new game automation or forced state.
            // Respect existing pending orders. For an otherwise unanswered encounter choose an
            // offered explicit Remain response, with its exact revision, then acknowledge it.
            foreach (var row in ScoutQueries.Project(session.Content, session.State, owner).Vessels.Where(v => v.Stage is not null && v.EffectiveDestination is null && v.RiskAction is null))
                Execute(row.Actions.OfType<SetAmebaRiskOrderCommand>().Single(c => c.Action == AmebaRiskAction.Remain));
            var crisis = CrisisQueries.PendingAttention(session.State, owner);
            if (!crisis.IsEmpty) Execute(new AcknowledgeCrisisCommand(owner, session.State.Turn, crisis));
            var attention = ScoutQueries.Project(session.Content, session.State, owner).Attention;
            if (!attention.IsEmpty) Execute(new AcknowledgeAmebaCommand(owner, session.State.Turn, attention));
            Execute(new SetReadyCommand(owner, session.State.Turn, true));
        }
        if (session.State.Turn != from + 1) throw new InvalidOperationException(name + ": did not resolve exactly one boundary.");
        Observe(session.State); future.Add(Canonical(session)); commands.Add(steps);
        var roundTrip = SaveGame.Deserialize(SaveGame.Serialize(session.Content, session.State));
        if (!roundTrip.Succeeded || Canonical(new(roundTrip.Game!.Content, roundTrip.Game.State)) != future[^1]) throw new InvalidOperationException(name + ": reload changed a frozen future.");
        void Execute(Command command)
        {
            var outcome = session.Execute(command);
            if (!outcome.Accepted) throw new InvalidOperationException(name + " turn" + from + " " + command.GetType().Name + ": " + outcome.Rejection!.Message);
            steps.Add(new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()) });
        }
    }
    string futurePath = "futures/" + name + ".json", commandsPath = "commands/" + name + ".json";
    WriteJson(futurePath, future); WriteJson(commandsPath, commands);
    cases.Add(new { name, save, saveSha256 = Hash(File.ReadAllBytes(Path.Combine(staging, save))), futures = futurePath, commands = commandsPath, provenance });
}
