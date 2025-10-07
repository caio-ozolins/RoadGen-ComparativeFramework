using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation.Abstractions;
using _Project.Scripts.Generation.Agents;

namespace _Project.Scripts.Generation
{
    public class RandomWalkGenerator : IRoadNetworkGenerator
    {
        // Configuration parameters
        public Terrain Terrain;
        public int GlobalStepLimit;
        public int MaxStepsPerAgent;
        public float StepSize;
        public float MaxTurnAngle;
        public float MaxSteepness;
        public float BranchingChance;
        
        // Internal state variables
        private List<Intersection> _intersections;
        private List<Road> _roads;
        private int _nextAvailableId;

        public (List<Intersection> intersections, List<Road> roads) Generate()
        {
            _intersections = new List<Intersection>();
            _roads = new List<Road>();
            _nextAvailableId = 0;

            var activeAgents = new List<Agent>();

            Vector3 startPosition = Terrain.transform.position + new Vector3(Terrain.terrainData.size.x / 2.0f, 0, Terrain.terrainData.size.z / 2.0f);
            startPosition.y = Terrain.SampleHeight(startPosition);
            
            var initialIntersection = new Intersection(GetNextId(), startPosition);
            _intersections.Add(initialIntersection);

            var initialAgent = new Agent(startPosition, Random.Range(0, 360f), initialIntersection);
            activeAgents.Add(initialAgent);
            
            int globalSteps = 0;
            while (activeAgents.Count > 0 && globalSteps < GlobalStepLimit)
            {
                var newAgentsThisIteration = new List<Agent>();

                for (int i = activeAgents.Count - 1; i >= 0; i--)
                {
                    var agent = activeAgents[i];
                    bool agentShouldContinue = ProcessAgentStep(agent, out List<Agent> newBranches);
                    
                    if (agentShouldContinue)
                    {
                        newAgentsThisIteration.AddRange(newBranches);
                    }
                    else
                    {
                        activeAgents.RemoveAt(i);
                    }
                }
                
                activeAgents.AddRange(newAgentsThisIteration);
                globalSteps++;
            }
            
            if (globalSteps >= GlobalStepLimit)
                Debug.LogWarning($"[RandomWalkGenerator] Simulation stopped by global step limit of {GlobalStepLimit}.");
    
            Debug.Log($"[RandomWalkGenerator] Generation complete! {_intersections.Count} intersections and {_roads.Count} roads created.");
            
            return (_intersections, _roads);
        }
        
        // SIMPLIFIED DIAGNOSTIC VERSION
        private bool ProcessAgentStep(Agent agent, out List<Agent> newAgents)
        {
            newAgents = new List<Agent>();

            if (agent.StepsTaken >= MaxStepsPerAgent)
                return false; 

            agent.DirectionAngle += Random.Range(-MaxTurnAngle, MaxTurnAngle);
            Vector3 direction = new Vector3(Mathf.Cos(agent.DirectionAngle * Mathf.Deg2Rad), 0, Mathf.Sin(agent.DirectionAngle * Mathf.Deg2Rad));
            Vector3 nextPosition = agent.Position + direction * StepSize;

            float normalizedX = (nextPosition.x - Terrain.transform.position.x) / Terrain.terrainData.size.x;
            float normalizedZ = (nextPosition.z - Terrain.transform.position.z) / Terrain.terrainData.size.z;
                
            if (normalizedX < 0 || normalizedX > 1 || normalizedZ < 0 || normalizedZ > 1)
                return false; 

            float steepness = Terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
            
            if (steepness > MaxSteepness)
            {
                return false; // Agent stops at the first obstacle.
            }
    
            var previousIntersection = agent.PreviousIntersection;
            agent.Position = nextPosition;
            agent.Position.y = Terrain.SampleHeight(agent.Position);
            
            var newIntersection = new Intersection(GetNextId(), agent.Position);
            _intersections.Add(newIntersection);
            var newRoad = new Road(GetNextId(), previousIntersection, newIntersection);
            _roads.Add(newRoad);
            
            agent.StepsTaken++;
            agent.PreviousIntersection = newIntersection;

            if (Random.value < BranchingChance)
            {
                float newAngle = agent.DirectionAngle + (Random.value > 0.5f ? 90 : -90);
                newAgents.Add(new Agent(agent.Position, newAngle, newIntersection));
            }
            
            return true;
        }
        
        private int GetNextId()
        {
            return _nextAvailableId++;
        }
    }
}