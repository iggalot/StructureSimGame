using System;

namespace StructureFailureSimulator
{
    public class GameManager
    {
        public GameState State { get; private set; } = GameState.Build;

        public RunContext Run { get; private set; }
        public Structure Structure { get; private set; }

        public SimulationEngine Engine { get; private set; }

        public void StartNewRun(int seed)
        {
            Run = new RunContext(seed);
            Structure = new Structure();
            Engine = new SimulationEngine();

            State = GameState.Build;
        }

        public void BeginSimulation()
        {
            Engine.Initialize(Structure, Run);
            State = GameState.Simulate;
        }

        public void EndSimulation()
        {
            State = GameState.Results;
        }

        public void Upgrade()
        {
            State = GameState.Upgrade;
        }

        public void ReturnToBuild()
        {
            Structure = new Structure();
            State = GameState.Build;
        }
    }
}