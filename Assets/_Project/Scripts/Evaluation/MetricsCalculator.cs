using System.Collections.Generic;
using _Project.Scripts.Core;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Evaluation
{
    public class MetricsCalculator
    {
        private const int CircuitySampleCount = 100;

        public MetricsResult Calculate(List<Intersection> intersections, List<Road> roads, double generationTimeSeconds)
        {
            var result = new MetricsResult
            {
                GenerationTimeSeconds = generationTimeSeconds,
                IntersectionCount = intersections?.Count ?? 0,
                RoadCount = roads?.Count ?? 0
            };

            // --- Basic Counts ---
            int v = result.IntersectionCount; // Vertices
            int e = result.RoadCount; // Edges

            // Road Lengths
            if (e > 0)
            {
                if (roads != null)
                {
                    result.TotalRoadLength = 0f;
                    foreach (var road in roads) { if (road is { StartNode: not null, EndNode: not null }) { result.TotalRoadLength += Vector3.Distance(road.StartNode.Position, road.EndNode.Position); } }
                    result.AverageRoadLength = result.RoadCount > 0 ? result.TotalRoadLength / result.RoadCount : 0f;
                }
                else { result.TotalRoadLength = 0f; result.AverageRoadLength = 0f; }
            }
            else { result.TotalRoadLength = 0f; result.AverageRoadLength = 0f; }

            // Degree Distribution
            result.DegreeDistribution = CalculateDegreeDistribution(intersections, roads);

            // --- Calculate Connectivity Metrics (Alpha & Gamma) ---
            if (v > 0 && e > 0)
            {
                // P (Connected Components)
                int p = CalculateConnectedComponents(intersections, roads);

                // Alpha Index (Planar Graph): (E - V + P) / (2V - 5P)
                double alphaDenominator = (2 * v) - (5 * p);
                if (alphaDenominator > 0)
                {
                    result.ConnectivityAlpha = (e - v + p) / alphaDenominator;
                }
                else
                {
                    result.ConnectivityAlpha = (e - v + p == 0) ? 0 : double.NaN;
                }

                // Gamma Index (Planar Graph): E / (3V - 6P)
                double gammaDenominator = (3 * v) - (6 * p);
                if (gammaDenominator > 0)
                {
                    result.ConnectivityGamma = e / gammaDenominator;
                }
                else
                {
                    result.ConnectivityGamma = double.NaN;
                }
            }
            else
            {
                result.ConnectivityAlpha = (v == 0 && e == 0) ? 0 : double.NaN;
                result.ConnectivityGamma = (v == 0 && e == 0) ? 0 : double.NaN;
            }
            // -----------------------------------------------------------

            // Circuity
            result.AverageCircuity = CalculateCircuity(intersections, roads);

            // Average Steepness
            result.AverageRoadSteepness = CalculateAverageSteepness(roads);
            
            return result;
        }

        private Dictionary<int, int> CalculateDegreeDistribution(List<Intersection> intersections, List<Road> roads)
        {
             var distribution = new Dictionary<int, int>();
             if (intersections == null || roads == null || intersections.Count == 0) { return distribution; }
             var intersectionDegrees = new Dictionary<int, int>();
             foreach (var intersection in intersections) { intersectionDegrees[intersection.Id] = 0; }
             foreach (var road in roads)
             {
                 if (road?.StartNode != null) { if (intersectionDegrees.ContainsKey(road.StartNode.Id)) { intersectionDegrees[road.StartNode.Id]++; } }
                 if (road?.EndNode != null) { if (intersectionDegrees.ContainsKey(road.EndNode.Id)) { intersectionDegrees[road.EndNode.Id]++; } }
             }
             foreach (var degree in intersectionDegrees.Values) { if (!distribution.TryAdd(degree, 1)) { distribution[degree]++; } }
             return distribution;
        }

        private double CalculateCircuity(List<Intersection> intersections, List<Road> roads)
        {
             if (intersections == null || roads == null || intersections.Count < 2 || roads.Count == 0) { return double.NaN; }
             
             // Build Adjacency List (for Dijkstra)
             var adjacencyList = new Dictionary<int, List<(int, float)>>();
             foreach(var i in intersections) adjacencyList[i.Id] = new List<(int, float)>();
             foreach(var road in roads)
             {
                 if(road is { StartNode: not null, EndNode: not null })
                 {
                     float length = Vector3.Distance(road.StartNode.Position, road.EndNode.Position);
                     if (length > 0) { adjacencyList[road.StartNode.Id].Add((road.EndNode.Id, length)); adjacencyList[road.EndNode.Id].Add((road.StartNode.Id, length)); }
                 }
             }
             
             List<double> sampleRatios = new List<double>();
             int validSamples = 0;
             for (int i = 0; i < CircuitySampleCount && validSamples < CircuitySampleCount; i++)
             {
                 int index1 = Random.Range(0, intersections.Count); int index2 = Random.Range(0, intersections.Count);
                 if (index1 == index2) continue;
                 Intersection start = intersections[index1]; Intersection end = intersections[index2];
                 float euclideanDistance = Vector3.Distance(start.Position, end.Position);
                 if (euclideanDistance <= 0.01f) continue;
                 float networkDistance = CalculateShortestPathDistance(start.Id, end.Id, adjacencyList);
                 if (networkDistance >= 0) { double ratio = networkDistance / euclideanDistance; if (ratio >= 0.99) { sampleRatios.Add(ratio); validSamples++; } }
             }
             
             if (sampleRatios.Count > 0) { return sampleRatios.Average(); }
             else { Debug.LogWarning("[MetricsCalculator] Could not calculate circuity: No valid paths found between sampled points."); return double.NaN; }
        }

        private float CalculateShortestPathDistance(int startId, int endId, Dictionary<int, List<(int, float)>> adjacencyList)
        {
            if (!adjacencyList.ContainsKey(startId) || !adjacencyList.ContainsKey(endId)) return -1f;
            var distances = new Dictionary<int, float>(); var priorityQueue = new List<int>();
            foreach (var nodeId in adjacencyList.Keys) { distances[nodeId] = float.MaxValue; priorityQueue.Add(nodeId); }
            distances[startId] = 0;
            while (priorityQueue.Count > 0)
            {
                priorityQueue.Sort((x, y) => distances[x].CompareTo(distances[y])); int currentId = priorityQueue[0]; priorityQueue.RemoveAt(0);
                if (currentId == endId) { return distances[endId] >= float.MaxValue ? -1f : distances[endId]; }
                if (distances[currentId] >= float.MaxValue) break;
                if (adjacencyList.TryGetValue(currentId, out var value))
                {
                    foreach (var (neighborId, edgeLength) in value)
                    {
                        if (priorityQueue.Contains(neighborId))
                        {
                            float altDistance = distances[currentId] + edgeLength;
                            if (altDistance < distances[neighborId]) { distances[neighborId] = altDistance; }
                        }
                    }
                }
            }
            return -1f;
        }
        
        private double CalculateAverageSteepness(List<Road> roads)
        {
            if (roads == null || roads.Count == 0)
            {
                return double.NaN; // Not applicable
            }

            double totalSteepness = 0;
            int validRoadCount = 0;

            foreach (var road in roads)
            {
                if (road is { StartNode: not null, EndNode: not null })
                {
                    Vector3 startPos = road.StartNode.Position;
                    Vector3 endPos = road.EndNode.Position;
                    
                    float dx = endPos.x - startPos.x;
                    float dz = endPos.z - startPos.z;
                    float horizontalDistance = Mathf.Sqrt(dx * dx + dz * dz);
                    float verticalDistance = Mathf.Abs(endPos.y - startPos.y);

                    if (horizontalDistance > 0.001f)
                    {
                        float angleRad = Mathf.Atan2(verticalDistance, horizontalDistance);
                        float angleDeg = angleRad * Mathf.Rad2Deg;
                        totalSteepness += angleDeg;
                        validRoadCount++;
                    }
                }
            }

            if (validRoadCount > 0)
            {
                return totalSteepness / validRoadCount;
            }
            else
            {
                return double.NaN; // No valid roads found to calculate steepness
            }
        }
        
        /// <summary>
        /// Calculates the number of connected components (P) in the graph.
        /// Uses Breadth-First Search (BFS) to traverse the graph.
        /// </summary>
        private int CalculateConnectedComponents(List<Intersection> intersections, List<Road> roads)
        {
            if (intersections == null || intersections.Count == 0) return 0;

            // Build a simple adjacency list (neighbor IDs only)
            var adjacencyList = new Dictionary<int, List<int>>();
            var allIntersectionIds = new HashSet<int>();
            
            foreach (var intersection in intersections)
            {
                adjacencyList[intersection.Id] = new List<int>();
                allIntersectionIds.Add(intersection.Id);
            }

            if (roads != null)
            {
                foreach (var road in roads)
                {
                    if (road is { StartNode: not null, EndNode: not null })
                    {
                        if (allIntersectionIds.Contains(road.StartNode.Id) && allIntersectionIds.Contains(road.EndNode.Id))
                        {
                            adjacencyList[road.StartNode.Id].Add(road.EndNode.Id);
                            adjacencyList[road.EndNode.Id].Add(road.StartNode.Id);
                        }
                    }
                }
            }

            var visitedIntersectionIds = new HashSet<int>();
            int componentCount = 0;

            foreach (var intersection in intersections)
            {
                if (!visitedIntersectionIds.Contains(intersection.Id))
                {
                    componentCount++;
                    
                    var queue = new Queue<int>();
                    queue.Enqueue(intersection.Id);
                    visitedIntersectionIds.Add(intersection.Id);

                    while (queue.Count > 0)
                    {
                        int currentId = queue.Dequeue();
                        
                        if (adjacencyList.TryGetValue(currentId, out var neighbors))
                        {
                            foreach (int neighborId in neighbors)
                            {
                                // Add() returns 'true' if the item was new (i.e., not visited).
                                if (visitedIntersectionIds.Add(neighborId))
                                {
                                    queue.Enqueue(neighborId);
                                }
                            }
                        }
                    }
                }
            }
            
            return componentCount;
        }
    }
}