namespace _Project.Scripts.Core
{
    /// <summary>
    /// Representa um segmento de via ou aresta que conecta duas interseções.
    /// </summary>
    public class Road
    {
        public int Id;
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