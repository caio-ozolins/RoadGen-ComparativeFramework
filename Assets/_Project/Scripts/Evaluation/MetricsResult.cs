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
        /// <summary>
        /// Average ratio of network path distance to Euclidean distance between sampled pairs of intersections.
        /// Values >= 1.0. Lower is more direct. Double.NaN if calculation failed or not applicable.
        /// </summary>
        public double AverageCircuity { get; set; } = double.NaN; // Initialize to Not-a-Number

        // --- Adaptability ---
        // TODO: Add metrics like Average Steepness later

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

            // --- NEW: Format Circuity ---
            sb.AppendLine(!double.IsNaN(AverageCircuity)
                ? $"Average Circuity (Detour Index): {AverageCircuity:F3}"
                : "Average Circuity (Detour Index): N/A");
            // ---------------------------

            sb.AppendLine("----------------------------");
            return sb.ToString();
        }
    }
}