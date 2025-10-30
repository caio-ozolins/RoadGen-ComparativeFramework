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
        public int IntersectionCount { get; set; } // V (Vertices)
        public int RoadCount { get; set; } // E (Edges)

        // --- Road Lengths ---
        public float TotalRoadLength { get; set; }
        public float AverageRoadLength { get; set; }

        // --- Structural (Graph) ---
        public Dictionary<int, int> DegreeDistribution { get; set; } = new Dictionary<int, int>();
        
        /// <summary>
        /// Measures the ratio of actual circuits (cycles) to the maximum possible circuits in the network.
        /// Range: 0 (no circuits) to 1 (fully connected planar graph).
        /// Formula: (E - V + P) / (2V - 5P) (for planar graphs, P = connected components)
        /// </summary>
        public double ConnectivityAlpha { get; set; } = double.NaN;

        /// <summary>
        /// Measures the ratio of actual edges to the maximum possible edges in the network.
        /// Range: 0 (no edges) to 1 (fully connected planar graph).
        /// Formula: E / (3V - 6P) (for planar graphs, P = connected components)
        /// </summary>
        public double ConnectivityGamma { get; set; } = double.NaN;


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
            sb.AppendLine($"Intersections (V): {IntersectionCount}");
            sb.AppendLine($"Roads (E): {RoadCount}");
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
            
            // --- NEW: Format Connectivity Metrics ---
            sb.AppendLine(!double.IsNaN(ConnectivityAlpha)
                ? $"Connectivity (Alpha Index): {ConnectivityAlpha:F3}"
                : "Connectivity (Alpha Index): N/A");
            
            sb.AppendLine(!double.IsNaN(ConnectivityGamma)
                ? $"Connectivity (Gamma Index): {ConnectivityGamma:F3}"
                : "Connectivity (Gamma Index): N/A");
            // ----------------------------------------

            sb.AppendLine(!double.IsNaN(AverageCircuity)
                ? $"Average Circuity (Detour Index): {AverageCircuity:F3}"
                : "Average Circuity (Detour Index): N/A");

            sb.AppendLine(!double.IsNaN(AverageRoadSteepness)
                ? $"Average Road Steepness: {AverageRoadSteepness:F1} degrees"
                : "Average Road Steepness: N/A");

            sb.AppendLine("----------------------------");
            return sb.ToString();
        }
    }
}