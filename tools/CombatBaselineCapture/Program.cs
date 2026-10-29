using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;

if (args.Length != 3 || args[0] != "--capture") throw new ArgumentException("Use --capture ROOT OUTPUT; this tool never overwrites archives.");
string root = Path.GetFullPath(args[1]);
string output = Path.GetFullPath(Path.Combine(root, args[2]));
if (!output.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output must remain inside checkout.");
if (Directory.Exists(output)) throw new InvalidOperationException("Archive already exists; never regenerate frozen expectations.");
if (Rules.CurrentVersion != 8 || SaveGame.SchemaVersion != 9 || Git("diff", "dc19093", "--", "src", "game", "content").Length != 0 || Git("ls-files", "--others", "--exclude-standard", "src", "game", "content").Length != 0)
    throw new InvalidOperationException("Capture requires unchanged reviewed rules8/writer9 product, including no untracked product edits.");
string staging = output + ".attempt-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff", CultureInfo.InvariantCulture);
Directory.CreateDirectory(staging);
var options = new JsonSerializerOptions { WriteIndented = true };
var loaded = ContentLoader.LoadDirectory(Path.Combine(root, "content/core"));
if (!loaded.Succeeded) throw new InvalidOperationException(string.Join("; ", loaded.Errors));
var content = loaded.Content!;
var cases = new List<object>();
var origins = new List<object>();
var coverage = new SortedDictionary<string, bool>(StringComparer.Ordinal);
int generated = 0;
foreach (string species in content.Species.Keys.OrderBy(x => x, StringComparer.Ordinal))
foreach (string size in content.GalaxySizes.Keys.OrderBy(x => x, StringComparer.Ordinal))
foreach (bool crises in new[] { false, true })
foreach (bool amebas in new[] { false, true })
{
    var settings = new NewGameSettings(size, 42, 8, species, 1, "peldra", crises, amebas);
    Capture(content, settings, new[] { BaselinePolicy.HoldReady, BaselinePolicy.LegalOwnViewPlay }, staging);
}
string evidence = Path.Combine(root, "reports/fleet-integration-20261028/fleet");
string[] named = { "generated-ui-800.json", "generated-ui-1920.json", "minor-return-800.json", "minor-return-1920.json", "review-safety-cancel-0-800.json", "review-safety-cancel-1-800.json" };
var capturedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
int supplemental = 0;
foreach (string name in named) AddSupplement(Path.Combine(evidence, name));
// Add one actual saved source for each still-missing important state. Sources remain labelled
// as retained UI/supplements: a format/version match does not establish untouched generation.
foreach (string path in Directory.EnumerateFiles(Path.Combine(root, "reports/fleet-execution"), "*.json", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
{
    if (capturedSources.Contains(path) || supplemental >= 24) continue;
    string prefix; using (var reader = File.OpenText(path)) { char[] buffer = new char[256]; int count = reader.Read(buffer, 0, buffer.Length); prefix = new(buffer, 0, count); }
    if (!prefix.Contains("openantares-save", StringComparison.Ordinal)) continue;
    var saved = SaveGame.ReadFile(path);
    if (!saved.Succeeded || saved.Game!.State.RulesVersion != 8) continue;
    if (Facts(saved.Game.State).Any(pair => pair.Value && !coverage.GetValueOrDefault(pair.Key))) AddSupplement(path);
}
if (!coverage.GetValueOrDefault("plague"))
{
    var onset = GalaxyGenerator.CreateNewGame(content, new("small", 42, 8, "harl", 1, "peldra", CrisesEnabled: true, AmebasEnabled: false));
    if (!onset.Succeeded) throw new InvalidOperationException(onset.Error);
    var session = new GameSession(content, onset.State!); var priming = new List<object>();
    while (!session.State.Colonies.Any(c => c.Crisis?.Case?.Stage == CrisisStage.Active) && session.State.Turn < 200)
    {
        foreach (var owner in session.State.Empires.Where(e => e.Controller == ControllerKind.Human && !e.Ready).OrderBy(e => e.Id).Select(e => e.Id).ToArray())
        {
            foreach (var row in ScoutQueries.Project(content, session.State, owner).Vessels.Where(v => v.Stage is null && v.Blocker is not null && v.Posture is not null).ToArray()) Apply(new SetScoutPostureCommand(owner, session.State.Turn, row.Id, ScoutPosture.Observe));
            var health = CrisisQueries.PendingAttention(session.State, owner);
            if (!health.IsEmpty) Apply(new AcknowledgeCrisisCommand(owner, session.State.Turn, health));
            var attention = ScoutQueries.Project(content, session.State, owner).Attention;
            if (!attention.IsEmpty) Apply(new AcknowledgeAmebaCommand(owner, session.State.Turn, attention));
            Apply(new SetReadyCommand(owner, session.State.Turn, true));
        }
    }
    if (!session.State.Colonies.Any(c => c.Crisis?.Case?.Stage == CrisisStage.Active)) throw new InvalidOperationException("Ordinary unchanged seed42 continuation did not reach Active plague within 200 turns.");
    string source = "sources/ordinary-plague.json";
    Write(source, SaveGame.Serialize(content, session.State)); Write("priming/ordinary-plague.json", JsonSerializer.Serialize(priming, options));
    origins.Add(new { source = "ordinary generated seed42 rules8 continuation via recorded acknowledgements/Ready; no state/content edits", copy = source, sha256 = Hash(File.ReadAllBytes(Path.Combine(staging, source))), provenance = "Additional generated long-horizon health coverage, not part of the 48 initial-state matrix." });
    CaptureSource("ordinary-plague", source, new(content, session.State), "Old-engine normally generated Active plague with priming command trace."); supplemental++;
    void Apply(Command command)
    {
        var outcome = session.Execute(command);
        if (!outcome.Accepted) throw new InvalidOperationException("Ordinary plague priming: " + outcome.Rejection!.Message);
        priming.Add(new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()) });
    }
}
if (!coverage.GetValueOrDefault("recovery"))
{
    var start = GalaxyGenerator.CreateNewGame(content, new("small", 42, 8, "wenth", CrisesEnabled: false, AmebasEnabled: true));
    if (!start.Succeeded) throw new InvalidOperationException(start.Error);
    var session = new GameSession(content, start.State!); var trace = new List<object>();
    var initial = session.View(session.State.Empires[0].Id); var startingShip = initial.Vessels.Single();
    var create = new FleetAction.Create("Old-engine recovery expedition", ImmutableArray.Create(startingShip.Id), startingShip.Id);
    var createPreview = FleetQueries.PreviewAction(content, initial, create);
    if (!createPreview.Allowed) throw new InvalidOperationException(createPreview.Rejection);
    PrimeExecute(session, trace, new FleetCommand(initial.Viewer, initial.Turn, create) { Fingerprint = createPreview.Fingerprint });
    var appointed = session.View(initial.Viewer);
    PrimeExecute(session, trace, new OfficerCommand(initial.Viewer, appointed.Turn, new OfficerAction.AssignFlagship(appointed.Fleets!.Officers.Single().Id, appointed.Fleets.Fleets.Single().Id)));
    while (!session.State.Empires.Any(e => e.Fleets!.Officers.Any(o => o.Status == OfficerStatus.Recovering)) && session.State.Turn < 100)
    {
        var view = session.View(session.State.Empires[0].Id);
        foreach (var row in view.Scouting!.Vessels.Where(v => v.Stage is not null))
        {
            var remain = row.Actions.OfType<SetAmebaRiskOrderCommand>().Single(c => c.Action == AmebaRiskAction.Remain);
            PrimeExecute(session, trace, remain);
        }
        var ship = view.Vessels.SingleOrDefault();
        if (ship is not null && ship.Transit is null && ship.PendingMove is null && !view.Scouting.Vessels.Any(v => v.Stage is not null))
        {
            var target = view.Stars.Where(s => s.LastObservedTurn is null).OrderBy(s => s.Chart.Id).FirstOrDefault() ?? throw new InvalidOperationException("Normally explored core map had no ameba warning.");
            var fleet = view.Fleets!.Fleets.Single();
            var action = new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty, ImmutableArray.Create(fleet.Id), target.Chart.Id);
            var preview = FleetQueries.PreviewAction(content, view, action);
            if (!preview.Allowed) throw new InvalidOperationException(preview.Rejection);
            PrimeExecute(session, trace, new FleetCommand(view.Viewer, view.Turn, action) { Fingerprint = preview.Fingerprint });
        }
        PrimeReady(session, trace);
    }
    if (!session.State.Empires.Any(e => e.Fleets!.Officers.Any(o => o.Status == OfficerStatus.Recovering))) throw new InvalidOperationException("Ordinary Wenth loss did not enter link recovery.");
    FreezeOrdinary("ordinary-wenth-recovery", session, trace, "Normally generated Wenth seed42 Solo; public-chart exploration then explicit offered Remain at observed ameba; guaranteed source-body link recovery, no forced outcomes.");
}
if (!coverage.GetValueOrDefault("founding"))
{
    var start = GalaxyGenerator.CreateNewGame(content, new("small", 42, 8, "harl", CrisesEnabled: false, AmebasEnabled: false));
    if (!start.Succeeded) throw new InvalidOperationException(start.Error);
    var session = new GameSession(content, start.State!); var trace = new List<object>(); var owner = session.State.Empires[0].Id;
    var view = session.View(owner); var home = view.Colonies.Single();
    string settlerRole = home.AvailableVesselRoleIds.Where(id => content.Vessels[id].CanFoundColony).OrderBy(id => id, StringComparer.Ordinal).First();
    PrimeExecute(session, trace, new SetProductionTargetCommand(owner, view.Turn, home.Id, new(ProductionKinds.Vessel, settlerRole)));
    StarId? destination = null;
    while (destination is null && session.State.Turn < 100)
    {
        view = session.View(owner);
        destination = view.Stars.Where(s => s.Chart.Id != home.StarId && s.Planets?.Any(p => p.OwnerId is null && p.Colonizable && p.Potential?.NativeHabitatAllowed == true) == true).OrderBy(s => s.Chart.Id).Select(s => (StarId?)s.Chart.Id).FirstOrDefault();
        if (destination is not null) break;
        var scout = view.Vessels.Single(v => v.RoleId == "scout");
        if (scout.Transit is null && scout.PendingMove is null)
        {
            var target = view.Stars.Where(s => s.LastObservedTurn is null).OrderBy(s => s.Chart.Id).First();
            var fleet = view.Fleets!.Fleets.SingleOrDefault(f => f.Members.Contains(scout.Id));
            var action = fleet is null ? new FleetAction.MoveSelection(ImmutableArray.Create(scout.Id), ImmutableArray<FleetId>.Empty, target.Chart.Id)
                : new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty, ImmutableArray.Create(fleet.Id), target.Chart.Id);
            var preview = FleetQueries.PreviewAction(content, view, action);
            if (!preview.Allowed) throw new InvalidOperationException(preview.Rejection);
            PrimeExecute(session, trace, new FleetCommand(owner, view.Turn, action) { Fingerprint = preview.Fingerprint });
        }
        PrimeReady(session, trace);
    }
    while (!session.View(owner).Vessels.Any(v => v.RoleId == settlerRole) && session.State.Turn < 120) PrimeReady(session, trace);
    view = session.View(owner); var carrier = view.Vessels.Single(v => v.RoleId == settlerRole);
    if (destination is null) throw new InvalidOperationException("Ordinary scouting found no suitable settlement destination.");
    PrimeExecute(session, trace, new SetMoveOrderCommand(owner, view.Turn, carrier.Id, destination));
    while (session.View(owner).Vessels.Single(v => v.Id == carrier.Id).StationaryStarId != destination && session.State.Turn < 150) PrimeReady(session, trace);
    view = session.View(owner); var legal = view.Vessels.Single(v => v.Id == carrier.Id).FoundingPreviews.First(p => p.RejectionCode is null);
    PrimeExecute(session, trace, new SetFoundingOrderCommand(owner, view.Turn, carrier.Id, legal.Target));
    FreezeOrdinary("ordinary-pending-founding", session, trace, "Normally generated Harl seed42 Solo; paid settlement hull, ordinary public-view survey/travel and accepted pending founding; no state/resource injection.");
}
if (generated != 48) throw new InvalidOperationException("Core matrix changed: expected four species and three sizes.");
foreach (string fact in new[] { "transit", "officer", "request", "queue", "cargo", "plague", "founding", "recovery" })
    if (!coverage.GetValueOrDefault(fact)) throw new InvalidOperationException("Missing genuine-source state coverage: " + fact + "; retain failed attempt, add an old-engine ordinary source.");
