using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;

namespace OpenAntares.Simulation.Commands;

/// <summary>
/// The single validation and application path for every command. Never mutates the state it is
/// given: changes are applied to a copy, which becomes the published state only if the whole
/// command succeeds.
/// </summary>
public static partial class CommandProcessor
{
    // Explicit test seam; no ambient hook or global mutable callback.
    internal static CommandOutcome ExecuteDiplomacy(ContentSet content,GameState state,SetReadyCommand command,Action<Diplomacy.DiplomacyResolutionPhase> hook)=>ExecuteWithHooks(content,state,command,null,hook);
    internal static CommandOutcome ExecuteShipDesigns(ContentSet content,GameState state,SetReadyCommand command,Action<ShipDesigns.ShipDesignResolutionPhase> hook)=>ExecuteWithHooks(content,state,command,hook);
    public static CommandOutcome Execute(ContentSet content, GameState state, Command command)=>ExecuteWithHooks(content,state,command,null);
    private static CommandOutcome ExecuteWithHooks(ContentSet content,GameState state,Command command,Action<ShipDesigns.ShipDesignResolutionPhase>? shipDesignHook,Action<Diplomacy.DiplomacyResolutionPhase>? diplomacyHook=null)
    {
        try{return ExecuteTransaction(content,state,command,shipDesignHook:shipDesignHook,diplomacyHook:diplomacyHook);}
        catch(OverflowException)when(Rules.HasFleets(state.RulesVersion))
        {return CommandOutcome.Reject(state,RejectionCodes.ResolutionFailed,"The order exceeds supported production or command limits.");}
        catch(InvalidOperationException)when(Rules.HasFleets(state.RulesVersion))
        {return CommandOutcome.Reject(state,RejectionCodes.ResolutionFailed,"The order could not be prepared from the current state.");}
    }
    internal static CommandOutcome ExecuteRivalDiplomacy(ContentSet content,GameState state,Diplomacy.DiplomacyCommand command)=>ExecuteTransaction(content,state,command,allowAllHumansReady:true);
    internal static CommandOutcome ExecuteRivalFleetMove(ContentSet content,GameState state,SetMoveOrderCommand command)
    {
        var issuer=state.FindEmpire(command.Issuer);var vessel=state.FindVessel(command.Vessel);
        if(!Rules.HasFleets(state.RulesVersion)||issuer?.Controller!=ControllerKind.Ai||vessel?.EmpireId!=issuer.Id||vessel.Fleet is not {} id||issuer.Fleets!.Fleets.SingleOrDefault(f=>f.Id==id) is not {} fleet||fleet.Members.Count!=1||fleet.Flagship!=vessel.Id)
            return CommandOutcome.Reject(state,"rival_fleet_unavailable","The existing rival adapter requires its own one-ship fleet.");
        return ExecuteTransaction(content,state,new Fleets.FleetCommand(command.Issuer,command.ExpectedTurn,new Fleets.FleetAction.MoveSelection(System.Collections.Immutable.ImmutableArray<VesselId>.Empty,System.Collections.Immutable.ImmutableArray.Create(id),command.Destination)),allowAllHumansReady:true);
    }
    // Only the scout coordinator can pass its already-validated finite preparation plan here.
    // Public manual movement retains the explicit detachment gate; route/consent handling is shared.
    internal static CommandOutcome ExecuteScoutPreparationMove(ContentSet content,GameState state,SetMoveOrderCommand command,Scouting.ScoutPlan validatedPlan)
    {
        if(!Rules.HasFleets(state.RulesVersion)||state.FindVessel(command.Vessel) is not {Scout:not null} vessel||vessel.EmpireId!=command.Issuer||!validatedPlan.Commands.Contains(command))
            return CommandOutcome.Reject(state,"scout_preparation_unavailable","Move is outside the validated scout preparation.");
        return ExecuteTransaction(content,state,command,scoutPreparationMove:true);
    }
    private static CommandOutcome ExecuteTransaction(ContentSet content, GameState state, Command command,bool allowAllHumansReady=false,bool scoutPreparationMove=false,Action<ShipDesigns.ShipDesignResolutionPhase>? shipDesignHook=null,Action<Diplomacy.DiplomacyResolutionPhase>? diplomacyHook=null)
    {
        var result = ExecuteCore(content, state, command,scoutPreparationMove,shipDesignHook:shipDesignHook,diplomacyHook:diplomacyHook);
        if(Rules.HasCombat(state.RulesVersion))result=Combat.CombatRepair.Override(content,state,result,command);
        if (result.Changed && Rules.HasDevelopment(state.RulesVersion)) ApplyManualIntervention(result.State, command);
        if(result.Changed&&result.State.Turn==state.Turn)Diplomacy.DiplomacyReconciliation.AfterCommand(content,result.State);
        if(result.Changed&&result.State.Turn==state.Turn&&Rules.HasScouting(state.RulesVersion))
        {
            try{if(command is not CrisisCommand)ReconcileScoutConsent(content,state,result.State,command.Issuer);}
            catch(OverflowException){return CommandOutcome.Reject(state,"ameba_encounter_stale","Encounter revision limit reached.");}
            Scouting.ScoutReports.RecordCommand(state,result.State,command);
        }
        // New commands may change an encounter's material consequences. Validate only
        // after the shared receipt reconciliation has removed the obsolete consent.
        if(result.Changed&&(command is Fleets.FleetCommand or Fleets.OfficerCommand or Fleets.AssemblyCommand or Combat.CombatActionCommand or ShipDesigns.ShipDesignCommand or ShipDesigns.RefitCommand or Diplomacy.DiplomacyCommand)&&GameStateValidator.Validate(content,result.State,allowAllHumansReady).Count!=0)
            return CommandOutcome.Reject(state,"fleet_invalid","The complete fleet or officer action could not be applied coherently.");
        return result;
    }

