using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation;

namespace _Project.Scripts
{
    public class RoadNetworkGenerator : MonoBehaviour
    {
        [Header("Terrain Configuration")]
        public Terrain terrain;
        [Header("Random Walker Parameters")]
        public int numberOfSteps = 50;
        public float stepSize = 10.0f;
        [Range(0, 45)]
        public float maxTurnAngle = 15.0f;
        [Range(0, 90)]
        public float maxSteepness = 30.0f;
        [Range(0, 10)]
        public int maxDetourAttempts = 5;

        private readonly List<Intersection> _intersections = new List<Intersection>();
        private readonly List<Road> _roads = new List<Road>();

        [ContextMenu("Generate Road Network")]
        private void Generate()
        {
            Debug.Log("Orchestrator starting generation...");
            ClearPreviousNetwork();

            var generator = new RandomWalkGenerator
            {
                Terrain = this.terrain,
                NumberOfSteps = this.numberOfSteps,
                StepSize = this.stepSize,
                MaxTurnAngle = this.maxTurnAngle,
                MaxSteepness = this.maxSteepness,
                MaxDetourAttempts = this.maxDetourAttempts
            };
            
            var result = generator.Generate();

            _intersections.AddRange(result.intersections);
            _roads.AddRange(result.roads);
        }

        [ContextMenu("Clear Road Network")]
        private void ClearPreviousNetwork()
        {
            Debug.Log("Clearing previous network...");
            _intersections.Clear();
            _roads.Clear();
        }
        
        private void OnDrawGizmos()
        {
            if (_roads == null) return;
            Gizmos.color = Color.white;
            foreach (var road in _roads) { Gizmos.DrawLine(road.StartNode.Position, road.EndNode.Position); }
            Gizmos.color = Color.red;
            foreach (var intersection in _intersections) { Gizmos.DrawSphere(intersection.Position, 1.0f); }
        }
    }
}