using System;
using System.Linq;
using System.Numerics;

namespace StructureFailureSimulator
{
    public class SimulationEngine
    {
        public Structure Structure { get; private set; }
        public RunContext Run { get; private set; }

        public void Initialize(Structure structure, RunContext run)
        {
            Structure = structure;
            Run = run;
        }

        public void Step()
        {
            Run.Advance();

            ApplyWindLoad();
            ApplySeismicLoad();

            SolveLoadDistribution();
            ApplySupports();

            EvaluateFailure();
        }

        #region LOADS

        private void ApplyWindLoad()
        {
            // directional wind (left to right bias)
            Vector2 windDir = new Vector2(1, 0);

            foreach (var n in Structure.Nodes)
            {
                float heightFactor = 1f + (n.Position.Y * 0.002f);
                n.AppliedLoad += Run.WindStrength * heightFactor;
            }
        }

        private void ApplySeismicLoad()
        {
            // global shaking pulse
            foreach (var n in Structure.Nodes)
            {
                n.AppliedLoad += Run.SeismicPulse * 0.5f;
            }
        }

        #endregion

        #region SOLVER (LIGHTWEIGHT STIFFNESS APPROX)

        private void SolveLoadDistribution()
        {
            foreach (var m in Structure.Members)
            {
                if (m.Failed) continue;

                float a = m.A.AppliedLoad;
                float b = m.B.AppliedLoad;

                // stiffness-weighted transfer
                float avg = (a + b) * 0.5f;

                float stiffnessFactor = 1f / (1f + m.Length * 0.01f);

                m.CurrentLoad = avg * stiffnessFactor;

                // propagate
                m.A.AppliedLoad += m.CurrentLoad * 0.01f;
                m.B.AppliedLoad += m.CurrentLoad * 0.01f;
            }
        }

        #endregion

        #region SUPPORTS

        private void ApplySupports()
        {
            foreach (var n in Structure.Nodes)
            {
                if (n.IsFixedSupport)
                {
                    // fixed base absorbs load
                    n.ReactionForce += n.AppliedLoad;
                    n.AppliedLoad *= 0.1f;
                }
                else if (n.IsPinnedSupport)
                {
                    // partial reduction
                    n.ReactionForce += n.AppliedLoad * 0.5f;
                    n.AppliedLoad *= 0.5f;
                }
            }
        }

        #endregion

        #region FAILURE

        private void EvaluateFailure()
        {
            foreach (var m in Structure.Members)
            {
                if (m.Failed) continue;

                if (m.CurrentLoad > m.Capacity)
                {
                    m.Failed = true;

                    // cascade amplification
                    m.A.AppliedLoad += 15;
                    m.B.AppliedLoad += 15;

                    // weaken neighbors indirectly
                    foreach (var other in Structure.Members)
                    {
                        if (!other.Failed)
                            other.Capacity *= 0.995f;
                    }
                }
            }
        }

        #endregion
    }
}