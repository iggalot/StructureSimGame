using System.Numerics;

namespace StructureFailureSimulator
{
    public class Node
    {
        public int Id { get; set; }
        public Vector2 Position { get; set; }

        public float AppliedLoad;

        // NEW: structural boundary condition
        public bool IsFixedSupport;
        public bool IsPinnedSupport;

        public float ReactionForce;
    }
}