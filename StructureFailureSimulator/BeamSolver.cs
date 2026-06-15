using System;
using System.Collections.Generic;

namespace StructureFailureSimulator
{
    public class BeamSolver
    {
        private Structure _structure;

        public void Solve(Structure structure)
        {
            _structure = structure;

            int n = structure.Nodes.Count * 3;

            double[,] K = new double[n, n];
            double[] F = new double[n];
            double[] D = new double[n];

            BuildGlobalMatrix(K, F);
            ApplySupports(K, F);
            D = SolveSystem(K, F);

            ApplyDisplacements(D);
            RecoverMemberForces(D);
        }

        private void BuildGlobalMatrix(double[,] K, double[] F)
        {
            foreach (var m in _structure.Members)
            {
                int i = _structure.Nodes.IndexOf(m.A);
                int j = _structure.Nodes.IndexOf(m.B);

                double L = m.Length;

                double EA_L = (m.E * m.AArea) / L;
                double EI_L3 = (m.E * m.I) / (L * L * L);

                // VERY SIMPLIFIED frame stiffness contributions
                Add(K, i * 3 + 0, i * 3 + 0, EA_L);
                Add(K, i * 3 + 1, i * 3 + 1, EA_L);

                Add(K, j * 3 + 0, j * 3 + 0, EA_L);
                Add(K, j * 3 + 1, j * 3 + 1, EA_L);

                // coupling stiffness
                Add(K, i * 3 + 0, j * 3 + 0, -EA_L);
                Add(K, i * 3 + 1, j * 3 + 1, -EA_L);

                // load distribution (gravity + wind placeholder)
                F[i * 3 + 1] -= 10;
                F[j * 3 + 1] -= 10;
            }
        }

        private void Add(double[,] K, int i, int j, double v)
        {
            K[i, j] += v;
        }

        private void ApplySupports(double[,] K, double[] F)
        {
            for (int i = 0; i < _structure.Nodes.Count; i++)
            {
                var n = _structure.Nodes[i];

                if (n.IsFixedSupport)
                {
                    int idx = i * 3;

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
                if (Math.Abs(pivot) < 1e-9) continue;

                for (int j = i + 1; j < n; j++)
                {
                    double factor = K[j, i] / pivot;

                    for (int k = i; k < n; k++)
                        K[j, k] -= factor * K[i, k];

                    F[j] -= factor * F[i];
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
            for (int i = 0; i < _structure.Nodes.Count; i++)
            {
                var n = _structure.Nodes[i];

                n.Ux = D[i * 3 + 0];
                n.Uy = D[i * 3 + 1];
                n.Rz = D[i * 3 + 2];
            }
        }

        private void RecoverMemberForces(double[] D)
        {
            foreach (var m in _structure.Members)
            {
                int i = _structure.Nodes.IndexOf(m.A);
                int j = _structure.Nodes.IndexOf(m.B);

                double du = (m.B.Ux - m.A.Ux);
                double dv = (m.B.Uy - m.A.Uy);

                m.AxialForce = Math.Sqrt(du * du + dv * dv) * m.E;

                if (m.AxialForce > m.YieldStrength)
                    m.Failed = true;
            }
        }
    }
}