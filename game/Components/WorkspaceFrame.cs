using Godot;

namespace OpenAntares.Game.Components;

/// <summary>Fits existing window controllers to the routed workspace; no duplicate or proxy controls.</summary>
public partial class WorkspaceFrame : Control
{
    private Window? _window;
    public WorkspaceFrame() { Name = "WorkspaceFrame"; SizeFlagsVertical = SizeFlags.ExpandFill; MouseFilter = MouseFilterEnum.Ignore; }
    public override void _Ready() { Resized += Fit; }
    public void Open(Window window)
    {
        _window = window;
        window.Exclusive = false;
        window.Borderless = true;
        window.WrapControls = false;
        window.MinSize = Vector2I.Zero;
        Fit();
        Callable.From(Fit).CallDeferred();
    }
    public void Close() { _window = null; }
    private void Fit()
    {
        if (_window is null || !_window.Visible || !IsInsideTree()) return;
        Rect2 rect = GetGlobalRect();
        _window.Position = new Vector2I((int)rect.Position.X, (int)rect.Position.Y);
        _window.Size = new Vector2I(System.Math.Max(1, (int)rect.Size.X), System.Math.Max(1, (int)rect.Size.Y));
    }
}
