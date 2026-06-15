using System.Collections.Generic;

namespace StructureFailureSimulator
{
    public class Structure
    {
        public List<Node> Nodes { get; set; } = new();
        public List<Member> Members { get; set; } = new();

        // NEW: supports quick lookup
        public List<Node> Supports { get; set; } = new();
    }
}