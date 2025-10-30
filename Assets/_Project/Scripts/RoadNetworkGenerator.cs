using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation;
using _Project.Scripts.Generation.Analysis;
using _Project.Scripts.Generation.Abstractions;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using _Project.Scripts.Evaluation;
using System.Diagnostics;
using System; // --- NEW: Added for System.GC ---

using Debug = UnityEngine.Debug;

namespace _Project.Scripts
{
    public enum GenerationTechnique
    {
        AgentBasedRandomWalk,
        // ReSharper disable once InconsistentNaming
        PathBasedAStarPOIs,
        LSystem
    }

    public class RoadNetworkGenerator : MonoBehaviour
    {
        // ... (All parameters remain the same) ...
        [Header("System Configuration")] public Terrain terrain;
        [Header("Generation Technique")] public GenerationTechnique selectedTechnique = GenerationTechnique.AgentBasedRandomWalk;
        [Header("General / Agent Parameters")] public int globalStepLimit = 1000; public int maxStepsPerAgent = 50; public float stepSize = 10.0f; [Range(0, 45)] public float maxTurnAngle = 15.0f; [Range(0, 90)] public float maxSteepness = 30.0f; [Header("Agent Branching (Divided Pool)")] [Range(0, 5)] public float totalBranchingChance = 1.0f;
        [Header("Path Parameters (A* POIs)")] public float minimumSegmentLength = 15.0f; [Range(2, 50)] public int numberOfPointsOfInterest = 5; [Header("Transversal Connections (A* POIs)")] public bool addTransversalConnections = true; public float maxConnectionDistance = 50.0f; public int maxConnectionAttempts = 100;
        [Header("L-System Parameters")] [Tooltip("The starting string (axiom).")] public string lSystemAxiom = "X"; [Tooltip("Number of rewriting iterations.")] [Range(1, 7)] public int lSystemIterations = 4; [Tooltip("Base angle (degrees) for '+' and '-' commands.")] public float lSystemTurnAngle = 25.0f; [Tooltip("Maximum random variation (+/- degrees) added to each turn.")] [Range(0f, 45f)] public float lSystemAngleVariance = 5.0f; [Tooltip("Maximum random variation (+/- units) added to each segment length.")] [Range(0f, 10f)] public float lSystemLengthVariance = 2.0f;
        [Header("Safety Limits")] public float maxGenerationTimeSeconds = 10f; public int maxActiveAgents = 5000;

        // --- Internal State ---
        private readonly List<Intersection> _intersections = new List<Intersection>();
        private readonly List<Road> _roads = new List<Road>();
        private CostMap _costMap;
        private readonly List<Vector2Int> _pointsOfInterest = new List<Vector2Int>();
        [Header("Debug")] public RawImage debugCostMapImage;

        [ContextMenu("Generate Road Network")]
        private void Generate()
        {
            Debug.Log($"[Orchestrator] Starting generation process using: {selectedTechnique}");
            ClearPreviousNetwork();

            Debug.Log("[Orchestrator] Analysis Phase: Generating Cost Map...");
            var costMapGenerator = new CostMapGenerator();
            _costMap = costMapGenerator.Generate(this.terrain);
            if (_costMap == null) { Debug.LogError("[Orchestrator] Cost Map generation failed. Aborting road network generation."); return; }

            _pointsOfInterest.Clear();
            IRoadNetworkGenerator generator;
            switch (selectedTechnique)
            {
                case GenerationTechnique.AgentBasedRandomWalk:
                    Debug.Log("[Orchestrator] Initializing AgentBasedRandomWalk generator...");
                    generator = new RandomWalkGenerator
                    {
                        Terrain = this.terrain, GlobalStepLimit = this.globalStepLimit, MaxStepsPerAgent = this.maxStepsPerAgent,
                        StepSize = this.stepSize, MaxTurnAngle = this.maxTurnAngle, MaxSteepness = this.maxSteepness,
                        TotalBranchingChance = this.totalBranchingChance, MaxGenerationTimeSeconds = this.maxGenerationTimeSeconds,
                        MaxActiveAgents = this.maxActiveAgents
                    };
                    break;

                case GenerationTechnique.PathBasedAStarPOIs:
                    Debug.Log("[Orchestrator] Initializing PathBasedAStarPOIs generator...");
                    int gridWidth = _costMap.Width; int gridHeight = _costMap.Height; Vector2Int centerPoint = new Vector2Int(gridWidth / 2, gridHeight / 2); _pointsOfInterest.Add(centerPoint);
                    for (int i = 1; i < numberOfPointsOfInterest; i++) { Vector2Int randomPoi = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight)); if (!_pointsOfInterest.Contains(randomPoi)) { _pointsOfInterest.Add(randomPoi); } else { i--; } }
                    Debug.Log($"[Orchestrator] Generated {_pointsOfInterest.Count} Points of Interest.");
                    var pathGenerator = new PathBasedGenerator
                    {
                        Terrain = this.terrain, CostMap = this._costMap, PointsOfInterest = _pointsOfInterest, MinimumSegmentLength = this.minimumSegmentLength,
                        AddTransversalConnections = this.addTransversalConnections, MaxConnectionDistance = this.maxConnectionDistance, MaxConnectionAttempts = this.maxConnectionAttempts
                    };
                    generator = pathGenerator;
                    break;

                case GenerationTechnique.LSystem:
                    Debug.Log("[Orchestrator] Initializing LSystem generator...");
                    
                    string axiom = "F"; // Start with F
                    var defaultRules = new Dictionary<char, string> {
                        { 'F', "F[+F]F[-F]F" } // Rule: F -> F[+F]F[-F]F
                    };
                    float turnAngle = 25.7f; // Common angle for this rule

