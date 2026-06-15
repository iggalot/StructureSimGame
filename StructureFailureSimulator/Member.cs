namespace StructureFailureSimulator
{
    public class Member
    {
        public Node A;
        public Node B;

        public double E = 200e9;
        public double I = 1.0;
        public double AArea = 1.0;

        public double YieldStrength = 250;

        public double AxialForce;
        public double Moment;

        public bool Failed;

        public double Length =>
            System.Numerics.Vector2.Distance(A.Position, B.Position);

        // COMPATIBILITY PROPERTY FOR UI
        public double CurrentLoad => AxialForce;
        public double Capacity => YieldStrength;
    }
}