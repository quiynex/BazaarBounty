using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace BazaarBounty;

public class NavigationHelper
{
    /// <summary>
    /// Given a source and target position of the grid coordinates, and a 2D grid map
    /// find the path with A* algorithm.
    /// </summary>
    /// <param name="source"> source coordinates, tuple of size 2</param>
    /// <param name="target"> target coordinates, tuple of size 2</param>
    /// <param name="type">Used to determine whether can move to a tile</param>
    /// <returns>Return a list of grid coordinates that represents the path</returns>
    public static List<(int, int)> FindPath((int, int) source, (int, int) target, CharacterType type)
    {
        // heuristic function
        var heuristicFunc = (int x, int y, int cost) => { return cost + Math.Abs(x - target.Item1) + Math.Abs(y - target.Item2); };

        List<(int, int)> path = new();
        int xs = source.Item1, ys = source.Item2;
        int xt = target.Item1, yt = target.Item2;
        // Create a list of open nodes and closed nodes
        PriorityQueue<(int, int), int> openQueue = new();
        HashSet<(int, int)> openSet = new();
        HashSet<(int, int)> closedSet = new();
        // Add the source node to the open list
        openSet.Add((xs, ys));
        openQueue.Enqueue((xs, ys), heuristicFunc(xs, ys, 0));
        // Create a dictionary to store the parent of each node
        Dictionary<(int, int), (int, int)> parent = new();
        // Create a dictionary to store the cost of each node
        Dictionary<(int, int), int> cost = new();
        // Set the cost of the source node to 0
        cost.Add((xs, ys), 0);

        var map = BazaarBountyGame.levelManager.CurrentMap;
        if (xs < 0 || xs >= map.MapCols || ys < 0 || ys >= map.MapRows
            || xt < 0 || xt >= map.MapCols || yt < 0 || yt >= map.MapRows)
        {
            // just in case invalid input
            Console.WriteLine("Source or target out of map range");
            return path;
        }
        while (openQueue.Count != 0)
        {
            // Get the node with the lowest cost
            var current = openQueue.Dequeue();
            var (currentX, currentY) = current;
            // If the current node is the target node
            if (currentX == xt && currentY == yt)
            {
                // Reconstruct the path
                var node = current;
                path.Add(node);
                while (parent.ContainsKey(node))
                {
                    node = parent[node];
                    path.Add(node);
                }

                path.Reverse();
                return path;
            }

            // Remove the current node from the open list
            openSet.Remove(current);
            // Add the current node to the closed list
            closedSet.Add(current);
            // Get the neighbors of the current node
            List<(int, int)> neighbors = new();
            if (currentX > 0) neighbors.Add((currentX - 1, currentY));
            if (currentX < map.MapCols - 1) neighbors.Add((currentX + 1, currentY));
            if (currentY > 0) neighbors.Add((currentX, currentY - 1));
            if (currentY < map.MapRows - 1) neighbors.Add((currentX, currentY + 1));
            // For each neighbor
            foreach (var neighbor in neighbors)
            {
                var (neighborX, neighborY) = neighbor;
                // If the neighbor is not walkable or is in the closed list
                if (!map.IsTileWalkable(neighborX, neighborY, type) || closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Calculate the cost of the neighbor
                int newCost = cost[current] + map.GetTileWalkCost(neighborX, neighborY, type);
                // If the neighbor is not in the open list or the new cost is less than the cost of the neighbor
                if (!openSet.Contains(neighbor) || newCost < cost[neighbor])
                {
                    // Set the parent of the neighbor to the current node
                    parent[neighbor] = current;
                    // Set the cost of the neighbor to the new cost
                    cost[neighbor] = newCost;
                    // If the neighbor is not in the open list
                    if (!openSet.Contains(neighbor))
                    {
                        // Add the neighbor to the open list
                        openSet.Add(neighbor);
                        openQueue.Enqueue(neighbor, heuristicFunc(neighborX, neighborY, newCost));
                    }
                }
            }
        }
        return path;
    }

    private static readonly Dictionary<(int, int, int, int), List<(int, int)>> LinePointsCache = new();

    /// <summary>
    /// Retrieve the grid points on the line between two grid coordinates
    /// </summary>
    public static List<(int, int)> GetLinePoints(int startX, int startY, int endX, int endY)
    {
        if (LinePointsCache.ContainsKey((startX, startY, endX, endY)))
        {
            return LinePointsCache[(startX, startY, endX, endY)];
        }

        List<(int, int)> points = new();
        int dx = Math.Abs(endX - startX);
        int dy = Math.Abs(endY - startY);
        int sx = startX < endX ? 1 : -1;
        int sy = startY < endY ? 1 : -1;
        int err = dx - dy;
        int e2;
        while (true)
        {
            points.Add((startX, startY));
            if (startX == endX && startY == endY)
            {
                break;
            }

            e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                startX += sx;
            }
            else if (e2 < dx)
            {
                err += dx;
                startY += sy;
            }
        }

        LinePointsCache[(startX, startY, endX, endY)] = points;
        return points;
    }

