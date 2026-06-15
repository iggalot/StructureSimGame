namespace StructureFailureSimulator
{
    public class Member
    {
        public Node A { get; set; }
        public Node B { get; set; }

        public float Capacity { get; set; } = 50f;
        public float CurrentLoad { get; set; }

        public bool Failed { get; set; }
    }
}