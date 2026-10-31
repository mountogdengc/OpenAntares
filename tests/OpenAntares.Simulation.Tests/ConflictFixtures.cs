using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Conflict;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Tests;
// Labelled pre-session supplements only; never earned campaign evidence.
internal static class ConflictFixtures
{
    internal static GameSession Create(string species="harl",int humans=1)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var g=GalaxyGenerator.CreateNewGame(c,new("small",42,12,species,1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        if(!g.Succeeded)throw new InvalidOperationException(g.Error);
        if(humans==2){g.State!.Empires[1].Controller=ControllerKind.Human;g.State.Empires[1].AiPolicyId=null;}
        return new(c,g.State!);
    }
    internal static string Canonical(GameState state)=>JsonSerializer.Serialize(state);
    internal static GameSession WithContact(GameSession source)
    {
        var s=source.State;var a=s.Empires[0];var b=s.Empires[1];
        if(!a.Contacts.Any(c=>c.OtherEmpireId==b.Id))a.Contacts.Add(new(b.Id,b.SpeciesId!,s.Turn));
        if(!b.Contacts.Any(c=>c.OtherEmpireId==a.Id))b.Contacts.Add(new(a.Id,a.SpeciesId!,s.Turn));
        var key=DiplomacyPair.For(a.Id,b.Id);
        if(!s.Diplomacy!.Pairs.Any(p=>p.Pair==key))s.Diplomacy.Pairs.Add(new(){Pair=key,Parties=new(){new(){Owner=key.Low},new(){Owner=key.High}}});
        if(s.Conflict!.Owners.Count==0)foreach(var e in s.Empires)s.Conflict.Owners.Add(new(){Owner=e.Id});
        if(!s.Conflict.Pairs.Any(p=>p.Pair==key))s.Conflict.Pairs.Add(new(){Pair=key,Parties=new(){new(){Owner=key.Low},new(){Owner=key.High}}});
        return new(source.Content,s);
    }
    internal static VesselState AddEscort(GameSession session,EmpireId owner,StarId star,int hull=100)
    {
        var s=session.State;var ship=new VesselState{Id=new(s.AllocateId()),EmpireId=owner,RoleId=session.Content.ShipDesigns!.WarshipRoleId,Design=new(1),StationaryStarId=star,Crisis=new()};
        ScoutInitializer.InitializeVessel(session.Content,s.RulesVersion,ship);CombatInitializer.InitializeVessel(session.Content,s,ship);ship.Combat!.Hull=hull;s.Vessels.Add(ship);return ship;
    }
    internal static GameSession WithEncounter(string species="harl")
    {
        var session=WithContact(Create(species,humans:2));var s=session.State;var p=s.Conflict!.Pairs[0];p.Relation=RelationKind.War;p.WarTurn=s.Turn;p.Declarers.Add(p.Pair.Low);
        p.Parties[1].DeclarationGrievance=true;p.Parties[1].LastCounterpartDeclarationTurn=s.Turn;
        var star=s.FindPlanet(s.Colonies[0].PlanetId)!.StarId;var low=AddEscort(session,p.Pair.Low,star);var high=AddEscort(session,p.Pair.High,star);
        var fleets=s.FindEmpire(p.Pair.Low)!.Fleets!;var f=new FleetRecord{Id=new(fleets.NextFleetId++),Name="Labelled conflict fleet",Members=new(){low.Id},Flagship=low.Id};fleets.Fleets.Add(f);low.Fleet=f.Id;
        s.Conflict.Encounters.Add(new(){Id=new(1),Pair=p.Pair,Star=star,CreatedTurn=s.Turn,LowSide=new(){Owner=p.Pair.Low,Fleet=f.Id,Anchor=low.Id,Flagship=low.Id,Members=new(){low.Id}},HighSide=new(){Owner=p.Pair.High,Anchor=high.Id,Flagship=high.Id,Members=new(){high.Id}}});s.Conflict.NextEncounterId=2;OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s,s.Turn);return session;
    }
    internal static GameSession FromFrozenSupplement(string name)
    {
        var load=SaveGame.ReadFile(System.IO.Path.Combine(CompatibilityFixtures.PathFor("rules11-conflict-baselines"),"sources",name+".json"));
        if(!load.Succeeded)throw new InvalidOperationException(string.Join("; ",load.Errors));
        var core=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var c=load.Game!.Content with{Conflict=core.Conflict};var s=load.Game.State;s.RulesVersion=12;ConflictInitializer.Initialize(c,s);return new(c,s);
    }
    internal static GameSession WithMixedDefense()
    {
        var g=WithEncounter();var s=g.State;var encounter=s.Conflict!.Encounters[0];var side=encounter.HighSide;
        var role=g.Content.Species[s.FindEmpire(side.Owner)!.SpeciesId!].AllowedVesselIds.Select(id=>g.Content.Vessels[id]).First(x=>x.CanFoundColony);
        var ship=new VesselState{Id=new(s.AllocateId()),EmpireId=side.Owner,RoleId=role.Id,StationaryStarId=encounter.Star,CarriedPopulation=role.EmbarkPopulation,Crisis=new()};CombatInitializer.InitializeVessel(g.Content,s,ship);s.Vessels.Add(ship);
        var data=s.FindEmpire(side.Owner)!.Fleets!;var fleet=new FleetRecord{Id=new(data.NextFleetId++),Name="Labelled protected convoy",Members=side.Members.Concat(new[]{ship.Id}).OrderBy(id=>id).ToList(),Flagship=ship.Id};data.Fleets.Add(fleet);
        foreach(var id in fleet.Members)s.FindVessel(id)!.Fleet=fleet.Id;side.Fleet=fleet.Id;side.Flagship=ship.Id;side.ProtectedCivilians.Add(ship.Id);return g;
    }
    internal static EmpireId AddNeutralObserver(GameSession game,StarId star)
    {
        var s=game.State;var extra=s.Empires[0].Clone();extra.Id=new(s.AllocateId());extra.Controller=ControllerKind.Human;extra.AiPolicyId=null;extra.Ready=false;extra.Contacts.Clear();extra.Surveys.Clear();
        extra.Crisis=new(){Random=OpenAntares.Simulation.Crises.CrisisInitializer.CreateRandom(s.GalaxySeed!.Value,extra.Id)};extra.Scouting=new();extra.Combat=new();
        var f=extra.Fleets!;f.Fleets.Clear();f.Requests.Clear();f.PendingEvents.Clear();f.RecentEvents.Clear();f.SurveyCredits.Clear();f.StudyCredits.Clear();f.HullCredits.Clear();f.RecoveryRandom=FleetInitializer.RecoveryRandom(s.GalaxySeed.Value,extra.Id);f.RecoveryRollCount=0;
        foreach(var o in f.Officers){o.Status=OfficerStatus.Retired;o.FinalSummary="Labelled neutral-observer appointment retired before this supplement.";o.PostedColony=null;o.Flagship=null;o.Request=null;o.RecoveryColony=null;o.AvailableAtTurn=null;}
        s.Empires.Add(extra);s.Empires.Sort((a,b)=>a.Id.CompareTo(b.Id));s.Conflict!.Owners.Add(new(){Owner=extra.Id});s.Conflict.Owners.Sort((a,b)=>a.Owner.CompareTo(b.Owner));
        var role=game.Content.Expansion!.StartingVesselId;var scout=new VesselState{Id=new(s.AllocateId()),EmpireId=extra.Id,RoleId=role,StationaryStarId=star,Crisis=new()};ScoutInitializer.InitializeVessel(game.Content,s.RulesVersion,scout);CombatInitializer.InitializeVessel(game.Content,s,scout);s.Vessels.Add(scout);
        foreach(var owner in s.Empires)foreach(var other in s.Empires.Where(e=>e.Id!=owner.Id))
        {if(!owner.Contacts.Any(c=>c.OtherEmpireId==other.Id))owner.Contacts.Add(new(other.Id,other.SpeciesId!,s.Turn));owner.Contacts.Sort((a,b)=>a.OtherEmpireId.CompareTo(b.OtherEmpireId));DiplomacyInitializer.EnsurePair(s,owner.Id,other.Id);ConflictInitializer.EnsurePair(s,owner.Id,other.Id);}
        OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s,s.Turn);return extra.Id;
    }
}
