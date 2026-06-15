using System.Numerics;

namespace StructureFailureSimulator
{
    public class Node
    {
        public int Id { get; set; }
        public Vector2 Position { get; set; }

        public float AppliedLoad;
    }
}