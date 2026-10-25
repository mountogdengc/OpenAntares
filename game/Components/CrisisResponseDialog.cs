using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
namespace OpenAntares.Game.Components;
public partial class CrisisResponseDialog:AcceptDialog
{
    private VBoxContainer _body=null!;private Button _confirm=null!;private Command? _command;
    public event Action<Command>? CommandRequested;
    public override void _Ready(){OkButtonText="Cancel";Name="CrisisResponse";_body=DevelopmentUi.DialogBody(this);_confirm=DevelopmentUi.Button(_body,"Confirm purge",()=>{if(_command is {} command){Hide();CommandRequested?.Invoke(command);}},"CrisisConfirmPurge");}
    private void Begin(string title,string name){Title=title;DevelopmentUi.Clear(_body);_command=null;Name=name;}
    private void Confirm(string text,Command? command,string name){_command=command;_confirm=DevelopmentUi.Button(_body,text,()=>{if(_command is {} order){Hide();CommandRequested?.Invoke(order);}},name);_confirm.Disabled=command is null;PopupCenteredClamped(new(660,510));}
    public void ShowPurge(PurgePreview p)
    {
        Begin("Confirm destructive containment","CrisisPurgePreview");
        _body.AddChild(DevelopmentUi.Text($"{p.Location?.Name??"Unavailable colony"} ({p.Location?.Colony}) — population {p.PopulationBefore}: lose {p.PopulationLost}, retain {p.PopulationAfter}.","WarningLabel"));
        _body.AddChild(DevelopmentUi.Text($"Production reserve {Format.Points(p.ReserveBefore)} → {Format.Points(p.ReserveAfter)} points. Growth progress resets to 0. Workforce S/P/R {p.WorkforceBefore.Support}/{p.WorkforceBefore.Production}/{p.WorkforceBefore.Research} → {p.WorkforceAfter.Support}/{p.WorkforceAfter.Production}/{p.WorkforceAfter.Research}."));
        _body.AddChild(DevelopmentUi.Text($"Empire civic strain {p.CivicLevelBefore} → {p.NextCivicLevel}, expires at turn {p.NextCivicExpiresAt}. Production/research penalty: {p.NextCivicLevel*10}% throughout the empire. Other cases ({p.OtherCases.Length}), carriers ({p.Carriers.Length}) and cure progress remain."));
        _body.AddChild(DevelopmentUi.Text(p.ColonyLost?$"Colony and all local buildings, queue and assets are lost. {(p.LastColony?"This is your last colony. ":"")}Surviving founders: {p.SurvivingFounders}. Recovery requires an existing founder with population cargo; no free colony or population is created.":$"Buildings ({p.Assets?.Buildings.Length??0}), active project, queue and assigned plan survive. This case ends; the purge does not cure the rest of the empire."));
        _body.AddChild(DevelopmentUi.Text($"Cargo orders affected: {p.Orders.Count(o=>o.Move is not null||o.Founding is not null)}. Source port {(p.SourcePortClosedBefore?"closed":"open")} → {(p.SourcePortClosedAfter?"closed":"open")}."));
        if(!p.Allowed)_body.AddChild(DevelopmentUi.Text("Unavailable: "+p.RejectionReason,"WarningLabel"));
        Confirm("Confirm purge",p.Allowed?new PurgeColonyCommand(p.Owner,p.Turn,p):null,"CrisisConfirmPurge");
    }
    public void ShowQuarantine(QuarantinePreview p)
    {
        Begin(p.Enable?"Confirm quarantine":"Confirm reopening","CrisisQuarantinePreview");
        foreach(var c in p.Colonies)_body.AddChild(DevelopmentUi.Text($"{c.Location.Name} ({c.Colony}): {(c.Before?"quarantined":"open")} → {(c.After?"quarantined":"open")}. Release: {(c.ReleaseAfter.HasValue?"when this episode recovers":"only on your order")}."));
        _body.AddChild(DevelopmentUi.Text($"Owned cargo ports closed: {p.ClosedPortsBefore.Length} → {p.ClosedPortsAfter.Length}. Cancel {p.CancelledOrders.Length} stationary cargo orders. Committed transit continues; arrivals wait for one full inspection interval. Other empires' ports are independent."));
        foreach(var e in p.Economy)_body.AddChild(DevelopmentUi.Text($"Colony {e.Colony}: production {Format.Points(e.ProductionBefore)} → {Format.Points(e.ProductionAfter)}, research {Format.Points(e.ResearchBefore)} → {Format.Points(e.ResearchAfter)}, growth {e.GrowthBefore} → {e.GrowthAfter}. Cargo completion {(e.CargoHeld?"waits":"permitted")}; reserve retained."));
        if(!p.Allowed)_body.AddChild(DevelopmentUi.Text("Unavailable: "+p.RejectionReason,"WarningLabel"));
        Confirm("Apply response",p.Allowed?new SetQuarantineCommand(p.Owner,p.Turn,p):null,"CrisisConfirmQuarantine");
    }
}
