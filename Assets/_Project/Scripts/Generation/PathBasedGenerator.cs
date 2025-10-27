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
        public Vector2Int StartPoint { get; set; }
        public Vector2Int EndPoint { get; set; }
        
        // --- NEW: Minimum Segment Length ---
        public float MinimumSegmentLength { get; set; }

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
            
            // Set a default minimum length if none was provided
            if (MinimumSegmentLength <= 0) MinimumSegmentLength = 15.0f; 

            // 1. Find the optimal path
            Debug.Log($"[PathBasedGenerator] Finding path from grid coordinates Start={StartPoint} to End={EndPoint}...");
            var pathfinder = new AStarPathfinder();
            List<Node> pathNodes = pathfinder.FindPath(StartPoint.x, StartPoint.y, EndPoint.x, EndPoint.y, CostMap);

            if (pathNodes == null)
            {
                Debug.LogError($"[PathBasedGenerator] A* search failed completely and returned null. No path found between {StartPoint} and {EndPoint}.");
            }
            else if (pathNodes.Count == 0)
            {
                Debug.LogWarning($"[PathBasedGenerator] A* search completed but returned an empty path. No path found between {StartPoint} and {EndPoint}.");
            }
            else
            {
                Debug.Log($"[PathBasedGenerator] A* search successful! Path found with {pathNodes.Count} nodes.");
            }

            // 2. Build the network from the path
            if (pathNodes is { Count: > 0 }) 
            {
                Debug.Log($"[PathBasedGenerator] Building network along the path with minimum segment length: {MinimumSegmentLength}...");
                BuildNetworkFromPath(pathNodes);
                Debug.Log($"[PathBasedGenerator] Network build completed. {_intersections.Count} intersections, {_roads.Count} roads created.");
            }
            
            return (_intersections, _roads);
        }

        /// <summary>
        /// Iterates over a list of path nodes, creating Intersection and Road objects
        /// while ensuring a minimum distance between intersections.
        /// </summary>
        private void BuildNetworkFromPath(List<Node> path)
        {
            if (path.Count < 2)
            {
                Debug.LogWarning("[PathBasedGenerator] Path is too short to build a network (requires at least 2 nodes).");
                return;
            }

            // --- UPDATED LOGIC: Path Simplification ---
            
            // Start with the first node always
            Intersection lastPlacedIntersection = CreateIntersectionAtNode(path[0]);
            float accumulatedDistance = 0f;
            Vector3 lastCheckedWorldPosition = lastPlacedIntersection.Position;

            for (int i = 1; i < path.Count; i++)
            {
                Node currentNode = path[i];
                Vector3 currentWorldPosition = ConvertNodeToWorldPosition(currentNode);
                
                // Calculate distance from the last *checked* A* node position
                float segmentDistance = Vector3.Distance(currentWorldPosition, lastCheckedWorldPosition);
                accumulatedDistance += segmentDistance;

                // Update the last checked position for the next iteration's distance calculation
                lastCheckedWorldPosition = currentWorldPosition;

                // Place an intersection if the accumulated distance is sufficient
                // OR if it's the very last node in the A* path
                if (accumulatedDistance >= MinimumSegmentLength || i == path.Count - 1)
                {
                    Intersection newIntersection = CreateIntersectionAtNode(currentNode);
                    
                    // Create road segment connecting the last *placed* intersection to this new one
                    var newRoad = new Road(GetNextId(), lastPlacedIntersection, newIntersection);
                    _roads.Add(newRoad);

                    // Update tracking variables
                    lastPlacedIntersection = newIntersection;
                    accumulatedDistance = 0f; // Reset distance accumulator
                }
            }
            // ------------------------------------------
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