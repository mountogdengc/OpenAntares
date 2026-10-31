using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Knowledge;
namespace OpenAntares.Game.Components;

/// <summary>Only discovered identities and current stationary sightings from the viewer projection.</summary>
public partial class ContactsPanel : VBoxContainer
{
    public event System.Action<OpenAntares.Simulation.EmpireId>? DiplomacyRequested;
    public void Show(EmpireView view)
    {
        foreach(Node child in GetChildren()){RemoveChild(child);child.QueueFree();}
        Visible=Rules.HasRivals(view.RulesVersion);if(!Visible)return;
        AddChild(new Label{Text="Contacts",ThemeTypeVariation="HeaderLabel"});
        if(view.Contacts.Length==0)AddChild(Text("No other civilization observed yet."));
        foreach(var contact in view.Contacts)
        {
            AddChild(Text($"{contact.CultureName} — {contact.SpeciesName}. First observed turn {contact.FirstObservedTurn}."));
            if(view.Diplomacy is not null){var id=contact.OtherEmpireId;DevelopmentUi.Button(this,"Open diplomacy with "+contact.CultureName,()=>DiplomacyRequested?.Invoke(id));}
        }
        foreach(var ship in view.ForeignVessels)
            AddChild(Text($"Foreign vessel observed at {System.Linq.Enumerable.Single(view.Stars,s=>s.Chart.Id==ship.StarId).Chart.Name} (turn {view.Turn})."));
    }
    private static Label Text(string value)=>new(){Text=value,ThemeTypeVariation="SubtleLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart};
}

