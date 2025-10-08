using UnityEngine;
using _Project.Scripts.Generation.Analysis; // Import the namespace for our CostMap class

namespace _Project.Scripts.Generation
{
    /// <summary>
    /// Responsible for analyzing a terrain and generating a cost map.
    /// The cost of each cell is based on terrain metrics, such as steepness.
    /// </summary>
    public class CostMapGenerator
    {
        /// <summary>
        /// Generates a cost map from a given terrain's data.
        /// </summary>
        /// <param name="terrain">The Unity Terrain object to be analyzed.</param>
        /// <returns>A CostMap object containing the calculated costs for each point on the terrain grid.</returns>
        public CostMap Generate(Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("[CostMapGenerator] Provided terrain is null or its TerrainData is invalid.");
                return null;
            }

            TerrainData terrainData = terrain.terrainData;
            // We use the heightmap resolution, which defines the grid of points on the terrain.
            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;

            var costMap = new CostMap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // The GetSteepness function expects normalized coordinates (from 0.0 to 1.0).
                    float normalizedX = x / (float)(width - 1);
                    float normalizedY = y / (float)(height - 1);

                    // The steepness is returned in degrees, from 0 (flat) to 90 (vertical).
                    float steepness = terrainData.GetSteepness(normalizedX, normalizedY);

                    // Convert steepness to cost and set it in our CostMap object.
                    // For now, a 1:1 conversion is sufficient for testing.
                    // Higher steepness values will represent a higher cost to traverse.
                    costMap.SetCost(x, y, steepness);
                }
            }

            Debug.Log($"[CostMapGenerator] Cost Map generated successfully. Dimensions: {width}x{height}.");
            return costMap;
        }
    }
}