var hashes = Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal)
    .ToDictionary(p => Path.GetRelativePath(staging, p).Replace('\\', '/'), p => Hash(File.ReadAllBytes(p)), StringComparer.Ordinal);
var manifest = new
{
    rulesVersion = 8, writerVersion = 9, generatedSources = generated, generatedCheckpoints = generated * 2 * 2,
    sourceCommit = Git("rev-parse", "HEAD"), approvedPlanCommit = "dc190937ab94a464ba81823d25738164db538e73",
    productTree = Git("rev-parse", "dc19093:src/OpenAntares.Simulation"),
    compiler = Environment.Version.ToString(), engineSha256 = Hash(File.ReadAllBytes(typeof(Rules).Assembly.Location)),
    programSha256 = Hash(File.ReadAllBytes(Path.Combine(root, "tools/CombatBaselineCapture/Program.cs"))),
    supplementalSources = supplemental, origins, coverage, files = hashes, cases
};
Write("manifest.json", JsonSerializer.Serialize(manifest, options));
if (Directory.Exists(output)) throw new InvalidOperationException("Archive appeared during capture; inspect staging.");
Directory.Move(staging, output);
Console.WriteLine(JsonSerializer.Serialize(new { generatedSources = generated, generatedCheckpoints = generated * 4, supplemental, cases = cases.Count, coverage, output }, options));