    private static CommandOutcome ExecuteCore(ContentSet content, GameState state, Command command,bool scoutPreparationMove=false,bool boundProduction=false,Action<ShipDesigns.ShipDesignResolutionPhase>? shipDesignHook=null,Action<Diplomacy.DiplomacyResolutionPhase>? diplomacyHook=null)
    {
        EmpireState? issuer = state.FindEmpire(command.Issuer);
        if (issuer is null)
        {
            return CommandOutcome.Reject(state, RejectionCodes.UnknownEmpire, $"Empire {command.Issuer} does not exist.");
        }

        if (command.ExpectedTurn != state.Turn)
        {
            return CommandOutcome.Reject(state, RejectionCodes.StaleTurn,
                $"Command was issued for turn {command.ExpectedTurn}, but the current turn is {state.Turn}.");
        }

        var planningView=Rules.HasCombat(state.RulesVersion)?Knowledge.EmpireViewProjector.Project(content,state,command.Issuer):null;
        if(Rules.HasShipDesigns(state.RulesVersion)&&ShipDesigns.RefitGuards.Conflicts(planningView!,command))
            return CommandOutcome.Reject(state,"refit_locked","Cancel or complete the refit before replacing its project or changing the affected ship, fleet or officer.");
        if(Rules.HasCombat(state.RulesVersion)&&Combat.CombatCommandGuards.Conflicts(planningView!,command))
            return CommandOutcome.Reject(state,Combat.CombatRejectionCodes.Unavailable,"Committed combat members require combat orders or completed withdrawal.");
        return command switch
        {
            Diplomacy.DiplomacyCommand c => Diplomacy.DiplomacyCommandProcessor.Execute(content,state,c),
            ShipDesigns.RefitCommand c => ShipDesigns.RefitCommandProcessor.Execute(content,state,c),
            ShipDesigns.ShipDesignCommand c => ShipDesigns.ShipDesignCommandProcessor.Execute(content,state,c),
            Combat.CombatActionCommand c => Combat.CombatCommandProcessor.Execute(content,state,c),
            Fleets.FleetCommand c => Fleets.FleetCommandProcessor.Execute(content,state,c),
            Fleets.OfficerCommand c => Fleets.OfficerCommandProcessor.Execute(content,state,c),
            Fleets.AssemblyCommand c => Fleets.AssemblyCommandProcessor.Execute(content,state,c),
            ScoutCommand c => ExecuteScout(content,state,issuer,c),
            CrisisCommand c => ExecuteCrisis(content,state,issuer,c),
            DevelopmentCommand c => ExecuteDevelopment(content, state, issuer, c),
            SetProductionTargetCommand c => SetProductionTarget(content, state, issuer, c,allowArchived:boundProduction),
            SetFoundingOrderCommand c => SetFoundingOrder(content, state, c),
            SetMoveOrderCommand c => SetMoveOrder(content, state, c,group:scoutPreparationMove),
            SetWorkforceCommand c => SetWorkforce(state, c),
            SetProjectCommand c => SetProject(content, state, issuer, c),
            SetResearchTargetCommand c => SetResearchTarget(content, state, issuer, c),
            SetReadyCommand c => SetReady(content, state, issuer, c,shipDesignHook,diplomacyHook),
            _ => throw new ArgumentException($"Unsupported command type {command.GetType().Name}.", nameof(command)),
        };
    }

