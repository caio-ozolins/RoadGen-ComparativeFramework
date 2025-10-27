using System.Collections.Generic;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;
using _Project.Scripts.Generation.Analysis;
using _Project.Scripts.Generation.Pathfinding;
using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

using Debug = UnityEngine.Debug;

namespace _Project.Scripts.Generation
{
    public class PathBasedGenerator : IRoadNetworkGenerator
    {
        // --- Configuration (Set by the Orchestrator) ---
        public Terrain Terrain { get; set; }
        public CostMap CostMap { get; set; }
        public float MinimumSegmentLength { get; set; }
        public List<Vector2Int> PointsOfInterest { get; set; }

        // --- NEW: Parameters for Phase 2 ---
        public bool AddTransversalConnections { get; set; }
        public float MaxConnectionDistance { get; set; }
        public int MaxConnectionAttempts { get; set; }
        // ------------------------------------

        // --- Internal State ---
        private List<Intersection> _intersections;
        private List<Road> _roads;
        private int _nextAvailableId;
        private Dictionary<Vector2Int, Intersection> _gridPositionToIntersection;

        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
            _intersections = new List<Intersection>();
            _roads = new List<Road>();
            _nextAvailableId = 0;
            _gridPositionToIntersection = new Dictionary<Vector2Int, Intersection>();

            // --- Basic Validation ---
            if (CostMap == null || Terrain == null || PointsOfInterest == null) { /* Error Handling */ return (_intersections, _roads); }
            if (PointsOfInterest.Count < 2) { /* Error Handling */ return (_intersections, _roads); }
            if (MinimumSegmentLength <= 0) MinimumSegmentLength = 15.0f;
            if (MaxConnectionDistance <= 0) MaxConnectionDistance = 50.0f;
            if (MaxConnectionAttempts <= 0) MaxConnectionAttempts = 100;
             if (CostMap == null || Terrain == null || PointsOfInterest == null)
            {
                Debug.LogError("[PathBasedGenerator] CostMap, Terrain, or PointsOfInterest list is null. Aborting.");
                return (_intersections, _roads);
            }
            if (PointsOfInterest.Count < 2)
            {
                Debug.LogWarning("[PathBasedGenerator] Requires at least 2 Points of Interest to generate a network. Aborting.");
                return (_intersections, _roads);
            }


            // --- Phase 1: Build Radial Network ---
            var pathfinder = new AStarPathfinder();
            Vector2Int startPoi = PointsOfInterest[0];
            Debug.Log($"[PathBasedGenerator] Phase 1: Connecting {PointsOfInterest.Count - 1} POIs to hub {startPoi}...");
            for (int i = 1; i < PointsOfInterest.Count; i++)
            {
                Vector2Int endPoi = PointsOfInterest[i];
                List<Node> pathNodes = pathfinder.FindPath(startPoi.x, startPoi.y, endPoi.x, endPoi.y, CostMap);
                if (pathNodes is { Count: > 0 }) { BuildNetworkFromPath(pathNodes); }
                else { Debug.LogWarning($"[PathBasedGenerator] Phase 1: No path found between {startPoi} and {endPoi}."); }
            }
            Debug.Log($"[PathBasedGenerator] Phase 1 completed. Current network: {_intersections.Count} intersections, {_roads.Count} roads.");
            // ------------------------------------

            // --- Phase 2: Add Transversal Connections ---
            if (AddTransversalConnections && _intersections.Count > 1) // Need at least 2 intersections to connect
            {
                 Debug.Log($"[PathBasedGenerator] Phase 2: Attempting to add transversal connections (Max Attempts: {MaxConnectionAttempts}, Max Distance: {MaxConnectionDistance})...");
                 AddTransversalConnectionsPhase(pathfinder);
            }
            // -----------------------------------------

            Debug.Log($"[PathBasedGenerator] Network generation process finished. Final count: {_intersections.Count} intersections, {_roads.Count} roads created.");
            return (_intersections, _roads);
        }

        /// <summary>
        /// Phase 2: Attempts to find and add connections between existing, nearby intersections
        /// that are not already directly connected.
        /// </summary>
        private void AddTransversalConnectionsPhase(AStarPathfinder pathfinder)
        {
            int attempts = 0;
            int addedConnections = 0;

            // Simple random sampling approach
            while (attempts < MaxConnectionAttempts && _intersections.Count >= 2)
            {
                attempts++;

                // Pick two distinct random intersections
                int index1 = Random.Range(0, _intersections.Count);
                int index2 = Random.Range(0, _intersections.Count);
                if (index1 == index2) continue; // Skip if same intersection picked

                Intersection int1 = _intersections[index1];
                Intersection int2 = _intersections[index2];

                 // Check if already directly connected
                 bool alreadyConnected = _roads.Exists(r =>
                     (r.StartNode == int1 && r.EndNode == int2) ||
                     (r.StartNode == int2 && r.EndNode == int1));
                 if (alreadyConnected) continue;

                // Check distance
                float worldDistance = Vector3.Distance(int1.Position, int2.Position);
                if (worldDistance > 0 && worldDistance <= MaxConnectionDistance)
                {
                     // Convert world positions back to grid coordinates for A*
                    Vector2Int gridPos1 = ConvertWorldToGridPosition(int1.Position);
                    Vector2Int gridPos2 = ConvertWorldToGridPosition(int2.Position);

                    // Avoid pathfinding if grid points are identical (can happen due to discretization)
                    if (gridPos1 == gridPos2) continue;

                    // Find path
                    List<Node> pathNodes = pathfinder.FindPath(gridPos1.x, gridPos1.y, gridPos2.x, gridPos2.y, CostMap);

                    if (pathNodes is { Count: > 0 })
                    {
                        // TODO: Optional - Add check here: Is path length significantly longer than worldDistance? If so, skip.
                        // Example: float pathLength = CalculatePathWorldLength(pathNodes); if (pathLength > worldDistance * 1.5f) continue;

                        Debug.Log($"[PathBasedGenerator] Phase 2: Found potential connection between Intersection {int1.Id} and {int2.Id} (Distance: {worldDistance:F1}). Adding path segment...");
                        BuildNetworkFromPath(pathNodes); // Reuse existing build/merge logic
                        addedConnections++;
                    }
                }
            }
            Debug.Log($"[PathBasedGenerator] Phase 2 completed after {attempts} attempts. Added {addedConnections} new connection segments.");
        }

