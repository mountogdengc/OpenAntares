using OpenAntares.Simulation.Combat;
namespace OpenAntares.Simulation.ShipDesigns;
public sealed record VesselLoadout(string RoleId,DesignId? Design,string? FrameId,long BuildCost,int BaseSpeed,CombatProfile? Profile);
