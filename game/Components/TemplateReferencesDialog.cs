using System.Linq;
using Godot;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

public partial class TemplateReferencesDialog : AcceptDialog
{
    private VBoxContainer _body = null!;

    public override void _Ready()
    {
        Name = "TemplateReferencesDialog";
        Title = "Template in use";
        OkButtonText = "Close";
        _body = DevelopmentUi.DialogBody(this);
    }

    public void ShowReferences(EmpireView view, GovernorTemplateReferences references)
    {
        DevelopmentUi.Clear(_body);
        _body.AddChild(DevelopmentUi.Text("Reassign these references before deleting the template. Existing colony plans have been retained."));
        var owned = view.Colonies.Select(c => c.Id).ToHashSet();
        if (!references.Colonies.IsDefaultOrEmpty)
        {
            foreach (var id in references.Colonies.Where(owned.Contains).Distinct().OrderBy(id => id))
            {
                string name = view.Research.ColonyNames.TryGetValue(id.ToString(), out var known) ? known : "Unnamed colony";
                var label = DevelopmentUi.Text($"{name} ({id})");
                label.Name = "TemplateReference_" + id.Value;
                _body.AddChild(label);
            }
        }
        if (references.NewColonyInheritance)
            _body.AddChild(DevelopmentUi.Text("New-colony inheritance references this template. Choose another inheritance template or remove the inheritance assignment."));
        PopupCenteredClamped(new(650, 500));
    }
}
