using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Represents an intersection or node in the road network.
    /// </summary>
    public class Intersection
    {
        public readonly int Id;
        public readonly Vector3 Position;

        public Intersection(int id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
    }
}