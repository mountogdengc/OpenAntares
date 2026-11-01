using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Game.Validation;
// Engine-free command rehearsal. These saves are NOT Main UI evidence.
public static class EmpireConflictCampaignChecks
{
    public const int TurnBudget=160;
    public static readonly string[] RequiredStages={"pending-declaration","queued-peace-answer","pending-attack","active-battle","queued-withdrawal","effective-blockade","truce"};
    public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static bool Run(ContentSet content,string species,ulong seed,string output)
    {
        if(seed is <42 or >49||!new[]{"harl","peldra","sulan","wenth"}.Contains(species))throw new ArgumentException("Approved species and seeds42..49 only.");
        Directory.CreateDirectory(output);var journal=new List<object>();var stages=new Dictionary<string,string>();var futures=new List<object>();var expiry=new List<object>();var payments=new Dictionary<EmpireId,long>();var incomes=new Dictionary<EmpireId,long>();var completed=new Dictionary<EmpireId,int>();var startingBanks=new Dictionary<EmpireId,long>();bool battleDamage=false,repair=false,repairTick=false,contested=false,lifted=false;string? error=null;GameSession? game=null;
        try
        {
            var generated=GalaxyGenerator.CreateNewGame(content,new("small",seed,12,species,1,"wenth",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));if(!generated.Succeeded)throw new InvalidOperationException(generated.Error);
            game=new(content,generated.State!);var owner=game.State.Empires[0].Id;var other=game.State.Empires[1].Id;
            foreach(var empire in game.State.Empires){startingBanks[empire.Id]=game.State.Colonies.Where(c=>c.EmpireId==empire.Id).Sum(c=>c.ProductionReserve);payments[empire.Id]=0;incomes[empire.Id]=0;completed[empire.Id]=0;}
            void Capture(string name){if(stages.ContainsKey(name))return;string saved=SaveGame.Serialize(content,game.State);stages.Add(name,saved);File.WriteAllText(Path.Combine(output,name+".engine-save.json"),saved);}
            bool Send(Command command,string? stage=null)
            {
                if(command is FleetCommand fleet){var preview=FleetQueries.PreviewAction(content,game.View(owner),fleet.Action);if(!preview.Allowed)return false;command=fleet with{Fingerprint=preview.Fingerprint};}
                if(command is OpenAntares.Simulation.Combat.CombatActionCommand combat){var preview=OpenAntares.Simulation.Combat.CombatQueries.Preview(content,game.View(owner),combat.Action);if(!preview.Allowed)return false;command=combat with{PreviewToken=preview.Token};}
                if(command is ConflictCommand conflict){var preview=ConflictQueries.Preview(content,game.View(owner),conflict.Action);if(!preview.Allowed)return false;command=conflict with{Token=preview.Token};}
                var beforeHull=game.View(owner).Conflict!.Ships.ToDictionary(s=>s.Id,s=>s.Hull);
                object? action=command switch{ConflictCommand c=>c.Action,FleetCommand c=>c.Action,OpenAntares.Simulation.Combat.CombatActionCommand c=>c.Action,_=>null};
                var result=game.Execute(command);journal.Add(new{turn=command.ExpectedTurn,type=command.GetType().AssemblyQualifiedName,payload=JsonSerializer.SerializeToElement(command,command.GetType()),actionType=action?.GetType().AssemblyQualifiedName,action=action is null?(JsonElement?)null:JsonSerializer.SerializeToElement(action,action.GetType()),result.Accepted,result.Changed,rejection=result.Rejection?.Message,hash=Hash(SaveGame.Serialize(content,game.State))});
                if(!result.Accepted)return false;if(stage is not null)Capture(stage);
                if(result.Report is {} report)
                {
                    foreach(var colony in report.Economy.Colonies){incomes[colony.Empire]+=colony.NetProduction.Total;payments[colony.Empire]+=colony.Production.Change.Lines.Where(l=>l.Kind=="cost_paid").Sum(l=>-l.Amount);if(colony.Production.CompletedId is not null&&colony.Production.TargetKind==ProductionKinds.ShipDesign)completed[colony.Empire]++;}
                    if(game.View(owner).Conflict!.Encounters.Any(e=>e.Round>0&&e.Phase==ConflictPhase.Active))Capture("active-battle");battleDamage|=game.View(owner).Conflict!.Ships.Any(s=>s.Military&&s.Hull<s.MaxHull);
                    repairTick|=game.View(owner).Conflict!.Ships.Any(s=>s.Military&&beforeHull.TryGetValue(s.Id,out int hull)&&s.Hull>hull);
                    if(game.View(owner).Conflict!.Blockades.Any(b=>b.Status=="Effective"))Capture("effective-blockade");
                    if(game.View(owner).Conflict!.Relations.Any(r=>r.TruceEndTurn>game.State.Turn))Capture("truce");
                }
                return true;
            }
            bool Conflict(ConflictAction action,string? stage=null){var preview=ConflictQueries.Preview(content,game.View(owner),action);return preview.Allowed&&Send(new ConflictCommand(owner,game.State.Turn,action,preview.Token),stage);}
            for(int boundary=0;boundary<TurnBudget;boundary++)
            {
                PilotEconomyAndExpansion(content,game,owner,c=>Send(c));var view=game.View(owner);var own=view.Conflict!;
                foreach(var response in own.Encounters.Where(e=>e.OwnNeedsResponse))Conflict(new ConflictAction.Respond(response.Id,response.Revision));
                foreach(var ship in own.Ships.Where(s=>s.Military&&s.Fleet is null&&s.NewlyCompleted))Send(new FleetCommand(owner,view.Turn,new FleetAction.DismissNewMarker(ImmutableArray.Create(ship.Id))));
                // Use the ordinary policy for paid military production/grouping, never mutate state.
                foreach(var command in ConflictPlanner.Plan(content,game.View(owner),ConflictPlanningStage.Production))Send(command);
                for(int i=0;i<2;i++)foreach(var command in ConflictPlanner.Plan(content,game.View(owner),ConflictPlanningStage.Grouping))Send(command);
                view=game.View(owner);own=view.Conflict!;var relation=own.Relations.FirstOrDefault();
                if(relation is {Relation:RelationKind.Peace,TruceEndTurn:null}&&own.Ships.Count(s=>s.Military&&!s.NewlyCompleted)>=2)Conflict(new ConflictAction.DeclareWar(other),"pending-declaration");
                if(relation is {Relation:RelationKind.War})
                {
                    // Human rehearsal policy2: pay for a larger relief force and commit it explicitly.
                    // This changes no rival policy or gameplay cap. All additions use ordinary paid production.
                    var design=view.ShipDesigns!.Designs.Where(d=>d.Unlocked&&!d.Archived&&d.Stats.Attack>0).OrderBy(d=>d.Stats.BuildCost).ThenBy(d=>d.Id).FirstOrDefault();
                    if(design is not null&&own.Ships.Count(s=>s.Military)+view.Colonies.Count(c=>c.ProjectKind==ProductionKinds.ShipDesign&&c.ProjectId is not null)<12)
                        foreach(var port in view.Colonies.Where(c=>c.ProjectId is null&&c.Development?.Queue.IsEmpty!=false).Take(12-own.Ships.Count(s=>s.Military)-view.Colonies.Count(c=>c.ProjectKind==ProductionKinds.ShipDesign&&c.ProjectId is not null)))Send(new SetProductionTargetCommand(owner,view.Turn,port.Id,ShipDesignTargets.Build(design.Id)));
                    contested|=own.Blockades.Any(b=>b.Status=="Contested");
                    foreach(var command in ConflictPlanner.Plan(content,game.View(owner),ConflictPlanningStage.Orders))
                    {
                        if(!contested&&command is ConflictCommand{Action:ConflictAction.Attack attack}&&own.Ships.FirstOrDefault(s=>s.Fleet==attack.Fleet)?.Star is {} station&&view.Stars.Any(s=>s.Chart.Id==station&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==other)))continue;
                        string? stage=command is ConflictCommand{Action:ConflictAction.Attack}?"pending-attack":null;Send(command,stage);
                    }
                    view=game.View(owner);own=view.Conflict!;
                    bool Available(OwnConflictShipView s)=>s.Military&&!s.NewlyCompleted&&!s.InConflict&&!s.InSentry&&!s.Refitting&&!s.Repairing&&s.Star is not null&&s.Hull==s.MaxHull&&s.Cohesion>=90&&view.Vessels.Single(v=>v.Id==s.Id) is {Transit:null,PendingMove:null};
                    foreach(var station in own.Ships.Where(Available).GroupBy(s=>s.Star).OrderBy(g=>g.Key))
                    {
                        var ungrouped=station.Where(s=>s.Fleet is null).Select(s=>s.Id).OrderBy(id=>id).ToImmutableArray();if(!ungrouped.IsEmpty)Send(new FleetCommand(owner,view.Turn,new FleetAction.Create("Paid relief force",ungrouped)));
                        var here=game.View(owner);var groups=here.Fleets!.Fleets.Where(f=>f.Members.All(id=>station.Any(s=>s.Id==id))).OrderBy(f=>f.Id).ToArray();
                        if(groups.Length>1)Send(new FleetCommand(owner,view.Turn,new FleetAction.Merge(groups[0].Id,groups.Skip(1).Select(f=>f.Id).ToImmutableArray())));
                    }
                    view=game.View(owner);own=view.Conflict!;
                    foreach(var fleet in view.Fleets!.Fleets.Where(f=>f.Members.Length>=2&&f.Members.All(id=>own.Ships.Any(s=>s.Id==id&&Available(s)))).OrderByDescending(f=>f.Members.Length))
                    {
                        var star=own.Ships.Single(s=>s.Id==fleet.Flagship).Star!.Value;var port=view.Colonies.Where(c=>c.StarId!=star).OrderBy(c=>c.Id).FirstOrDefault();
                        var enemy=own.Military.Where(m=>m.Owner==other&&m.Current&&m.Star==star).OrderBy(m=>m.Vessel).FirstOrDefault();
                        var orders=new ConflictOrders(OpenAntares.Simulation.Combat.CombatDoctrine.Hold,port?.Id,OpenAntares.Simulation.Combat.StragglerPolicy.KeepTogether,OpenAntares.Simulation.Combat.CombatArrivalOrder.Repair);
                        if(enemy is not null){bool pendingContest=!contested&&Conflict(new ConflictAction.Blockade(fleet.Id,other));contested|=game.View(owner).Conflict!.Blockades.Any(b=>b.Status=="Contested");if(!pendingContest&&port is not null)Conflict(new ConflictAction.Attack(fleet.Id,enemy.Vessel,orders),"pending-attack");}
                        else if(view.Stars.Any(s=>s.Chart.Id==star&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==other)))Conflict(new ConflictAction.Blockade(fleet.Id,other));
                        else if(fleet.Members.Length>=6&&view.Stars.Where(s=>s.Chart.Id!=star&&s.Planets is {} planets&&planets.Any(p=>p.OwnerId==other)).OrderBy(s=>s.Chart.Id).FirstOrDefault() is {} destination)Send(new FleetCommand(owner,view.Turn,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),destination.Chart.Id)));
                    }
                    own=game.View(owner).Conflict!;
                    if(battleDamage&&!stages.ContainsKey("queued-withdrawal"))foreach(var encounter in own.Encounters.Take(1))Conflict(new ConflictAction.Withdraw(encounter.Id,encounter.Revision),"queued-withdrawal");
                    foreach(var blockade in own.Blockades){contested|=blockade.Status=="Contested";if(stages.ContainsKey("effective-blockade")&&!lifted)lifted=Conflict(new ConflictAction.LiftBlockade(blockade.Fleet));}
                    if(stages.ContainsKey("effective-blockade")&&stages.ContainsKey("queued-withdrawal"))
                    {
                        if(relation.Peace is {} offer&&offer.Proposer!=owner)Conflict(new ConflictAction.AnswerPeace(other,offer.Id,true),"queued-peace-answer");
                    }
                }
                foreach(var ship in game.View(owner).Conflict!.Ships.Where(s=>s.Military&&s.Hull<s.MaxHull&&!s.InConflict))
                {
                    var action=new OpenAntares.Simulation.Combat.CombatAction.SetRepair(ImmutableArray.Create(ship.Id),true);var preview=OpenAntares.Simulation.Combat.CombatQueries.Preview(content,game.View(owner),action);if(preview.Allowed)repair|=Send(new OpenAntares.Simulation.Combat.CombatActionCommand(owner,game.State.Turn,action,preview.Token));
                }
                if(!Send(new SetReadyCommand(owner,game.State.Turn,true)))throw new InvalidOperationException("Public Ready failed; inspect concrete journal.");
                if(RequiredStages.All(stages.ContainsKey)&&game.State.Colonies.Count(c=>c.EmpireId==owner)>=2&&game.State.Colonies.Count(c=>c.EmpireId==other)>=2&&completed.Values.All(n=>n>0)&&battleDamage&&repairTick&&contested&&lifted)break;
            }
            var missing=RequiredStages.Where(s=>!stages.ContainsKey(s)).ToArray();if(missing.Length>0||!battleDamage||!repairTick||!contested||!lifted||completed.Values.Any(n=>n==0))throw new InvalidOperationException("Required earned coverage absent: "+string.Join(", ",missing)+$"; paid hulls {string.Join(",",completed.Values)}; damage={battleDamage}, repair tick={repairTick}, contested={contested}, lifted={lifted}.");
            foreach(var stage in stages)
            {
                var loaded=SaveGame.Deserialize(stage.Value);if(!loaded.Succeeded)throw new InvalidOperationException("Stage reload failed: "+stage.Key);var original=new GameSession(content,loaded.Game!.State.Clone());var replay=new GameSession(loaded.Game.Content,loaded.Game.State);
                var hashes=new List<string>();for(int i=0;i<3;i++){Future(content,original,owner);Future(content,replay,owner);string saved=SaveGame.Serialize(original.Content,original.State);if(saved!=SaveGame.Serialize(replay.Content,replay.State))throw new InvalidOperationException("Future differs: "+stage.Key);hashes.Add(Hash(saved));}futures.Add(new{stage=stage.Key,equalFutureBoundaries=3,hashes});
            }
            bool expired=false;for(int i=0;i<40;i++){Future(content,game,owner);var relation=game.View(owner).Conflict!.Relations.Single();expiry.Add(new{turn=game.State.Turn,relation.Relation,relation.TruceEndTurn,relation.OwnGrievance,relation.OtherGrievance,hash=Hash(SaveGame.Serialize(content,game.State))});if(relation.Relation==RelationKind.Peace&&relation.TruceEndTurn<=game.State.Turn&&relation.OwnGrievance==0&&relation.OtherGrievance==0){expired=true;break;}}if(!expired)throw new InvalidOperationException("Truce/grievance expiry not witnessed within40 extra boundaries.");
        }
        catch(Exception exception){error=exception.ToString();}
        finally
        {
            if(game is not null)File.WriteAllText(Path.Combine(output,"final.engine-save.json"),SaveGame.Serialize(content,game.State));
            File.WriteAllText(Path.Combine(output,"journal.json"),JsonSerializer.Serialize(journal,new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(output,"three-futures.json"),JsonSerializer.Serialize(futures,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(output,"expiry-continuation.json"),JsonSerializer.Serialize(expiry,new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(new{species,seed,rules=12,TurnBudget,turn=game?.State.Turn,passed=error is null,error,injectedAssets=false,method="Engine-free real public commands; NOT Main saves/native acceptance",nativeAcceptancePending=true,stages=stages.Keys,payments=payments.Select(p=>new{owner=p.Key.Value,paid=p.Value,income=incomes[p.Key],startingBank=startingBanks[p.Key],finalBank=game?.State.Colonies.Where(c=>c.EmpireId==p.Key).Sum(c=>c.ProductionReserve),paidHulls=completed[p.Key]}),battleDamage,repairOrder=repair,repairTick,contested,lifted},new JsonSerializerOptions{WriteIndented=true}));
        }
        return error is null;
    }
    private static void Future(ContentSet content,GameSession game,EmpireId owner)
    {
        foreach(var e in game.View(owner).Conflict!.Encounters.Where(e=>e.OwnNeedsResponse)){var action=new ConflictAction.Respond(e.Id,e.Revision);var p=ConflictQueries.Preview(content,game.View(owner),action);if(!game.Execute(new ConflictCommand(owner,game.State.Turn,action,p.Token)).Accepted)throw new InvalidOperationException("Future response failed.");}
        if(!game.Execute(new SetReadyCommand(owner,game.State.Turn,true)).Accepted)throw new InvalidOperationException("Future boundary failed.");
    }
    private static void PilotEconomyAndExpansion(ContentSet content,GameSession game,EmpireId owner,Func<Command,bool> send)
    {
        var view=game.View(owner);var context=RivalPlanningQueries.Create(content,game.State,owner);
        foreach(var colony in view.Colonies)
        {
            var target=colony.ProjectId is null?null:new ProductionTarget(colony.ProjectKind,colony.ProjectId);
            for(int support=0;support<=colony.Population;support++)
            {var work=new Workforce(support,colony.Population-support,0);var estimate=context.Forecast(colony.Id,work,target);if(estimate is null||estimate.SupportProduced.Total<estimate.SupportRequired.Total)continue;if(colony.Population<colony.Economy.Growth.Capacity&&work.Production>0)work=work with{Support=work.Support+1,Production=work.Production-1};if(work!=colony.Workforce)send(new SetWorkforceCommand(owner,view.Turn,colony.Id,work));break;}
        }
        view=game.View(owner);context=RivalPlanningQueries.Create(content,game.State,owner);
        if(view.Colonies.Length<2)
        {
            var role=view.AvailableVesselRoleIds.Select(id=>content.Vessels[id]).Where(r=>r.CanFoundColony).OrderBy(r=>r.Cost).ThenBy(r=>r.Id,StringComparer.Ordinal).First();
            if(!view.Vessels.Any(v=>content.Vessels[v.RoleId].CanFoundColony)&&!view.Colonies.Any(c=>c.ProjectId==role.Id)&&view.Colonies.FirstOrDefault(c=>c.ProjectId is null) is {} port)send(new SetProductionTargetCommand(owner,view.Turn,port.Id,new(ProductionKinds.Vessel,role.Id)));
            foreach(var founder in view.Vessels.Where(v=>content.Vessels[v.RoleId].CanFoundColony&&v.Transit is null&&v.PendingMove is null&&v.PendingFounding is null))
            {
                var opportunities=view.Stars.Where(s=>s.Planets is not null).SelectMany(s=>s.Planets!.Value.Where(p=>p.OwnerId is null&&context.PreviewSettlement(p.Id,founder.RoleId)?.RejectionCode is null).Select(p=>new{Star=s.Chart.Id,Planet=p.Id})).OrderBy(p=>p.Star==founder.StationaryStarId?0:1).ThenBy(p=>context.TravelTurns(founder.StationaryStarId!.Value,p.Star,founder.RoleId)??int.MaxValue).ThenBy(p=>p.Planet).FirstOrDefault();
                if(opportunities is not null)send(opportunities.Star==founder.StationaryStarId?new SetFoundingOrderCommand(owner,view.Turn,founder.Id,opportunities.Planet):new SetMoveOrderCommand(owner,view.Turn,founder.Id,opportunities.Star));
            }
        }
        foreach(var scout in view.Vessels.Where(v=>!content.Vessels[v.RoleId].CombatOnly&&!content.Vessels[v.RoleId].CanFoundColony&&v.Transit is null&&v.PendingMove is null))
        {var next=view.Stars.Where(s=>s.Chart.Id!=scout.StationaryStarId).OrderBy(s=>s.LastObservedTurn??-1).ThenBy(s=>context.TravelTurns(scout.StationaryStarId!.Value,s.Chart.Id,scout.RoleId)??int.MaxValue).ThenBy(s=>s.Chart.Id).FirstOrDefault();if(next is not null)send(new SetMoveOrderCommand(owner,view.Turn,scout.Id,next.Chart.Id));}
    }
}
