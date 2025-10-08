namespace _Project.Scripts.Generation.Pathfinding
{
    /// <summary>
    /// Represents a single node on the grid for the A* pathfinding algorithm.
    /// It contains its position, costs, and a reference to its parent node to reconstruct the path.
    /// </summary>
    public class Node
    {
        // The node's position on the grid (not world coordinates).
        public int X { get; }
        public int Y { get; }

        // --- A* Cost Properties ---
        
        /// <summary>
        /// G-Cost: The actual cost of the path from the starting node to this node.
        /// </summary>
        public float GCost { get; set; }

        /// <summary>
        /// H-Cost: The heuristic (estimated) cost from this node to the end node.
        /// </summary>
        public float HCost { get; set; }
        
        /// <summary>
        /// F-Cost: The total cost of the node (G-Cost + H-Cost). A* prioritizes nodes with the lowest F-Cost.
        /// </summary>
        public float FCost => GCost + HCost;

        /// <summary>
        /// A reference to the previous node in the path. Used to trace the path back once the target is found.
        /// </summary>
        public Node Parent { get; set; }

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}