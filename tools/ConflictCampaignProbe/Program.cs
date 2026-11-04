using OpenAntares.Game.Validation;
using OpenAntares.Simulation.Content;
if(args.Length==3&&args[0]=="--verify-save-proof")
{EmpireConflictCampaignChecks.VerifySaveProof(args[1],args[2]);Console.WriteLine("Authoritative save load and three recorded future boundaries verified.");return;}
if(args.Length!=3)throw new ArgumentException("Usage: <species> <seed42..49> <fresh-output-directory>");
if(Directory.Exists(args[2]))throw new IOException("Retain earlier attempts: choose a new output directory.");
Directory.CreateDirectory(args[2]);
var source=new[]{"src","game","content/core","tests/OpenAntares.Game.Validation","tools/ConflictCampaignProbe","tools/ConflictValidation"}.SelectMany(root=>Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories)).Concat(new[]{"OpenAntares.csproj","project.godot"}).Where(path=>new[]{".cs",".csproj",".json",".py",".ps1",".targets",".tscn",".tres",".godot"}.Contains(Path.GetExtension(path))&&!path.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part=>part is "bin" or "obj")).OrderBy(path=>path,StringComparer.Ordinal).Select(path=>new{path=path.Replace('\\','/'),sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()}).ToArray();
File.WriteAllText(Path.Combine(args[2],"source-manifest.json"),System.Text.Json.JsonSerializer.Serialize(source,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
var content=ContentLoader.LoadDirectory("content/core").Content??throw new InvalidOperationException("Core content failed.");
bool passed=EmpireConflictCampaignChecks.Run(content,args[0],ulong.Parse(args[1]),args[2]);
var changed=source.Where(item=>!File.Exists(item.path)||Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(item.path))).ToLowerInvariant()!=item.sha256).Select(item=>item.path).ToArray();
File.WriteAllText(Path.Combine(args[2],"source-verification.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=changed.Length==0,changed},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
Environment.ExitCode=passed&&changed.Length==0?0:1;
