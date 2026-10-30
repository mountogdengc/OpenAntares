using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignRequestTests
{
    [Fact]
    public void RequestsMatchExactDesignWithoutSubstitution()
    {
        var session=ShipDesignMathTests.Equipped("gun");var s=session.State;var gun=s.Vessels[0];var standard=gun.Clone();standard.Id=new(s.AllocateId());standard.Design=new(1);s.Vessels.Add(standard);Assert.Empty(GameStateValidator.Validate(session.Content,s));
        var draft=Draft(session,new AssemblyQuota("escort",new(2))) with{AcceptedIdleVessels=ImmutableArray.Create(standard.Id)};Assert.False(Preview(session,draft).Allowed);
        draft=draft with{AcceptedIdleVessels=ImmutableArray.Create(gun.Id)};var preview=Preview(session,draft);Assert.True(preview.Allowed,string.Join("; ",preview.Rejections));Assert.Equal(gun.Id,Assert.Single(preview.Slots).Vessel);Assert.Equal(new DesignId(2),preview.Slots[0].Design);Assert.DoesNotContain(preview.IdleCandidates,v=>v.Id==standard.Id);
        Accept(session,draft);Assert.Equal(new DesignId(2),session.State.Empires[0].Fleets!.Requests.Single().Slots.Single().Design);
    }
    [Theory]
    [InlineData(10,1,2280L)][InlineData(30,1,2160L)][InlineData(10,2,2660L)][InlineData(30,2,2520L)]
    public void OrganizerDiscountAppliesOnceToWholeRecipe(int xp,int design,long expected)
    {
        var session=Organizer(xp);var id=ShipDesignFixtures.SaveDesign(session,"Gun",new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")));Assert.Equal(new DesignId(2),id);
        var owner=session.State.Empires[0];var port=session.State.Colonies[0];port.ProductionReserve=10000;
        var draft=Draft(session,new AssemblyQuota("escort",new(design))) with{Coordinator=owner.Fleets!.Officers.Single(o=>o.Specialty==OfficerSpecialty.Organizer).Id};var preview=Preview(session,draft);Assert.True(preview.Allowed,string.Join("; ",preview.Rejections));Assert.Equal(expected,Assert.Single(preview.Slots).Cost);Accept(session,draft);
        var prepared=GovernorTurnCoordinator.Prepare(session.Content,session.State,owner.Id);Assert.True(prepared.Succeeded);session=new(session.Content,prepared.State);var target=ShipDesignTargets.Build(new(design));
        Assert.Equal(expected,OfficerBonuses.RequestShipCost(session.Content,session.State,port.Id,target));Assert.Equal(expected,session.Forecast().Colony(port.Id).Production.TargetCost);Assert.Equal(expected,FleetPlanningQueries.Create(session.Content,session.View(owner.Id)).Forecast(port.Id,port.Workforce,target)!.Production.TargetCost);
        var done=Execute(session,new SetReadyCommand(owner.Id,session.State.Turn,true));Assert.Equal(expected,done.Report!.Economy.Colony(port.Id).Production.TargetCost);Assert.Equal(new DesignId(design),session.State.Vessels.Last().Design);
    }
    [Fact]
    public void ContradictoryRolesAndArchivedNewQuotasReject()
    {
        var session=ShipDesignMathTests.Equipped("gun");var draft=Draft(session,new AssemblyQuota("escort",new(2))) with{Roles=ImmutableArray.Create("escort")};Assert.False(Preview(session,draft).Allowed);
        var e=session.State.Empires[0];Execute(session,new ShipDesignCommand(e.Id,0,e.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(new(2))));Assert.False(Preview(session,Draft(session,new AssemblyQuota("escort",new(2)))).Allowed);
        Assert.False(Preview(session,Draft(session,new AssemblyQuota("scout",new(1)))).Allowed);
    }
    [Fact]
    public void StaleLaterPageIdleSelectionRejectsAtomically()
    {
        var session=ShipDesignMathTests.Equipped("gun");var s=session.State;for(int n=1;n<36;n++){var ship=s.Vessels[0].Clone();ship.Id=new(s.AllocateId());s.Vessels.Add(ship);}var owner=s.Empires[0];var selected=s.Vessels.Last();var draft=Draft(session,new AssemblyQuota("escort",new(2))) with{AcceptedIdleVessels=ImmutableArray.Create(selected.Id)};
        var preview=Preview(session,draft);Assert.True(preview.Allowed,string.Join("; ",preview.Rejections));Assert.Equal(36,preview.IdleCandidates.Length);
        var star=s.Stars.First(st=>st.Id!=selected.StationaryStarId).Id;Execute(session,new SetMoveOrderCommand(owner.Id,0,selected.Id,star));string before=JsonSerializer.Serialize(session.State);var rejected=session.Execute(new AssemblyCommand(owner.Id,0,new AssemblyAction.Accept(draft,preview.Fingerprint)));Assert.False(rejected.Accepted);Assert.Equal(before,JsonSerializer.Serialize(session.State));
    }
    [Fact]
    public void ArchivedBoundQuotaCanPauseResumeAndFinish()
    {
        var session=ShipDesignFixtures.Session();var id=ShipDesignFixtures.SaveDesign(session,"Bound",session.Content.ShipDesigns!.StartingRecipe);var draft=Draft(session,new AssemblyQuota("escort",id));Accept(session,draft);var e=session.State.Empires[0];var r=e.Fleets!.Requests.Single();
        Action(session,new AssemblyAction.Pause(r.Id,r.Revision));e=session.State.Empires[0];Execute(session,new ShipDesignCommand(e.Id,0,e.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(id)));r=session.State.Empires[0].Fleets!.Requests.Single();
        var retained=AssemblyPlanner.PreviewRetained(FleetPlanningQueries.Create(session.Content,session.View(e.Id)),r.Id);Assert.True(retained.Allowed,string.Join("; ",retained.Rejections));Action(session,new AssemblyAction.Resume(r.Id,r.Revision,retained.Fingerprint));session.State.Colonies[0].ProductionReserve=10000;
        Execute(session,new SetReadyCommand(e.Id,0,true));Assert.Equal(id,session.State.Vessels.Last().Design);Assert.Equal(id,session.State.Empires[0].Fleets!.Requests.Single().Slots.Single().Design);
    }
    [Theory]
    [InlineData("harl")][InlineData("peldra")][InlineData("sulan")][InlineData("wenth")]
    public void ExactWarshipAndCargoQuotasKeepOriginalSpeciesAuthority(string species)
    {
        var session=ShipDesignFixtures.Session(species:species);var role=session.Content.Species[species].AllowedVesselIds.Select(id=>session.Content.Vessels[id]).First(v=>v.CanFoundColony);
        var draft=Draft(session,new AssemblyQuota("escort",new(1)),new AssemblyQuota(role.Id,null));Assert.False(Preview(session,draft).Allowed);draft=draft with{AuthorizeCargo=true};var p=Preview(session,draft);Assert.True(p.Allowed,string.Join("; ",p.Rejections));Assert.Equal(role.EmbarkPopulation,p.Slots[1].Cargo);Accept(session,draft);Assert.Equal(new DesignId(1),session.State.Empires[0].Fleets!.Requests[0].Slots[0].Design);Assert.Null(session.State.Empires[0].Fleets!.Requests[0].Slots[1].Design);
    }
    [Fact]
    public void CompletedRequestRetainsHistoricalDesignAndDoesNotRepeatXp()
    {
        var session=ShipDesignMathTests.Equipped("gun");var owner=session.State.Empires[0];var gun=session.State.Vessels[0];Accept(session,Draft(session,new AssemblyQuota("escort",new(2))) with{AcceptedIdleVessels=ImmutableArray.Create(gun.Id)});
        Execute(session,new SetReadyCommand(owner.Id,0,true));var completed=session.State.Empires[0].Fleets!.Requests.Single();Assert.Equal(RequestStatus.Complete,completed.Status);Assert.Equal(new DesignId(2),completed.Slots.Single().Design);
        string career=JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers);for(int n=0;n<3;n++)Execute(session,new SetReadyCommand(owner.Id,session.State.Turn,true));Assert.Equal(career,JsonSerializer.Serialize(session.State.Empires[0].Fleets!.Officers));
        // Labelled equipment-state supplement; Task9 adds the actual paid refit continuation.
        var changed=session.State.Clone();changed.FindVessel(gun.Id)!.Design=new(1);Assert.Empty(GameStateValidator.Validate(session.Content,changed));Assert.Equal(new DesignId(2),changed.Empires[0].Fleets!.Requests.Single().Slots.Single().Design);
    }
    [Fact]
    public void AmendmentPreservesBuiltIdentityAndChangesOnlyUnstartedQuota()
    {
        var session=ShipDesignMathTests.Equipped("gun");var gun=session.State.Vessels[0];var draft=Draft(session,new AssemblyQuota("escort",new(2)),new AssemblyQuota("escort",new(1))) with{AcceptedIdleVessels=ImmutableArray.Create(gun.Id)};Accept(session,draft);var r=session.State.Empires[0].Fleets!.Requests.Single();
        var amended=Draft(session,new AssemblyQuota("escort",new(2)),new AssemblyQuota("escort",new(2)));var p=AssemblyPlanner.Preview(FleetPlanningQueries.Create(session.Content,session.View(session.State.Empires[0].Id)),amended,r.Id);Assert.True(p.Allowed,string.Join("; ",p.Rejections));Action(session,new AssemblyAction.Amend(r.Id,r.Revision,amended,p.Fingerprint));
        r=session.State.Empires[0].Fleets!.Requests.Single();Assert.Equal(gun.Id,r.Slots.Single(s=>s.Vessel is not null).Vessel);Assert.All(r.Slots,s=>Assert.Equal(new DesignId(2),s.Design));Assert.Single(session.State.Colonies[0].Development!.Queue);
    }
    [Fact]
    public void ManualTakeoverAndCancellationPreserveReserveAndExactQueue()
    {
        var session=ShipDesignMathTests.Equipped("gun");var owner=session.State.Empires[0];var port=session.State.Colonies[0];port.ProductionReserve=700;Accept(session,Draft(session,new AssemblyQuota("escort",new(2))));
        Execute(session,new SetProductionTargetCommand(owner.Id,0,port.Id,ShipDesignTargets.Build(new(1))));var r=session.State.Empires[0].Fleets!.Requests.Single();Assert.Contains(port.Id,r.PausedContributions);var queue=session.State.Colonies[0].Development!.Queue.ToArray();Action(session,new AssemblyAction.Cancel(r.Id,r.Revision,RequestCancellation.KeepQueue));Assert.Equal(queue,session.State.Colonies[0].Development!.Queue);Assert.Equal(700,session.State.Colonies[0].ProductionReserve);
    }
    [Fact]
    public void MissingExactSlotDoesNotAuthorizeAutomaticReplacement()
    {
        var session=ShipDesignMathTests.Equipped("gun");var ship=session.State.Vessels[0];var owner=session.State.Empires[0];Accept(session,Draft(session,new AssemblyQuota("escort",new(2))) with{AcceptedIdleVessels=ImmutableArray.Create(ship.Id)});
        var r=session.State.Empires[0].Fleets!.Requests.Single();Action(session,new AssemblyAction.DetachSlot(r.Id,r.Revision,r.Slots.Single().Ordinal));int before=session.State.Vessels.Count;session.State.Colonies[0].ProductionReserve=20000;
        Execute(session,new SetReadyCommand(owner.Id,0,true));Assert.Equal(before,session.State.Vessels.Count);r=session.State.Empires[0].Fleets!.Requests.Single();Assert.Equal(SlotStatus.Missing,r.Slots.Single().Status);Assert.Equal(new DesignId(2),r.Slots.Single().Design);Assert.Empty(session.State.Colonies[0].Development!.Queue);
    }
    [Fact]
    public void AlternativeDeclaredRoleFulfillsItsPinnedQuota()
    {
        var original=ShipDesignFixtures.Session();var c=original.Content;var role=c.Vessels["escort"] with{Id="guardian"};c=c with{Vessels=c.Vessels.Remove("escort").Add(role.Id,role),ShipDesigns=c.ShipDesigns! with{WarshipRoleId=role.Id},Combat=c.Combat! with{RoleProfiles=c.Combat.RoleProfiles.Remove("escort").Add(role.Id,c.Combat.RoleProfiles["escort"])}};
        var session=new GameSession(c,original.State);var id=ShipDesignFixtures.SaveDesign(session,"Gun",new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")));session.State.Colonies[0].ProductionReserve=10000;
        Accept(session,Draft(session,new AssemblyQuota(role.Id,id)));var e=session.State.Empires[0];Execute(session,new SetReadyCommand(e.Id,0,true));Assert.Equal(role.Id,session.State.Vessels.Last().RoleId);Assert.Equal(id,session.State.Vessels.Last().Design);Execute(session,new SetReadyCommand(e.Id,1,true));Assert.Equal(RequestStatus.Complete,session.State.Empires[0].Fleets!.Requests.Single().Status);
    }
    [Fact]
    public void ForeignIdleShipCannotBecomeAnOwnExactSlot()
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;var made=OpenAntares.Simulation.Galaxy.GalaxyGenerator.CreateNewGame(c,new("small",42,10,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.True(made.Succeeded,made.Error);
        var session=new GameSession(c,made.State!);var foreign=session.State.Vessels.Single(v=>v.EmpireId!=session.State.Empires[0].Id);var draft=Draft(session,new AssemblyQuota("escort",new(1))) with{AcceptedIdleVessels=ImmutableArray.Create(foreign.Id)};Assert.False(Preview(session,draft).Allowed);
    }
    private static AssemblyDraft Draft(GameSession s,params AssemblyQuota[] quotas)=>new("Exact request",s.State.Vessels[0].StationaryStarId!.Value,null,ImmutableArray<string>.Empty,ImmutableArray.Create(s.State.Colonies[0].Id),RequestPriority.Normal,ImmutableArray<VesselId>.Empty,false,null){Quotas=quotas.ToImmutableArray()};
    private static AssemblyPreview Preview(GameSession s,AssemblyDraft d)=>AssemblyPlanner.Preview(FleetPlanningQueries.Create(s.Content,s.View(s.State.Empires[0].Id)),d);
    private static void Accept(GameSession s,AssemblyDraft d){var p=Preview(s,d);Assert.True(p.Allowed,string.Join("; ",p.Rejections));Action(s,new AssemblyAction.Accept(d,p.Fingerprint));}
    private static void Action(GameSession s,AssemblyAction a)=>Execute(s,new AssemblyCommand(s.State.Empires[0].Id,s.State.Turn,a));
    private static CommandOutcome Execute(GameSession s,Command c){var r=s.Execute(c);Assert.True(r.Accepted,r.Rejection?.Message);return r;}
    private static GameSession Organizer(int xp)
    {
        // Labelled pre-session rank/commission-completion supplement, matching the established officer fixture.
        var session=ShipDesignFixtures.Session();var c=session.Content;var s=session.State;var e=s.Empires[0];var profile=c.FleetCommand!.Profiles.Single(p=>p.CultureId==c.Species["harl"].CommandCultureId&&p.Specialty==OfficerSpecialty.Organizer).Id;
        var result=FleetOfficerTests.Run(new LoadedGame(c,s),new OfficerAction.Commission(profile,s.Colonies[0].Id));Assert.True(result.Accepted,result.Rejection?.Message);session=new(c,result.State);s=session.State;s.Turn=2;var o=s.Empires[0].Fleets!.Officers.Last();o.Status=OfficerStatus.Posted;o.AvailableAtTurn=null;o.Xp=xp;o.Rank=xp>=30?2:1;ObservationUpdater.Refresh(s,2);Assert.Empty(GameStateValidator.Validate(c,s));return session;
    }
}
