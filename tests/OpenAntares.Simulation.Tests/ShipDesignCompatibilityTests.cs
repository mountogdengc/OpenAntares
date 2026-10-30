using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Saves;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class ShipDesignCompatibilityTests
{
    [Fact]
    public void FrozenRules9ActionCommandsRetainOriginalFingerprintsAndFutures()
    {
        string root=CompatibilityFixtures.PathFor("rules9-ship-design-action-baselines");
        string path=Path.Combine(root,"manifest.json");
        Assert.True(File.Exists(path),"Freeze explicit old-engine fleet/officer/assembly actions without regenerating the accepted72-source archive.");
        var manifest=JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal(9,manifest["rulesVersion"]!.GetValue<int>());
        foreach(var file in manifest["files"]!.AsObject())
            Assert.Equal(file.Value!.GetValue<string>(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,file.Key)))).ToLowerInvariant());
        var saved=SaveGame.ReadFile(Path.Combine(root,"source.json"));Assert.True(saved.Succeeded,string.Join("; ",saved.Errors));
        var session=new GameSession(saved.Game!.Content,saved.Game.State);
        var commands=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"commands.json")))!.AsArray();
        var futures=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"checkpoints.json")))!;
        var kinds=commands[0]!.AsArray().Select(s=>s!["kind"]!.GetValue<string>()).ToArray();
        Assert.Contains(nameof(FleetCommand),kinds);Assert.Contains(nameof(OfficerCommand),kinds);Assert.Contains(nameof(AssemblyCommand),kinds);
        Assert.Equal(10,commands.Count);
        for(int n=0;n<10;n++)
        {
            int turn=session.State.Turn;
            foreach(var step in commands[n]!.AsArray()){var outcome=session.Execute(Decode(step!));Assert.True(outcome.Accepted,outcome.Rejection?.Message);}
            Assert.Equal(turn+1,session.State.Turn);
            if(n is 2 or 9)Assert.Equal(futures[(n+1).ToString(System.Globalization.CultureInfo.InvariantCulture)]!.GetValue<string>(),CompatibilityFixtures.CanonicalWithoutSchema(session.Content,session.State));
        }
    }

    [Fact]
    public void FrozenRules9SourcesAndFuturesExistBeforeProductChanges()
    {
        string root=CompatibilityFixtures.PathFor("rules9-ship-design-baselines");
        string manifestPath=Path.Combine(root,"manifest.json");
        Assert.True(File.Exists(manifestPath),"Freeze genuine unchanged rules9 sources/futures before ship-design product changes.");
        var manifest=JsonNode.Parse(File.ReadAllText(manifestPath))!;
        Assert.Equal(9,manifest["rulesVersion"]!.GetValue<int>());
        Assert.Equal(10,manifest["writerVersion"]!.GetValue<int>());
        Assert.Equal(72,manifest["generatedSources"]!.GetValue<int>());
        Assert.Equal(144,manifest["generatedCheckpoints"]!.GetValue<int>());
        foreach(string key in new[]{"off","officer","project","queue","activeRequest","pausedRequest","cargo","plague","ameba","firstHumanReady","alternativeRole","pendingCombat","activeCombat","withdrawal","return","repair","damage"})
            Assert.True(manifest["coverage"]![key]!.GetValue<bool>(),"Missing original9 source/future coverage: "+key);
        foreach(var file in manifest["files"]!.AsObject())
            Assert.Equal(file.Value!.GetValue<string>(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,file.Key)))).ToLowerInvariant());
        foreach(var entry in manifest["cases"]!.AsArray())
        {
            var saved=SaveGame.ReadFile(Path.Combine(root,entry!["save"]!.GetValue<string>()));
            Assert.True(saved.Succeeded,string.Join("; ",saved.Errors));
            Assert.Equal(9,saved.Game!.State.RulesVersion);
            var session=new GameSession(saved.Game.Content,saved.Game.State);
            var commands=JsonNode.Parse(File.ReadAllText(Path.Combine(root,entry["commands"]!.GetValue<string>())))!.AsArray();
            var expected=JsonNode.Parse(File.ReadAllText(Path.Combine(root,entry["checkpoints"]!.GetValue<string>())))!.AsObject();
            Assert.Equal(10,commands.Count);
            for(int boundary=0;boundary<10;boundary++)
            {
                int before=session.State.Turn;
                foreach(var step in commands[boundary]!.AsArray())
                {
                    var result=session.Execute(Decode(step!));
                    Assert.True(result.Accepted,entry["name"]+": "+result.Rejection?.Message);
                }
                Assert.Equal(before+1,session.State.Turn);
                if(boundary is 2 or 9)
                {
                    string canonical=CompatibilityFixtures.CanonicalWithoutSchema(session.Content,session.State);
                    Assert.Equal(expected[(boundary+1).ToString(System.Globalization.CultureInfo.InvariantCulture)]!.GetValue<string>(),canonical);
                    var reloaded=SaveGame.Deserialize(SaveGame.Serialize(session.Content,session.State));
                    Assert.True(reloaded.Succeeded,string.Join("; ",reloaded.Errors));
                    Assert.Equal(canonical,CompatibilityFixtures.CanonicalWithoutSchema(reloaded.Game!.Content,reloaded.Game.State));
                    session=new(reloaded.Game.Content,reloaded.Game.State);
                }
            }
        }
    }

    private static Command Decode(JsonNode step)
    {
        string kind=step["kind"]!.GetValue<string>(),json=step["json"]!.GetValue<string>();
        if(step["actionKind"] is not null)
        {
            var outer=JsonNode.Parse(json)!;string action=step["actionKind"]!.GetValue<string>(),inner=step["actionJson"]!.GetValue<string>();
            var issuer=JsonSerializer.Deserialize<EmpireId>(outer["Issuer"]!.ToJsonString());int turn=outer["ExpectedTurn"]!.GetValue<int>();
            if(kind==nameof(CombatActionCommand))
            {
                CombatAction value=action switch {
                    nameof(CombatAction.AttackPocket)=>JsonSerializer.Deserialize<CombatAction.AttackPocket>(inner)!,
                    nameof(CombatAction.ChangeCombatOrders)=>JsonSerializer.Deserialize<CombatAction.ChangeCombatOrders>(inner)!,
                    nameof(CombatAction.WithdrawCombat)=>JsonSerializer.Deserialize<CombatAction.WithdrawCombat>(inner)!,
                    nameof(CombatAction.RespondCombat)=>JsonSerializer.Deserialize<CombatAction.RespondCombat>(inner)!,
                    nameof(CombatAction.SetRepair)=>JsonSerializer.Deserialize<CombatAction.SetRepair>(inner)!,
                    _=>throw new InvalidOperationException("Unknown frozen combat action: "+action)};
                return new CombatActionCommand(issuer,turn,value,outer["PreviewToken"]!.GetValue<string>());
            }
            if(kind==nameof(FleetCommand))
            {
                FleetAction value=action switch {
                    nameof(FleetAction.MoveSelection)=>JsonSerializer.Deserialize<FleetAction.MoveSelection>(inner)!,
                    nameof(FleetAction.SetOrder)=>JsonSerializer.Deserialize<FleetAction.SetOrder>(inner)!,
                    nameof(FleetAction.Create)=>JsonSerializer.Deserialize<FleetAction.Create>(inner)!,
                    _=>throw new InvalidOperationException("Unknown frozen fleet action: "+action)};
                return new FleetCommand(issuer,turn,value){Fingerprint=outer["Fingerprint"]?.GetValue<string>()};
            }
            if(kind==nameof(OfficerCommand)&&action==nameof(OfficerAction.AssignFlagship))
                return new OfficerCommand(issuer,turn,JsonSerializer.Deserialize<OfficerAction.AssignFlagship>(inner)!);
            if(kind==nameof(AssemblyCommand))
            {
                AssemblyAction value=action switch {
                    nameof(AssemblyAction.Accept)=>JsonSerializer.Deserialize<AssemblyAction.Accept>(inner)!,
                    nameof(AssemblyAction.Pause)=>JsonSerializer.Deserialize<AssemblyAction.Pause>(inner)!,
                    nameof(AssemblyAction.Resume)=>JsonSerializer.Deserialize<AssemblyAction.Resume>(inner)!,
                    _=>throw new InvalidOperationException("Unknown frozen assembly action: "+action)};
                return new AssemblyCommand(issuer,turn,value);
            }
            throw new InvalidOperationException("Unknown nested frozen command: "+kind);
        }
        return kind switch {
            nameof(SetReadyCommand)=>JsonSerializer.Deserialize<SetReadyCommand>(json)!,
            nameof(AcknowledgeCrisisCommand)=>JsonSerializer.Deserialize<AcknowledgeCrisisCommand>(json)!,
            nameof(AcknowledgeAmebaCommand)=>JsonSerializer.Deserialize<AcknowledgeAmebaCommand>(json)!,
            nameof(SetAmebaRiskOrderCommand)=>JsonSerializer.Deserialize<SetAmebaRiskOrderCommand>(json)!,
            nameof(SetScoutRetreatOrderCommand)=>JsonSerializer.Deserialize<SetScoutRetreatOrderCommand>(json)!,
            nameof(SetScoutPostureCommand)=>JsonSerializer.Deserialize<SetScoutPostureCommand>(json)!,
            nameof(SetMoveOrderCommand)=>JsonSerializer.Deserialize<SetMoveOrderCommand>(json)!,
            nameof(SetFoundingOrderCommand)=>JsonSerializer.Deserialize<SetFoundingOrderCommand>(json)!,
            nameof(SetProductionTargetCommand)=>JsonSerializer.Deserialize<SetProductionTargetCommand>(json)!,
            nameof(SetResearchTargetCommand)=>JsonSerializer.Deserialize<SetResearchTargetCommand>(json)!,
            nameof(SetColonyQueueCommand)=>JsonSerializer.Deserialize<SetColonyQueueCommand>(json)!,
            nameof(SetColonyAutomationCommand)=>JsonSerializer.Deserialize<SetColonyAutomationCommand>(json)!,
            _=>throw new InvalidOperationException("Unknown frozen command: "+kind)};
    }
}
