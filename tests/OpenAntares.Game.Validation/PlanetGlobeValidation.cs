using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace OpenAntares.Game.Validation;

public partial class PlanetGlobeValidation : Control
{
    private readonly List<string> _checks = new();
    private string _output = "";
    private string _case = "";
    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        _checks.Add(name);
    }
    private async Task Frames(int count = 4)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    public override async void _Ready()
    {
        string? failure = null;
        try
        {
            var args = OS.GetCmdlineUserArgs();
            _output = args.Single(a => a.StartsWith("--validation-output="))[20..];
            _case = args.Single(a => a.StartsWith("--globe-case="))[13..];
            Check(DisplayServer.GetName() != "headless", "Native rendered display");
            await Frames();
            await RunCase();
        }
        catch (Exception error) { failure = error.ToString(); GD.PrintErr(failure); }
        finally
        {
            File.WriteAllText(Path.Combine(_output, "results.json"), JsonSerializer.Serialize(new {
                @case = _case, passed = failure is null, error = failure, checks = _checks,
                renderer = RenderingServer.GetCurrentRenderingMethod().ToString(), adapter = RenderingServer.GetVideoAdapterName(),
                method = "Native rendered controls; physical readability and appearance require owner observation"
            }, new JsonSerializerOptions { WriteIndented = true }));
            GetTree().Quit(failure is null ? 0 : 1);
        }
    }
}
