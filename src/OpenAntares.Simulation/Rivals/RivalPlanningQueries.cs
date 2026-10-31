using System;
using System.Linq;
using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.Species;

namespace OpenAntares.Simulation.Rivals;

public static class RivalPlanningQueries
{
    public static IRivalPlanningContext Create(ContentSet content, GameState working, EmpireId owner)
    {
        if (!Rules.HasRivals(working.RulesVersion) || content.RivalAi is null)
            throw new ArgumentException("Rival planning requires explicit rival rules and policy content.");
        var view = EmpireViewProjector.Project(content, working, owner);
        return new Context(content, view with { Galaxy = view.Galaxy with { Seed = null } },Rules.HasScouting(working.RulesVersion)?Scouting.ScoutQueries.PreviewPreparation(content,working,owner):null);
    }

    private sealed record Context(ContentSet Content, EmpireView View,Scouting.ScoutPreparationPreview? ScoutPreparation) : IRivalPlanningContext
    {
        // This frame is constructed solely from permitted facts, never cloned from the world.
        private readonly GameState _frame = OwnFrame(View);
        public RivalAiDefinition Policy => Content.RivalAi!;
        public ColonyEconomy? Forecast(ColonyId colony, Workforce workforce, ProductionTarget? target, string? completedBuildingId = null)
        {
            var source = _frame.FindColony(colony);
            if (source is null || workforce.HasNegativeCount || (long)workforce.Support + workforce.Production + workforce.Research != source.Population) return null;
            var frame = _frame.Clone(); var proposed = frame.FindColony(colony)!;
            if (completedBuildingId is { } addition)
            {
                if (!LegalBuilding(proposed, addition)) return null;
                proposed.CompletedBuildingIds.Add(addition); proposed.CompletedBuildingIds.Sort(StringComparer.Ordinal);
            }
            if (target is { Kind: ProductionKinds.Building } building && !LegalBuilding(proposed, building.ContentId)) return null;
            if (target is { Kind: ProductionKinds.Vessel } vessel && !SpeciesRules.CanBuildVessel(Content, frame, View.Viewer, vessel.ContentId)) return null;
            if (target is not null && target.Kind is not (ProductionKinds.Building or ProductionKinds.Vessel)) return null;
            proposed.Workforce = workforce; proposed.ProjectKind = target?.Kind ?? ProductionKinds.Building; proposed.ProjectId = target?.ContentId;
            try { return EconomyCalculator.CalculateColony(Content, frame, proposed); }
            catch (OverflowException) { return null; }
        }

        private bool LegalBuilding(ColonyState colony, string id) => Content.Buildings.TryGetValue(id, out var building)
            && !colony.CompletedBuildingIds.Contains(id) && Rules.PrerequisitesKnown(_frame.Empires[0], building.PrerequisiteTechnologyIds);

        public FoundingPreview? PreviewSettlement(PlanetId target, string roleId)
        {
            if(View.Stars.Any(s=>View.SettlementRestrictedSystems.Contains(s.Chart.Id)&&s.Planets is {} planets&&planets.Any(p=>p.Id==target)))return null;
            var known = _frame.Empires[0].Surveys.SelectMany(o => o.Planets).FirstOrDefault(p => p.PlanetId == target);
            if (known is null || known.OwnerId is not null || !Content.Vessels.TryGetValue(roleId, out var role) || !role.CanFoundColony
                || !SpeciesRules.CanBuildVessel(Content, _frame, View.Viewer, roleId)) return null;
            var frame = _frame.Clone();
            // A private calculator input, not an allocated or published entity. No source or global ID is read.
            frame.Vessels = new() { new VesselState { Id = new(1), EmpireId = View.Viewer, RoleId = roleId,
                CarriedPopulation = role.EmbarkPopulation, Crisis=Rules.HasCrises(frame.RulesVersion)?new():null, StationaryStarId = frame.FindPlanet(target)!.StarId } };
            try
            {
                var preview = FoundingRules.Preview(Content, frame, View.Viewer, new(1), target);
                return preview.RejectionCode is null ? preview : null;
            }
            catch (OverflowException) { return null; }
        }