    /// <summary>
    /// Check if source character can see the target character with ray intersection
    /// </summary>
    public static bool CheckLineIntersection(Vector2 source, Vector2 target, CharacterType type,
        List<(int, int)> outLinePoints = null, bool preventBorder = false)
    {
        outLinePoints?.Clear();
        var level = BazaarBountyGame.levelManager;
        var positionCache = level.CurrentMap.PositionCache;
        var (sourceX, sourceY) = positionCache.GetGridPosition(source);
        var (targetX, targetY) = positionCache.GetGridPosition(target);
        var linePoints = GetLinePoints(sourceX, sourceY, targetX, targetY);
        foreach (var (x, y) in linePoints)
        {
            if (!level.CurrentMap.IsTileWalkable(x, y, type, preventBorder))
            {
                return false;
            }
        }
        outLinePoints?.AddRange(linePoints);
        return true;
    }

    public static List<Vector2> GridToWorldPath(List<(int, int)> gridPath)
    {
        List<Vector2> worldPath = new();
        var level = BazaarBountyGame.levelManager;
        var positionMap = level.CurrentMap.PositionCache;
        foreach (var (x, y) in gridPath)
        {
            worldPath.Add(positionMap.GetWorldPosition(x, y));
        }

        return worldPath;
    }
}

/// <summary>
/// Store the character reference or target position info.
/// For target character, update the path whenever target move to a new grid
/// Static path for target position
/// </summary>
public class NavigationInfo
{
    public Character Source { get; }
    public Character Target { get; }
    public Vector2 TargetPosition { get; }
    private List<Vector2> path;

    private int targetGridX;
    private int targetGridY;

    private bool dynamicPath;
    private int currentPathIndex;

    private double lastUpdateTime;
    private List<(int, int)> linePointsCache = new();

    public NavigationInfo(Character source, Character target)
    {
        Source = source;
        Target = target;
        dynamicPath = true;
        var posCache = BazaarBountyGame.levelManager.CurrentMap.PositionCache;
        var sourceGridPos = posCache.GetGridPosition(source.Position);
        var targetGridPos = posCache.GetGridPosition(target.Position);
        path = NavigationHelper.GridToWorldPath(NavigationHelper.FindPath(sourceGridPos, targetGridPos, source.Type));
        (targetGridX, targetGridY) = targetGridPos;
        currentPathIndex = 0;
        // Console.WriteLine($"Navigation info {source} - {target}");
    }

    public NavigationInfo(Character source, Vector2 targetPosition)
    {
        Source = source;
        TargetPosition = targetPosition;
        dynamicPath = false;
        var posCache = BazaarBountyGame.levelManager.CurrentMap.PositionCache;
        var sourceGridPos = posCache.GetGridPosition(source.Position);
        var targetGridPos = posCache.GetGridPosition(targetPosition);
        path = NavigationHelper.GridToWorldPath(NavigationHelper.FindPath(sourceGridPos, targetGridPos, source.Type));
        (targetGridX, targetGridY) = targetGridPos;
    }

    public void Update()
    {
        if (!dynamicPath) return;
        // Update the path if the source or target moves to a new grid
        var posCache = BazaarBountyGame.levelManager.CurrentMap.PositionCache;
        var targetGridPos = posCache.GetGridPosition(Target.Position);
        if ((targetGridX, targetGridY) != targetGridPos)
        {
            var sourceGridPos = posCache.GetGridPosition(Source.Position);
            path = NavigationHelper.GridToWorldPath(NavigationHelper.FindPath(sourceGridPos, targetGridPos, Source.Type));
            (targetGridX, targetGridY) = targetGridPos;
            currentPathIndex = 0;
        }
    }