    private static CommandOutcome SetWorkforce(GameState state, SetWorkforceCommand command)
    {
        if (!TryGetOwnedColony(state, command.Issuer, command.Colony, out ColonyState? colony, out CommandOutcome? rejection))
        {
            return rejection;
        }

        Workforce workforce = command.Workforce;
        if (workforce.HasNegativeCount)
        {
            return CommandOutcome.Reject(state, RejectionCodes.NegativeWorkforce, "Workforce counts must not be negative.");
        }

        long total = (long)workforce.Support + workforce.Production + workforce.Research;
        if (total != colony.Population)
        {
            return CommandOutcome.Reject(state, RejectionCodes.WorkforceSumMismatch,
                $"Workforce assigns {total} units, but {command.Colony} has population {colony.Population}.");
        }

        if (workforce == colony.Workforce)
        {
            return CommandOutcome.NoChange(state);
        }

        return ApplyPlanningChange(state, command.Issuer, candidate => candidate.FindColony(command.Colony)!.Workforce = workforce);
    }

    private static CommandOutcome SetProject(ContentSet content, GameState state, EmpireState issuer, SetProjectCommand command)
    {
        if (!TryGetOwnedColony(state, command.Issuer, command.Colony, out ColonyState? colony, out CommandOutcome? rejection))
        {
            return rejection;
        }

        string? buildingId = command.BuildingId;
        if (buildingId is not null)
        {
            if (!content.Buildings.TryGetValue(buildingId, out BuildingDefinition? building))
            {
                return CommandOutcome.Reject(state, RejectionCodes.UnknownBuilding, $"Building '{buildingId}' is not defined.");
            }

            if (colony.CompletedBuildingIds.Contains(buildingId))
            {
                return CommandOutcome.Reject(state, RejectionCodes.AlreadyCompleted,
                    $"{command.Colony} has already completed '{buildingId}'.");
            }

            if (!Rules.PrerequisitesKnown(issuer, building.PrerequisiteTechnologyIds))
            {
                return CommandOutcome.Reject(state, RejectionCodes.PrerequisiteNotMet,
                    $"'{buildingId}' requires technologies the empire does not know.");
            }
        }

        if (buildingId == colony.ProjectId && colony.ProjectKind == ProductionKinds.Building)
        {
            return CommandOutcome.NoChange(state);
        }

        return ApplyPlanningChange(state, command.Issuer, candidate => { var target = candidate.FindColony(command.Colony)!; target.ProjectId = buildingId; target.ProjectKind = ProductionKinds.Building; });
    }

