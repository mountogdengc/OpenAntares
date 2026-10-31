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
        try{_output=OS.GetCmdlineUserArgs().Single(a=>a.StartsWith("--validation-output="))[20..];Directory.CreateDirectory(_output);Check(DisplayServer.GetName()!="headless","Actual graphical display");await Navigation();}
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
}
