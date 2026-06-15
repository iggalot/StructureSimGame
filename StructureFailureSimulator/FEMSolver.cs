using System;
using System.Collections.Generic;

namespace StructureFailureSimulator
{
    public class FEMSolver
    {
        private Structure _s;

        public void Solve(Structure structure)
        {
            _s = structure;

            int n = _s.Nodes.Count * 3;

            double[,] K = new double[n, n];
            double[] F = new double[n];

            Assemble(K, F);
            ApplySupports(K, F);

            double[] D = SolveSystem(K, F);

            ApplyDisplacements(D);
            ComputeMemberForces(D);
        }

        private void Assemble(double[,] K, double[] F)
        {
            for (int e = 0; e < _s.Members.Count; e++)
            {
                var m = _s.Members[e];

                int i = _s.Nodes.IndexOf(m.A);
                int j = _s.Nodes.IndexOf(m.B);

                double L = m.Length;
                double EA = m.E * m.AArea;
                double EI = m.E * m.I;

                // simplified axial + bending coupling (game FEM-lite)
                double k = EA / L;
                double kb = EI / (L * L * L);

                int[] dof = new[]
                {
                    i*3+0, i*3+1, i*3+2,
                    j*3+0, j*3+1, j*3+2
                };

                // AXIAL
                Add(K, dof[0], dof[0], k);
                Add(K, dof[0], dof[3], -k);
                Add(K, dof[3], dof[0], -k);
                Add(K, dof[3], dof[3], k);

                // BENDING (very simplified coupling)
                Add(K, dof[2], dof[2], kb);
                Add(K, dof[5], dof[5], kb);

                // gravity load
                F[dof[1]] -= 10;
                F[dof[4]] -= 10;
            }
        }

        private void Add(double[,] K, int i, int j, double v)
        {
            K[i, j] += v;
        }

        private void ApplySupports(double[,] K, double[] F)
        {
            for (int i = 0; i < _s.Nodes.Count; i++)
            {
                var n = _s.Nodes[i];

                if (!n.IsFixedSupport) continue;

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

        private void ComputeMemberForces(double[] D)
        {
            foreach (var m in _s.Members)
            {
                int i = _s.Nodes.IndexOf(m.A);
                int j = _s.Nodes.IndexOf(m.B);

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