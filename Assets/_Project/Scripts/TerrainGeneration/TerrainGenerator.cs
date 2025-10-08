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
        
        // Estes campos agora serão atualizados aleatoriamente a cada geração.
        public float offsetX;
        public float offsetY;

        [Header("Multi-Octave Parameters")]
        [Range(1, 8)]
        public int octaves = 4;

        [Range(0.1f, 1f)]
        public float persistence = 0.5f;

        [Range(1f, 4f)]
        public float lacunarity = 2.0f;
        
        // Removemos o método OnValidate(), pois a randomização será feita no GenerateTerrain().

        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("The Terrain object has not been assigned in the Inspector!");
                return;
            }
            
            // Gera novos offsets aleatórios a cada execução.
            offsetX = Random.Range(0f, 9999f);
            offsetY = Random.Range(0f, 9999f);

            Debug.Log($"Starting terrain generation with new random offsets: ({offsetX}, {offsetY})");

            var terrainData = terrain.terrainData;
            int width = terrainData.heightmapResolution;
            int length = terrainData.heightmapResolution;

            float[,] heights = new float[width, length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
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
                    
                    heights[x, y] = noiseHeight / maxAmplitude;
                }
            }
    
            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain generation complete!");
        }
    }
}