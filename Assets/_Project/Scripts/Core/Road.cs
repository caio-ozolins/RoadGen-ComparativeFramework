namespace _Project.Scripts.Core
{
    /// <summary>
    /// Represents a road segment or edge connecting two intersections.
    /// </summary>
    public class Road
    {
        public readonly int Id;
        public readonly Intersection StartNode;
        public readonly Intersection EndNode;

        public Road(int id, Intersection startNode, Intersection endNode)
        {
            Id = id;
            StartNode = startNode;
            EndNode = endNode;
        }
    }
}