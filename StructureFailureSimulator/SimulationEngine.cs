using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StructureFailureSimulator
{
    public class SimulationEngine
    {
        public Structure Structure { get; private set; }
        private readonly Random _rng = new Random();

        public void Initialize(Structure structure)
        {
            Structure = structure;
        }

        public void Step()
        {
            if (Structure == null) return;

            ApplyRandomLoad();
            PropagateLoads();
            EvaluateFailure();
        }

        private void ApplyRandomLoad()
        {
            // apply load to top nodes (simple heuristic)
            foreach (var node in Structure.Nodes)
            {
                node.AppliedLoad *= 0.95f; // damping

                if (_rng.NextDouble() < 0.2)
                    node.AppliedLoad += (float)_rng.Next(5, 20);
            }
        }

        private void PropagateLoads()
        {
            foreach (var m in Structure.Members)
            {
                if (m.Failed) continue;

                float loadA = m.A.AppliedLoad;
                float loadB = m.B.AppliedLoad;

                m.CurrentLoad = (loadA + loadB) * 0.5f;

                // distribute back into structure
                m.A.AppliedLoad += m.CurrentLoad * 0.02f;
                m.B.AppliedLoad += m.CurrentLoad * 0.02f;
            }
        }

        private void EvaluateFailure()
        {
            foreach (var m in Structure.Members)
            {
                if (m.Failed) continue;

                if (m.CurrentLoad > m.Capacity)
                {
                    m.Failed = true;

                    // failure cascade
                    m.A.AppliedLoad += 10;
                    m.B.AppliedLoad += 10;
                }
            }
        }
    }
}