        public int? TravelTurns(StarId origin, StarId destination, string roleId)
        {
            if (!View.AvailableVesselRoleIds.Contains(roleId) || !Content.Vessels.TryGetValue(roleId, out var role) || role.Speed <= 0
                || _frame.FindStar(origin) is not { } from || _frame.FindStar(destination) is not { } to) return null;
            if(role.EmbarkPopulation>0&&(Crises.CrisisQueries.PortClosed(_frame,View.Viewer,origin)||Crises.CrisisQueries.PortClosed(_frame,View.Viewer,destination)))return null;
            try { return origin == destination ? 0 : TravelCalculator.Duration(from.X, from.Y, to.X, to.Y, role.Speed); }
            catch (Exception error) when (error is OverflowException or ArgumentOutOfRangeException) { return null; }
        }

        private static GameState OwnFrame(EmpireView view)
        {
            var owner = new EmpireState { Id = view.Viewer, SpeciesId = view.SpeciesId, Controller = ControllerKind.Ai,
                ResearchReserve = view.Research.Reserve, ResearchTargetId = view.Research.TargetId,
                KnownTechnologyIds = view.Research.KnownTechnologyIds.ToList() };
            if(view.Scouting is {} scouting)owner.Scouting=new(){KnownHabitats=scouting.Habitats.Select(h=>new Scouting.KnownAmeba(h.HabitatId,h.Star,h.ObservedTurn)).ToList()};
            if(view.Combat is {} combat)owner.Combat=new(){Observations=combat.Pockets.Select(p=>new Combat.PocketObservation(p.Id,p.Star,p.ObservedTurn,p.Cleared,p.Sentries)).ToList()};
            foreach (var star in view.Stars.Where(s => s.Planets.HasValue))
                owner.Surveys.Add(new SystemObservation { StarId = star.Chart.Id, LastObservedTurn = star.LastObservedTurn!.Value,
                    Planets = star.Planets!.Value.Select(p => new PlanetObservation(p.Id,p.Orbit,p.PlanetTypeId,p.ColonyId,p.OwnerId) { EnvironmentId = p.EnvironmentId }).ToList() });
            var frame=new GameState
            {
                RulesVersion = view.RulesVersion, Turn = view.Turn, Random = new Pcg32 { State = 0, Increment = 1 },
                Empires = new() { owner },
                Stars = view.Galaxy.Stars.Select(s => new StarState { Id=s.Id,Name=s.Name,X=s.X,Y=s.Y,RegionId=s.RegionId }).ToList(),
                Planets = owner.Surveys.SelectMany(o => o.Planets.Select(p => new PlanetState { Id=p.PlanetId,StarId=o.StarId,
                    Orbit=p.Orbit,PlanetTypeId=p.PlanetTypeId,EnvironmentId=p.EnvironmentId })).OrderBy(p => p.Id).ToList(),
                Colonies = view.Colonies.Select(c => new ColonyState { Id=c.Id,EmpireId=view.Viewer,PlanetId=c.PlanetId,Population=c.Population,
                    Workforce=c.Workforce,GrowthProgress=c.GrowthProgress,ProductionReserve=c.SpendableProduction,ProjectId=c.ProjectId,ProjectKind=c.ProjectKind,
                    HabitatMode=c.HabitatMode,CompletedBuildingIds=c.CompletedBuildingIds.ToList() }).ToList()
            };
            Crises.CrisisForecastFrame.Apply(frame,view);
            Diplomacy.DiplomacyForecastFrame.Apply(frame,view);
            Conflict.ConflictForecastFrame.Apply(frame,view);
            return frame;
        }
    }
}
