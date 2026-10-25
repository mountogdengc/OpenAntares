using System;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Commands;

namespace OpenAntares.Simulation.Crises;

public interface ICrisisPlanningContext
{
    ContentSet Content { get; }
    CrisisView View { get; }
    int Turn { get; }
    EmpireId Owner { get; }
}
public sealed record CrisisPlanningResult(bool Succeeded,int CommandCount);
public delegate CrisisPlanningResult CrisisPlanningPolicy(Func<ICrisisPlanningContext> current,Func<Command,bool> submit,bool ai);
public static class CrisisPlanningQueries
{
    public static ICrisisPlanningContext Create(ContentSet content,GameState state,EmpireId owner)=>new Context(content,CrisisQueries.Project(content,state,owner));
    private sealed record Context(ContentSet Content,CrisisView View):ICrisisPlanningContext
    {
        public int Turn=>View.Turn;
        public EmpireId Owner=>View.Viewer;
    }
}
