using System.Collections.Generic;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;
using _Project.Scripts.Generation.Analysis;
using _Project.Scripts.Generation.Pathfinding;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace _Project.Scripts.Generation
{
    public class PathBasedGenerator : IRoadNetworkGenerator
    {
        // --- Configuration (Set by the Orchestrator) ---
        public Terrain Terrain { get; set; }
        public CostMap CostMap { get; set; }
        public float MinimumSegmentLength { get; set; }

        // --- UPDATED: Use List of POIs ---
        public List<Vector2Int> PointsOfInterest { get; set; }
        // ----------------------------------

        // --- Internal State ---
        private List<Intersection> _intersections;
        private List<Road> _roads;
        private int _nextAvailableId;
        // --- NEW: Dictionary to track existing intersections by grid position ---
        private Dictionary<Vector2Int, Intersection> _gridPositionToIntersection;
        // --------------------------------------------------------------------

        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
            _intersections = new List<Intersection>();
            _roads = new List<Road>();
            _nextAvailableId = 0;
            // --- NEW: Initialize the dictionary ---
            _gridPositionToIntersection = new Dictionary<Vector2Int, Intersection>();
            // ------------------------------------

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

            // Set a default minimum length if none was provided
            if (MinimumSegmentLength <= 0) MinimumSegmentLength = 15.0f;

            // --- UPDATED: Connect POIs ---
            var pathfinder = new AStarPathfinder();
            Vector2Int startPoi = PointsOfInterest[0]; // Assume the first POI (center) is the main hub

            Debug.Log($"[PathBasedGenerator] Connecting {PointsOfInterest.Count - 1} POIs to the central hub at {startPoi}...");

            for (int i = 1; i < PointsOfInterest.Count; i++)
            {
                Vector2Int endPoi = PointsOfInterest[i];
                Debug.Log($"[PathBasedGenerator] Finding path from {startPoi} to {endPoi}...");

                List<Node> pathNodes = pathfinder.FindPath(startPoi.x, startPoi.y, endPoi.x, endPoi.y, CostMap);

                if (pathNodes is { Count: > 0 })
                {
                    Debug.Log($"[PathBasedGenerator] Path found ({pathNodes.Count} nodes). Building/Merging network segment...");
                    BuildNetworkFromPath(pathNodes);
                }
                else
                {
                    Debug.LogWarning($"[PathBasedGenerator] No path found between {startPoi} and {endPoi}. Skipping this connection.");
                }
            }
            Debug.Log($"[PathBasedGenerator] Network generation process finished. Final count: {_intersections.Count} intersections, {_roads.Count} roads created.");
            // -----------------------------

            return (_intersections, _roads);
        }

        /// <summary>
        /// Iterates over a list of path nodes, creating/reusing Intersection objects
        /// and creating Road objects, ensuring minimum distance and merging with existing network.
        /// </summary>
        private void BuildNetworkFromPath(List<Node> path)
        {
             // Need at least two nodes (start and one more) to form a segment
            if (path.Count < 2) return;

            // --- UPDATED LOGIC: Path Simplification AND Merging ---

            // Get or create the starting intersection for this path segment
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

                // Place/Get an intersection if distance is sufficient OR it's the last node
                if (accumulatedDistance >= MinimumSegmentLength || i == path.Count - 1)
                {
                    // --- UPDATED: Use GetOrCreate ---
                    Intersection currentIntersection = GetOrCreateIntersectionAtNode(currentNode);
                    // -----------------------------

                    // Avoid creating zero-length roads if the same intersection was retrieved
                    if (currentIntersection != lastPlacedIntersection)
                    {
                         // Check if a road between these two intersections already exists (in either direction)
                         // Simple check, could be optimized later if needed
                         bool roadExists = _roads.Exists(r =>
                             (r.StartNode == lastPlacedIntersection && r.EndNode == currentIntersection) ||
                             (r.StartNode == currentIntersection && r.EndNode == lastPlacedIntersection));

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
            // ------------------------------------------
        }

        /// <summary>
        /// Gets an existing Intersection at the node's grid position or creates a new one if none exists.
        /// Handles coordinate conversion and adds the intersection to the list and dictionary if new.
        /// </summary>
        private Intersection GetOrCreateIntersectionAtNode(Node node)
        {
            Vector2Int gridPos = new Vector2Int(node.X, node.Y);

            // Check if an intersection already exists at this grid position
            if (_gridPositionToIntersection.TryGetValue(gridPos, out Intersection existingIntersection))
            {
                return existingIntersection; // Reuse existing
            }
            else
            {
                // Create new intersection
                Vector3 worldPosition = ConvertNodeToWorldPosition(node);
                var newIntersection = new Intersection(GetNextId(), worldPosition);
                _intersections.Add(newIntersection);
                _gridPositionToIntersection.Add(gridPos, newIntersection); // Add to dictionary
                return newIntersection;
            }
        }

        /// <summary>
        /// Converts a node's grid coordinates into world-space coordinates.
        /// </summary>
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

        private int GetNextId()
        {
            return _nextAvailableId++;
        }
    }
}