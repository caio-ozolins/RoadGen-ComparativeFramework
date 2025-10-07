using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.Generation.Agents
{
    /// <summary>
    /// Represents the state of a single "walker" in the network generation process.
    /// This is a plain C# data class, not a MonoBehaviour.
    /// </summary>
    public class Agent
    {
        public Vector3 Position;
        public float DirectionAngle; // The agent's current direction in degrees.
        public int StepsTaken;     // How many steps this agent has already taken.
        public Intersection PreviousIntersection; // The last intersection this agent created.
        
        public Agent(Vector3 position, float directionAngle, Intersection previousIntersection)
        {
            Position = position;
            DirectionAngle = directionAngle;
            StepsTaken = 0;
            PreviousIntersection = previousIntersection;
        }
    }
}