    private static CommandOutcome SetResearchTarget(ContentSet content, GameState state, EmpireState issuer, SetResearchTargetCommand command)
    {
        string? technologyId = command.TechnologyId;
        if (technologyId is not null)
        {
            if (!Rules.TechnologyAvailable(content, state.RulesVersion, technologyId) || !content.Technologies.TryGetValue(technologyId, out TechnologyDefinition? technology))
            {
                return CommandOutcome.Reject(state, RejectionCodes.UnknownTechnology, $"Technology '{technologyId}' is not defined.");
            }

            if (issuer.KnownTechnologyIds.Contains(technologyId))
            {
                return CommandOutcome.Reject(state, RejectionCodes.AlreadyKnown, $"The empire already knows '{technologyId}'.");
            }

            if (!Rules.PrerequisitesKnown(issuer, technology.PrerequisiteTechnologyIds))
            {
                return CommandOutcome.Reject(state, RejectionCodes.PrerequisiteNotMet,
                    $"'{technologyId}' requires technologies the empire does not know.");
            }
        }

        if (technologyId == issuer.ResearchTargetId)
        {
            return CommandOutcome.NoChange(state);
        }

        return ApplyPlanningChange(state, command.Issuer, candidate => candidate.FindEmpire(command.Issuer)!.ResearchTargetId = technologyId);
    }