        // --- BuildNetworkFromPath remains largely the same, but benefits from the dictionary ---
        private void BuildNetworkFromPath(List<Node> path)
        {
            if (path.Count < 2) return;
            Intersection lastPlacedIntersection = GetOrCreateIntersectionAtNode(path[0]);
            float accumulatedDistance = 0f;
            Vector3 lastCheckedWorldPosition = lastPlacedIntersection.Position;

            for (int i = 1; i < path.Count; i++)
            {
                Node currentNode = path[i];
                Vector3 currentWorldPosition = ConvertNodeToWorldPosition(currentNode);
                float segmentDistance = Vector3.Distance(currentWorldPosition, lastCheckedWorldPosition);
                accumulatedDistance += segmentDistance;
                lastCheckedWorldPosition = currentWorldPosition;

                if (accumulatedDistance >= MinimumSegmentLength || i == path.Count - 1)
                {
                    Intersection currentIntersection = GetOrCreateIntersectionAtNode(currentNode);
                    if (currentIntersection != lastPlacedIntersection)
                    {
                         bool roadExists = _roads.Exists(r => (r.StartNode == lastPlacedIntersection && r.EndNode == currentIntersection) || (r.StartNode == currentIntersection && r.EndNode == lastPlacedIntersection));
                        if (!roadExists)
                        {
                            var newRoad = new Road(GetNextId(), lastPlacedIntersection, currentIntersection);
                            _roads.Add(newRoad);
                        }
                    }
                    lastPlacedIntersection = currentIntersection;
                    accumulatedDistance = 0f;
                }
            }
        }

        // --- GetOrCreateIntersectionAtNode remains the same ---
        private Intersection GetOrCreateIntersectionAtNode(Node node)
        {
             Vector2Int gridPos = new Vector2Int(node.X, node.Y);
             if (_gridPositionToIntersection.TryGetValue(gridPos, out Intersection existingIntersection)) { return existingIntersection; }
             else
             {
                 Vector3 worldPosition = ConvertNodeToWorldPosition(node);
                 var newIntersection = new Intersection(GetNextId(), worldPosition);
                 _intersections.Add(newIntersection);
                 _gridPositionToIntersection.Add(gridPos, newIntersection);
                 return newIntersection;
             }
        }

        // --- ConvertNodeToWorldPosition remains the same ---
        private Vector3 ConvertNodeToWorldPosition(Node node)
        {
            TerrainData terrainData = Terrain.terrainData;
            float normX = (float)node.X / (terrainData.heightmapResolution - 1);
            float normZ = (float)node.Y / (terrainData.heightmapResolution - 1);
            float worldX = (normX * terrainData.size.x) + Terrain.transform.position.x;
            float worldZ = (normZ * terrainData.size.z) + Terrain.transform.position.z;
            float worldY = Terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
            return new Vector3(worldX, worldY, worldZ);
        }

        // --- NEW: Helper to convert world position back to nearest grid coordinate ---
        private Vector2Int ConvertWorldToGridPosition(Vector3 worldPosition)
        {
             if (Terrain == null || Terrain.terrainData == null) return Vector2Int.zero;

             TerrainData terrainData = Terrain.terrainData;
             int gridWidth = terrainData.heightmapResolution;
             int gridHeight = terrainData.heightmapResolution;

             // Convert world coordinates to normalized terrain coordinates (0.0 to 1.0)
             float normX = (worldPosition.x - Terrain.transform.position.x) / terrainData.size.x;
             float normZ = (worldPosition.z - Terrain.transform.position.z) / terrainData.size.z; // World Z maps to Normalized Z

             // Convert normalized coordinates to grid coordinates (0 to gridWidth/Height - 1)
             int gridX = Mathf.RoundToInt(normX * (gridWidth - 1));
             int gridY = Mathf.RoundToInt(normZ * (gridHeight - 1)); // Normalized Z maps to Grid Y

             // Clamp to ensure values are within bounds
             gridX = Mathf.Clamp(gridX, 0, gridWidth - 1);
             gridY = Mathf.Clamp(gridY, 0, gridHeight - 1);

             return new Vector2Int(gridX, gridY);
        }
        // --------------------------------------------------------------------------

        private int GetNextId()
        {
            return _nextAvailableId++;
        }
    }
}