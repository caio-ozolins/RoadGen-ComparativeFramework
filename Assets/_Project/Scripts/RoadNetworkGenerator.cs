using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation;
using _Project.Scripts.Generation.Analysis;
using UnityEngine.UI;

namespace _Project.Scripts
{
    public class RoadNetworkGenerator : MonoBehaviour
    {
        [Header("Terrain Configuration")]
        public Terrain terrain;

        [Header("Generation Parameters")]
        public int globalStepLimit = 1000;
        public int maxStepsPerAgent = 50;
        public float stepSize = 10.0f;

        [Header("Agent Parameters")]
        [Range(0, 45)]
        public float maxTurnAngle = 15.0f;
        [Range(0, 90)]
        public float maxSteepness = 30.0f;
        
        // --- UPDATED: Replaced Min/Max with your new model ---
        [Header("Branching Behaviour (Divided Pool)")]
        [Tooltip("The total branching 'pool'. This value is divided by the number of active agents to get the individual agent's branching chance.")]
        [Range(0, 5)]
        public float TotalBranchingChance = 1.0f; // A value of 1.0 means if there are 10 agents, each has a 10% chance
        // ----------------------------------------------------

        [Header("Safety Limits")]
        [Tooltip("The maximum time in seconds the generation is allowed to run before a safety stop.")]
        public float maxGenerationTimeSeconds = 10f;
        
        [Tooltip("The maximum number of active agents allowed. This is also used to calculate the branching decay.")]
        public int maxActiveAgents = 5000;

        private readonly List<Intersection> _intersections = new List<Intersection>();
        private readonly List<Road> _roads = new List<Road>();
        
        private CostMap _costMap;
        
        [Header("Debug")]
        [Tooltip("Assign a UI RawImage component here to visualize the cost map.")]
        public RawImage debugCostMapImage;

        [ContextMenu("Generate Road Network")]
        private void Generate()
        {
            Debug.Log("[Orchestrator] Starting generation process...");
            ClearPreviousNetwork();
            
            Debug.Log("[Orchestrator] Analysis Phase: Generating Cost Map...");
            var costMapGenerator = new CostMapGenerator();
            _costMap = costMapGenerator.Generate(this.terrain);

            if (_costMap == null)
            {
                Debug.LogError("[Orchestrator] Cost Map generation failed. Aborting road network generation.");
                return;
            }

            Debug.Log("[Orchestrator] Generation Phase: Initializing RandomWalkGenerator...");
            
            // --- UPDATED: Pass the new TotalBranchingChance ---
            var generator = new RandomWalkGenerator
            {
                Terrain = this.terrain,
                GlobalStepLimit = this.globalStepLimit,
                MaxStepsPerAgent = this.maxStepsPerAgent,
                StepSize = this.stepSize,
                MaxTurnAngle = this.maxTurnAngle,
                MaxSteepness = this.maxSteepness,
                
                // Pass the new branching value
                TotalBranchingChance = this.TotalBranchingChance,
                
                // Pass the safety values
                MaxGenerationTimeSeconds = this.maxGenerationTimeSeconds,
                MaxActiveAgents = this.maxActiveAgents
            };
            // --------------------------------------------------
            
            var result = generator.Generate();

            _intersections.AddRange(result.intersections);
            _roads.AddRange(result.roads);
        }

        // ... (O resto do arquivo permanece o mesmo: ClearPreviousNetwork, OnDrawGizmos, VisualizeCostMap) ...

        [ContextMenu("Clear Road Network")]
        private void ClearPreviousNetwork()
        {
            Debug.Log("[Orchestrator] Clearing previous network...");
            _intersections.Clear();
            _roads.Clear();
        }
        
        private void OnDrawGizmos()
        {
            if (_intersections == null || _roads == null) return;
            
            Gizmos.color = Color.white;
            foreach (var road in _roads)
            {
                Gizmos.DrawLine(road.StartNode.Position, road.EndNode.Position);
            }
            
            Gizmos.color = Color.red;
            foreach (var intersection in _intersections)
            {
                Gizmos.DrawSphere(intersection.Position, 1.0f);
            }
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
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (_costMap.GetCost(x, y) > maxCost)
                    {
                        maxCost = _costMap.GetCost(x, y);
                    }
                }
            }
            
            if (Mathf.Approximately(maxCost, 0)) maxCost = 1.0f;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cost = _costMap.GetCost(x, y);
                    float normalizedCost = cost / maxCost; 
                    Color pixelColor = new Color(normalizedCost, normalizedCost, normalizedCost);
                    costMapTexture.SetPixel(x, y, pixelColor);
                }
            }
            
            costMapTexture.Apply();
            
            debugCostMapImage.texture = costMapTexture;
            
            Debug.Log($"[Orchestrator] Cost map visualized. Max cost found: {maxCost}");
        }
    }
}