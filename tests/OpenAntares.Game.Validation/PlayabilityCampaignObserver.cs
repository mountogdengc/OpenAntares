using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;

namespace OpenAntares.Game.Validation;

/// <summary>Observes the production screen's command events after its real handler executes.</summary>
internal sealed class PlayabilityCampaignObserver
{
    private readonly Node _host;
    private readonly Action<bool,string> _check;
    private readonly string _journalPath;
    private GameScreen _screen;
    private GameSession? _reference;
    private string _ui="";
    private int _commandCount;
    private readonly List<object> _journal=new();
    private readonly List<(object Component,EventInfo Event,Delegate Handler)> _subscriptions=new();
    public event Action<Command,bool>? Recorded;
    public long OwnProductionPaid {get;private set;}
    public long OwnProductionIncome {get;private set;}
    public int CommittedBoundaries {get;private set;}
    public GameSession Game=>(GameSession)typeof(GameScreen).GetField("_session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(_screen)!;
    public EmpireId Owner {get;}
    public IReadOnlyList<object> Journal=>_journal;
    public static string Snapshot(GameSession game)=>OpenAntares.Simulation.Saves.SaveGame.Serialize(game.Content,game.State);
    public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public PlayabilityCampaignObserver(Node host,GameScreen screen,string journalPath,Action<bool,string> check)
    {
        _host=host;_screen=screen;_journalPath=journalPath;_check=check;
        Owner=Game.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id;
        Attach(screen);
    }
    public void Attach(GameScreen screen)
    {
        foreach(var entry in _subscriptions)entry.Event.RemoveEventHandler(entry.Component,entry.Handler);
        _subscriptions.Clear();_screen=screen;
        foreach(string name in new[]{"_researchBar","_colonyPanel","_vessels","_fleetCommand","_diplomacy","_crisisOverview","_crisisResponse","_scoutOverview","_conflictOrders","_management","_assemblyRequest","_officerDetails"})
        {
            object component=typeof(GameScreen).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(screen)!;
            var eventInfo=component.GetType().GetEvent("CommandIssued")??component.GetType().GetEvent("CommandRequested");
            if(eventInfo is null)throw new InvalidOperationException("No production command event: "+name);
            Action<Command> observer=command=>Observe(command,component.GetType().Name+"."+eventInfo.Name);
            var handler=Delegate.CreateDelegate(eventInfo.EventHandlerType!,observer.Target,observer.Method);
            eventInfo.AddEventHandler(component,handler);_subscriptions.Add((component,eventInfo,handler));
        }
        var end=Controls<Button>(screen).Single(b=>b.Name=="EndTurn");
        end.Pressed+=()=>
        {
            if(_reference is null)throw new InvalidOperationException("Unexpected Ready activation outside an explicit recorded UI step.");
            Observe(new SetReadyCommand(Owner,_reference.State.Turn,!_reference.View(Owner).Research.Ready),"EndTurn.Pressed");
        };
    }
    private void Observe(Command command,string source)
    {
        if(_reference is null)throw new InvalidOperationException("Unexpected campaign command during navigation: "+command.GetType().Name);
        string before=Snapshot(_reference);int turn=_reference.State.Turn;
        var outcome=_reference.Execute(command);_commandCount++;
        _check(Snapshot(Game)==Snapshot(_reference)&&JsonSerializer.Serialize(Game.State)==JsonSerializer.Serialize(_reference.State),"Actual Main UI command exactly matches ordinary engine authority: "+command.GetType().Name);
        var entry=new{sequence=_journal.Count,ui=_ui,source,command=EmpireConflictCampaignChecks.JournalCommand(command),accepted=outcome.Accepted,changed=outcome.Changed,beforeTurn=turn,afterTurn=_reference.State.Turn,beforeSave=Hash(before),afterSave=Hash(Snapshot(Game)),afterState=Hash(JsonSerializer.Serialize(Game.State)),actualCommandCount=1,rejection=outcome.Rejection?.Message};
        _journal.Add(entry);File.AppendAllText(Path.ChangeExtension(_journalPath,"jsonl"),JsonSerializer.Serialize(entry)+"\n");
        if(outcome.Report is {} report)
        {
            CommittedBoundaries++;
            var owned=report.Economy.Colonies.Where(c=>c.Empire==Owner);
            OwnProductionIncome+=owned.Sum(c=>c.NetProduction.Total);
            OwnProductionPaid+=owned.SelectMany(c=>c.Production.Change.Lines).Where(l=>l.Kind=="cost_paid").Sum(l=>-l.Amount);
        }
        Recorded?.Invoke(command,outcome.Accepted);
    }
    public void FlushJournal()=>File.WriteAllText(_journalPath,JsonSerializer.Serialize(_journal,new JsonSerializerOptions{WriteIndented=true}));
    public async Task ExecuteUi(string label,Func<Task> action,int expectedCommands=1,Command? reviewedCallback=null)
    {
        if(_reference is not null)throw new InvalidOperationException("Nested campaign UI command step.");
        _ui=label;_commandCount=0;_reference=new GameSession(Game.Content,Game.State.Clone());
        try
        {
            await action();await Frames(3);
            // CombatOrdersDialog deliberately invokes the production screen callback rather
            // than exposing a public command event. Reconstruct only its unchanged public preview.
            if(_commandCount==0&&reviewedCallback is not null)Observe(reviewedCallback,"CombatOrdersDialog.ReviewedCallback");
            _check(_commandCount==expectedCommands,"Recorded native UI step emits its exact command count: "+label);
            _check(Snapshot(Game)==Snapshot(_reference),"UI refresh after the explicit step adds no unrecorded command: "+label);
        }
        finally{_reference=null;_ui="";}
    }
    public async Task Navigate(string label,Func<Task> action)
    {
        string before=Snapshot(Game);await action();await Frames(3);
        _check(before==Snapshot(Game),"Campaign inspection issues no command: "+label);
    }
    public async Task Frames(int count=2){for(int i=0;i<count;i++)await _host.ToSignal(_host.GetTree(),SceneTree.SignalName.ProcessFrame);}
    public static IEnumerable<T> Controls<T>(Node root) where T:Node
    {foreach(Node child in root.GetChildren()){if(child is T typed)yield return typed;foreach(var nested in Controls<T>(child))yield return nested;}}
}
