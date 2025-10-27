using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation;
using _Project.Scripts.Generation.Analysis;
using _Project.Scripts.Generation.Abstractions;
using UnityEngine.UI;

namespace _Project.Scripts
{
    public enum GenerationTechnique
    {
        AgentBased_RandomWalk,
        PathBased_AStar
    }

    public class RoadNetworkGenerator : MonoBehaviour
    {
        [Header("System Configuration")]
        public Terrain terrain;

        [Header("Generation Technique")]
        public GenerationTechnique selectedTechnique = GenerationTechnique.AgentBased_RandomWalk;

        [Header("General Generation Parameters")]
        public int globalStepLimit = 1000;
        public int maxStepsPerAgent = 50;
        public float stepSize = 10.0f;

        [Header("Agent Parameters (Random Walk)")]
        [Range(0, 45)]
        public float maxTurnAngle = 15.0f;
        [Range(0, 90)]
        public float maxSteepness = 30.0f;
        [Header("Branching Behaviour (Divided Pool)")]
        [Range(0, 5)]
        public float totalBranchingChance = 1.0f;

        // --- UPDATED: Removed Start/End Point from Inspector ---
        // [Header("Path Parameters (A*)")]
        // public Vector2Int startPoint = Vector2Int.zero;
        // public Vector2Int endPoint = new Vector2Int(100, 100);
        [Header("Path Parameters (A*)")] // Kept header for clarity
        [Tooltip("The minimum desired length for road segments created by the PathBased generator.")]
        public float minimumSegmentLength = 15.0f;
        // --------------------------------------------------------

        [Header("Safety Limits")]
        public float maxGenerationTimeSeconds = 10f;
        public int maxActiveAgents = 5000;

        private readonly List<Intersection> _intersections = new List<Intersection>();
        private readonly List<Road> _roads = new List<Road>();
        private CostMap _costMap;

        [Header("Debug")]
        public RawImage debugCostMapImage;

        [ContextMenu("Generate Road Network")]
        private void Generate()
        {
            Debug.Log($"[Orchestrator] Starting generation process using: {selectedTechnique}");
            ClearPreviousNetwork();

            Debug.Log("[Orchestrator] Analysis Phase: Generating Cost Map...");
            var costMapGenerator = new CostMapGenerator();
            _costMap = costMapGenerator.Generate(this.terrain);

            if (_costMap == null)
            {
                Debug.LogError("[Orchestrator] Cost Map generation failed. Aborting road network generation.");
                return;
            }

            IRoadNetworkGenerator generator = null;

            switch (selectedTechnique)
            {
                case GenerationTechnique.AgentBased_RandomWalk:
                    Debug.Log("[Orchestrator] Initializing AgentBased_RandomWalk generator...");
                    generator = new RandomWalkGenerator
                    {
                        Terrain = this.terrain,
                        GlobalStepLimit = this.globalStepLimit,
                        MaxStepsPerAgent = this.maxStepsPerAgent,
                        StepSize = this.stepSize,
                        MaxTurnAngle = this.maxTurnAngle,
                        MaxSteepness = this.maxSteepness,
                        TotalBranchingChance = this.totalBranchingChance,
                        MaxGenerationTimeSeconds = this.maxGenerationTimeSeconds,
                        MaxActiveAgents = this.maxActiveAgents
                    };
                    break;

                case GenerationTechnique.PathBased_AStar:
                    Debug.Log("[Orchestrator] Initializing PathBased_AStar generator...");

                    // --- NEW: Calculate Center Start and Random End ---
                    int gridWidth = _costMap.Width;
                    int gridHeight = _costMap.Height;

                    // Calculate center point
                    Vector2Int centerPoint = new Vector2Int(gridWidth / 2, gridHeight / 2);

                    // Generate random end point within bounds
                    Vector2Int randomEndPoint = new Vector2Int(
                        Random.Range(0, gridWidth),
                        Random.Range(0, gridHeight)
                    );

                    // Ensure start and end points are not the same
                    while (randomEndPoint == centerPoint)
                    {
                         randomEndPoint = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
                         Debug.Log("[Orchestrator] Generated EndPoint was same as CenterPoint, regenerating EndPoint...");
                    }

                    Debug.Log($"[Orchestrator] PathBased_AStar: StartPoint={centerPoint}, EndPoint={randomEndPoint}");
                    // --------------------------------------------------

                    generator = new PathBasedGenerator
                    {
                        Terrain = this.terrain,
                        CostMap = this._costMap,
                        // --- UPDATED: Use calculated points ---
                        StartPoint = centerPoint,
                        EndPoint = randomEndPoint,
                        // ------------------------------------
                        MinimumSegmentLength = this.minimumSegmentLength
                    };
                    break;

                default:
                    Debug.LogError($"[Orchestrator] Unknown GenerationTechnique selected: {selectedTechnique}");
                    return;
            }

            if (generator != null)
            {
                var result = generator.Generate();
                _intersections.AddRange(result.intersections);
                _roads.AddRange(result.roads);
                Debug.Log($"[Orchestrator] Generation completed by {selectedTechnique}.");
            }
            else
            {
                 Debug.LogError($"[Orchestrator] Failed to create a generator instance for {selectedTechnique}.");
            }
        }