    private static CommandOutcome SetReady(ContentSet content, GameState state, EmpireState issuer, SetReadyCommand command,Action<ShipDesigns.ShipDesignResolutionPhase>? shipDesignHook=null,Action<Diplomacy.DiplomacyResolutionPhase>? diplomacyHook=null)
    {
        if (issuer.Ready == command.Ready)
        {
            return CommandOutcome.NoChange(state);
        }

        if(command.Ready&&issuer.Controller==ControllerKind.Human&&Rules.HasCombat(state.RulesVersion))
        {
            var blockers=Combat.CombatReadyQueries.ValidateResponse(content,state,issuer.Id);
            if(!blockers.IsEmpty)return CommandOutcome.Reject(state,blockers[0],"Choose the displayed combat response or a legal return destination before Ready.");
        }
        if(command.Ready&&issuer.Controller==ControllerKind.Human&&Rules.HasScouting(state.RulesVersion))
        {
            if(!Scouting.ScoutReadyQueries.HasConsent(content,state,issuer.Id))return CommandOutcome.Reject(state,"ameba_acknowledgement_required","Review and acknowledge the current ameba responses before Ready.");
            if(Scouting.ScoutQueries.PreviewPreparation(content,state,issuer.Id).Blockers.Length>0)return CommandOutcome.Reject(state,"ameba_response_required","Choose an actual legal departure or a displayed risk response before Ready.");
        }
        if(command.Ready&&issuer.Controller==ControllerKind.Human&&Rules.HasCrises(state.RulesVersion)&&issuer.Crisis!.RequireAcknowledgement&&!Crises.CrisisQueries.PendingAttention(state,issuer.Id).IsEmpty)
            return CommandOutcome.Reject(state,"crisis_acknowledgement_required","Review and acknowledge this empire's current crisis facts before Ready, or explicitly change the saved acknowledgement requirement.");
        GameState prepared = state;
        if(command.Ready&&issuer.Controller==ControllerKind.Human&&Rules.HasFleets(state.RulesVersion))
        {
            if(Rules.HasCombat(state.RulesVersion))
            {
                var combat=Combat.CombatTurnCoordinator.PrepareHuman(content,state,issuer.Id);if(!combat.Succeeded)return CommandOutcome.Reject(state,RejectionCodes.ResolutionFailed,"Combat, fleet and survival preparation could not be completed. Review current responses.");prepared=combat.State;
            }
            else
            {
                var fleet=Fleets.FleetTurnCoordinator.PrepareHuman(content,state,issuer.Id);
                if(!fleet.Succeeded)return CommandOutcome.Reject(state,RejectionCodes.ResolutionFailed,"Fleet and survival preparation could not be completed. Review current responses.");prepared=fleet.State;
            }
        }
        if(command.Ready&&issuer.Controller==ControllerKind.Human&&Rules.HasCrises(state.RulesVersion)&&!Rules.HasFleets(state.RulesVersion))
        {
            var crisis=Crises.CrisisTurnCoordinator.Prepare(content,state,issuer.Id);
            if(!crisis.Succeeded)return CommandOutcome.Reject(state,RejectionCodes.ResolutionFailed,"The turn could not be completed.");
            prepared=crisis.State;
        }
        if (command.Ready && issuer.Controller == ControllerKind.Human && Rules.HasDevelopment(state.RulesVersion)&&!Rules.HasFleets(state.RulesVersion))
        {
            var preparation = Development.GovernorTurnCoordinator.Prepare(content, prepared, issuer.Id);
            if (!preparation.Succeeded) return CommandOutcome.Reject(state, RejectionCodes.ResolutionFailed, "The turn could not be completed.");
            prepared = preparation.State;
        }
        if(command.Ready&&issuer.Controller==ControllerKind.Human&&Rules.HasScouting(state.RulesVersion)&&!Rules.HasFleets(state.RulesVersion))
        {
            var scouting=Scouting.ScoutTurnCoordinator.Prepare(content,prepared,issuer.Id);
            if(!scouting.Succeeded)return CommandOutcome.Reject(state,RejectionCodes.ResolutionFailed,"Scout survival preparation could not be completed. Review current legal responses.");
            prepared=scouting.State;
        }
        GameState candidate = prepared.Clone();
        candidate.FindEmpire(command.Issuer)!.Ready = command.Ready;

        bool allHumansReady = candidate.Empires
            .Where(empire => empire.Controller == ControllerKind.Human)
            .All(empire => empire.Ready);
        if (!allHumansReady)
        {
            return new CommandOutcome(candidate, Rejection: null, Changed: true, Report: null);
        }

        // The readiness change and the resolution it triggers succeed or fail together.
        TurnResolution resolution = diplomacyHook is not null?TurnResolver.ResolveDiplomacy(content,candidate,diplomacyHook):shipDesignHook is null?TurnResolver.Resolve(content, candidate):TurnResolver.ResolveShipDesigns(content,candidate,shipDesignHook);
        if (!resolution.Succeeded)
        {
            return CommandOutcome.Reject(state, RejectionCodes.ResolutionFailed,
                "Turn resolution failed: " + string.Join("; ", resolution.Errors));
        }

        return new CommandOutcome(resolution.State, Rejection: null, Changed: true, resolution.Report);
    }

    /// <summary>Applies a planning change to a copy of the state and clears the issuer's readiness.</summary>
    private static CommandOutcome ApplyPlanningChange(GameState state, EmpireId issuer, Action<GameState> change)
    {
        GameState candidate = state.Clone();
        change(candidate);
        candidate.FindEmpire(issuer)!.Ready = false;
        return new CommandOutcome(candidate, Rejection: null, Changed: true, Report: null);
    }

    private static bool TryGetOwnedColony(
        GameState state,
        EmpireId issuer,
        ColonyId colonyId,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ColonyState? colony,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out CommandOutcome? rejection)
    {
        colony = state.FindColony(colonyId);
        if (colony is null)
        {
            rejection = CommandOutcome.Reject(state, RejectionCodes.UnknownColony, $"Colony {colonyId} does not exist.");
            return false;
        }

        if (colony.EmpireId != issuer)
        {
            rejection = CommandOutcome.Reject(state, RejectionCodes.NotOwner, $"{issuer} does not own {colonyId}.");
            colony = null;
            return false;
        }

        rejection = null;
        return true;
    }
}
