namespace _Project.Scripts.Generation.Analysis
{
    /// <summary>
    /// Encapsulates the cost map data, including the cost matrix and its dimensions.
    /// This class serves as a data container to facilitate passing information
    /// between the map generator and the algorithms that consume it (like A*).
    /// </summary>
    public class CostMap
    {
        // Fields are now private and prefixed with an underscore (_), a common convention.
        private readonly float[,] _costs;
        
        // Public properties provide controlled, read-only access to the data.
        public int Width { get; }
        public int Height { get; }

        public CostMap(int width, int height)
        {
            Width = width;
            Height = height;
            _costs = new float[width, height];
        }

        /// <summary>
        /// Gets the cost at a specific coordinate, with bounds checking.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The cost value, or float.MaxValue if the coordinate is out of bounds.</returns>
        public float GetCost(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return float.MaxValue; // Infinite cost for nodes outside the map
            }
            return _costs[x, y];
        }

        /// <summary>
        /// Sets the cost at a specific coordinate. This method should only be
        /// called by the generator class that creates the map.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="cost">The cost value to set.</param>
        public void SetCost(int x, int y, float cost)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                // Optionally, log a warning if trying to set cost out of bounds
                return;
            }
            _costs[x, y] = cost;
        }
    }
}