using System.Collections.Generic;
using _Project.Scripts.Core;

namespace _Project.Scripts.Generation.Abstractions
{
    /// <summary>
    /// Defines the "contract" that every road network generation algorithm must follow.
    /// </summary>
    public interface IRoadNetworkGenerator
    {
        /// Generates the road network.
        (List<Intersection> intersections, List<Road> roads) Generate();
    }
}