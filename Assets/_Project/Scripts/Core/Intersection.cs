using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Representa uma interseção ou nó na malha viária.
    /// </summary>
    public class Intersection
    {
        public int Id;
        public Vector3 Position;

        public Intersection(int id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
    }
}