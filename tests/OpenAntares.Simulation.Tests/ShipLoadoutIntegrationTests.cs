using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipLoadoutIntegrationTests
{
    [Theory]
    [InlineData("standard",24,4,100,22,72)]
    [InlineData("fast",24,4,100,22,72)]
    [InlineData("gun",48,0,100,46,64)]
    [InlineData("frigate",60,8,180,58,160)]
    public void LoadoutCallSitesUseInstalledStatsForOneActualVolley(string kind,int attack,int armor,int hull,int damage,int after)
    {
        var session=Game(kind,home:false);var action=CombatCommandTests.Attack(session.State);var preview=CombatQueries.Preview(session.Content,session.View(session.State.Empires[0].Id),action);Assert.True(preview.Allowed,preview.RejectionCode);var frame=Assert.Single(preview.Frame!.Ships);Assert.Equal((attack,armor,hull),(frame.Attack,frame.Armor,frame.MaxHull));Assert.NotNull(frame.Equipment);Assert.Equal(damage,Assert.Single(preview.Exchange!.Shots,s=>!s.Source.Sentry).Damage);
        Combat(session,action);var result=Ready(session);var receipt=Assert.Single(result.Report!.CombatRounds);Assert.Equal(after,session.State.Vessels[0].Combat!.Hull);Assert.Equal(80-damage,session.State.Combat!.Pockets[0].Sentries[0].Hull);Assert.Equal(damage,Assert.Single(receipt.Result.Shots,s=>!s.Source.Sentry).Damage);Assert.NotNull(receipt.Before.Ships[0].Equipment);Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
    }
    [Theory]
    [InlineData("standard",50,75,100)]
    [InlineData("frigate",90,135,180)]
    public void FiniteRepairUsesEquippedMaximum(string kind,int damaged,int first,int maximum)
    {
        var session=Game(kind);var ship=session.State.Vessels[0];ship.Combat!.Hull=damaged;ship.Combat.Cohesion=50;var action=new CombatAction.SetRepair(ImmutableArray.Create(ship.Id),true);var p=CombatQueries.Preview(session.Content,session.View(ship.EmpireId),action);Assert.True(p.Allowed,p.RejectionCode);Assert.Equal(maximum,p.AffectedShips[0].MaxHull);
        Combat(session,action);Ready(session);Assert.Equal(first,session.State.Vessels[0].Combat!.Hull);Assert.Equal(70,session.State.Vessels[0].Combat!.Cohesion);Ready(session);Assert.Equal(maximum,session.State.Vessels[0].Combat!.Hull);Assert.Equal(90,session.State.Vessels[0].Combat!.Cohesion);Ready(session);Assert.Equal(100,session.State.Vessels[0].Combat!.Cohesion);Assert.False(session.State.Vessels[0].Combat!.Repairing);
    }
    [Theory]
    [InlineData("fast",400,false)]
    [InlineData("frigate",225,true)]
    public void WithdrawalUsesInstalledEngineAndFrame(string kind,int speed,bool exposed)
    {
        var session=Game(kind,home:false);Combat(session,CombatCommandTests.Attack(session.State));Ready(session);var e=session.State.Combat!.Encounters.Single();var p=CombatQueries.Preview(session.Content,session.View(session.State.Empires[0].Id),new CombatAction.WithdrawCombat(e.Id,e.Revision));Assert.True(p.Allowed,p.RejectionCode);Assert.Equal(speed,Assert.Single(p.Departures).Speed);Assert.Equal(exposed,!p.Exposed.IsEmpty);
    }
    [Fact]
    public void DamageThenNavigatorThenFleetMinimumAgreeWithActualLaunch()
    {
        var session=Game("fast");var s=session.State;var fast=s.Vessels[0];fast.Combat!.Hull=50;var owner=s.Empires[0];var d=session.Content.ShipDesigns!;var recipe=new ShipRecipe("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank","pulse_bank"));owner.ShipDesigns!.Designs.Add(new(new(3),"Slow damaged frigate",recipe,false));owner.ShipDesigns.NextDesignId=4;
        var frigate=fast.Clone();frigate.Id=new(s.AllocateId());frigate.Design=new(3);CombatInitializer.InitializeVessel(session.Content,s,frigate);frigate.Combat!.Hull=90;s.Vessels.Add(frigate);ObservationUpdater.Refresh(s,0);Fleet(session,new FleetAction.Create("Mixed",ImmutableArray.Create(fast.Id,frigate.Id),fast.Id));var group=session.State.Empires[0].Fleets!.Fleets.Single();var officer=session.State.Empires[0].Fleets!.Officers[0];officer.Xp=30;officer.Rank=2;
        var assigned=FleetOfficerTests.Run(new LoadedGame(session.Content,session.State),new OfficerAction.AssignFlagship(officer.Id,group.Id));Assert.True(assigned.Accepted,assigned.Rejection?.Message);session=new(session.Content,assigned.State);
        Assert.Equal(330,OfficerBonuses.EffectiveSpeed(session.Content,session.State,fast.Id));Assert.Equal(184,OfficerBonuses.EffectiveSpeed(session.Content,session.State,frigate.Id));var home=session.State.FindStar(fast.StationaryStarId!.Value)!;var target=session.State.Stars.Where(st=>st.Id!=home.Id&&st.Id!=session.State.Combat!.Pockets[0].Star).MaxBy(st=>(long)(st.X-home.X)*(st.X-home.X)+(long)(st.Y-home.Y)*(st.Y-home.Y))!;int expected=TravelCalculator.Duration(home.X,home.Y,target.X,target.Y,184);
        var view=session.View(owner.Id);Assert.Equal(expected,FleetTravel.PreviewDuration(session.Content,view,view.Vessels.Single(v=>v.Id==fast.Id),target.Id));Assert.Equal(expected,FleetTravel.Duration(session.Content,session.State,owner.Id,group.Id,target.Id));Fleet(session,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(group.Id),target.Id));Ready(session);Assert.All(session.State.Vessels,v=>Assert.Equal(expected,v.Transit!.TotalTurns));
    }
    [Fact]
    public void LoadoutTravelChangesWithoutChangingAmebaRiskInput()
    {
        var session=Game("fast",amebas:true);var c=session.Content;var s=session.State;var v=s.Vessels[0];var habitat=s.Scouting!.Habitat!;var home=s.FindStar(v.StationaryStarId!.Value)!;var target=s.FindStar(habitat.Star)!;home.X=0;home.Y=0;target.X=701;target.Y=0;Assert.Empty(GameStateValidator.Validate(c,s));var view=session.View(v.EmpireId);int expected=2;
        Assert.Equal(expected,ScoutQueries.Duration(c,view,view.Vessels.Single(),target.Id));Assert.Equal(expected,FleetTravel.PreviewDuration(c,view,view.Vessels.Single(),target.Id));Assert.Equal(300,view.Scouting!.Vessels.Single().Speed);
        // Labelled direct-risk supplement isolating the old role-speed calculation from travel.
        v.StationaryStarId=habitat.Star;v.PendingMove=home.Id;v.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id,ArrivalTurn=0,Stage=AmebaStage.Exposed};s.Empires[0].Scouting!.NextEncounterNumber=2;s.Empires[0].Scouting!.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));var risk=s.Empires[0].Scouting!.RiskRandom!.Clone();int percent=AmebaOdds.LateEscape(c.SpaceAmeba!,300);bool survives=risk.NextInt(100)<percent;var candidate=s.Clone();var context=AmebaResolutionContext.Capture(c,s);AmebaResolver.ResolveExisting(c,s,candidate,context);
        Assert.Equal(percent,Assert.Single(context.Events).Percent);Assert.Equal(survives,candidate.Vessels.Any(ship=>ship.Id==v.Id));Assert.Equal(risk.State,candidate.Empires[0].Scouting!.RiskRandom?.State);
    }
    [Fact]
    public void RefitNeverRepricesCommittedTripOrPastVolley()
    {
        var session=Game("gun",home:false);session.State.Colonies[0].ProductionReserve=10000;var ship=session.State.Vessels[0];var fast=ShipDesignFixtures.SaveDesign(session,"Later fast",session.Content.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"});Combat(session,CombatCommandTests.Attack(session.State));var round=Ready(session);var receipt=Assert.Single(round.Report!.CombatRounds);string volley=JsonSerializer.Serialize(receipt);Assert.Equal(48,receipt.Before.Ships[0].Attack);
        var encounter=session.State.Combat!.Encounters.Single();Combat(session,new CombatAction.WithdrawCombat(encounter.Id,encounter.Revision));int? committed=null;
        for(int n=0;n<24&&(session.State.Combat!.Encounters.Count>0||session.State.FindVessel(ship.Id)!.Transit is not null||session.State.FindVessel(ship.Id)!.Combat!.ReturnColony is not null||session.State.FindVessel(ship.Id)!.Combat!.Repairing);n++)
        {
            var v=session.State.FindVessel(ship.Id)!;if(v.Transit is {} transit){committed??=transit.TotalTurns;Assert.Equal(committed,transit.TotalTurns);Assert.False(RefitCommandTests.Preview(session,new RefitAction.Start(ship.Id,session.State.Colonies[0].Id,fast)).Allowed);}Ready(session);
        }
        Assert.NotNull(committed);Assert.Equal(100,session.State.FindVessel(ship.Id)!.Combat!.Hull);Assert.Equal(100,session.State.FindVessel(ship.Id)!.Combat!.Cohesion);RefitCommandTests.Apply(session,new RefitAction.Start(ship.Id,session.State.Colonies[0].Id,fast));Ready(session);Assert.Equal(fast,session.State.FindVessel(ship.Id)!.Design);Assert.Equal(400,OfficerBonuses.EffectiveSpeed(session.Content,session.State,ship.Id));
        var owner=session.State.Empires[0];Do(session,new ShipDesignCommand(owner.Id,session.State.Turn,owner.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Archive(new(2))));owner=session.State.Empires[0];Do(session,new ShipDesignCommand(owner.Id,session.State.Turn,owner.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Delete(new(2))));var history=session.State.Empires[0].Combat!.History.Single();Assert.Equal(volley,JsonSerializer.Serialize(history.Details[0]));Assert.Empty(GameStateValidator.Validate(session.Content,session.State));
        var current=session.State.FindVessel(ship.Id)!;var home=session.State.FindStar(current.StationaryStarId!.Value)!;var target=session.State.Stars.Where(st=>st.Id!=home.Id).MaxBy(st=>(long)(st.X-home.X)*(st.X-home.X)+(long)(st.Y-home.Y)*(st.Y-home.Y))!;int expected=TravelCalculator.Duration(home.X,home.Y,target.X,target.Y,400);var fleet=session.State.Empires[0].Fleets!.Fleets.Single();Fleet(session,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),target.Id));Ready(session);current=session.State.FindVessel(ship.Id)!;if(expected==1)Assert.Equal(target.Id,current.StationaryStarId);else Assert.Equal(expected,current.Transit!.TotalTurns);Assert.Equal(volley,JsonSerializer.Serialize(session.State.Empires[0].Combat!.History.Single().Details[0]));
    }
    [Fact]
    public void ClonedCustomCombatContinuesIdenticallyWithAllRandomStreams()
    {
        var first=Game("gun",home:false);Combat(first,CombatCommandTests.Attack(first.State));var second=new GameSession(first.Content,first.State.Clone());
        for(int n=0;n<3;n++){Ready(first);Ready(second);Assert.Equal(JsonSerializer.Serialize(first.State),JsonSerializer.Serialize(second.State));Assert.Equal(first.State.Random.State,second.State.Random.State);Assert.Equal(first.State.Empires[0].Scouting!.RiskRandom?.State,second.State.Empires[0].Scouting!.RiskRandom?.State);Assert.Equal(first.State.Empires[0].Fleets!.RecoveryRandom!.State,second.State.Empires[0].Fleets!.RecoveryRandom!.State);}
    }
    [Fact]
    public void DetachedCrippledFrigateUsesFrozenInstalledBaseSpeed()
    {
        var session=Game("fast",home:false);var s=session.State;var e=s.Empires[0];var recipe=new ShipRecipe("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank","pulse_bank"));e.ShipDesigns!.Designs.Add(new(new(3),"Crippled frigate",recipe,false));e.ShipDesigns.NextDesignId=4;var frigate=s.Vessels[0].Clone();frigate.Id=new(s.AllocateId());frigate.Design=new(3);CombatInitializer.InitializeVessel(session.Content,s,frigate);frigate.Combat!.Hull=45;s.Vessels.Add(frigate);e.Fleets!.Fleets[0].Members.Add(frigate.Id);ObservationUpdater.Refresh(s,0);Assert.Empty(GameStateValidator.Validate(session.Content,s));
        var action=CombatCommandTests.Attack(s);action=action with{Orders=action.Orders with{Stragglers=StragglerPolicy.DetachCrippled}};var p=CombatQueries.Preview(session.Content,session.View(e.Id),action);Assert.True(p.Allowed,p.RejectionCode);var departures=CombatWithdrawal.Preview(session.Content,p.Frame!);Assert.Equal(2,departures.Length);Assert.Equal(112,departures.Single(d=>d.Members.Contains(frigate.Id)).Speed);Assert.Equal(400,departures.Single(d=>!d.Members.Contains(frigate.Id)).Speed);
    }
    internal static GameSession Game(string kind="standard",bool home=true,bool amebas=false)
    {
        // Labelled pre-session equipment/combat supplement, not paid generated-campaign evidence.
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;var made=GalaxyGenerator.CreateNewGame(c,new("small",42,10,"harl",CrisesEnabled:false,AmebasEnabled:amebas,SentriesEnabled:true));Assert.True(made.Succeeded,made.Error);var s=made.State!;var e=s.Empires[0];e.KnownTechnologyIds=c.Technologies.Keys.ToList();var d=c.ShipDesigns!;
        var recipe=kind switch{"fast"=>d.StartingRecipe with{EngineId="vector_drive"},"gun"=>new ShipRecipe("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")),"frigate"=>new ShipRecipe("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank","pulse_bank")),_=>d.StartingRecipe};
        DesignId id=new(1);if(kind!="standard"){id=new(2);e.ShipDesigns!.Designs.Add(new(id,"Fixture "+kind,recipe,false));e.ShipDesigns.NextDesignId=3;}
        var v=s.Vessels[0];v.RoleId=d.WarshipRoleId;v.Design=id;ScoutInitializer.InitializeVessel(c,10,v);CombatInitializer.InitializeVessel(c,s,v);
        if(!home)
        {
            var pocket=s.Combat!.Pockets[0];v.StationaryStarId=pocket.Star;v.Fleet=new(1);e.Fleets!.Fleets.Add(new(){Id=new(1),Name="Fixture combat",Members=new(){v.Id},Flagship=v.Id});e.Fleets.NextFleetId=2;
            e.Combat!.Observations.Add(new(pocket.Id,pocket.Star,0,false,pocket.Sentries.Select(x=>new ObservedSentry(x.Id,x.Profile,x.Hull,c.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
        }
        ObservationUpdater.Refresh(s,0);Assert.Empty(GameStateValidator.Validate(c,s));return new(c,s);
    }
    private static void Combat(GameSession s,CombatAction a){var p=CombatQueries.Preview(s.Content,s.View(s.State.Empires[0].Id),a);Assert.True(p.Allowed,p.RejectionCode);Do(s,new CombatActionCommand(s.State.Empires[0].Id,s.State.Turn,a,p.Token));}
    private static CommandOutcome Ready(GameSession s)=>Do(s,new SetReadyCommand(s.State.Empires[0].Id,s.State.Turn,true));
    private static CommandOutcome Do(GameSession s,Command c){var r=s.Execute(c);Assert.True(r.Accepted,r.Rejection?.Message);return r;}
    private static void Fleet(GameSession s,FleetAction a){var p=FleetQueries.PreviewAction(s.Content,s.View(s.State.Empires[0].Id),a);Assert.True(p.Allowed,p.Rejection);Do(s,new FleetCommand(s.State.Empires[0].Id,s.State.Turn,a){Fingerprint=p.Fingerprint});}
}
