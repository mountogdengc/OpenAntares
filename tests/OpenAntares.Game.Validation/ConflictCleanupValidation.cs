using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Galaxy;
using static OpenAntares.Game.Validation.ScoutUiControlChecks;
namespace OpenAntares.Game.Validation;
public partial class ConflictCleanupValidation : Node
{
    public override async void _Ready()
    {
        int exit = 0;
        try
        {
            foreach (int rules in new[] { 8, 9, 11, 12 })
            {
                var content = CompleteContent();
                var made = GalaxyGenerator.CreateNewGame(content, new("small", 42, rules, "harl", CrisesEnabled:false, AmebasEnabled:false, SentriesEnabled:rules >= 9 ? false : null));
                if (!made.Succeeded) throw new InvalidOperationException(made.Error);
                var screen = new GameScreen(new GameSession(content, made.State!), made.State!.Empires[0].Id);
                AddChild(screen);
                await Frames(this);
                try
                {
                    if (!Descendants<Label>(screen).Any(label => label.Name == "ConflictAttentionBadge"))
                        throw new InvalidOperationException($"Rules{rules}: conflict badge has no screen owner and will leak.");
                }
                finally { RemoveChild(screen); screen.QueueFree(); await Frames(this); }
            }
            GD.Print("PASS: Conflict badge has screen ownership for rules8/9/11/12.");
        }
        catch (Exception error) { GD.PrintErr(error); exit = 1; }
        GC.Collect(); GC.WaitForPendingFinalizers(); await Frames(this);
        GetTree().Quit(exit);
    }
}
