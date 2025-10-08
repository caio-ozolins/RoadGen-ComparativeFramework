using System.Collections.Generic;
using _Project.Scripts.Generation.Analysis;
using UnityEngine;

namespace _Project.Scripts.Generation.Pathfinding
{
    /// <summary>
    /// Contains the logic for the A* pathfinding algorithm.
    /// </summary>
    public class AStarPathfinder
    {
        /// <summary>
        /// Finds the lowest-cost path from a start to an end point on a given cost map.
        /// </summary>
        /// <returns>A list of nodes representing the path, or null if no path is found.</returns>
        public List<Node> FindPath(int startX, int startY, int endX, int endY, CostMap costMap)
        {
            int width = costMap.Width;
            int height = costMap.Height;
            Node[,] grid = new Node[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    grid[x, y] = new Node(x, y);
                }
            }

            Node startNode = grid[startX, startY];
            Node endNode = grid[endX, endY];

            List<Node> openList = new List<Node>();
            HashSet<Node> closedList = new HashSet<Node>();

            openList.Add(startNode);

            while (openList.Count > 0)
            {
                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < currentNode.FCost ||
                        (Mathf.Approximately(openList[i].FCost, currentNode.FCost) && openList[i].HCost < currentNode.HCost))
                    {
                        currentNode = openList[i];
                    }
                }

                if (currentNode == endNode)
                {
                    return ReconstructPath(endNode);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // --- NEW: Neighbor Processing Logic ---
                foreach (Node neighbour in GetNeighbours(currentNode, grid, width, height))
                {
                    // If the neighbor is already evaluated, skip it.
                    if (closedList.Contains(neighbour))
                    {
                        continue;
                    }
                    
                    // Calculate the cost to move from the current node to the neighbor.
                    // This includes the base movement cost (10 for straight, 14 for diagonal)
                    // plus the terrain cost from the CostMap.
                    float moveCost = CalculateHeuristic(currentNode, neighbour) + costMap.GetCost(neighbour.X, neighbour.Y);
                    float newGCost = currentNode.GCost + moveCost;

                    // If this new path to the neighbor is better than any previous one,
                    // or if the neighbor has not been evaluated yet (is not in openList), update it.
                    if (newGCost < neighbour.GCost || !openList.Contains(neighbour))
                    {
                        neighbour.GCost = newGCost;
                        neighbour.HCost = CalculateHeuristic(neighbour, endNode);
                        neighbour.Parent = currentNode;

                        if (!openList.Contains(neighbour))
                        {
                            openList.Add(neighbour);
                        }
                    }
                }
            }
            
            // If the loop finishes, and we haven't found the end node, no path exists.
            return null;
        }

        private List<Node> GetNeighbours(Node node, Node[,] grid, int width, int height)
        {
            List<Node> neighbours = new List<Node>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.X + x;
                    int checkY = node.Y + y;

                    if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                    {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }
        
        private float CalculateHeuristic(Node a, Node b)
        {
            // Using Manhattan-like distance with diagonal cost consideration.
            // Cost for straight move is 10, cost for diagonal is 14.
            int dstX = Mathf.Abs(a.X - b.X);
            int dstY = Mathf.Abs(a.Y - b.Y);
            
            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        private List<Node> ReconstructPath(Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != null)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            
            path.Reverse();
            return path;
        }
    }
}