namespace StructureFailureSimulator
{
    public class Member
    {
        public Node A;
        public Node B;

        public double E = 29000000; // psi
        public double I = 100.0; // in^4
        public double AArea = 1.0; // sq. in.

        public double YieldStrength = 50000;  // psi

        public double AxialForce;  // lbs
        public double Moment;  // lb-in

        public bool Failed;

        public double Length =>
            System.Numerics.Vector2.Distance(A.Position, B.Position);

        // COMPATIBILITY PROPERTY FOR UI
        public double CurrentLoad => AxialForce;
        public double Capacity => YieldStrength;
    }
}