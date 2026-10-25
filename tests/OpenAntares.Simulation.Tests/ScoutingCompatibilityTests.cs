using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Saves;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class ScoutingCompatibilityTests
{
    [Fact]
    public void GenuineRules6SourcesAndFuturesAreFrozen()
    {
        string root=CompatibilityFixtures.PathFor("rules6-scout-baselines");
        string path=Path.Combine(root,"manifest.json");
        Assert.True(File.Exists(path),"Capture genuine rules6 futures using unchanged engine before product edits.");
        var manifest=JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal(32,manifest["uiSources"]!.GetValue<int>());
        Assert.Equal(16,manifest["generatedSources"]!.GetValue<int>());
        Assert.Equal(49,manifest["cases"]!.AsArray().Count);
        foreach(var entry in manifest["cases"]!.AsArray())
        {
            string save=Path.Combine(root,entry!["save"]!.GetValue<string>());
            Assert.Equal(entry["saveSha256"]!.GetValue<string>(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(save))).ToLowerInvariant());
            var loaded=SaveGame.ReadFile(save);Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));
            Assert.Equal(6,loaded.Game!.State.RulesVersion);
            var session=new GameSession(loaded.Game.Content,loaded.Game.State);
            var futures=JsonNode.Parse(File.ReadAllText(Path.Combine(root,entry["futures"]!.GetValue<string>())))!.AsArray();
            var commands=JsonNode.Parse(File.ReadAllText(Path.Combine(root,entry["commands"]!.GetValue<string>())))!.AsArray();
            Assert.Equal(4,futures.Count);Assert.Equal(4,commands.Count);
            for(int i=0;i<4;i++)
            {
                foreach(var command in commands[i]!.AsArray())
                {
                    var owner=new EmpireId(command!["issuer"]!.GetValue<int>());int turn=command["turn"]!.GetValue<int>();
                    Assert.Equal(turn,session.State.Turn);
                    Command c;
                    if(command["kind"]!.GetValue<string>()=="ack")
                    {
                        var expected=CrisisQueries.PendingAttention(session.State,owner);
                        Assert.Equal(command["expected"]!.GetValue<string>(),JsonSerializer.Serialize(expected));
                        c=new AcknowledgeCrisisCommand(owner,turn,expected);
                    }
                    else c=new SetReadyCommand(owner,turn,true);
                    var result=session.Execute(c);Assert.True(result.Accepted,result.Rejection?.Message);
                }
                string expectedFuture=futures[i]!.GetValue<string>();
                Assert.Equal(expectedFuture,CompatibilityFixtures.CanonicalWithoutSchema(session.Content,session.State));
                var reload=SaveGame.Deserialize(SaveGame.Serialize(session.Content,session.State));Assert.True(reload.Succeeded,string.Join("; ",reload.Errors));
                Assert.Equal(expectedFuture,CompatibilityFixtures.CanonicalWithoutSchema(reload.Game!.Content,reload.Game.State));
            }
        }
    }
}
