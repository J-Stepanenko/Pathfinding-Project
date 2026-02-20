using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GridManager : Node
{
	public static GridManager Instance { get; private set; }

	public Dictionary<Vector2I, Tile> tiles = new();
	private Dictionary<Vector2I, Agent> agents = new();
	private Agent selectedAgent;
    private AStar2D astar = new AStar2D();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Instance = this;
		GD.Print("GridManager loaded");

        // Wait until all tiles have registered before building A* graph
        CallDeferred(nameof(InitGrid));
        CallDeferred(nameof(BuildAStar));
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    private void InitGrid()
    {
        foreach (Tile tile in tiles.Values)
        {
            tile.Init();
        }
    }

	public void RegisterTile(Vector2I gridPos, Tile tile)
	{
		tiles[gridPos] = tile;
	}

	public void RegisterAgent(Vector2I agentPos, Agent agent)
	{
		agents[agentPos] = agent;
	}

	public bool DeregisterAgent(Vector2I agentPos)
	{
		return agents.Remove(agentPos);
	}

	public Tile GetTile(Vector2I gridPos)
	{
		return tiles.TryGetValue(gridPos, out Tile tile) ? tile : null;
	}

	public Agent GetAgent(Vector2I agentPos)
	{
		return agents.TryGetValue(agentPos, out Agent agent) ? agent : null;
	}

	public bool CheckTileHasAgent(Vector2I gridPos)
	{
		if (!tiles.ContainsKey(gridPos)) return false;
		return agents.ContainsKey(gridPos);
	}

	// Djikstra's algorithm
    public List<Tile> GetReachableTiles(Vector2I start, int moveRange)
    {
        var reachable = new List<Tile>();
        var costSoFar = new Dictionary<Vector2I, int>();
        // priority queue: (cost, position)
        var queue = new PriorityQueue<Vector2I, int>();

        costSoFar[start] = 0;
        queue.Enqueue(start, 0);

        Vector2I[] directions = 
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentCost = costSoFar[current];

            foreach (var dir in directions)
            {
                var neighbor = current + dir;

                if (!tiles.ContainsKey(neighbor)) continue;

                var tile = GetTile(neighbor);
                if (!tile.CanPassThisTurn) continue;

                int newCost = currentCost + tile.MoveCost;

                if (newCost > moveRange) continue; // too far

                // only visit if we found a cheaper path
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    queue.Enqueue(neighbor, newCost);
                    reachable.Add(tile);
                }
            }
        }

        return reachable;
    }

    public Tile FindClosestTile(Dictionary<Vector2I, Tile> tiles, Vector2I start)
    {
        if (tiles.Count == 1)
        {
            return tiles.First().Value;
        }
        List<List<Vector2I>> paths = new List<List<Vector2I>>();
        foreach(var tile in tiles)
        {
            paths.Add(GetPath(start, tile.Value.GridPosition));
        }
        var shortestPathLength = -1;
        Tile closestTile = null;
        foreach (var path in paths)
        {
            if (path.Count < shortestPathLength || shortestPathLength == -1)
            {
                shortestPathLength = path.Count;
                closestTile = GetTile(path.Last());
            }
        }
        return closestTile;
    }

    public void HighlightTiles(List<Tile> tiles, bool highlight)
	{
        foreach (var tile in tiles)
        {
            tile.SetHighlight(highlight);
        }
    }
    public void BuildAStar()
    {
        // Add all tiles as points
        foreach (var (gridPos, tile) in tiles)
        {
            long id = GetIdFromGridPos(gridPos);
            astar.AddPoint(id, (Vector2)gridPos, tile.MoveCost);
        }

        // Connect neighbours
        Vector2I[] directions = {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };

        foreach (var (gridPos, tile) in tiles)
        {
            foreach (var dir in directions)
            {
                var neighbour = gridPos + dir;
                if (!tiles.ContainsKey(neighbour)) continue;
                if (!tiles[neighbour].IsWalkable) continue;

                long idA = GetIdFromGridPos(gridPos);
                long idB = GetIdFromGridPos(neighbour);

                if (!astar.ArePointsConnected(idA, idB))
                    astar.ConnectPoints(idA, idB);

                if (!tile.CanPassThisTurn)
                {
                    astar.SetPointDisabled(idA, true);
                }
            }
        }
    }
    public List<Vector2I> GetPath(Vector2I from, Vector2I to)
    {
        long idFrom = GetIdFromGridPos(from);
        long idTo = GetIdFromGridPos(to);

        var path = astar.GetPointPath(idFrom, idTo);

        var gridPath = new List<Vector2I>();
        foreach (var point in path)
            gridPath.Add((Vector2I)point);

        GD.Print("path:");
        foreach(var point in path)
        {
            GD.Print(point);
        }

        return gridPath;
    }

    public void SetTileOccupied(Vector2I gridPos, bool occupied)
    {
        long id = GetIdFromGridPos(gridPos);
        astar.SetPointDisabled(id, occupied);
    }

    // Create long ID for A*
    private long GetIdFromGridPos(Vector2I gridPos)
    {
        return gridPos.X + gridPos.Y * 1000; // Assume grid is never wider than 1000
    }
}
