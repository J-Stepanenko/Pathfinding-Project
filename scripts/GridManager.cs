using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : Node
{
	public static GridManager Instance { get; private set; }

	private Dictionary<Vector2I, Tile> tiles = new();
	private Dictionary<Vector2I, Agent> agents = new();
	private Agent selectedAgent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		GD.Print("GridManager loaded");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
                if (!tile.IsWalkable) continue;

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

    public void HighlightTiles(List<Tile> tiles, bool highlight)
	{
        foreach (var tile in tiles)
        {
            tile.SetHighlight(highlight);
        }
    }
}