                    generator = new LSystemGenerator
                    {
                        Terrain = this.terrain,
                        Axiom = axiom, // Use the simple axiom
                        Iterations = this.lSystemIterations,
                        TurnAngle = turnAngle, // Use the specific angle
                        SegmentLength = this.stepSize,
                        Rules = defaultRules,
                        AngleVariance = this.lSystemAngleVariance,
                        LengthVariance = this.lSystemLengthVariance
                    };
                    break;

                default:
                    Debug.LogError($"[Orchestrator] Unknown GenerationTechnique selected: {selectedTechnique}");
                    return;
            }

            // --- Generation Execution, Timing, and Metrics ---
            
             Debug.Log($"[Orchestrator] Executing {selectedTechnique} generator...");
             
             // --- NEW: Memory Measurement ---
             // 1. Force garbage collection to get a clean baseline
             GC.Collect();
             GC.WaitForPendingFinalizers();
             long memoryBefore = GC.GetTotalMemory(true);
             // -----------------------------

             Stopwatch generationStopwatch = new Stopwatch(); 
             generationStopwatch.Start();
             
             var result = generator.Generate();
             
             generationStopwatch.Stop(); 
             double elapsedSeconds = generationStopwatch.Elapsed.TotalSeconds;
             
             // --- NEW: Memory Measurement ---
             // 2. Measure memory *after* generation
             long memoryAfter = GC.GetTotalMemory(false);
             long memoryUsed = memoryAfter - memoryBefore;
             // Ensure we don't log negative memory if GC ran mid-generation
             if (memoryUsed < 0) memoryUsed = 0;
             // -----------------------------
             
             _intersections.AddRange(result.intersections); 
             _roads.AddRange(result.roads);
             
             Debug.Log($"[Orchestrator] Generation completed by {selectedTechnique} in {elapsedSeconds:F3} seconds.");
             Debug.Log("[Orchestrator] Calculating metrics...");
             
             var calculator = new MetricsCalculator();
             
             // --- UPDATED: Pass the new 'memoryUsed' parameter ---
             MetricsResult metrics = calculator.Calculate(_intersections, _roads, elapsedSeconds, memoryUsed);
             // ----------------------------------------------------
             
             Debug.Log(metrics.ToString());
        }

        // ... (OnDrawGizmos, ClearPreviousNetwork, VisualizeCostMap, ConvertGridToWorldPosition remain unchanged) ...
        
        private void OnDrawGizmos()
        {
            // Draw existing network
            Gizmos.color = Color.white;
            foreach (var road in _roads) { if (road is { StartNode: not null, EndNode: not null }) { Gizmos.DrawLine(road.StartNode.Position, road.EndNode.Position); } }
            Gizmos.color = Color.red;
            foreach (var intersection in _intersections) { Gizmos.DrawSphere(intersection.Position, 1.0f); }

            // Draw POIs if relevant
             if (selectedTechnique == GenerationTechnique.PathBasedAStarPOIs && _pointsOfInterest != null && terrain is { terrainData: not null })
             {
                 Gizmos.color = Color.yellow;
                 foreach (Vector2Int poi in _pointsOfInterest)
                 {
                     Vector3 poiWorldPos = ConvertGridToWorldPosition(poi);
                     Gizmos.DrawSphere(poiWorldPos, 5.0f);
                     Gizmos.DrawLine(poiWorldPos, poiWorldPos + Vector3.up * 20f);
                 }
             }
        }
        
        [ContextMenu("Clear Road Network")] private void ClearPreviousNetwork() { Debug.Log("[Orchestrator] Clearing previous network..."); _intersections.Clear(); _roads.Clear(); _pointsOfInterest.Clear(); }
        private Vector3 ConvertGridToWorldPosition(Vector2Int gridPoint) { if (terrain == null || terrain.terrainData == null) return Vector3.zero; TerrainData terrainData = terrain.terrainData; int gridWidth = terrainData.heightmapResolution; int gridHeight = terrainData.heightmapResolution; int x = Mathf.Clamp(gridPoint.x, 0, gridWidth - 1); int y = Mathf.Clamp(gridPoint.y, 0, gridHeight - 1); float normX = (float)x / (gridWidth - 1); float normZ = (float)y / (gridHeight - 1); float worldX = (normX * terrainData.size.x) + terrain.transform.position.x; float worldZ = (normZ * terrainData.size.z) + terrain.transform.position.z; float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)); return new Vector3(worldX, worldY, worldZ); }
        [ContextMenu("Visualize Cost Map")] private void VisualizeCostMap() { if (_costMap == null) { Debug.LogWarning("[Orchestrator] Cost Map has not been generated yet. Please run 'Generate Road Network' first."); return; } if (debugCostMapImage == null) { Debug.LogWarning("[Orchestrator] No RawImage assigned to 'debugCostMapImage' field. Cannot visualize cost map."); return; } int width = _costMap.Width; int height = _costMap.Height; Texture2D costMapTexture = new Texture2D(width, height); float maxCost = 0f; for (int y = 0; y < height; y++) { for (int x = 0; x < width; x++) { if (_costMap.GetCost(x, y) > maxCost) { maxCost = _costMap.GetCost(x, y); } } } if (Mathf.Approximately(maxCost, 0)) maxCost = 1.0f; for (int y = 0; y < height; y++) { for (int x = 0; x < width; x++) { float cost = _costMap.GetCost(x, y); float normalizedCost = cost / maxCost; Color pixelColor = new Color(normalizedCost, normalizedCost, normalizedCost); costMapTexture.SetPixel(x, y, pixelColor); } } costMapTexture.Apply(); debugCostMapImage.texture = costMapTexture; Debug.Log($"[Orchestrator] Cost map visualized. Max cost found: {maxCost}"); }

    }
}