using System.Collections.Generic;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;
using UnityEngine;
using System.Text;
using Random = UnityEngine.Random;

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
        public string Axiom { get; set; } = "F"; // Axiom might be overridden if default rules are used
        public Dictionary<char, string> Rules { get; set; } = new Dictionary<char, string>();
        public int Iterations { get; set; } = 3;

        // --- Turtle Parameters (Set by Orchestrator) ---
        public float SegmentLength { get; set; } = 15f;
        public float TurnAngle { get; set; } = 25f;
        public float AngleVariance { get; set; }
        public float LengthVariance { get; set; }

        // --- Internal State ---
        private List<Intersection> _intersections;
        private List<Road> _roads;
        private int _nextAvailableId;
        private const float MergeDistance = 0.1f; // Tolerance for merging nodes (10cm)

        // --- Turtle State ---
        private Vector3 _currentPosition;
        private float _currentAngle;
        
        // --- FIX 1: The stack now stores the branch's origin Intersection ---
        private Stack<(Intersection branchIntersection, float angle)> _stateStack;


        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
             _intersections = new List<Intersection>();
             _roads = new List<Road>();
             _nextAvailableId = 0;
             
             // --- FIX 2: Initialize the new stack type ---
             _stateStack = new Stack<(Intersection branchIntersection, float angle)>();

             if (Terrain == null) { Debug.LogError("[LSystemGenerator] Terrain is null. Aborting."); return (_intersections, _roads); }

             // Use default branching rule if none provided by orchestrator
             if (Rules == null || Rules.Count == 0)
             {
                 Debug.LogWarning("[LSystemGenerator] No rules provided. Using default branching rule F -> F[+F]F[-F]F.");
                 Axiom = "F"; // Ensure Axiom matches the default rule
                 Rules = new Dictionary<char, string> { { 'F', "F[+F]F[-F]F" } };
                 if (TurnAngle <= 0) TurnAngle = 25.7f; // Use angle often associated with this rule if not set
             }

             // Safety defaults/clamps
             if (SegmentLength <= 0) SegmentLength = 15f;
             if (TurnAngle <= 0) TurnAngle = 25.7f; // Default to the common angle if invalid
             AngleVariance = Mathf.Clamp(AngleVariance, 0f, 90f);
             LengthVariance = Mathf.Clamp(LengthVariance, 0f, SegmentLength * 0.5f);


            Debug.Log($"[LSystemGenerator] Generating L-system string. Axiom='{Axiom}', Iterations={Iterations}");
            string lSystemString = GenerateString();
            Debug.Log($"[LSystemGenerator] Generated string length: {lSystemString.Length}");

            Debug.Log("[LSystemGenerator] Interpreting string to build network...");
            InterpretString(lSystemString);

            Debug.Log($"[LSystemGenerator] Network build completed. {_intersections.Count} intersections, {_roads.Count} roads created.");
            return (_intersections, _roads);
        }

        private string GenerateString()
        {
             string currentString = Axiom; StringBuilder nextString = new StringBuilder();
             for (int i = 0; i < Iterations; i++)
             {
                 nextString.Clear();
                 foreach (char c in currentString) { nextString.Append(Rules.TryGetValue(c, out var rule) ? rule : c.ToString()); }
                 currentString = nextString.ToString();
                 if (currentString.Length > 200000) { Debug.LogWarning($"[LSystemGenerator] String length exceeded 200k limit at iteration {i}. Stopping early."); break; }
             }
             return currentString;
        }

        private void InterpretString(string commands)
        {
            // Initial Setup
            _currentPosition = Terrain.transform.position + new Vector3(Terrain.terrainData.size.x / 2.0f, 0, Terrain.terrainData.size.z / 2.0f);
            _currentPosition.y = Terrain.SampleHeight(_currentPosition);
            _currentAngle = 90f;
            _stateStack.Clear();

            // The initial node (intersection) where the turtle starts.
            Intersection intersectionBeforeMove = GetOrCreateIntersectionAt(_currentPosition); // Initial node

            foreach (char command in commands)
            {
                switch (command)
                {
                    case 'F':
                        // 1. Store the intersection where we are (start node of the segment).
                        Intersection startIntersectionForSegment = intersectionBeforeMove;

                        // 2. Calculate the position where we will be.
                        float currentSegmentLength = SegmentLength + Random.Range(-LengthVariance, LengthVariance);
                        currentSegmentLength = Mathf.Max(1f, currentSegmentLength);
                        Vector3 direction = new Vector3(Mathf.Cos(_currentAngle * Mathf.Deg2Rad), 0, Mathf.Sin(_currentAngle * Mathf.Deg2Rad));
                        Vector3 nextPosition = _currentPosition + direction * currentSegmentLength;

                        // 3. Bounds Check
                        TerrainData td = Terrain.terrainData; Vector3 terrainPos = Terrain.transform.position;
                        if (nextPosition.x < terrainPos.x || nextPosition.x > terrainPos.x + td.size.x || nextPosition.z < terrainPos.z || nextPosition.z > terrainPos.z + td.size.z)
                        {
                            continue; // Skip move if out of bounds
                        }
                        nextPosition.y = Terrain.SampleHeight(nextPosition);

                        // 4. Get/Create the intersection where we landed (end node of the segment).
                        Intersection endIntersectionForSegment = GetOrCreateIntersectionAt(nextPosition); // Use the merging version

                        // 5. Create the road segment BETWEEN start and end, if they are different nodes.
                        if (endIntersectionForSegment != startIntersectionForSegment)
                        {
                             // Check if road already exists (avoids duplicates)
                             bool roadExists = _roads.Exists(r => (r.StartNode == startIntersectionForSegment && r.EndNode == endIntersectionForSegment) || (r.StartNode == endIntersectionForSegment && r.EndNode == startIntersectionForSegment));
                             if (!roadExists)
                             {
                                 var newRoad = new Road(GetNextId(), startIntersectionForSegment, endIntersectionForSegment);
                                 _roads.Add(newRoad);
                             }
                        }

                        // 6. Update the turtle's state AFTER creating the road.
                        _currentPosition = nextPosition;
                        intersectionBeforeMove = endIntersectionForSegment; // The place we landed is the start for the next move.
                        break;

                    case '+':
                        float currentTurnAngleR = TurnAngle + Random.Range(-AngleVariance, AngleVariance);
                        _currentAngle -= currentTurnAngleR;
                        break;

                    case '-':
                        float currentTurnAngleL = TurnAngle + Random.Range(-AngleVariance, AngleVariance);
                        _currentAngle += currentTurnAngleL;
                        break;

                    case '[':
                        // --- FIX 3: Push the current INTERSECTION and angle ---
                        _stateStack.Push((intersectionBeforeMove, _currentAngle));
                        break;

                    case ']':
                        // --- FIX 4: Restore the state from the stack ---
                        if (_stateStack.Count > 0)
                        {
                            var state = _stateStack.Pop();
                            
                            // Restore state FROM the saved intersection
                            _currentPosition = state.branchIntersection.Position; 
                            _currentAngle = state.angle;

                            // CRITICAL: Reuse the saved intersection as the starting point
                            intersectionBeforeMove = state.branchIntersection; 
                        }
                        else { Debug.LogWarning("[LSystemGenerator] Attempted to pop from empty stack."); }
                        break;
                }
            }
        }


        /// <summary>
        /// --- FIX 5: FINAL VERSION WITH MERGING ---
        /// Retrieves an existing Intersection at a position (within tolerance)
        /// or creates a new one if none is found.
        /// </summary>
        private Intersection GetOrCreateIntersectionAt(Vector3 position)
        {
            // 1. Try to find an existing node nearby
            foreach (var existingIntersection in _intersections)
            {
                if (Vector3.Distance(existingIntersection.Position, position) < MergeDistance)
                {
                    // FOUND! Return the existing node.
                    return existingIntersection; 
                }
            }

            // 2. Not found? Create a new one.
            var newIntersection = new Intersection(GetNextId(), position);
            _intersections.Add(newIntersection);
            return newIntersection;
        }

        private int GetNextId() { return _nextAvailableId++; }
    }
}