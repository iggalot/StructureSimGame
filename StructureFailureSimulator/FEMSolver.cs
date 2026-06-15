using System;
using System.Collections.Generic;

namespace StructureFailureSimulator
{
    public class FEMSolver
    {
        private Structure _s;

        // cached node lookup (CRITICAL FIX)
        private Dictionary<Node, int> _nodeIndex = new();

        public void Solve(Structure structure)
        {
            _s = structure;

            BuildNodeIndex(); // 🔥 FIX 1

            int n = _s.Nodes.Count * 3;

            double[,] K = new double[n, n];
            double[] F = new double[n];

            Assemble(K, F);
            ApplySupports(K, F);

            double[] D = SolveSystem(K, F);

            ApplyDisplacements(D);
            ComputeMemberForces(D);
        }

        // =========================
        // FIX 1: O(1) NODE LOOKUP
        // =========================
        private void BuildNodeIndex()
        {
            _nodeIndex.Clear();

            for (int i = 0; i < _s.Nodes.Count; i++)
            {
                _nodeIndex[_s.Nodes[i]] = i;
            }
        }

        // =========================
        // ASSEMBLY
        // =========================
        private void Assemble(double[,] K, double[] F)
        {
            for (int e = 0; e < _s.Members.Count; e++)
            {
                var m = _s.Members[e];

                if (!_nodeIndex.TryGetValue(m.A, out int i)) continue;
                if (!_nodeIndex.TryGetValue(m.B, out int j)) continue;

                double L = m.Length;
                double EA = m.E * m.AArea;
                double EI = m.E * m.I;

                double k = EA / L;
                double kb = EI / (L * L * L);

                int i0 = i * 3;
                int j0 = j * 3;

                int[] dof =
                {
                    i0 + 0, i0 + 1, i0 + 2,
                    j0 + 0, j0 + 1, j0 + 2
                };

                // axial
                K[dof[0], dof[0]] += k;
                K[dof[0], dof[3]] -= k;
                K[dof[3], dof[0]] -= k;
                K[dof[3], dof[3]] += k;

                // bending (simplified)
                K[dof[2], dof[2]] += kb;
                K[dof[5], dof[5]] += kb;

                // gravity load
                F[dof[1]] -= 10;
                F[dof[4]] -= 10;
            }
        }

        // =========================
        // SUPPORTS
        // =========================
        private void ApplySupports(double[,] K, double[] F)
        {
            for (int i = 0; i < _s.Nodes.Count; i++)
            {
                if (!_s.Nodes[i].IsFixedSupport)
                    continue;

                for (int d = 0; d < 3; d++)
                {
                    int idx = i * 3 + d;

                    for (int j = 0; j < K.GetLength(0); j++)
                    {
                        K[idx, j] = 0;
                        K[j, idx] = 0;
                    }

                    K[idx, idx] = 1;
                    F[idx] = 0;
                }
            }
        }

        // =========================
        // GAUSSIAN ELIMINATION (UNCHANGED)
        // =========================
        private double[] SolveSystem(double[,] K, double[] F)
        {
            int n = F.Length;
            double[] x = new double[n];

            for (int i = 0; i < n; i++)
            {
                double pivot = K[i, i];
                if (Math.Abs(pivot) < 1e-10) continue;

                for (int j = i + 1; j < n; j++)
                {
                    double f = K[j, i] / pivot;

                    for (int k = i; k < n; k++)
                        K[j, k] -= f * K[i, k];

                    F[j] -= f * F[i];
                }
            }

            for (int i = n - 1; i >= 0; i--)
            {
                double sum = F[i];

                for (int j = i + 1; j < n; j++)
                    sum -= K[i, j] * x[j];

                x[i] = sum / (K[i, i] == 0 ? 1 : K[i, i]);
            }

            return x;
        }

        // =========================
        // DISPLACEMENTS
        // =========================
        private void ApplyDisplacements(double[] D)
        {
            for (int i = 0; i < _s.Nodes.Count; i++)
            {
                var n = _s.Nodes[i];

                n.Ux = D[i * 3 + 0];
                n.Uy = D[i * 3 + 1];
                n.Rz = D[i * 3 + 2];
            }
        }

        // =========================
        // MEMBER FORCES
        // =========================
        private void ComputeMemberForces(double[] D)
        {
            foreach (var m in _s.Members)
            {
                if (!_nodeIndex.TryGetValue(m.A, out int i)) continue;
                if (!_nodeIndex.TryGetValue(m.B, out int j)) continue;

                double dx = m.B.Ux - m.A.Ux;
                double dy = m.B.Uy - m.A.Uy;

                double strain = Math.Sqrt(dx * dx + dy * dy);

                m.AxialForce = strain * m.E * 0.01;

                if (m.AxialForce > m.YieldStrength)
                    m.Failed = true;
            }
        }
    }
}
