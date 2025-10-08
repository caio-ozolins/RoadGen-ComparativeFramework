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
        
        [Header("Multi-Octave Parameters")]
        [Range(1, 8)]
        public int octaves = 4; // Número de camadas de ruído
        [Range(0.1f, 1f)]
        public float persistence = 0.5f; // Controla a diminuição da amplitude a cada oitava
        [Range(1f, 4f)]
        public float lacunarity = 2.0f; // Controla o aumento da frequência a cada oitava
        
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

            Debug.Log("Starting multi-octave terrain generation...");

            var terrainData = terrain.terrainData;
            int width = terrainData.heightmapResolution;
            int length = terrainData.heightmapResolution;

            float[,] heights = new float[width, length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    // Variáveis para o cálculo das oitavas
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;
                    float maxAmplitude = 0f;

                    // Loop das oitavas (camadas de ruído)
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x + offsetX) / noiseScale * frequency;
                        float sampleY = (y + offsetY) / noiseScale * frequency;
                
                        // Usamos o ruído de Perlin, que varia de 0 a 1.
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                
                        noiseHeight += perlinValue * amplitude;

                        maxAmplitude += amplitude; // Usado para normalizar o resultado
                
                        amplitude *= persistence; // Amplitude diminui a cada oitava
                        frequency *= lacunarity; // Frequência aumenta a cada oitava
                    }
            
                    // Normaliza a altura final para garantir que ela permaneça entre 0 e 1.
                    heights[x, y] = noiseHeight / maxAmplitude;
                }
            }
    
            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain generation complete!");
        }
    }
}