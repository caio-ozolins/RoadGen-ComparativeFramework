using System.Collections.Generic;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;
using UnityEngine;
using System.Text; // For StringBuilder

// Alias for Debug
using Debug = UnityEngine.Debug;

namespace _Project.Scripts.Generation
{
    /// <summary>
    /// Implements a road network generator using L-systems (Lindenmayer systems).
    /// This technique uses string rewriting rules to generate fractal-like patterns.
    /// Represents the "L-systems" approach for the comparative analysis.
    /// </summary>
    public class LSystemGenerator : IRoadNetworkGenerator
    {
        // --- L-System Parameters (Set by Orchestrator) ---
        public Terrain Terrain { get; set; }
        public string Axiom { get; set; } = "F"; // Starting string
        public Dictionary<char, string> Rules { get; set; } = new Dictionary<char, string>(); // Rewriting rules
        public int Iterations { get; set; } = 3; // Number of times to apply rules

        // --- Turtle Parameters (Set by Orchestrator) ---
        public float SegmentLength { get; set; } = 15f; // Similar to StepSize or MinimumSegmentLength
        public float TurnAngle { get; set; } = 25f; // Angle in degrees for '+' and '-' commands

        // --- Internal State ---
        private List<Intersection> _intersections;
        private List<Road> _roads;
        private int _nextAvailableId;
        private Dictionary<Vector3, Intersection> _positionToIntersection; // For merging

        // --- Turtle State ---
        private Vector3 _currentPosition;
        private float _currentAngle; // Degrees
        private Stack<(Vector3 position, float angle)> _stateStack;


        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
            _intersections = new List<Intersection>();
            _roads = new List<Road>();
            _nextAvailableId = 0;
            _positionToIntersection = new Dictionary<Vector3, Intersection>();
            _stateStack = new Stack<(Vector3 position, float angle)>();

            if (Terrain == null)
            {
                Debug.LogError("[LSystemGenerator] Terrain is null. Aborting.");
                return (_intersections, _roads);
            }
            if (Rules == null || Rules.Count == 0)
            {
                 Debug.LogWarning("[LSystemGenerator] No rules provided. Using default simple rule F -> F+F-F.");
                 // Example simple rule: F -> F+F-F (creates a simple branching structure)
                 Rules = new Dictionary<char, string> { { 'F', "F+F-F" } };
                 // You might want more complex rules like:
                 // { 'X', "F+[[X]-X]-F[-FX]+X" }, { 'F', "FF" } with Axiom "X" for a plant-like structure.
            }


            // 1. Generate the L-system string
            Debug.Log($"[LSystemGenerator] Generating L-system string. Axiom='{Axiom}', Iterations={Iterations}");
            string lSystemString = GenerateString();
            Debug.Log($"[LSystemGenerator] Generated string length: {lSystemString.Length}");
             // Debug.Log($"[LSystemGenerator] String: {lSystemString}"); // Uncomment for debugging small strings

            // 2. Interpret the string to build the network
            Debug.Log("[LSystemGenerator] Interpreting string to build network...");
            InterpretString(lSystemString);

            Debug.Log($"[LSystemGenerator] Network build completed. {_intersections.Count} intersections, {_roads.Count} roads created.");
            return (_intersections, _roads);
        }

        /// <summary>
        /// Generates the final L-system string by applying rules iteratively.
        /// </summary>
        private string GenerateString()
        {
            string currentString = Axiom;
            StringBuilder nextString = new StringBuilder();

            for (int i = 0; i < Iterations; i++)
            {
                nextString.Clear();
                foreach (char c in currentString)
                {
                    // If a rule exists for this character, apply it. Otherwise, keep the character.
                    nextString.Append(Rules.ContainsKey(c) ? Rules[c] : c.ToString());
                }
                currentString = nextString.ToString();
                 // Optional: Add a safety break for extremely long strings
                 // if (currentString.Length > 100000) { Debug.LogWarning($"[LSystemGenerator] String length exceeded limit at iteration {i}. Stopping early."); break; }
            }
            return currentString;
        }

