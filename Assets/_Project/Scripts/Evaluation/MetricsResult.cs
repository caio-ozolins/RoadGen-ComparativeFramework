using System.Text; // For StringBuilder

namespace _Project.Scripts.Evaluation
{
    /// <summary>
    /// A simple data class to store the calculated metrics for a generated road network.
    /// </summary>
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
        // TODO: Add metrics like Degree Distribution later

        // --- Geometric ---
        // TODO: Add metrics like Circuity later

        // --- Adaptability ---
        // TODO: Add metrics like Average Steepness later

        /// <summary>
        /// Provides a formatted string representation of the metrics.
        /// </summary>
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
            sb.AppendLine("----------------------------");
            return sb.ToString();
        }
    }
}