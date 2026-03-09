
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class TileScorer
{
	public static Tile FindBestTile(Agent agent, AgentState state)
	{
		Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
		switch (state)
		{
			case AgentState.Attacking:
				bestTiles = FindBestAttackTiles(agent);
				break;
			case AgentState.Forming_up:
				bestTiles = FindBestFormingUpTiles(agent);
				break;
			case AgentState.Chasing:
				bestTiles = FindBestChasingTiles(agent);
				break;
			case AgentState.Retreating:
				bestTiles = FindBestRetreatingTiles(agent);
                break;
        }

		// Choose reachable tiles over non-reachable ones if bestTiles contains multiple tiles
		if (bestTiles.Count > 0)
		{
			var reachable = GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange);
			var temp = new Dictionary<Vector2I, Tile>();
            foreach (var tile in bestTiles)
			{
				if (reachable.Contains(tile.Value)) 
				{ 
					temp.Add(tile.Key, tile.Value); 
				}
			}
			if (temp.Count > 0)
			{
				bestTiles = temp;
			}
		}
        return GridManager.Instance.FindClosestTile(bestTiles, agent.GridPosition);

	}

	private static Dictionary<Vector2I, Tile> FindBestAttackTiles(Agent agent)
    {
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        bestTiles.Add(agent.GridPosition, GridManager.Instance.GetTile(agent.GridPosition));
        var bestScore = 0;
        var tiles = GridManager.Instance.Tiles;
        var targetPos = FindAttackTarget(agent).GridPosition;

        bestScore = ScoreTileAttacking(GridManager.Instance.GetTile(agent.GridPosition), agent, targetPos);
        foreach (var tile in tiles)
        {
            if (tile.Key == agent.GridPosition) continue;
            var score = ScoreTileAttacking(tile.Value, agent, targetPos);
            if (score > bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(tile.Key, tile.Value);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(tile.Key, tile.Value);
            }
        }
        foreach (var tile in bestTiles)
        {
            GD.Print(agent.Name + " Best tile is: " + tile.Key + " Score: " + bestScore);
        }
        return bestTiles;
	}
    private static Dictionary<Vector2I, Tile> FindBestFormingUpTiles(Agent agent)
    {
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        bestTiles.Add(agent.GridPosition, GridManager.Instance.GetTile(agent.GridPosition));
        var bestScore = 0;
        var tiles = GridManager.Instance.Tiles;

        bestScore = ScoreTileFormingUp(GridManager.Instance.GetTile(agent.GridPosition), agent);
        foreach (var tile in tiles)
        {
			if (tile.Key == agent.GridPosition) continue;
            var score = ScoreTileFormingUp(tile.Value, agent);
            if (score > bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(tile.Key, tile.Value);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(tile.Key, tile.Value);
            }
        }
        foreach (var tile in bestTiles)
        {
            GD.Print(agent.Name + " Best tile is: " + tile.Key + " Score: " + bestScore);
        }
        return bestTiles;
    }

    private static Dictionary<Vector2I, Tile> FindBestChasingTiles(Agent agent)
    {
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        bestTiles.Add(agent.GridPosition, GridManager.Instance.GetTile(agent.GridPosition));
        var bestScore = 0;
        var tiles = GridManager.Instance.Tiles;
        var targetPos = FindAttackTarget(agent).GridPosition;

        bestScore = ScoreTileChasing(GridManager.Instance.GetTile(agent.GridPosition), agent, targetPos);
        foreach (var tile in tiles)
        {
            if (tile.Key == agent.GridPosition) continue;
            var score = ScoreTileChasing(tile.Value, agent, targetPos);
            if (score > bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(tile.Key, tile.Value);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(tile.Key, tile.Value);
            }
        }
        foreach (var tile in bestTiles)
        {
            GD.Print(agent.Name + " Best tile is: " + tile.Key + " Score: " + bestScore);
        }
        return bestTiles;
    }
    private static Dictionary<Vector2I, Tile> FindBestRetreatingTiles(Agent agent)
    {
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        bestTiles.Add(agent.GridPosition, GridManager.Instance.GetTile(agent.GridPosition));
        var bestScore = 0;
		var tiles = GridManager.Instance.Tiles;

        bestScore = ScoreTileRetreating(GridManager.Instance.GetTile(agent.GridPosition), agent);
        foreach (var tile in tiles)
        {
            if (tile.Key == agent.GridPosition) continue;
            var score = ScoreTileRetreating(tile.Value, agent);
            if (score > bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(tile.Key, tile.Value);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(tile.Key, tile.Value);
            }
        }
        foreach (var tile in bestTiles)
        {
            GD.Print(agent.Name + " Best tile is: " + tile.Key + " Score: " + bestScore);
        }
        return bestTiles;
    }
    private static Agent FindAttackTarget(Agent agent)
	{
		var tiles = GridManager.Instance.Tiles;
		var agents = GridManager.Instance.Agents;
		var lowestCost = -1;
		Agent target = null;
		Vector2I[] directions =
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};
		foreach (var possibleTarget in agents)
		{
			if (possibleTarget.Value.Team == TurnManager.Instance.TeamTurn)
			{
				continue;
			}
			foreach (var dir in directions)
			{
				if (GridManager.Instance.GetTile(possibleTarget.Key + dir) == null) continue;
				if (GridManager.Instance.CheckTileHasAgent(possibleTarget.Key + dir)) continue;
				// Get neighbours as tile with agent is disabled in A*
				GridManager.Instance.GetPath(agent.GridPosition, possibleTarget.Key + dir, out var cost);
				if (lowestCost == -1 || cost < lowestCost)
				{
					lowestCost = cost;
					target = possibleTarget.Value;
				}
			}
        }
        if (target == null) return agent;
        GD.Print("Agent:" + agent.Name + " targetting " + target.GridPosition + " cost: " + lowestCost);
        return target;
	}

	private static int ScoreTileAttacking(Tile tile, Agent thisAgent, Vector2I targetPos)
	{
		if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
		{
			if (tile.GridPosition != thisAgent.GridPosition) 
			{ 
				return 0;
			}
		}
		var tiles = GridManager.Instance.Tiles;

		var score = 0;
		var enemies = 0;
		var friendlies = 0;
		Vector2I[] directions =
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};

		foreach (var dir in directions)
		{
			var neighbourPos = tile.GridPosition + dir;

			if (!tiles.ContainsKey(neighbourPos)) continue;

			tiles.TryGetValue(neighbourPos, out Tile neighbour);
			if (GridManager.Instance.CheckTileHasAgent(neighbourPos))
			{
				var neighbourAgent = GridManager.Instance.GetAgent(neighbourPos);
				if (neighbourPos == targetPos)
				{
					score += 2;
				}
				if (neighbourAgent.Team != thisAgent.Team)
				{
					enemies++;
				}
				else
				{
					friendlies++;
				}
			}
		}

		if (enemies > 0)
		{
			score+= 2;
			switch (tile.Terrain)
			{
				case TileTerrain.Plains:
					break;
				case TileTerrain.Forest:
					score++;
					break;
				case TileTerrain.Mountain:
					score += 2;
					break;
				case TileTerrain.River:
					score--;
					break;
			}
		}

		// Incentivise attacking enemies together with teammates
		if (enemies > 0 && friendlies > 0)
		{
			for (int i = 1; i <= friendlies; i++)
			{
				score++;
			}
		}
		if (score < 0)
		{
			score = 0;
		}
		if (score > 0)
		{
			//GD.Print("Agent: " + agent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
		}
		return score;
	}

	private static int ScoreTileFormingUp(Tile tile, Agent thisAgent)
	{
		if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
		{
			return 0;
		}
		var tiles = GridManager.Instance.GetReachableTiles(thisAgent.GridPosition, thisAgent.MoveRange);

		var score = 0;
		Vector2I[] directions =
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};

		foreach (var dir in directions)
		{
			var neighbourPos = tile.GridPosition + dir;

			if (GridManager.Instance.GetTile(neighbourPos) == null) continue;

			var neighbourTile = GridManager.Instance.GetTile(neighbourPos);
			if (GridManager.Instance.CheckTileHasAgent(neighbourPos))
			{
				var neighbourAgent = GridManager.Instance.GetAgent(neighbourPos);
				if (neighbourAgent.Team == thisAgent.Team && neighbourAgent != thisAgent)
				{
					if (!neighbourAgent.InFormation)
					{
						score += 10;
					}
					else
					{
						score += 5;
					}
				}
			}
		}
		if (score == 0)
		{
			foreach (var dir in directions)
			{
				var neighbourTile = (GridManager.Instance.GetTile(tile.GridPosition + dir));
				if (neighbourTile == null) continue;

                if (CheckForFriendlyAgentsThatCanMoveHere(neighbourTile, thisAgent))
				{
					score += 10;
					break;
				}
			}
		}
		if (score > 0)
        {
            var agents = GridManager.Instance.Agents;
			var bestCostDifference = -1;
			foreach (var agent in agents)
			{
				if (agent.Value.Team == TurnManager.Instance.TeamTurn) continue;
				foreach (var dir in directions)
				{
					if (GridManager.Instance.GetTile(agent.Key + dir) == null) continue;

					GridManager.Instance.GetPath(thisAgent.GridPosition, agent.Key + dir, out var oldCost);
					GridManager.Instance.GetPath(tile.GridPosition, agent.Key + dir, out var newCost);
					var costDifference = oldCost - newCost;
					if (costDifference > bestCostDifference)
					{
						bestCostDifference = costDifference;
					}
					// Terrain should only matter if there is an enemy within 5 tiles
					if (newCost <= 5)
					{
                        switch (tile.Terrain)
                        {
                            case TileTerrain.Plains:
                                break;
                            case TileTerrain.Forest:
                                score += 5;
                                break;
                            case TileTerrain.Mountain:
                                score += 10;
                                break;
                            case TileTerrain.River:
                                score -= 5;
                                break;
                        }
                    }
				}
			}
			GridManager.Instance.GetPath(thisAgent.GridPosition, tile.GridPosition, out var cost);
			if (cost > 4)
			{
				score += Math.Max(Math.Min(bestCostDifference, 1), 0);
			}
			else
			{
				score += Math.Max(Math.Min(bestCostDifference, thisAgent.MoveRange), 0);
			}

			//GD.Print("Agent: " + agent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
		}
		return score;
	}

	private static bool CheckForFriendlyAgentsThatCanMoveHere(Tile tile, Agent callingAgent)
	{
		var agents = GridManager.Instance.Agents;
		foreach (var agent in agents)
		{
			if (agent.Value == callingAgent) continue;
			if (agent.Value.Team != callingAgent.Team) continue;

			if (agent.Value.CanMove && !agent.Value.InFormation)
			{
				GridManager.Instance.GetPath(agent.Key, tile.GridPosition, out var cost);
				if (cost == 0) continue;
				if (cost > 4) continue;
				else return true;
			}
		}
		return false;
	}

	private static int ScoreTileChasing(Tile tile, Agent thisAgent, Vector2I targetPos)
	{
		if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
		{
			return 0;
		}
		var agents = GridManager.Instance.Agents;

		var score = 0;
		Vector2I[] directions =
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};

        var getsCloserToTarget = false;
        foreach (var agent in agents)
		{
			foreach (var dir in directions)
			{
				if (GridManager.Instance.GetTile(agent.Key + dir) == null) continue;

				GridManager.Instance.GetPath(thisAgent.GridPosition, agent.Key + dir, out var oldCost);
				GridManager.Instance.GetPath(tile.GridPosition, agent.Key + dir, out var newCost);

				if (agent.Value.Team == thisAgent.Team)
				{
					score += oldCost - newCost;
                }
				else if (agent.Key == targetPos)
				{
					score += (oldCost - newCost) * 10;

					// Prevent going away from target
					if (newCost <= oldCost)
					{
						getsCloserToTarget = true;
					}
				}
				else
				{
					score += (oldCost - newCost) * 3;
				}
			}
        }
        if (!getsCloserToTarget)
        {
            return -10;
        }
        if (score > 0)
		{
			//GD.Print("Agent: " + thisAgent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
		}
		return score;
	}
    private static int ScoreTileRetreating(Tile tile, Agent thisAgent)
    {
        if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
        {
            return 0;
        }
		Dictionary<Vector2I, Tile> bases;
		if (thisAgent.Team == 1)
		{
			bases = GridManager.Instance.Team1Bases;
		}
		else
		{
			bases = GridManager.Instance.Team2Bases;
		}

		var agents = GridManager.Instance.Agents;

        var score = 0;
        Vector2I[] directions =
        {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };
        foreach (var _base in bases)
        {
            GridManager.Instance.GetPath(thisAgent.GridPosition, _base.Key, out var oldCost);
            GridManager.Instance.GetPath(tile.GridPosition, _base.Key, out var newCost);
            score += (oldCost - newCost) * 6;
        }

		foreach (var agent in agents)
		{
			foreach (var dir in directions)
			{
				if (GridManager.Instance.GetTile(agent.Key + dir) == null) continue;

				GridManager.Instance.GetPath(thisAgent.GridPosition, agent.Key + dir, out var oldCost);
				GridManager.Instance.GetPath(tile.GridPosition, agent.Key + dir, out var newCost);

				if (agent.Value.Team == thisAgent.Team)
				{
					score += Math.Min(oldCost - newCost, 5);
				}
				else
				{
					score += Math.Min((newCost - oldCost) * 3, 15);
				}
			}
		}
		if (score > 0)
        {
        //	GD.Print("Agent: " + thisAgent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
        }
        return score;
    }

	public static void ScoreTileManually(Agent agent, Tile tile)
	{
		var target = FindAttackTarget(agent);
		var attacking = ScoreTileAttacking(tile, agent, target.GridPosition);
		var formingUp = ScoreTileFormingUp(tile, agent);
		var chasing = ScoreTileChasing(tile, agent, target.GridPosition);
		var retreating = ScoreTileRetreating(tile, agent);

		GD.Print("Score for attacking: " + attacking);
        GD.Print("Score for forming: " + formingUp);
        GD.Print("Score for chasing: " + chasing);
        GD.Print("Score for retreating: " + retreating);
    }
}
