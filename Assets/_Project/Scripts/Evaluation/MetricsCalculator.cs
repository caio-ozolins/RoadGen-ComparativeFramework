using System.Collections.Generic;
using _Project.Scripts.Core; // Needed for Intersection and Road
using UnityEngine; // Needed for Vector3.Distance

namespace _Project.Scripts.Evaluation
{
    /// <summary>
    /// Calculates various metrics based on a generated road network.
    /// </summary>
    public class MetricsCalculator
    {
        /// <summary>
        /// Calculates metrics for a given road network.
        /// </summary>
        /// <param name="intersections">The list of generated intersections.</param>
        /// <param name="roads">The list of generated roads.</param>
        /// <param name="generationTimeSeconds">The time taken to generate the network.</param>
        /// <returns>A MetricsResult object containing the calculated values.</returns>
        public MetricsResult Calculate(List<Intersection> intersections, List<Road> roads, double generationTimeSeconds)
        {
            var result = new MetricsResult
            {
                // --- Efficiency ---
                GenerationTimeSeconds = generationTimeSeconds,
                // --- Basic Counts ---
                IntersectionCount = intersections?.Count ?? 0,
                RoadCount = roads?.Count ?? 0
            };

            // --- Road Lengths ---
            if (result.RoadCount > 0)
            {
                if (roads != null)
                {
                    result.TotalRoadLength = 0f;
                    foreach (var road in roads)
                    {
                        if (road is { StartNode: not null, EndNode: not null })
                        {
                            result.TotalRoadLength += Vector3.Distance(road.StartNode.Position, road.EndNode.Position);
                        }
                    }
                    // Avoid division by zero if somehow RoadCount > 0 but no valid roads were found
                    result.AverageRoadLength = result.RoadCount > 0 ? result.TotalRoadLength / result.RoadCount : 0f;
                }
                else
                {
                    result.TotalRoadLength = 0f;
                    result.AverageRoadLength = 0f;
                }
            }
            else
            {
                result.TotalRoadLength = 0f;
                result.AverageRoadLength = 0f;
            }

            // --- NEW: Calculate Degree Distribution ---
            result.DegreeDistribution = CalculateDegreeDistribution(intersections, roads);
            // ----------------------------------------

            // --- Placeholder for future metrics ---
            // result.Circuity = CalculateCircuity(...);
            // result.AverageSteepness = CalculateAverageSteepness(...);

            return result;
        }

        // --- NEW: Helper method for Degree Distribution ---
        /// <summary>
        /// Calculates the distribution of node degrees (number of roads connected to each intersection).
        /// </summary>
        /// <param name="intersections">List of intersections.</param>
        /// <param name="roads">List of roads.</param>
        /// <returns>A dictionary where Key = Degree, Value = Count of intersections with that degree.</returns>
        private Dictionary<int, int> CalculateDegreeDistribution(List<Intersection> intersections, List<Road> roads)
        {
            var distribution = new Dictionary<int, int>();
            if (intersections == null || roads == null || intersections.Count == 0)
            {
                return distribution; // Return empty if no data
            }

            // 1. Count the degree for each intersection ID
            var intersectionDegrees = new Dictionary<int, int>();
            foreach (var intersection in intersections)
            {
                intersectionDegrees[intersection.Id] = 0; // Initialize all degrees to 0
            }

            foreach (var road in roads)
            {
                if (road?.StartNode != null)
                {
                    if (intersectionDegrees.ContainsKey(road.StartNode.Id))
                    {
                        intersectionDegrees[road.StartNode.Id]++;
                    }
                }
                if (road?.EndNode != null)
                {
                     if (intersectionDegrees.ContainsKey(road.EndNode.Id))
                     {
                        intersectionDegrees[road.EndNode.Id]++;
                     }
                }
            }

            // 2. Aggregate the counts into the final distribution
            foreach (var degree in intersectionDegrees.Values)
            {
                if (!distribution.TryAdd(degree, 1))
                {
                    distribution[degree]++;
                }
            }

            return distribution;
        }
        // -------------------------------------------------

        // --- TODO: Add helper methods for more complex metrics later ---
        /*
        private float CalculateCircuity(...) { ... }
        private float CalculateAverageSteepness(...) { ... }
        */
    }
}