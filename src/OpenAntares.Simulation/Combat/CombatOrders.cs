using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public enum CombatDoctrine { Cautious, Press, HitAndRun, Hold }
public enum StragglerPolicy { KeepTogether, DetachCrippled }
public enum CombatArrivalOrder { Repair, Hold }
public sealed record CombatOrders(CombatDoctrine Doctrine, ColonyId ReturnColony, StragglerPolicy Stragglers, CombatArrivalOrder OnArrival);