        // --- UPDATED: OnDrawGizmos removed visualization for Inspector points ---
        private void OnDrawGizmos()
        {
            // Draw existing network
            if (_intersections != null && _roads != null)
            {
                Gizmos.color = Color.white;
                foreach (var road in _roads)
                {
                    if (road?.StartNode != null && road.EndNode != null)
                    {
                        Gizmos.DrawLine(road.StartNode.Position, road.EndNode.Position);
                    }
                }

                Gizmos.color = Color.red;
                foreach (var intersection in _intersections)
                {
                     if (intersection != null)
                     {
                        Gizmos.DrawSphere(intersection.Position, 1.0f);
                     }
                }
            }
            // Gizmos for specific start/end points are removed as they are now dynamic
        }
        // -----------------------------------------------------------------------

        // ... (ClearPreviousNetwork, VisualizeCostMap, ConvertGridToWorldPosition remain unchanged) ...

        [ContextMenu("Clear Road Network")]
        private void ClearPreviousNetwork()
        {
            Debug.Log("[Orchestrator] Clearing previous network...");
            _intersections.Clear();
            _roads.Clear();
        }

        private Vector3 ConvertGridToWorldPosition(Vector2Int gridPoint) // Keep this helper function
        {
             if (terrain == null || terrain.terrainData == null) return Vector3.zero;
             TerrainData terrainData = terrain.terrainData;
             int gridWidth = terrainData.heightmapResolution;
             int gridHeight = terrainData.heightmapResolution;
             int x = Mathf.Clamp(gridPoint.x, 0, gridWidth - 1);
             int y = Mathf.Clamp(gridPoint.y, 0, gridHeight - 1);
             float normX = (float)x / (gridWidth - 1);
             float normZ = (float)y / (gridHeight - 1);
             float worldX = (normX * terrainData.size.x) + terrain.transform.position.x;
             float worldZ = (normZ * terrainData.size.z) + terrain.transform.position.z;
             float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
             return new Vector3(worldX, worldY, worldZ);
        }

        [ContextMenu("Visualize Cost Map")]
        private void VisualizeCostMap()
        {
            if (_costMap == null)
            {
                Debug.LogWarning("[Orchestrator] Cost Map has not been generated yet. Please run 'Generate Road Network' first.");
                return;
            }
            if (debugCostMapImage == null)
            {
                Debug.LogWarning("[Orchestrator] No RawImage assigned to 'debugCostMapImage' field. Cannot visualize cost map.");
                return;
            }
            int width = _costMap.Width;
            int height = _costMap.Height;
            Texture2D costMapTexture = new Texture2D(width, height);
            float maxCost = 0f;
            for (int y = 0; y < height; y++) { for (int x = 0; x < width; x++) { if (_costMap.GetCost(x, y) > maxCost) { maxCost = _costMap.GetCost(x, y); } } }
            if (Mathf.Approximately(maxCost, 0)) maxCost = 1.0f;
            for (int y = 0; y < height; y++) { for (int x = 0; x < width; x++) { float cost = _costMap.GetCost(x, y); float normalizedCost = cost / maxCost; Color pixelColor = new Color(normalizedCost, normalizedCost, normalizedCost); costMapTexture.SetPixel(x, y, pixelColor); } }
            costMapTexture.Apply();
            debugCostMapImage.texture = costMapTexture;
            Debug.Log($"[Orchestrator] Cost map visualized. Max cost found: {maxCost}");
        }
    }
}