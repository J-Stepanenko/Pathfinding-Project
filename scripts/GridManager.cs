using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class GridManager : Node
{
	public static GridManager Instance { get; private set; }

	public Dictionary<Vector2I, Tile> Tiles = new();
	public Dictionary<Vector2I, Agent> Agents = new();
	public Dictionary<Vector2I, Tile> Team1Bases = new();
    public Dictionary<Vector2I, Tile> Team2Bases = new();
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
		foreach (Tile tile in Tiles.Values)
		{
			tile.Init();
		}
		foreach (Agent agent in Agents.Values)
		{
			agent.Init();
			GD.Print("Initialised " + agent.Name+" at: "+agent.GridPosition);
		}
	}

	public void RegisterTile(Vector2I gridPos, Tile tile)
	{
		if (Tiles.ContainsKey(gridPos))
		{
			throw new Exception("Grid position: " + gridPos + " attempted to register when already existing");
		}
		Tiles[gridPos] = tile;
	}

	public void RegisterBase(Vector2I gridPos, Tile tile)
	{
		if (tile.BaseTeam == 1)
		{
			Team1Bases[gridPos] = tile;
		}
		else if (tile.BaseTeam == 2)
		{
			Team2Bases[gridPos] = tile;
		}
		GD.Print("Registered base for team " + tile.BaseTeam + " at: " + gridPos);
	}

	public void RegisterAgent(Vector2I agentPos, Agent agent)
    {
        if (Agents.ContainsKey(agentPos))
        {
            throw new Exception("Agent grid position: " + agentPos + " attempted to register in already occupied position");
        }
        Agents[agentPos] = agent;
	}

	public bool DeregisterAgent(Vector2I agentPos)
	{
		GD.Print(GetAgent(agentPos).Name + " Deregistered");
		SetTileOccupied(agentPos, false);
		return Agents.Remove(agentPos);
	}

	public Tile GetTile(Vector2I gridPos)
	{
		return Tiles.TryGetValue(gridPos, out Tile tile) ? tile : null;
	}

	public Agent GetAgent(Vector2I agentPos)
	{
		return Agents.TryGetValue(agentPos, out Agent agent) ? agent : null;
	}

	public bool CheckTileHasAgent(Vector2I gridPos)
	{
		if (!Tiles.ContainsKey(gridPos)) return false;
		return Agents.ContainsKey(gridPos);
	}

	public bool CheckTileIsNeighbour(Vector2I tile1Pos, Vector2I tile2Pos)
	{
		foreach (var tile in GetNeighbourTiles(tile1Pos))
		{
			if (tile.Key == tile2Pos) return true;
		}
		return false;
    }

	// Djikstra's algorithm
	public List<Tile> GetReachableTiles(Vector2I start, int moveRange)
	{
		var reachable = new Dictionary<Vector2I,Tile>();
		var costSoFar = new Dictionary<Vector2I, int>();
		// priority queue: (cost, position)
		var queue = new PriorityQueue<Vector2I, int>();

		costSoFar[start] = 0;
		queue.Enqueue(start, 0);

		reachable.Add(start, GetTile(start));
		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			int currentCost = costSoFar[current];

            foreach (var neighbourTile in GetNeighbourTiles(current))
			{
				if (!neighbourTile.Value.CanPassThisTurn) continue; 

				int newCost = currentCost + neighbourTile.Value.MoveCost;

                if (newCost > moveRange) continue; // too far

                // only visit if we found a cheaper path
                if (!costSoFar.ContainsKey(neighbourTile.Key) || newCost < costSoFar[neighbourTile.Key])
                {
                    costSoFar[neighbourTile.Key] = newCost;
                    queue.Enqueue(neighbourTile.Key, newCost);
                    reachable.Add(neighbourTile.Key, neighbourTile.Value);
                }
            }

        }

		foreach (var agent in Agents)
		{
			if (reachable.ContainsKey(agent.Key))
			{
				reachable.Remove(agent.Key);
			}
		}

		var returnedList = new List<Tile>();
		foreach (var tile in reachable)
		{
			returnedList.Add(tile.Value);
		}

		return returnedList;
	}

	public Tile FindClosestTile(Dictionary<Vector2I, Tile> tiles, Vector2I start)
	{
		if (tiles.ContainsKey(start))
		{
			return tiles[start];
		}
		if (tiles.Count == 1)
		{
			return tiles.First().Value;
		}
		List<List<Vector2I>> paths = new List<List<Vector2I>>();
		foreach(var tile in tiles)
		{
			paths.Add(GetPath(start, tile.Key));
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
		foreach (var (gridPos, tile) in Tiles)
		{
			long id = GetIdFromGridPos(gridPos);
			astar.AddPoint(id, (Vector2)gridPos, tile.MoveCost);
		}

		foreach (var (gridPos, tile) in Tiles)
		{
			foreach (var neighbourTile in GetNeighbourTiles(gridPos))
			{
				if (!neighbourTile.Value.IsWalkable) continue;
                long idA = GetIdFromGridPos(gridPos);
                long idB = GetIdFromGridPos(neighbourTile.Key);

                if (!astar.ArePointsConnected(idA, idB))
                    astar.ConnectPoints(idA, idB);

                if (!tile.CanPassThisTurn)
                {
                    astar.SetPointDisabled(idA, true);
                }
            }
		}
	}

	/// <summary>
	/// Return the path to a tile
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns></returns>
	public List<Vector2I> GetPath(Vector2I from, Vector2I to)
	{
		return GetPath(from, to, out _);
	}
	/// <summary>
	/// Return the path to a tile, cutting off after the path's cost exceeds the moveRange
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="moveRange"></param>
	/// <returns></returns>
	public List<Vector2I> GetPath(Vector2I from, Vector2I to, int moveRange)
	{
		long idFrom = GetIdFromGridPos(from);
		long idTo = GetIdFromGridPos(to);
		var cost = 0;
		var path = astar.GetPointPath(idFrom, idTo);

		var gridPath = new List<Vector2I>();

		foreach (var point in path)
		{
			if ((Vector2I)point != from)
			{
				if (cost + GetTile((Vector2I)point).MoveCost > moveRange) break;
				else
				{
					gridPath.Add((Vector2I)point);
					cost += GetTile((Vector2I)point).MoveCost;
				}
			}
		}
		return gridPath;
	}
	/// <summary>
	/// Return the path to a tile, and the cost to travel that path
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="cost"></param>
	/// <returns></returns>
	public List<Vector2I> GetPath(Vector2I from, Vector2I to, out int cost)
	{
		long idFrom = GetIdFromGridPos(from);
		long idTo = GetIdFromGridPos(to);
		cost = 0;

		var path = astar.GetPointPath(idFrom, idTo);

		var gridPath = new List<Vector2I>();

		foreach (var point in path)
		{
			gridPath.Add((Vector2I)point);
			if ((Vector2I)point != from)
			{
				cost += GetTile((Vector2I)point)
					.MoveCost;
			}
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

	public Dictionary<Vector2I, Tile> GetNeighbourTiles(Vector2I gridPos)
	{
        Vector2I[] directions = 
		{
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };
		var tiles = new Dictionary<Vector2I, Tile>();
		foreach(var dir in directions)
		{
			var tile = GetTile(gridPos + dir);
			if (tile != null) tiles.Add(gridPos + dir, tile);
		}

		return tiles;
    }

    /// <summary>
    /// Returns true if friendly agents can move to this tile on their turn.
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="agent"></param>
    /// <param name="onlyOutOfFormation">If true, only checks for agents that are out of formation</param>
    /// <returns></returns>
    public bool CheckForFriendlyAgentsThatCanMoveHere(Tile tile, Agent agent, bool onlyOutOfFormation)
    {
        var agents = GridManager.Instance.Agents;
        foreach (var otherAgent in agents)
        {
            if (otherAgent.Value == agent) continue;
            if (otherAgent.Value.Team != agent.Team) continue;

            if (otherAgent.Value.CanMove && (!otherAgent.Value.InFormation || !onlyOutOfFormation))
            {
                GridManager.Instance.GetPath(otherAgent.Key, tile.GridPosition, out var cost);
                if (cost == 0) continue;
                if (cost > agent.MoveRange) continue;
                else return true;
            }
        }
        return false;
    }
}
