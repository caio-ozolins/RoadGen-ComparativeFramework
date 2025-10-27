using System.Collections.Generic;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;
using _Project.Scripts.Generation.Analysis;
using _Project.Scripts.Generation.Pathfinding;
using UnityEngine;

using Debug = UnityEngine.Debug; 

namespace _Project.Scripts.Generation
{
    /// <summary>
    /// Implements a road network generator using a top-down, planned approach.
    /// ... (existing documentation) ...
    /// </summary>
    public class PathBasedGenerator : IRoadNetworkGenerator
    {
        // --- Configuration (Set by the Orchestrator) ---
        public Terrain Terrain { get; set; }
        public CostMap CostMap { get; set; }
        
        // --- Path Parameters ---
        public Vector2Int StartPoint { get; set; }
        public Vector2Int EndPoint { get; set; }
        
        // --- Internal State ---
        private List<Intersection> _intersections;
        private List<Road> _roads;
        private int _nextAvailableId;

        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
            _intersections = new List<Intersection>();
            _roads = new List<Road>();
            _nextAvailableId = 0;

            if (CostMap == null || Terrain == null)
            {
                Debug.LogError("[PathBasedGenerator] CostMap or Terrain is null. Aborting.");
                return (_intersections, _roads);
            }

            // 1. Find the optimal path
            Debug.Log($"[PathBasedGenerator] Finding path from {StartPoint} to {EndPoint}...");
            var pathfinder = new AStarPathfinder();
            List<Node> pathNodes = pathfinder.FindPath(StartPoint.x, StartPoint.y, EndPoint.x, EndPoint.y, CostMap);

            // 2. Build the network from the path
            if (pathNodes is { Count: > 0 })
            {
                Debug.Log($"[PathBasedGenerator] Path found with {pathNodes.Count} nodes. Building network...");
                BuildNetworkFromPath(pathNodes);
            }
            else
            {
                Debug.LogWarning($"[PathBasedGenerator] No path found between {StartPoint} and {EndPoint}.");
            }

            return (_intersections, _roads);
        }

        /// <summary>
        /// Iterates over a list of path nodes and creates the corresponding
        /// Intersection and Road objects in the world.
        /// </summary>
        /// <param name="path">The list of nodes returned by the A* algorithm.</param>
        private void BuildNetworkFromPath(List<Node> path)
        {
            if (path.Count < 2)
            {
                Debug.LogWarning("[PathBasedGenerator] Path is too short to build a network (requires at least 2 nodes).");
                return;
            }

            // Create the very first intersection from the start of the path
            Intersection previousIntersection = CreateIntersectionAtNode(path[0]);
            
            // Loop through the rest of the nodes to create segments
            for (int i = 1; i < path.Count; i++)
            {
                Intersection currentIntersection = CreateIntersectionAtNode(path[i]);
                
                // Create a road connecting the previous node to the current one
                var newRoad = new Road(GetNextId(), previousIntersection, currentIntersection);
                _roads.Add(newRoad);

                // The current node becomes the previous node for the next iteration
                previousIntersection = currentIntersection;
            }
        }
        
        /// <summary>
        /// Helper method to create an Intersection object at a node's location.
        /// Handles coordinate conversion and adds the intersection to the list.
        /// </summary>
        private Intersection CreateIntersectionAtNode(Node node)
        {
            Vector3 worldPosition = ConvertNodeToWorldPosition(node);
            var newIntersection = new Intersection(GetNextId(), worldPosition);
            _intersections.Add(newIntersection);
            return newIntersection;
        }

        /// <summary>
        /// Converts a node's grid coordinates (e.g., [12, 34]) into
        /// world-space coordinates (Vector3), including correct terrain height.
        /// </summary>
        private Vector3 ConvertNodeToWorldPosition(Node node)
        {
            TerrainData terrainData = Terrain.terrainData;
            
            // 1. Normalize node coordinates (from 0 to CostMap.Width-1) to normalized terrain coordinates (0.0 to 1.0)
            // We use heightmapResolution as it matches the CostMap dimensions
            float normX = (float)node.X / (terrainData.heightmapResolution - 1);
            float normZ = (float)node.Y / (terrainData.heightmapResolution - 1); // Node Y maps to Terrain Z

            // 2. Convert normalized coordinates to world coordinates
            float worldX = (normX * terrainData.size.x) + Terrain.transform.position.x;
            float worldZ = (normZ * terrainData.size.z) + Terrain.transform.position.z;
            
            // 3. Sample the terrain height at that world coordinate
            float worldY = Terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

            return new Vector3(worldX, worldY, worldZ);
        }
        
        private int GetNextId()
        {
            return _nextAvailableId++;
        }
    }
}