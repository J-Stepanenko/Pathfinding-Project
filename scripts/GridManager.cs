using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : Node
{
	public static GridManager Instance { get; private set; }

	private Dictionary<Vector2I, Tile> tiles = new();
	private Agent selectedAgent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		GD.Print("loaded");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void RegisterTile(Vector2I gridPos, Tile tile)
	{
		tiles[gridPos] = tile;
	}

	public Tile GetTile(Vector2I gridPos)
	{
		return tiles.TryGetValue(gridPos, out Tile tile) ? tile : null;
	}

	// Djikstra's algorithm
	public List<Tile> GetReachableTiles(Vector2I start, int moveRange)
	{
		var reachable = new List<Tile>();
		var visited = new HashSet<Vector2I>();
		var queue = new Queue<(Vector2I pos, int steps)>();

		queue.Enqueue((start, 0));
		visited.Add(start);

		Vector2I[] directions =
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};

		while (queue.Count > 0)
		{
			var (current, steps) = queue.Dequeue();

			if (steps > 0) // Dont highlight tile that is stood on
			{
				var tile = GetTile(current);
				if (tile != null) reachable.Add(tile);
			}

			if (steps >= moveRange) continue;

			foreach (var dir in directions)
			{
				var neighbour = current + dir;
				if (!visited.Contains(neighbour) && tiles.ContainsKey(neighbour))
				{
					visited.Add(neighbour);
					queue.Enqueue((neighbour, steps+1));
				}
			}
		}

		return reachable;
	}
}