    public Vector2 GetNextPathPosition()
    {
        Vector2 sourcePos = Source.Position;
        Vector2 targetPos = dynamicPath ? Target.Position : TargetPosition;

        // check input validity
        if (sourcePos.IsNaN() || targetPos.IsNaN())
        {
            Console.WriteLine("Invalid source or target position");
            return sourcePos;
        }
        
        // Update the line of sight every 0.1 seconds
        var gameTime = BazaarBountyGame.GetGameTime();
        if (gameTime.TotalGameTime.TotalSeconds - lastUpdateTime > 1)
        {
            lastUpdateTime = gameTime.TotalGameTime.TotalSeconds;
            NavigationHelper.CheckLineIntersection(sourcePos, targetPos, Source.Type, linePointsCache, true);
        }

        // If there is a direct line of sight to the target, return the target position
        if (linePointsCache.Count != 0)
        {
            return targetPos;
        }

        if (currentPathIndex >= path.Count) return sourcePos;
        Vector2 currentTarget = path[currentPathIndex];
        var posCache = BazaarBountyGame.levelManager.CurrentMap.PositionCache;
        var targetGridPos = posCache.GetGridPosition(currentTarget);
        var sourceGridPos = posCache.GetGridPosition(sourcePos);
        if (targetGridPos == sourceGridPos)
        {
            currentPathIndex++;
        }

        if (currentPathIndex < path.Count)
        {
            return path[currentPathIndex];
        }

        // Target is reached
        return Source.Position;
    }

    public void DrawPath(SpriteBatch spriteBatch)
    {
        if (!BazaarBountyGame.Settings.Debug.Mode)
            return;

        // if visible, draw the line of sight
        if (linePointsCache.Count != 0)
        {
            foreach (var (x, y) in linePointsCache)
            {
                Vector2 worldPos = BazaarBountyGame.levelManager.CurrentMap.PositionCache.GetWorldPosition(x, y);
                spriteBatch.DrawCircle(worldPos, 10, 32, Color.Orange, layerDepth: BazaarBountyGame.Settings.Debug.Depth);
            }
            var targetPos = dynamicPath ? Target.Position : TargetPosition;
            spriteBatch.DrawLine(Source.Position, targetPos, Color.Blue, layerDepth: BazaarBountyGame.Settings.Debug.Depth);
        }
        else // draw the navigation path
        {
            for (int i = currentPathIndex; i < path.Count - 1; ++i)
            {
                spriteBatch.DrawLine(path[i], path[i + 1], Color.Green, layerDepth: BazaarBountyGame.Settings.Debug.Depth);
            }
        }
    }
}

public class NavigationSystem
{
    // To prevent duplicate registration
    private static Dictionary<Character, NavigationInfo> _navigationInfos = new();

    // used to store the path for debug drawing, only store the paths that are queried in the current frame
    private static readonly List<NavigationInfo> NavigationInfoDrawCache = new();

    public static void RegisterNavigationInfo(Character source, Character target)
    {
        if (_navigationInfos.ContainsKey(source) && _navigationInfos[source].Target == target) return;
        _navigationInfos[source] = new NavigationInfo(source, target);
    }

    public static void ResetNavigationInfo(){
        _navigationInfos = new();
    }

    public static void RegisterNavigationInfo(Character source, Vector2 targetPosition)
    {
        if (_navigationInfos.ContainsKey(source) && _navigationInfos[source].TargetPosition == targetPosition) return;
        _navigationInfos[source] = new NavigationInfo(source, targetPosition);
    }

    public static void DeregisterNavigationInfo(Character source)
    {
        _navigationInfos.Remove(source);
    }

    public static Vector2 QueryCharacterNextPos(Character source)
    {
        var info = _navigationInfos[source];
        if (info != null)
        {
            // Assume query once per frame. Update before querying
            info.Update();
            NavigationInfoDrawCache.Add(info);
            return info.GetNextPathPosition();
        }

        Console.WriteLine("Character not found in navigation system");
        return source.Position;
    }

    public static void DrawPath(SpriteBatch spriteBatch)
    {
        foreach (var info in NavigationInfoDrawCache)
        {
            info.DrawPath(spriteBatch);
        }

        NavigationInfoDrawCache.Clear();
    }
}