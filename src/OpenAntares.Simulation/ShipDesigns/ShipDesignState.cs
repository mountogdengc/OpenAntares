using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.ShipDesigns;

public readonly record struct DesignId(int Value):IComparable<DesignId>{public int CompareTo(DesignId other)=>Value.CompareTo(other.Value);}
public readonly record struct RefitId(int Value):IComparable<RefitId>{public int CompareTo(RefitId other)=>Value.CompareTo(other.Value);}
public sealed record ShipDesign(DesignId Id,string Name,ShipRecipe Recipe,bool Archived);
public enum RefitStatus { Active,Paused }
public sealed class RefitJob
{
    public RefitId Id {get;set;}
    public VesselId Vessel {get;set;}
    public ColonyId Port {get;set;}
    public DesignId SourceDesign {get;set;}
    public DesignId TargetDesign {get;set;}
    public int AcceptedTurn {get;set;}
    public long Revision {get;set;}=1;
    public long Price {get;set;}
    public RefitStatus Status {get;set;}
    public string? Reason {get;set;}
    public RefitJob Clone()=>(RefitJob)MemberwiseClone();
}
public sealed record EquipmentFact(DesignId Id,string Name,ShipRecipe Recipe,DesignStats Stats);
public sealed record ShipDesignEvent(EmpireId Owner,long Id,int Turn,string Kind,string Reason,VesselId? Vessel,ColonyId? Port,EquipmentFact? Before,EquipmentFact? After,long Paid);
public sealed class ShipDesignEmpireState
{
    public int NextDesignId {get;set;}=1;
    public int NextRefitId {get;set;}=1;
    public long CatalogueRevision {get;set;}=1;
    public long NextEventId {get;set;}=1;
    public List<ShipDesign> Designs {get;set;}=new();
    public List<RefitJob> Refits {get;set;}=new();
    public List<ShipDesignEvent> PendingEvents {get;set;}=new();
    public List<ShipDesignEvent> RecentEvents {get;set;}=new();
    public ShipDesignEmpireState Clone()
    {
        var clone=(ShipDesignEmpireState)MemberwiseClone();
        clone.Designs=new(Designs);clone.Refits=Refits.Select(j=>j.Clone()).ToList();
        clone.PendingEvents=new(PendingEvents);clone.RecentEvents=new(RecentEvents);return clone;
    }
}
