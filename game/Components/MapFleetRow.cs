using System;
using Godot;
using OpenAntares.Simulation.Fleets;

namespace OpenAntares.Game.Components;

public partial class MapFleetRow : Button
{
    public FleetId? Fleet { get; set; }
    public int SystemId { get; set; }
    public bool AdditiveSelection { get; private set; }
    public Func<FleetId,FleetId,string?>? MergeRejection { get; set; }
    public event Action<Vector2>? ContextRequested;
    public event Action<FleetId,FleetId>? MergeRequested;
    public override void _GuiInput(InputEvent input)
    {
        if(input is InputEventMouseButton{ButtonIndex:MouseButton.Left} selection)AdditiveSelection=selection.ShiftPressed;
        else if(input is InputEventKey key)AdditiveSelection=key.ShiftPressed;
        if(input is InputEventMouseButton{Pressed:true,ButtonIndex:MouseButton.Right} click)
        { ContextRequested?.Invoke(GetGlobalRect().Position+click.Position); AcceptEvent(); }
    }
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if(Fleet is not {} fleet)return default;
        if(Input.IsMouseButtonPressed(MouseButton.Left))SetDragPreview(new Label{Text=Text,ThemeTypeVariation="ConsoleHeading"});
        return new Godot.Collections.Dictionary{["kind"]="map_fleet",["fleet"]=fleet.Value,["system"]=SystemId};
    }
    public override bool _CanDropData(Vector2 atPosition,Variant data)
    {
        if(Fleet is not {} target||data.VariantType!=Variant.Type.Dictionary)return false;
        var values=data.AsGodotDictionary();
        if(!values.TryGetValue("kind",out var kind)||kind.AsString()!="map_fleet"||!values.TryGetValue("fleet",out var source)||source.VariantType!=Variant.Type.Int||!values.TryGetValue("system",out var system)||system.AsInt32()!=SystemId||source.AsInt32()==target.Value)return false;
        return MergeRejection?.Invoke(new(source.AsInt32()),target) is null;
    }
    public override void _DropData(Vector2 atPosition,Variant data)
    {
        if(_CanDropData(atPosition,data)&&Fleet is {} target)MergeRequested?.Invoke(new(data.AsGodotDictionary()["fleet"].AsInt32()),target);
    }
}
