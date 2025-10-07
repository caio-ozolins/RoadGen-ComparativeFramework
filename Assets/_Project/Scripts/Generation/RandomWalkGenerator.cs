using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;

namespace _Project.Scripts.Generation
{
    /// <summary>
    /// A pure C# class that implements the Random Walk algorithm logic.
    /// </summary>
    public class RandomWalkGenerator : IRoadNetworkGenerator
    {
        // Configuration parameters passed in by the orchestrator.
        public Terrain Terrain;
        public int NumberOfSteps;
        public float StepSize;
        public float MaxTurnAngle;
        public float MaxSteepness;
        public int MaxDetourAttempts;

        private int _nextAvailableId;

        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
            var intersections = new List<Intersection>();
            var roads = new List<Road>();
            _nextAvailableId = 0;

            // Start position at the center of the terrain.
            Vector3 currentPosition = Terrain.transform.position + new Vector3(Terrain.terrainData.size.x / 2.0f, 0, Terrain.terrainData.size.z / 2.0f);
            currentPosition.y = Terrain.SampleHeight(currentPosition);

            Intersection previousIntersection = new Intersection(GetNextId(), currentPosition);
            intersections.Add(previousIntersection);

            float currentAngle = Random.Range(0, 360f);
            
            for (int i = 0; i < NumberOfSteps; i++)
            {
                int detourAttempts = 0;
                bool stepSuccessful = false;

                while (detourAttempts < MaxDetourAttempts)
                {
                    float nextAngle = currentAngle + Random.Range(-MaxTurnAngle, MaxTurnAngle);
                    Vector3 direction = new Vector3(Mathf.Cos(nextAngle * Mathf.Deg2Rad), 0, Mathf.Sin(nextAngle * Mathf.Deg2Rad));
                    Vector3 nextPosition = currentPosition + direction * StepSize;

                    float normalizedX = (nextPosition.x - Terrain.transform.position.x) / Terrain.terrainData.size.x;
                    float normalizedZ = (nextPosition.z - Terrain.transform.position.z) / Terrain.terrainData.size.z;
                    
                    if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
                    {
                        // Agent is trying to go off the map, force a turn.
                        currentAngle += Random.Range(90, 270);
                        detourAttempts++;
                        continue;
                    }

                    float steepness = Terrain.terrainData.GetSteepness(normalizedX, normalizedZ);

                    if (steepness <= MaxSteepness)
                    {
                        // Path is valid, commit to the new position and angle.
                        currentPosition = nextPosition;
                        currentPosition.y = Terrain.SampleHeight(currentPosition);
                        currentAngle = nextAngle;
                        stepSuccessful = true;
                        break; // Exit the while loop for detour attempts.
                    }

                    // Path is not valid, try another angle in the next attempt.
                    detourAttempts++;
                }

                if (!stepSuccessful)
                {
                    Debug.Log($"Agent gave up after {MaxDetourAttempts} detour attempts.");
                    break; // End the main for loop for this agent.
                }

                var newIntersection = new Intersection(GetNextId(), currentPosition);
                intersections.Add(newIntersection);
                var newRoad = new Road(GetNextId(), previousIntersection, newIntersection);
                roads.Add(newRoad);
                
                previousIntersection = newIntersection;
            }

            Debug.Log($"[RandomWalkGenerator] Generation complete! {intersections.Count} intersections and {roads.Count} roads created.");
            return (intersections, roads);
        }
        
        private int GetNextId()
        {
            return _nextAvailableId++;
        }
    }
}