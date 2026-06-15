using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StructureFailureSimulator
{
    public class SimulationEngine
    {
        public Structure Structure { get; private set; }
        public RunContext Run { get; private set; }

        private readonly FEMSolver _solver = new FEMSolver();

        // =========================
        // TEMP LOAD STORAGE (PER STEP)
        // =========================
        private readonly Dictionary<Node, (float ux, float uy)> _nodeLoads
            = new Dictionary<Node, (float ux, float uy)>();

        public void Initialize(Structure structure, RunContext run)
        {
            Structure = structure;
            Run = run;
        }

        // =========================
        // MAIN STEP
        // =========================
        public void Step()
        {
            if (Structure == null || Run == null)
                return;

            Run.Advance();

            ApplyEnvironmentLoads(); // optional influence via node properties

            _solver.Solve(Structure);

            ApplyFailureModel();
        }

        // =========================
        // LOADS (NO STRUCTURE MUTATION)
        // =========================
        private void ApplyEnvironmentLoads()
        {
            _nodeLoads.Clear();

            float wind = (float)Run.WindStrength;
            float seismic = (float)Run.SeismicPulse;

            for (int i = 0; i < Structure.Nodes.Count; i++)
            {
                var n = Structure.Nodes[i];

                _nodeLoads[n] = (
                    ux: seismic * 0.005f,
                    uy: wind * 0.01f
                );
            }
        }

        // =========================
        // FAILURE MODEL (POST-SOLVE ONLY)
        // =========================
        private void ApplyFailureModel()
        {
            foreach (var m in Structure.Members)
            {
                if (m.Failed)
                    continue;

                float force = (float)Math.Abs(m.AxialForce);

                if (force > m.YieldStrength)
                {
                    m.Failed = true;

                    // mild damping to avoid energy injection spikes
                    m.A.Ux *= 0.95f;
                    m.A.Uy *= 0.95f;
                    m.B.Ux *= 0.95f;
                    m.B.Uy *= 0.95f;
                }
            }
        }
    }
}