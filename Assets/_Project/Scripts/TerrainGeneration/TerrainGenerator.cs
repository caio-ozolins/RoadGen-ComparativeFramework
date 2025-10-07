using UnityEngine;

namespace _Project.Scripts.TerrainGeneration
{
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Configuration")]
        public Terrain terrain;

        [Header("Noise Parameters")]
        [Range(1, 200)]
        public float noiseScale = 25.0f;
        
        // We add an offset to generate different terrains each time.
        public float offsetX = 100f;
        public float offsetY = 100f;

        // In the editor, this generates a random offset for variety when tweaking parameters.
        void OnValidate()
        {
            offsetX = Random.Range(0f, 9999f);
            offsetY = Random.Range(0f, 9999f);
        }

        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("The Terrain object has not been assigned in the Inspector!");
                return;
            }

            Debug.Log("Starting adaptive terrain generation...");

            var terrainData = terrain.terrainData;
            
            // Reads the dimensions from the existing terrain, as you suggested.
            int width = terrainData.heightmapResolution;
            int length = terrainData.heightmapResolution;

            // Creates a 2D array with the dimensions read from the terrain.
            float[,] heights = new float[width, length];

            // Loop to iterate through each point of the heightmap.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    // Calculates the coordinates for noise sampling.
                    float sampleX = (x + offsetX) / noiseScale;
                    float sampleY = (y + offsetY) / noiseScale;
                    
                    // Gets the Perlin noise value.
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

                    heights[x, y] = perlinValue;
                }
            }

            // Applies the generated height array to the terrain.
            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain generation complete!");
        }
    }
}