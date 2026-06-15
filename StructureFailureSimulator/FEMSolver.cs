using System;
using System.Collections.Generic;

namespace StructureFailureSimulator
{
    public class FEMSolver
    {
        private Structure _s;
        double[,] K;  // global stiffness matrix
        double[] F;  // applied load vector

        // cached node lookup (CRITICAL FIX)
        private Dictionary<Node, int> _nodeIndex = new();

        public void Solve(Structure structure)
        {
            _s = structure;

            BuildNodeIndex(); //  FIX 1

            int n = _s.Nodes.Count * 3;

            K = new double[n, n];

            F = new double[n];

            Assemble();
            ApplySupports();

            double[] D = SolveSystem();

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
        private void Assemble()
        {
            for (int e = 0; e < _s.Members.Count; e++)
            {
                var m = _s.Members[e];

                if (!_nodeIndex.TryGetValue(m.A, out int i)) continue;
                if (!_nodeIndex.TryGetValue(m.B, out int j)) continue;

                // =========================
                // DOF MAP (GLOBAL SYSTEM)
                // =========================
                int i0 = i * 3;
                int j0 = j * 3;

                int[] dof =
                {
                    i0 + 0, i0 + 1, i0 + 2,
                    j0 + 0, j0 + 1, j0 + 2
                };

                // =========================
                // GEOMETRY
                // =========================
                double dx = m.B.Position.X - m.A.Position.X;
                double dy = m.B.Position.Y - m.A.Position.Y;

                double L = Math.Sqrt(dx * dx + dy * dy);
                if (L < 1e-9) continue;

                double c = dx / L;
                double s = dy / L;

                // =========================
                // MATERIAL PROPERTIES
                // =========================
                double EA = m.E * m.AArea;
                double EI = m.E * m.I;

                double k = EA / L;
                double kb = EI / (L * L * L);

                // =========================
                // LOCAL 6x6 STIFFNESS MATRIX
                // =========================
                double[,] kLocal = new double[6, 6];

                // axial
                kLocal[0, 0] = k;
                kLocal[0, 3] = -k;
                kLocal[3, 0] = -k;
                kLocal[3, 3] = k;

                // bending (y-direction local frame)
                kLocal[1, 1] = 12 * kb;
                kLocal[1, 2] = 6 * L * kb;
                kLocal[1, 4] = -12 * kb;
                kLocal[1, 5] = 6 * L * kb;

                kLocal[2, 1] = 6 * L * kb;
                kLocal[2, 2] = 4 * L * L * kb;
                kLocal[2, 4] = -6 * L * kb;
                kLocal[2, 5] = 2 * L * L * kb;

                kLocal[4, 1] = -12 * kb;
                kLocal[4, 2] = -6 * L * kb;
                kLocal[4, 4] = 12 * kb;
                kLocal[4, 5] = -6 * L * kb;

                kLocal[5, 1] = 6 * L * kb;
                kLocal[5, 2] = 2 * L * L * kb;
                kLocal[5, 4] = -6 * L * kb;
                kLocal[5, 5] = 4 * L * L * kb;

                // =========================
                // TRANSFORMATION MATRIX
                // =========================
                double[,] T =
                {
                    { c,  s, 0, 0, 0, 0 },
                    {-s,  c, 0, 0, 0, 0 },
                    { 0,  0, 1, 0, 0, 0 },

                    { 0,  0, 0, c,  s, 0 },
                    { 0,  0, 0,-s,  c, 0 },
                    { 0,  0, 0, 0,  0, 1 }
                };

                double[,] TT = Transpose(T);

                // =========================
                // kGlobal = Tᵀ kLocal T
                // =========================
                double[,] temp = Multiply(TT, kLocal);
                double[,] kGlobal = Multiply(temp, T);

                // =========================
                // ASSEMBLE INTO STRUCTURE MATRIX
                // =========================
                for (int r = 0; r < 6; r++)
                {
                    for (int c2 = 0; c2 < 6; c2++)
                    {
                        K[dof[r], dof[c2]] += kGlobal[r, c2];
                    }
                }

                // =========================
                // LOAD VECTOR (OPTIONAL)
                // =========================
                F[dof[3]] += 10000;
            }
        }

        private static double[,] Transpose(double[,] A)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);

            double[,] AT = new double[cols, rows];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    AT[j, i] = A[i, j];

            return AT;
        }

        private static double[,] Multiply(double[,] A, double[,] B)
        {
            int rows = A.GetLength(0);
            int cols = B.GetLength(1);
            int inner = A.GetLength(1);

            double[,] C = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0;

                    for (int k = 0; k < inner; k++)
                        sum += A[i, k] * B[k, j];

                    C[i, j] = sum;
                }
            }

            return C;
        }

        // =========================
        // SUPPORTS
        // =========================
        private void ApplySupports()
        {
            for (int i = 0; i < _s.Nodes.Count; i++)
            {
                if (!_s.Nodes[i].IsConstrained_DispX)
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
        private double[] SolveSystem()
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
