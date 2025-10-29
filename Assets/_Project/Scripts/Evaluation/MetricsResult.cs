using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace _Project.Scripts.Evaluation
{
    public class MetricsResult
    {
        // --- Efficiency ---
        public double GenerationTimeSeconds { get; set; }

        // --- Basic Counts ---
        public int IntersectionCount { get; set; }
        public int RoadCount { get; set; }

        // --- Road Lengths ---
        public float TotalRoadLength { get; set; }
        public float AverageRoadLength { get; set; }

        // --- Structural (Graph) ---
        public Dictionary<int, int> DegreeDistribution { get; set; } = new Dictionary<int, int>();

        // --- Geometric ---
        public double AverageCircuity { get; set; } = double.NaN;

        // --- Adaptability ---
        /// <summary>
        /// Average steepness (angle in degrees) of all road segments in the network.
        /// Lower values indicate better adaptation to flat terrain or contours.
        /// double.NaN if calculation failed or not applicable.
        /// </summary>
        public double AverageRoadSteepness { get; set; } = double.NaN; // Initialize to Not-a-Number

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Road Network Metrics ---");
            sb.AppendLine($"Generation Time: {GenerationTimeSeconds:F3} s");
            sb.AppendLine($"Intersections: {IntersectionCount}");
            sb.AppendLine($"Roads: {RoadCount}");
            if (RoadCount > 0)
            {
                sb.AppendLine($"Total Road Length: {TotalRoadLength:F1} units");
                sb.AppendLine($"Average Road Length: {AverageRoadLength:F1} units");
            }

            if (DegreeDistribution is { Count: > 0 })
            {
                sb.AppendLine("Degree Distribution (Degree: Count):");
                foreach (var kvp in DegreeDistribution.OrderBy(pair => pair.Key))
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                 sb.AppendLine("Degree Distribution: N/A");
            }

            sb.AppendLine(!double.IsNaN(AverageCircuity)
                ? $"Average Circuity (Detour Index): {AverageCircuity:F3}"
                : "Average Circuity (Detour Index): N/A");

            // --- NEW: Format Average Steepness ---
            sb.AppendLine(!double.IsNaN(AverageRoadSteepness)
                ? $"Average Road Steepness: {AverageRoadSteepness:F1} degrees"
                : "Average Road Steepness: N/A");
            // ------------------------------------

            sb.AppendLine("----------------------------");
            return sb.ToString();
        }
    }
}