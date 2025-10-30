using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core; // For Intersection/Road
using _Project.Scripts.Evaluation; // For MetricsResult
using System.Text; // For StringBuilder
using System.IO; // For File operations
using System; // For Enum
using System.Diagnostics; // For Stopwatch
using System.Linq; // For OrderBy
using _Project.Scripts.Generation.Abstractions; 

using Debug = UnityEngine.Debug;

namespace _Project.Scripts.Scenarios
{
    /// <summary>
    /// Orchestrates the execution of comparative experiments based on TCC Methodology (Section 7.6).
    /// This script runs each generation technique multiple times, collects all metrics,
    /// and saves the aggregated data to a CSV file for external analysis (e.g., in Python).
    /// </summary>
    public class ExperimentManager : MonoBehaviour
    {
        [Header("Experiment Configuration")]
        [Tooltip("Number of times to run each technique to ensure statistical robustness.")]
        [Range(1, 100)]
        public int repetitionsPerTechnique = 30;

        [Tooltip("The output filename (e.g., 'results.csv'). Saved in the project's root folder.")]
        public string outputFilename = "TCC_Experiment_Results.csv";

        [Header("System References")]
        [Tooltip("The main generator component that will be controlled by this experiment.")]
        public RoadNetworkGenerator roadNetworkGenerator;
        
        // Internal list to store all results before saving
        private readonly List<MetricsResult> _allResults = new List<MetricsResult>();
        private readonly StringBuilder _csvBuilder = new StringBuilder();

        /// <summary>
        /// Entry point to start the full experiment suite.
        /// </summary>
        [ContextMenu("Run All Experiments")]
        private void RunAllExperiments()
        {
            if (roadNetworkGenerator == null)
            {
                Debug.LogError("[ExperimentManager] RoadNetworkGenerator reference is not set! Aborting.", this);
                return;
            }

            Debug.Log($"[ExperimentManager] === STARTING EXPERIMENT SUITE ===");
            Debug.Log($"[ExperimentManager] Repetitions per technique: {repetitionsPerTechnique}");

            _allResults.Clear();
            _csvBuilder.Clear();
            _csvBuilder.AppendLine(GetCsvHeader()); // Add the header row to the CSV

            // Get all available techniques from the GenerationTechnique enum
            var allTechniques = (GenerationTechnique[])Enum.GetValues(typeof(GenerationTechnique));

            // TODO: Implement Scenario Iteration (Objective h)
            // For now, we run all experiments on the *currently active* terrain/scenario.
            string currentScenarioName = "DefaultScenario"; // Placeholder
            
            Debug.Log($"[ExperimentManager] Running on Scenario: {currentScenarioName}");

            foreach (var technique in allTechniques)
            {
                Debug.Log($"[ExperimentManager] --- Starting Technique: {technique} ---");
                for (int i = 0; i < repetitionsPerTechnique; i++)
                {
                    // Run and log a single experiment
                    RunSingleExperiment(technique, currentScenarioName, i + 1);
                }
                Debug.Log($"[ExperimentManager] --- Completed Technique: {technique} ---");
            }

            // After all experiments are done, save the results
            SaveResultsToCsv(); 
            
            Debug.Log($"[ExperimentManager] === EXPERIMENT SUITE FINISHED ===");
        }

        /// <summary>
        /// Runs a single generation pass for a given technique and logs its metrics.
        /// </summary>
        private void RunSingleExperiment(GenerationTechnique technique, string scenarioName, int repetition)
        {
            // 1. Configure the generator for this run
            roadNetworkGenerator.ClearPreviousNetwork();
            
            // 2. Initialize the correct generator algorithm
            IRoadNetworkGenerator generator;
            try
            {
                generator = roadNetworkGenerator.InitializeGenerator(technique);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExperimentManager] Failed to initialize generator {technique}: {e.Message}");
                return; // Skip this run
            }

            // 3. Measure Memory and Time
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long memoryBefore = GC.GetTotalMemory(true);

            Stopwatch generationStopwatch = new Stopwatch();
            generationStopwatch.Start();

            // 4. GENERATE
            (List<Intersection> intersections, List<Road> roads) result;
            try
            {
                result = generator.Generate();
            }
            catch (Exception e)
            {
                 Debug.LogError($"[ExperimentManager] Run {repetition}/{repetitionsPerTechnique} (Scenario: {scenarioName}, Technique: {technique}) FAILED during generation: {e.Message}");
                 return; // Skip this run
            }
            
            generationStopwatch.Stop();
            double elapsedSeconds = generationStopwatch.Elapsed.TotalSeconds;
            
            long memoryAfter = GC.GetTotalMemory(false);
            long memoryUsed = memoryAfter - memoryBefore;
            if (memoryUsed < 0) memoryUsed = 0;

