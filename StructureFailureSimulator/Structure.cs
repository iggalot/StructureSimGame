using System.Collections.Generic;
using System.Xml.Linq;

namespace StructureFailureSimulator
{
    public class Structure
    {
        public List<Node> Nodes { get; set; } = new();
        public List<Member> Members { get; set; } = new();
    }
}