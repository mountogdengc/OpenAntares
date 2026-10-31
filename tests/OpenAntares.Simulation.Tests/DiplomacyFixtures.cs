using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Tests;
internal static class DiplomacyFixtures
{
    internal static GameSession Create(string species="harl",int humans=1)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var result=GalaxyGenerator.CreateNewGame(c,new("small",42,10,species,1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        var state=result.State!;state.RulesVersion=11;
        if(humans==2){state.Empires[1].Controller=ControllerKind.Human;state.Empires[1].AiPolicyId=null;}
        DiplomacyInitializer.Initialize(c,state);return new(c,state);
    }
    internal static GameSession WithContact(GameSession s)
    {
        var state=s.State.Clone();var a=state.Empires[0];var b=state.Empires[1];
        a.Contacts.Add(new(b.Id,b.SpeciesId!,state.Turn));b.Contacts.Add(new(a.Id,a.SpeciesId!,state.Turn));
        state.Diplomacy!.Pairs.Add(new(){Pair=DiplomacyPair.For(a.Id,b.Id),Parties=new(){new(){Owner=a.Id},new(){Owner=b.Id}}});
        return new(s.Content,state);
    }
}
