namespace StructureFailureSimulator
{
    public class Member
    {
        public Node A { get; set; }
        public Node B { get; set; }

        public float Capacity = 50f;

        // NEW physics-ish properties
        public float Stiffness = 1.0f;   // resistance to deformation
        public float CurrentLoad;
        public bool Failed;

        public float Length =>
            System.Numerics.Vector2.Distance(A.Position, B.Position);
    }
}