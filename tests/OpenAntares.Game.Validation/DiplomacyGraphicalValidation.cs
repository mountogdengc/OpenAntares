using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Godot;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Diplomacy;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class DiplomacyGraphicalValidation:Node
{
    private string _output="";private readonly List<object> _checks=new();
    private void Check(bool value,string text){_checks.Add(new{passed=value,text});if(!value)throw new InvalidOperationException(text);}
    public override async void _Ready()
    {
        string? error=null;
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Actual graphical display");await Navigation();await OfferControls();}
        catch(Exception e){error=e.ToString();GD.PrintErr(error);}
        finally{if(_output.Length>0)File.WriteAllText(Path.Combine(_output,"results.json"),JsonSerializer.Serialize(new{scenario="Diplomacy",passed=error is null,error,method="Actual Godot control signals; labelled UI supplements. No physical input acceptance claim.",checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(error is null?0:1);}
    }
    private async Task Navigation()
    {
        var content=CompleteContent();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,10,"harl",0,null,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false)).State!;state.RulesVersion=11;DiplomacyInitializer.Initialize(content,state);
        var session=new GameSession(content,state);var screen=new GameScreen(session,state.Empires[0].Id);AddChild(screen);await Frames(this);
        try{foreach(var size in new[]{new Vector2I(800,600),new Vector2I(1920,1080)}){GetTree().Root.Size=size;await Frames(this);var entry=Descendants<Button>(screen).SingleOrDefault(b=>b.Name=="OpenDiplomacy");Check(entry is not null,"Reachable diplomacy navigation at "+size.X);entry!.EmitSignal(Button.SignalName.Pressed);await Frames(this);var dialog=Descendants<Window>(screen).Single(w=>w.Name=="DiplomacyDialog");Check(dialog.Visible,"Diplomacy opened");Check(string.Join(" ",Descendants<Label>(dialog).Select(l=>l.Text)).Contains("No other civilization"),"Solo explanation without invented contacts");dialog.Hide();entry.EmitSignal(Button.SignalName.Pressed);await Frames(this);Check(dialog.Visible,"Repeated open works");GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output,"navigation-"+size.X+".png"));dialog.Hide();}}
        finally{RemoveChild(screen);screen.QueueFree();await Frames(this);}
    }

    private async Task OfferControls()
    {
        var content=CompleteContent();var state=GalaxyGenerator.CreateNewGame(content,new("small",42,10,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false)).State!;state.RulesVersion=11;DiplomacyInitializer.Initialize(content,state);
        var a=state.Empires[0];var b=state.Empires[1];b.Controller=OpenAntares.Simulation.State.ControllerKind.Human;b.AiPolicyId=null;
        a.Contacts.Add(new(b.Id,b.SpeciesId!,0));b.Contacts.Add(new(a.Id,a.SpeciesId!,0));state.Diplomacy!.Pairs.Add(new(){Pair=DiplomacyPair.For(a.Id,b.Id),Parties=new(){new(){Owner=a.Id},new(){Owner=b.Id}}});foreach(var c in state.Colonies)c.ProductionReserve=500;
        Check(OpenAntares.Simulation.State.GameStateValidator.Validate(content,state).Count==0,"Valid labelled pre-session two-human exact-price supplement");
        var session=new GameSession(content,state);var screen=new GameScreen(session,a.Id);AddChild(screen);await Frames(this);
        try{Descendants<Button>(screen).Single(b=>b.Name=="OpenDiplomacy").EmitSignal(Button.SignalName.Pressed);await Frames(this);Check(Descendants<Button>(screen).Any(b=>b.Name=="DiplomacyOfferTrade"),"Explicit trade offer control exists");
            await Click(screen,"DiplomacyOfferTrade");var offer=Descendants<OpenAntares.Game.Components.DiplomacyOfferDialog>(screen).Single();
            Check(!Named<Button>(offer,"DiplomacySubmit").Disabled,"Exact price productive own hub can submit");
            Check(Named<Label>(offer,"DiplomacyTerms").Text.Contains("500 internal"),"Terms show internal and displayed setup funds");
            var colony=session.State.Colonies.Single(c=>c.EmpireId==a.Id);var w=colony.Workforce;
            Check(session.Execute(new OpenAntares.Simulation.Commands.SetWorkforceCommand(a.Id,0,colony.Id,w with{Production=w.Production-1,Research=w.Research+1})).Accepted,"Public own-workforce change makes open terms stale");
            await Click(offer,"DiplomacySubmit");Check(session.State.Diplomacy!.Pairs.Single().Pending is null,"Stale open preview rejected without reservation");Check(Named<Label>(offer,"DiplomacyError").Text.Length>0,"Stale rejection displayed");
            await Click(offer,"DiplomacySubmit");Check(session.State.Diplomacy!.Pairs.Single().Pending is not null,"Refreshed terms submit through actual control");
            var own=session.View(a.Id).Colonies.Single();Check(own.ReservedProduction==500&&own.SpendableProduction==0,"Exact price500 reserved0 spendable");
            await Click(screen,"DiplomacyWithdraw");await Click(offer,"DiplomacySubmit");Check(session.State.Diplomacy!.Pairs.Single().Pending is null&&session.View(a.Id).Colonies.Single().ReservedProduction==0,"Actual Withdraw releases reservation");
            await Click(screen,"DiplomacyOfferSettlement");Check(Named<Button>(offer,"DiplomacySubmit").Disabled,"Settlement disclosure requires explicit confirmation");offer.Hide();
            GetTree().Root.Size=new(800,600);await Click(screen,"DiplomacyOfferTrade");var apply=Named<Button>(offer,"DiplomacySubmit");var scroll=Descendants<ScrollContainer>(offer).Single();apply.GrabFocus();scroll.EnsureControlVisible(apply);await Frames(this);Check(apply.GetGlobalRect().Intersects(scroll.GetGlobalRect()),"Narrow-window Submit is reachable with keyboard focus");await Capture(this,offer,Path.Combine(_output,"trade-terms-800.png"));await Click(offer,"DiplomacySubmit");
            foreach(var window in Descendants<Window>(screen))window.Hide();var incoming=new GameScreen(session,b.Id);AddChild(incoming);await Frames(this);
            try{await Click(incoming,"OpenDiplomacy");var ready=Descendants<Button>(incoming).Single(x=>x.Text=="End Turn");ready.EmitSignal(Button.SignalName.Pressed);await Frames(this);Check(session.State.Diplomacy!.Pairs.Single().Pending!.Answer is null,"Ready does not silently answer incoming offer");Check(session.Execute(new OpenAntares.Simulation.Commands.SetReadyCommand(b.Id,0,false)).Accepted,"Withdraw nonfinal readiness");
                await Click(incoming,"DiplomacyAccept");var answer=Descendants<OpenAntares.Game.Components.DiplomacyOfferDialog>(incoming).Single();Check(Descendants<OptionButton>(answer).Single().ItemCount==1,"Trade Accept selects only own hub");await Click(answer,"DiplomacySubmit");Check(session.View(b.Id).Colonies.Single().ReservedProduction==500,"Accept reserves recipient setup once");
                var json=OpenAntares.Simulation.Saves.SaveGame.Serialize(content,session.State);var restored=OpenAntares.Simulation.Saves.SaveGame.Deserialize(json);Check(restored.Succeeded&&OpenAntares.Simulation.Saves.SaveGame.Serialize(restored.Game!.Content,restored.Game.State)==json,"Queued answer and earmarks survive exact codec reload");
                await Click(incoming,"DiplomacyRevoke");await Click(answer,"DiplomacySubmit");Check(session.State.Diplomacy!.Pairs.Single().Pending!.Answer is null,"Actual Revoke returns answer to unanswered");
                await Click(incoming,"DiplomacyDecline");await Click(answer,"DiplomacySubmit");Check(session.State.Diplomacy!.Pairs.Single().Pending!.Answer?.Accept==false,"Actual explicit Decline queues decline");}
            finally{RemoveChild(incoming);incoming.QueueFree();await Frames(this);}
}
        finally{RemoveChild(screen);screen.QueueFree();await Frames(this);}
    }
    private async Task Click(Node root,string name){var b=Named<Button>(root,name);Check(!b.Disabled&&b.IsVisibleInTree(),"Enabled visible control "+name);for(Node? n=b.GetParent();n is not null;n=n.GetParent())if(n is ScrollContainer scroll)scroll.EnsureControlVisible(b);b.GrabFocus();await Frames(this);b.EmitSignal(Button.SignalName.Pressed);await Frames(this);}
}

