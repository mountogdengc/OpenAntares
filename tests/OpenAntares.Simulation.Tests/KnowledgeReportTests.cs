using System.Linq;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using OpenAntares.Simulation.Economy;
using Xunit;
namespace OpenAntares.Simulation.Tests;

public class KnowledgeReportTests
{
    [Fact]
    public void ReportProjectionContainsOnlyViewerOutcomesAndEvents()
    {
        var s = ExpansionFixtures.AtHome(); var c = ExpansionFixtures.Content(); var other = Fixtures.AddEmpire(s, ControllerKind.Ai); var economy = EconomyCalculator.Calculate(c, s); var report = new TurnReport(0, 1, economy) { ExpansionEvents = System.Collections.Immutable.ImmutableArray.Create(new ExpansionEvent("arrived", Fixtures.Empire, new(5), new(6)), new ExpansionEvent("arrived", other, new(999), new(6))) };
        var view = EmpireViewProjector.ProjectReport(c, s, report, Fixtures.Empire); Assert.Single(view.Colonies); Assert.All(view.Colonies, x => Assert.Equal(Fixtures.Empire, x.Economy.Empire)); Assert.Single(view.Events); Assert.Equal(Fixtures.Empire, view.Research.Empire);
    }
}
