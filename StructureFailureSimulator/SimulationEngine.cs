using System;

namespace StructureFailureSimulator
{
    public class SimulationEngine
    {
        public Structure Structure { get; private set; }
        public RunContext Run { get; private set; }

        private FEMSolver _solver = new();

        public void Initialize(Structure structure, RunContext run)
        {
            Structure = structure;
            Run = run;
        }

        public void Step()
        {
            if (Structure == null || Run == null)
                return;

            Run.Advance();

            ApplyEnvironmentLoads();

            // 🔥 THIS IS THE KEY FIX
            _solver.Solve(Structure);

            ApplyFailureModel();
        }

        private void ApplyEnvironmentLoads()
        {
            foreach (var n in Structure.Nodes)
            {
                float wind = (float)Run.WindStrength;
                float seismic = (float)Run.SeismicPulse;

                n.Uy += wind * 0.01;
                n.Ux += seismic * 0.005;
            }
        }

        private void ApplyFailureModel()
        {
            foreach (var m in Structure.Members)
            {
                if (m.Failed)
                    continue;

                if (m.AxialForce > m.YieldStrength)
                {
                    m.Failed = true;

                    // cascade instability
                    m.A.Uy += 1;
                    m.B.Uy += 1;
                }
            }
        }
    }
}