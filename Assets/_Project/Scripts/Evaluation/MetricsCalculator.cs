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
            var result = new MetricsResult();

            // --- Efficiency ---
            result.GenerationTimeSeconds = generationTimeSeconds;

            // --- Basic Counts ---
            result.IntersectionCount = intersections?.Count ?? 0;
            result.RoadCount = roads?.Count ?? 0;

            // --- Road Lengths ---
            if (result.RoadCount > 0)
            {
                // --- ADDED NULL CHECK ---
                if (roads != null) // Explicitly check if the list itself is not null
                {
                    result.TotalRoadLength = 0f;
                    foreach (var road in roads) // Now this loop is safe
                    {
                        // Keep the inner null checks for road and its nodes
                        if (road?.StartNode != null && road.EndNode != null)
                        {
                            result.TotalRoadLength += Vector3.Distance(road.StartNode.Position, road.EndNode.Position);
                        }
                    }
                    result.AverageRoadLength = result.TotalRoadLength / result.RoadCount;
                }
                else // This case should technically not be reachable if RoadCount > 0, but handles the warning
                {
                    result.TotalRoadLength = 0f;
                    result.AverageRoadLength = 0f;
                    // Optionally log a warning here if this unexpected state occurs
                    // Debug.LogWarning("[MetricsCalculator] RoadCount > 0 but roads list is null!");
                }
                // --- END OF ADDED CHECK ---
            }
            else
            {
                result.TotalRoadLength = 0f;
                result.AverageRoadLength = 0f;
            }

            // ... (rest of the method) ...

            return result;
        }
        
        // --- TODO: Add helper methods for more complex metrics later ---
        /*
        private Dictionary<int, int> CalculateDegreeDistribution(List<Intersection> intersections, List<Road> roads)
        {
            // Implementation...
        }
        */
    }
}