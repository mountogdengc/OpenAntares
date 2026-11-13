using System;
using Godot;

namespace OpenAntares.Game.Presentation;

/// <summary>One presentation clock. Integer cycles keep every layer continuous at wrap.</summary>
public partial class PlanetAnimationClock : Node
{
    public const double PeriodSeconds = 4096;
    private double _seconds;
    public double ClockTurn => _seconds / PeriodSeconds;
    public bool IsStatic { get; private set; }
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        RenderingServer.GlobalShaderParameterSet("planet_clock_turn", (float)ClockTurn);
    }
    public void SetStatic(bool value) => IsStatic = value;
    public void Advance(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!IsStatic) _seconds = (_seconds + delta % PeriodSeconds) % PeriodSeconds;
    }
    public override void _Process(double delta)
    {
        if (IsStatic) return;
        Advance(delta);
        RenderingServer.GlobalShaderParameterSet("planet_clock_turn", (float)ClockTurn);
    }
}