        /// <summary>
        /// Interprets the L-system string commands using a "turtle" graphics approach.
        /// </summary>
        private void InterpretString(string commands)
        {
            // Start the turtle at the center of the terrain
            _currentPosition = Terrain.transform.position + new Vector3(Terrain.terrainData.size.x / 2.0f, 0, Terrain.terrainData.size.z / 2.0f);
            _currentPosition.y = Terrain.SampleHeight(_currentPosition);
            _currentAngle = 90f; // Start facing "up" (positive Z)

            // Create the initial intersection
            Intersection lastIntersection = GetOrCreateIntersectionAt(_currentPosition);

            foreach (char command in commands)
            {
                switch (command)
                {
case 'F': // Move forward and draw
                        // Calculate next position
                        Vector3 direction = new Vector3(Mathf.Cos(_currentAngle * Mathf.Deg2Rad), 0, Mathf.Sin(_currentAngle * Mathf.Deg2Rad));
                        Vector3 nextPosition = _currentPosition + direction * SegmentLength;
                        
                        // --- UPDATED: Boundary Check ---
                        TerrainData td = Terrain.terrainData;
                        Vector3 terrainPos = Terrain.transform.position;
                        // Check if next position is outside terrain bounds
                        if (nextPosition.x < terrainPos.x || nextPosition.x > terrainPos.x + td.size.x ||
                            nextPosition.z < terrainPos.z || nextPosition.z > terrainPos.z + td.size.z)
                        {
                            // Optional: Log warning or just stop this segment
                            // Debug.LogWarning($"[LSystemGenerator] Turtle went out of bounds at {nextPosition}. Stopping segment.");
                            continue; // Skip the rest of the 'F' command for this step
                        }
                        // --- END OF BOUNDARY CHECK ---

                        // Adjust height to terrain
                        nextPosition.y = Terrain.SampleHeight(nextPosition);

                        // Get/Create intersection at the new position
                        Intersection currentIntersection = GetOrCreateIntersectionAt(nextPosition);

                        // Create road if the intersection is different
                        if (currentIntersection != lastIntersection)
                        {
                           // ... (road creation logic remains the same) ...
                            bool roadExists = _roads.Exists(r => (r.StartNode == lastIntersection && r.EndNode == currentIntersection) || (r.StartNode == currentIntersection && r.EndNode == lastIntersection));
                            if (!roadExists)
                            {
                                var newRoad = new Road(GetNextId(), lastIntersection, currentIntersection);
                                _roads.Add(newRoad);
                            }
                        }

                        // Update turtle state
                        _currentPosition = nextPosition;
                        lastIntersection = currentIntersection;
                        break;

                    case '+': // Turn right
                        _currentAngle -= TurnAngle;
                        break;

                    case '-': // Turn left
                        _currentAngle += TurnAngle;
                        break;

                    case '[': // Push state
                        _stateStack.Push((_currentPosition, _currentAngle));
                        break;

                    case ']': // Pop state
                        if (_stateStack.Count > 0)
                        {
                            var state = _stateStack.Pop();
                            _currentPosition = state.position;
                            _currentAngle = state.angle;
                            // Update lastIntersection to the one at the restored position
                            lastIntersection = GetOrCreateIntersectionAt(_currentPosition);
                        }
                        else
                        {
                            Debug.LogWarning("[LSystemGenerator] Attempted to pop from empty stack.");
                        }
                        break;

                    // Ignore other characters (like 'X', 'Y' etc. used only in rules)
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Gets an existing Intersection very close to the position or creates a new one.
        /// Uses a dictionary with higher-precision rounded positions as keys.
        /// </summary>
        private Intersection GetOrCreateIntersectionAt(Vector3 position)
        {
            // --- UPDATED: Increased rounding precision ---
            // Round to 2 decimal places (or adjust as needed)
            // Higher precision means points must be closer to merge.
            float precisionFactor = 100f; // 100f for 2 decimal places, 1000f for 3, etc.
            Vector3 key = new Vector3(Mathf.Round(position.x * precisionFactor) / precisionFactor,
                Mathf.Round(position.y * precisionFactor) / precisionFactor,
                Mathf.Round(position.z * precisionFactor) / precisionFactor);
            // ------------------------------------------

            if (_positionToIntersection.TryGetValue(key, out Intersection existingIntersection))
            {
                return existingIntersection; // Reuse existing
            }
            else
            {
                // Create new intersection using the precise position
                var newIntersection = new Intersection(GetNextId(), position);
                _intersections.Add(newIntersection);
                _positionToIntersection.Add(key, newIntersection); // Add to dictionary with rounded key
                return newIntersection;
            }
        }


        private int GetNextId()
        {
            return _nextAvailableId++;
        }
    }
}