void AddSupplement(string path)
{
    var game = SaveGame.ReadFile(path);
    if (!game.Succeeded || game.Game!.State.RulesVersion != 8) throw new InvalidOperationException("Required source is not a genuine valid rules8 save: " + path);
    capturedSources.Add(path);
    string name = "retained-" + supplemental++.ToString("D3", CultureInfo.InvariantCulture);
    string copy = "sources/" + name + ".json";
    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(staging, copy))!);
    byte[] bytes = File.ReadAllBytes(path); File.WriteAllBytes(Path.Combine(staging, copy), bytes);
    origins.Add(new { source = Path.GetRelativePath(root, path).Replace('\\', '/'), copy, sha256 = Hash(bytes), provenance = "Actual retained save; generated versus forced setup remains as labelled in original scenario results. Not part of 48 new generated sources." });
    CaptureSource(name, copy, game.Game, "Retained old-engine source; refer to original scenario provenance.");
}
void FreezeOrdinary(string name, GameSession session, List<object> trace, string provenance)
{
    string source = "sources/" + name + ".json";
    Write(source, SaveGame.Serialize(session.Content, session.State)); Write("priming/" + name + ".json", JsonSerializer.Serialize(trace, options));
    origins.Add(new { source = provenance, copy = source, sha256 = Hash(File.ReadAllBytes(Path.Combine(staging, source))), provenance });
    CaptureSource(name, source, new(session.Content, session.State), provenance); supplemental++;
}
void PrimeExecute(GameSession session, List<object> trace, Command command)
{
    var result = session.Execute(command);
    if (!result.Accepted) throw new InvalidOperationException("Ordinary priming " + command.GetType().Name + ": " + result.Rejection!.Message);
    trace.Add(command is FleetCommand fleet ? new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()), actionKind = fleet.Action.GetType().Name, actionJson = JsonSerializer.Serialize(fleet.Action, fleet.Action.GetType()) }
        : command is OfficerCommand officer ? new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()), actionKind = officer.Action.GetType().Name, actionJson = JsonSerializer.Serialize(officer.Action, officer.Action.GetType()) }
        : (object)new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()) });
}
void PrimeReady(GameSession session, List<object> trace)
{
    foreach (var owner in session.State.Empires.Where(e => e.Controller == ControllerKind.Human && !e.Ready).OrderBy(e => e.Id).Select(e => e.Id).ToArray())
    {
        var health = CrisisQueries.PendingAttention(session.State, owner);
        if (!health.IsEmpty) PrimeExecute(session, trace, new AcknowledgeCrisisCommand(owner, session.State.Turn, health));
        var attention = ScoutQueries.Project(session.Content, session.State, owner).Attention;
        if (!attention.IsEmpty) PrimeExecute(session, trace, new AcknowledgeAmebaCommand(owner, session.State.Turn, attention));
        PrimeExecute(session, trace, new SetReadyCommand(owner, session.State.Turn, true));
    }
}
void Capture(ContentSet baseContent, NewGameSettings settings, IReadOnlyList<BaselinePolicy> policies, string outputDirectory)
{
    if (Path.GetFullPath(outputDirectory) != staging || policies.Count != 2 || policies.Distinct().Count() != 2) throw new ArgumentException("Use both declared policies in the checked staging output.");
    var result = GalaxyGenerator.CreateNewGame(baseContent, settings);
    if (!result.Succeeded) throw new InvalidOperationException(result.Error);
    string name = $"generated-{settings.SpeciesId}-{settings.GalaxySizeId}-crisis{settings.CrisesEnabled}-ameba{settings.AmebasEnabled}";
    string source = "sources/" + name + ".json";
    Write(source, SaveGame.Serialize(baseContent, result.State!));
    CaptureSource(name, source, new(baseContent, result.State!), "Generated normally by unchanged rules8 engine; explicit seed42/species/size/one Peldra rival/crisis/ameba settings.", policies);
    generated++;
}
void CaptureSource(string name, string source, LoadedGame saved, string provenance, IReadOnlyList<BaselinePolicy>? policies = null)
{
    Observe(saved.State);
    foreach (BaselinePolicy policy in policies ?? new[] { BaselinePolicy.HoldReady, BaselinePolicy.LegalOwnViewPlay })
    {
        var session = new GameSession(saved.Content, saved.State.Clone());
        var commands = new List<List<object>>(); var checkpoints = new SortedDictionary<string, string>(StringComparer.Ordinal);
        for (int boundary = 0; boundary < 10; boundary++)
        {
            int from = session.State.Turn; var steps = new List<object>();
            foreach (var owner in session.State.Empires.Where(e => e.Controller == ControllerKind.Human && !e.Ready).OrderBy(e => e.Id).Select(e => e.Id).ToArray())
            {
                if (policy == BaselinePolicy.LegalOwnViewPlay) Play(owner);
                foreach (var row in ScoutQueries.Project(session.Content, session.State, owner).Vessels.Where(v => v.Stage is not null && v.EffectiveDestination is null && v.RiskAction is null).ToArray())
                {
                    Command? safe = row.Actions.OfType<SetScoutRetreatOrderCommand>().FirstOrDefault();
                    safe ??= row.Actions.OfType<SetAmebaRiskOrderCommand>().FirstOrDefault(c => c.Action == AmebaRiskAction.Remain);
                    if (safe is not null) Execute(safe);
                }
                foreach (var row in ScoutQueries.Project(session.Content, session.State, owner).Vessels.Where(v => v.Stage is null && v.Blocker is not null && v.Posture is not null).ToArray())
                    Execute(new SetScoutPostureCommand(owner, session.State.Turn, row.Id, ScoutPosture.Observe));
                var health = CrisisQueries.PendingAttention(session.State, owner);
                if (!health.IsEmpty) Execute(new AcknowledgeCrisisCommand(owner, session.State.Turn, health));
                var attention = ScoutQueries.Project(session.Content, session.State, owner).Attention;
                if (!attention.IsEmpty) Execute(new AcknowledgeAmebaCommand(owner, session.State.Turn, attention));
                Execute(new SetReadyCommand(owner, session.State.Turn, true));
            }
            if (session.State.Turn != from + 1) throw new InvalidOperationException(name + ": did not resolve exactly one boundary.");
            Observe(session.State); commands.Add(steps);
            if (boundary is 2 or 9) checkpoints[(boundary + 1).ToString(CultureInfo.InvariantCulture)] = Canonical(session);

            void Execute(Command command)
            {
                var outcome = session.Execute(command);
                if (!outcome.Accepted) throw new InvalidOperationException($"{name}/{policy}/turn{from}/{command.GetType().Name}: {outcome.Rejection!.Code} {outcome.Rejection.Message}");
                steps.Add(command is FleetCommand fleet
                    ? new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()), actionKind = fleet.Action.GetType().Name, actionJson = JsonSerializer.Serialize(fleet.Action, fleet.Action.GetType()) }
                    : (object)new { kind = command.GetType().Name, json = JsonSerializer.Serialize(command, command.GetType()) });
            }
            void Play(EmpireId owner)
            {
                var view = session.View(owner);
                if (view.Research.TargetId is null)
                {
                    var tech = session.Content.Technologies.Values.OrderBy(t => t.Id, StringComparer.Ordinal).FirstOrDefault(t => !view.Research.KnownTechnologyIds.Contains(t.Id) && t.PrerequisiteTechnologyIds.All(view.Research.KnownTechnologyIds.Contains));
                    if (tech is not null) Execute(new SetResearchTargetCommand(owner, session.State.Turn, tech.Id));
                }
                foreach (var colony in session.View(owner).Colonies.OrderBy(c => c.Id))
                {
                    if (colony.ProjectId is not null || colony.Development?.Queue.Length > 0) continue;
                    var building = session.Content.Buildings.Values.OrderBy(b => b.Id, StringComparer.Ordinal).FirstOrDefault(b => !colony.CompletedBuildingIds.Contains(b.Id) && b.PrerequisiteTechnologyIds.All(colony.KnownTechnologyIds.Contains));
                    if (building is not null) Execute(new SetProductionTargetCommand(owner, session.State.Turn, colony.Id, new(ProductionKinds.Building, building.Id)));
                    else if (colony.AvailableVesselRoleIds.Contains("scout")) Execute(new SetProductionTargetCommand(owner, session.State.Turn, colony.Id, new(ProductionKinds.Vessel, "scout")));
                }
                foreach (var ship in session.View(owner).Vessels.OrderBy(v => v.Id).ToArray())
                {
                    if (ship.Transit is not null || ship.PendingMove is not null || ship.PendingFounding is not null) continue;
                    var founding = ship.FoundingPreviews.FirstOrDefault(p => p.RejectionCode is null);
                    if (founding is not null) { Execute(new SetFoundingOrderCommand(owner, session.State.Turn, ship.Id, founding.Target)); continue; }
                    var current = session.View(owner);
                    var fleet = current.Fleets!.Fleets.FirstOrDefault(f => f.Members.Contains(ship.Id));
                    if (fleet is not null && (fleet.Flagship != ship.Id || fleet.Order.Kind != FleetOrderKind.Hold)) continue;
                    if (current.Fleets.Requests.Any(r => FleetQueries.IsActive(r.Status) && r.Slots.Any(s => s.Vessel == ship.Id))) continue;
                    if (current.Scouting!.Vessels.Any(v => v.Id == ship.Id && (v.Stage is not null || v.Blocker is not null))) continue;
                    foreach (var target in current.Stars.Where(s => s.Chart.Id != ship.StationaryStarId && !current.Scouting.Habitats.Any(h => h.Star == s.Chart.Id) && !current.ForeignVessels.Any(v => v.StarId == s.Chart.Id) && !(s.Planets?.Any(p => p.OwnerId is {} other && other != owner) ?? false)).OrderBy(s => s.LastObservedTurn is null ? 0 : 1).ThenBy(s => s.Chart.Id))
                    {
                        var action = fleet is null ? new FleetAction.MoveSelection(ImmutableArray.Create(ship.Id), ImmutableArray<FleetId>.Empty, target.Chart.Id) : new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty, ImmutableArray.Create(fleet.Id), target.Chart.Id);
                        var preview = FleetQueries.PreviewAction(session.Content, current, action);
                        if (!preview.Allowed) continue;
                        Execute(new FleetCommand(owner, session.State.Turn, action) { Fingerprint = preview.Fingerprint }); break;
                    }
                }
            }
        }
        string suffix = name + "-" + policy;
        string commandPath = "commands/" + suffix + ".json", checkpointPath = "checkpoints/" + suffix + ".json";
        Write(commandPath, JsonSerializer.Serialize(commands, options)); Write(checkpointPath, JsonSerializer.Serialize(checkpoints, options));
        cases.Add(new { name = suffix, save = source, commands = commandPath, checkpoints = checkpointPath, provenance });
    }
}
void Observe(GameState state) { foreach (var pair in Facts(state)) coverage[pair.Key] = pair.Value || coverage.GetValueOrDefault(pair.Key); }
Dictionary<string, bool> Facts(GameState state) => new(StringComparer.Ordinal)
{
    ["transit"] = state.Vessels.Any(v => v.Transit is not null), ["pendingMove"] = state.Vessels.Any(v => v.PendingMove is not null),
    ["officer"] = state.Empires.Any(e => e.Fleets!.Officers.Count > 0),
    ["recovery"] = state.Empires.Any(e => e.Fleets!.Officers.Any(o => o.Status == OfficerStatus.Recovering)),
    ["request"] = state.Empires.Any(e => e.Fleets!.Requests.Count > 0), ["queue"] = state.Colonies.Any(c => c.Development!.Queue.Count > 0),
    ["cargo"] = state.Vessels.Any(v => v.CarriedPopulation > 0), ["plague"] = state.Colonies.Any(c => c.Crisis?.Case is not null),
    ["founding"] = state.Vessels.Any(v => v.PendingFounding is not null), ["ameba"] = state.Vessels.Any(v => v.AmebaEncounter is not null)
};
void Write(string relative, string value) { string path = Path.Combine(staging, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, value); }
string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
string Canonical(GameSession session) { var node = JsonNode.Parse(SaveGame.Serialize(session.Content, session.State))!.AsObject(); node.Remove("schema_version"); return node.ToJsonString(); }
string Git(params string[] args)
{
    var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
    foreach (string arg in args) start.ArgumentList.Add(arg);
    using var process = Process.Start(start)!; string value = process.StandardOutput.ReadToEnd(); process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException("Git provenance check failed."); return value.Trim();
}
enum BaselinePolicy { HoldReady, LegalOwnViewPlay }
