
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
            // Prioritise staying still instead of moving if possible
            if (bestTiles.ContainsKey(agent.GridPosition))
            {
                return bestTiles[agent.GridPosition];
            }
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
		var bestTileReachable = false;
        var reachableTiles = GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange);
        foreach (var tile in bestTiles)
        {
			if (reachableTiles.Contains(tile.Value)) bestTileReachable = true;
            GD.Print(agent.Name + " Best tile is: " + tile.Key + " Score: " + bestScore);
        }

        // If best tile not reachable and there is a neighbouring enemy, don't move
		if (!bestTileReachable)
		{
			foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(agent.GridPosition))
			{
				var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
				if (neighbourAgent != null && neighbourAgent.Team != agent.Team)
				{
                    bestTiles.Clear();
                    bestTiles.Add(agent.GridPosition, GridManager.Instance.GetTile(agent.GridPosition));
                }
			}
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

        var closest = GridManager.Instance.FindClosestTile(bestTiles, agent.GridPosition);
        var path = GridManager.Instance.GetPath(agent.GridPosition, closest.GridPosition, agent.MoveRange);

        // If the closest tile is out of agent's move range, but the path to that tile neighbours an agent not in formation, stop at the tile with neighbour agent
        if (path.Last() != closest.GridPosition)
        {
            var tilesWithNeighbours = new List<Vector2I>();
            foreach (var tile in path)
            {
                foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile))
                {
                    var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
                    if (neighbourAgent == null || neighbourAgent == agent) continue;
                    if (neighbourAgent.Team == agent.Team && !neighbourAgent.InFormation)
                    {
                        tilesWithNeighbours.Add(tile);
                    }
                }
            }
            if (tilesWithNeighbours.Count > 0)
            {
                bestTiles.Clear();
                bestTiles.Add(tilesWithNeighbours.Last(), GridManager.Instance.GetTile(tilesWithNeighbours.Last()));
            }
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
    public static Agent FindAttackTarget(Agent agent)
	{
		var tiles = GridManager.Instance.Tiles;
		var agents = GridManager.Instance.Agents;
		var lowestCost = -1;
		Agent target = null;

		foreach (var possibleTarget in agents)
		{
			if (possibleTarget.Value.Team == TurnManager.Instance.TeamTurn)
			{
				continue;
			}
            foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(possibleTarget.Key))
            {
				var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
				if (neighbourAgent != null) continue;

                // Get neighbours as tile with agent is disabled in A*
                GridManager.Instance.GetPath(agent.GridPosition, neighbourTile.Key, out var cost); 
				if (lowestCost == -1 || cost < lowestCost)
                {
                    lowestCost = cost;
                    target = possibleTarget.Value;
                }
            }
        }
        if (target == null) return agent;
        // GD.Print("Agent:" + agent.Name + " targetting " + target.Name + " at: " + target.GridPosition + " cost: " + lowestCost);
        return target;
	}

	private static int ScoreTileAttacking(Tile tile, Agent agent, Vector2I targetPos)
	{
		if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
		{
			if (tile.GridPosition != agent.GridPosition) 
			{ 
				return 0;
			}
		}
		var tiles = GridManager.Instance.Tiles;

		var score = 0;
		var enemies = 0;
		var friendlies = 0;

        foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
        {
			var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
			if (neighbourAgent == null) continue;

            if (neighbourTile.Key == targetPos)
            {
                score += 2;
            }
            if (neighbourAgent.Team != agent.Team)
            {
                enemies++;
            }
            else
            {
                friendlies++;
            }
        }

        // Only check for terrain if there is an enemy
		if (enemies > 0)
		{
			score+= 1;
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

	private static int ScoreTileFormingUp(Tile tile, Agent agent)
	{
		if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
		{
			return 0;
		}
		var tiles = GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange);

		var score = 0;
        foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
        {
			var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);

			if (neighbourAgent == null) continue;
			if (neighbourAgent.Team == agent.Team && neighbourAgent != agent)
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
        var agents = GridManager.Instance.Agents;
        // Check if a friendly agent that isn't in formation can move to a neighbour tile
        if (score == 0)
		{
            foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
            {
                if (GridManager.Instance.CheckForFriendlyAgentsThatCanMoveHere(neighbourTile.Value, agent, true))
                {
                    var enemyAgentNearby = false;
                    foreach (var otherAgent in agents)
                    {
                        if (otherAgent.Value.Team != agent.Team) {
                            foreach (var otherAgentNeighbour in GridManager.Instance.GetNeighbourTiles(otherAgent.Key))
                            {
                                GridManager.Instance.GetPath(tile.GridPosition, otherAgentNeighbour.Key, out var cost);
                                if (cost < agent.MoveRange + 2)
                                {
                                    enemyAgentNearby = true;
                                    break;
                                }
                            } 
                        }
                    }

                    if (!enemyAgentNearby)
                    {
                        score = 11;
                        break;
                    }
                    else
                    {
                        score = 8;
                    }
                }
            }
		}
		if (score > 0)
        {
            Dictionary<Agent, int> scoredAgents = new Dictionary<Agent, int>();
            foreach (var otherAgent in agents)
			{
				if (otherAgent.Value.Team == TurnManager.Instance.TeamTurn) continue;
                scoredAgents.Add(otherAgent.Value, -1);

                foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(otherAgent.Key))
                {
                    GridManager.Instance.GetPath(agent.GridPosition, neighbourTile.Key, out var oldCost);
                    GridManager.Instance.GetPath(tile.GridPosition, neighbourTile.Key, out var newCost);
                    var costDifference = oldCost - newCost;
                    if (costDifference > scoredAgents[otherAgent.Value])
                    {
                        scoredAgents[otherAgent.Value] = costDifference;
                    }

                    // Terrain should only matter if there is an enemy close to agent's move range
                    if (newCost <= agent.MoveRange + 1)
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
			GridManager.Instance.GetPath(agent.GridPosition, tile.GridPosition, out var cost);

            // Limit score gain from going closer to enemies at long distance
            if (scoredAgents.Count > 0)
            {
                if (cost > 4)
                {
                    var tempScore = 0;
                    foreach (var otherAgent in scoredAgents)
                    {
                        tempScore += Math.Max(Math.Min(otherAgent.Value, 5), 0);
                    }
                    tempScore /= scoredAgents.Count;
                    score += tempScore;
                }
                else
                {
                    var tempScore = 0;
                    foreach (var otherAgent in scoredAgents)
                    {
                        tempScore += Math.Max(otherAgent.Value, 0);
                    }
                    tempScore /= scoredAgents.Count;
                    score += tempScore;
                }
            }

			//GD.Print("Agent: " + agent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
		}
		return score;
	}
	private static int ScoreTileChasing(Tile tile, Agent agent, Vector2I targetPos)
	{
		if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
		{
			return 0;
		}
		var agents = GridManager.Instance.Agents;

		var score = 0;

        var getsCloserToTarget = false;
        Dictionary<Agent, int> scoredAgents = new Dictionary<Agent, int>();
        foreach (var otherAgent in agents)
        {
            var lowestCost = -1;
            scoredAgents.Add(otherAgent.Value, -1);
            foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(otherAgent.Key))
            {
                GridManager.Instance.GetPath(agent.GridPosition, neighbourTile.Key, out var oldCost);
                GridManager.Instance.GetPath(tile.GridPosition, neighbourTile.Key, out var newCost);

                var costDifference = oldCost - newCost;
                if (costDifference > scoredAgents[otherAgent.Value])
                {
                    scoredAgents[otherAgent.Value] = costDifference;
                }

                // Prevent going away from target
                if (otherAgent.Key == targetPos)
                {
                    if (newCost <= oldCost)
                    {
                        getsCloserToTarget = true;
                    }
                }

                if (lowestCost == -1 || newCost < lowestCost && otherAgent.Value.Team != agent.Team)
                {
                    lowestCost = newCost;
                }
            }

            // If there is an enemy within agent's move range + 2 tiles, never break formation
            if (lowestCost > -1 && lowestCost <= agent.MoveRange + 2)
            {
                if (agent.InFormation)
                {
                    var canReach = false;
                    foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
                    {
                        if (GridManager.Instance.CheckForFriendlyAgentsThatCanMoveHere(neighbourTile.Value, agent, false))
                        {
                            canReach = true;
                        }
                        else
                        {
                            var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
                            if (neighbourAgent != null && neighbourAgent.Team == agent.Team)
                            {
                                canReach = true;
                            }
                        }
                    }
                    if (!canReach)
                    {
                        return -5;
                    }
                }
            }
        }
        // Only add score if moving closer to agent's target
        if (!getsCloserToTarget)
        {
            return -10;
        }
        else
        {
            foreach (var otherAgent in scoredAgents)
            {
                if (otherAgent.Key.Team == agent.Team)
                {
                    score += otherAgent.Value;
                }
                else if (otherAgent.Key.GridPosition == targetPos)
                {
                    score += otherAgent.Value * 10;
                }
                else
                {
                    score += otherAgent.Value * 3;
                }
            }
        }
        if (score > 0)
        {
            //GD.Print("Agent: " + thisAgent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
        }
		return score;
	}
    private static int ScoreTileRetreating(Tile tile, Agent agent)
    {
        if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
        {
            return 0;
        }
		Dictionary<Vector2I, Tile> bases;
		if (agent.Team == 1)
		{
			bases = GridManager.Instance.Team1Bases;
		}
		else
		{
			bases = GridManager.Instance.Team2Bases;
		}

		var agents = GridManager.Instance.Agents;

        var score = 0;
        if (tile.IsBase && GridManager.Instance
            .GetReachableTiles(agent.GridPosition, agent.MoveRange)
            .Contains(tile))
        {
            score = 100;
        }
        if (score == 0)
        {
            Dictionary<Agent, int> scoredAgents = new Dictionary<Agent, int>();
            foreach (var _base in bases)
            {
                GridManager.Instance.GetPath(agent.GridPosition, _base.Key, out var oldCost);
                GridManager.Instance.GetPath(tile.GridPosition, _base.Key, out var newCost);
                score += (oldCost - newCost) * 6;
            }

            foreach (var otherAgent in agents)
            {
                var lowestCost = -1;
                scoredAgents.Add(otherAgent.Value, -1);
                foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(otherAgent.Key))
                {
                    GridManager.Instance.GetPath(agent.GridPosition, neighbourTile.Key, out var oldCost);
                    GridManager.Instance.GetPath(tile.GridPosition, neighbourTile.Key, out var newCost);

                    var costDifference = oldCost - newCost;
                    if (costDifference > scoredAgents[otherAgent.Value])
                    {
                        scoredAgents[otherAgent.Value] = costDifference;
                    }

                    if (lowestCost == -1 || newCost < lowestCost)
                    {
                        lowestCost = newCost;
                    }
                }
            }

            foreach (var otherAgent in scoredAgents)
            {
                if (otherAgent.Key.Team == agent.Team)
                {
                    score += otherAgent.Value;
                }
                else
                {
                    score -= otherAgent.Value * 3;
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

    public static int GetScoreForTile(Vector2I tilePos, Agent agent)
    {
        var tile = GridManager.Instance.GetTile(tilePos);
        var score = 0;
        var target = FindAttackTarget(agent);
        switch (agent.State)
        {
            case AgentState.Attacking:
                score = ScoreTileAttacking(tile, agent, target.GridPosition);
                break;
            case AgentState.Forming_up:
                score = ScoreTileFormingUp(tile, agent);
                break;
            case AgentState.Chasing:
                score = ScoreTileChasing(tile, agent, target.GridPosition);
                break;
            case AgentState.Retreating:
                score = ScoreTileRetreating(tile, agent);
                break;
        }
        return score;
    }
}