            // 5. Calculate Metrics
            var calculator = new MetricsCalculator();
            var metrics = calculator.Calculate(result.intersections, result.roads, elapsedSeconds, memoryUsed);
            
            // 6. Log and Store Results
            string logMessage = $"[ExperimentManager] Run {repetition}/{repetitionsPerTechnique} (Scenario: {scenarioName}, Technique: {technique}) " +
                                $"| Time: {metrics.GenerationTimeSeconds:F3}s " +
                                $"| Mem: {(metrics.MemoryUsedBytes / 1024.0 / 1024.0):F2}MB " +
                                $"| V: {metrics.IntersectionCount} | E: {metrics.RoadCount}";
            Debug.Log(logMessage);
            
            _allResults.Add(metrics);
            
            // 7. Append data to CSV string builder
            _csvBuilder.AppendLine(FormatAsCsvRow(metrics, technique.ToString(), scenarioName, repetition));
        }

        /// <summary>
        /// Defines the column headers for the output CSV file.
        /// </summary>
        private string GetCsvHeader()
        {
            // --- Basic Info ---
            var headers = new List<string>
            {
                "Scenario", "Technique", "Repetition",
                "GenerationTime_s", "MemoryUsed_bytes",
                "IntersectionCount_V", "RoadCount_E",
                "TotalRoadLength", "AverageRoadLength",
                "Connectivity_Alpha", "Connectivity_Gamma",
                "AverageCircuity", "AverageRoadSteepness"
            };

            // --- Dynamic Headers for Dictionaries ---
            // Degree Distribution (e.g., "Degree_0", "Degree_1", "Degree_2", ...)
            for (int i = 0; i <= 10; i++) // Up to degree 10
            {
                headers.Add($"Degree_{i}_Count");
            }
            headers.Add("Degree_Other_Count"); // For > 10

            // Angle Distribution (e.g., "Angle_0_Count", "Angle_15_Count", ...)
            for (int angleBin = 0; angleBin < 180; angleBin += 15) // Using 15-degree bins
            {
                 headers.Add($"Angle_{angleBin}_Count");
            }
            
            return string.Join(",", headers);
        }

        /// <summary>
        /// Formats a single MetricsResult object into a CSV row string.
        /// </summary>
        private string FormatAsCsvRow(MetricsResult metrics, string technique, string scenario, int repetition)
        {
            var row = new List<string>
            {
                // Basic Info
                scenario,
                technique,
                repetition.ToString(),
                metrics.GenerationTimeSeconds.ToString("F6"), // Use high precision
                metrics.MemoryUsedBytes.ToString(),
                metrics.IntersectionCount.ToString(),
                metrics.RoadCount.ToString(),
                metrics.TotalRoadLength.ToString("F2"),
                metrics.AverageRoadLength.ToString("F2"),
                metrics.ConnectivityAlpha.ToString("F6"),
                metrics.ConnectivityGamma.ToString("F6"),
                metrics.AverageCircuity.ToString("F6"),
                metrics.AverageRoadSteepness.ToString("F2")
            };

            // --- Degree Distribution Data ---
            for (int i = 0; i <= 10; i++)
            {
                row.Add(metrics.DegreeDistribution.TryGetValue(i, out int count) ? count.ToString() : "0");
            }
            
            int otherDegreeCount = metrics.DegreeDistribution.Where(kvp => kvp.Key > 10).Sum(kvp => kvp.Value);
            row.Add(otherDegreeCount.ToString());

            // --- Angle Distribution Data ---
            for (int angleBin = 0; angleBin < 180; angleBin += 15) // Must match GetCsvHeader
            {
                row.Add(metrics.IntersectionAngleDistribution.TryGetValue(angleBin, out int angleCount) ? angleCount.ToString() : "0");
            }

            return string.Join(",", row);
        }

        /// <summary>
        /// Saves the collected data from the StringBuilder to a CSV file.
        /// </summary>
        private void SaveResultsToCsv()
        {
            // Application.dataPath points to the 'Assets' folder.
            string projectRootPath = Path.Combine(Application.dataPath, "..");
            
            // --- UPDATED: Define a dedicated output folder ---
            string outputDirectory = Path.Combine(projectRootPath, "ExperimentResults");

            // --- UPDATED: Ensure the directory exists ---
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    Debug.Log($"[ExperimentManager] Created output directory at: {outputDirectory}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExperimentManager] Failed to create output directory. Error: {e.Message}");
                return; // Abort saving
            }

            // Combine the directory and filename
            string fullPath = Path.Combine(outputDirectory, outputFilename);

            try
            {
                File.WriteAllText(fullPath, _csvBuilder.ToString());
                Debug.Log($"[ExperimentManager] Successfully saved results to: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExperimentManager] Failed to save CSV file at {fullPath}. Error: {e.Message}");
            }
        }
    }
}