namespace StructureFailureSimulator
{
    public class Node
    {
        public System.Numerics.Vector2 Position;

        // FEM DOF (2D frame)
        public double Ux;
        public double Uy;
        public double Rz;

        public bool IsFixedSupport;
    }
}