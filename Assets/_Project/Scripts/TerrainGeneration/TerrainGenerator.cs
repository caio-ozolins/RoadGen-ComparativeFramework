using UnityEngine;

namespace _Project.Scripts.TerrainGeneration
{
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Configuration")]
        public Terrain terrain;

        [Header("Noise Parameters")]
        // --- CHANGE 1: Increased the maximum range for more control over smoothness ---
        [Range(1, 1000)]
        public float noiseScale = 25.0f;
        
        // --- CHANGE 2: Added a height multiplier for vertical control ---
        [Tooltip("Controls the overall vertical scale of the terrain. Values < 1 flatten the terrain, > 1 exaggerate it.")]
        [Range(0f, 1f)]
        public float terrainHeightMultiplier = 0.5f;

        // These fields will be updated randomly at generation.
        public float offsetX;
        public float offsetY;

        [Header("Multi-Octave Parameters")]
        [Range(1, 8)]
        public int octaves = 4;

        [Range(0.1f, 1f)]
        public float persistence = 0.5f;

        [Range(1f, 4f)]
        public float lacunarity = 2.0f;
        
        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("The Terrain object has not been assigned in the Inspector!");
                return;
            }
            
            offsetX = Random.Range(0f, 9999f);
            offsetY = Random.Range(0f, 9999f);

            Debug.Log($"Starting terrain generation with new random offsets: ({offsetX}, {offsetY})");

            var terrainData = terrain.terrainData;
            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;

            float[,] heights = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;
                    float maxAmplitude = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x + offsetX) / noiseScale * frequency;
                        float sampleY = (y + offsetY) / noiseScale * frequency;
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;
                        maxAmplitude += amplitude;
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }
                    
                    // --- CHANGE 3: Apply the height multiplier to the final normalized height ---
                    float normalizedHeight = noiseHeight / maxAmplitude;
                    heights[x, y] = normalizedHeight * terrainHeightMultiplier;
                }
            }
            
            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain generation complete!");
        }
    }
}