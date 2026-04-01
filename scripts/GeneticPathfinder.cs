using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static partial class GeneticPathfinder
{
	const int PopulationSize = 30;
	const int TournamentSelectionAmount = 10;
	const double MutationChance = 0.05;
	const int MutationChangeMax = 3;
	const int Generations = 10;
	const double CrossoverRate = 0.8;
	const int ElitismCount = 1;

	private static Dictionary<Vector2I, int> CreatePopulationAndGetFitness(Agent agent)
	{
		var selectedTiles = new Dictionary<Vector2I, int>();
		var tilesArray = new List<Vector2I>();
		var indexes = new List<int>();
        foreach (var tile in GridManager.Instance.Tiles)
        {
            // Make sure tile isnt occupied
            if (GridManager.Instance.GetAgent(tile.Key) == null)
            {
                tilesArray.Add(tile.Key);
            }
        }
		// Always add bases to population if retreating
		if (agent.State == AgentState.Retreating)
		{
			if (agent.Team == 1)
			{
				foreach (var tile in GridManager.Instance.Team1Bases)
				{
					selectedTiles.Add(tile.Key, FindFitness(tile.Key, agent, GridManager.Instance.Agents));
					indexes.Add(tilesArray.IndexOf(tile.Key));

                }
            }
            if (agent.Team == 2)
            {
                foreach (var tile in GridManager.Instance.Team2Bases)
                {
                    selectedTiles.Add(tile.Key, FindFitness(tile.Key, agent, GridManager.Instance.Agents));
                    indexes.Add(tilesArray.IndexOf(tile.Key));
                }
            }
        }
        for (var i = 1; i <= PopulationSize; i++)
		{
			if (i >= tilesArray.Count) break;
			var nextTileFound = false;
			var loopTimes = 0;
			while (!nextTileFound)
            {
                if (loopTimes >= 100) break;
                var rng = new Random();
				var idx = rng.Next(0, tilesArray.Count);
				if (!indexes.Contains(idx))
				{
					indexes.Add(idx);
					nextTileFound = true;
				}
				loopTimes++;
			}
			if (loopTimes >= 100) break;
			var tile = tilesArray[indexes.Last()];
			selectedTiles.Add(tile, FindFitness(tile, agent, GridManager.Instance.Agents));
		}
		return selectedTiles;
	}

	private static int FindFitness(Vector2I tile, Agent agent, Dictionary<Vector2I, Agent> agents)
	{
		var tileScorer = new TileScorer(agents);
		return tileScorer.GetScoreForTile(tile, agent);
    }

    private static Dictionary<Vector2I, int> SelectElites(Dictionary<Vector2I, int> scores)
    {
        var selectedTiles = new Dictionary<Vector2I, int>();

#pragma warning disable CS0162 // Unreachable code detected
        if (ElitismCount == 0) return selectedTiles;
#pragma warning restore CS0162 // Unreachable code detected

        // Doesn't randomly select tiles, so will always select tiles in same order if there are multiple of same score
        foreach (var tile in scores)
        {
            if (selectedTiles.Count < ElitismCount)
            {
                selectedTiles.Add(tile.Key, tile.Value);
            }
            else if (tile.Value > selectedTiles.MinBy(kvp => kvp.Value).Value)
            {
                selectedTiles.Remove(selectedTiles.MinBy(kvp => kvp.Value).Key);
                selectedTiles.Add(tile.Key, tile.Value);
            }
        }
        return selectedTiles;
    }

    private static Vector2I TournamentSelection(Dictionary<Vector2I, int> scores)
    {
		var scoresList = scores.Keys.ToList();
		var selectedTiles = new List<Vector2I>();
		var rng = new Random();
		var indexes = new List<int>();
		while (indexes.Count < TournamentSelectionAmount)
		{
			indexes.Add(rng.Next(0, scoresList.Count));
            selectedTiles.Add(scoresList[indexes.Last()]);
            scoresList.Remove(scoresList[indexes.Last()]);
		}
		var parent = scores.MaxBy(kvp => selectedTiles.Contains(kvp.Key)).Key;

		return parent;
    }

    private static Vector2I TryCrossoverAndMutate(Vector2I parent1, Vector2I parent2, Vector2I maxGridSize)
	{
		var combinedX = (int)Math.Round((double)(parent1.X + parent2.X) / 2);
        var combinedY = (int)Math.Round((double)(parent1.Y + parent2.Y) / 2);
		var child = new Vector2I(combinedX, combinedY);
		var rng = new Random();
		if (rng.NextDouble() > CrossoverRate) 
		{
			if (rng.Next(1,3) == 1)
			{
				child = parent1;
			}
			else
			{
				child = parent2;
			}
		}
		if (rng.NextDouble() <= MutationChance)
		{
			if (rng.Next(1, 3) == 1)
			{
				child.X += rng.Next(-MutationChangeMax, MutationChangeMax + 1);
            }
			else
			{
                child.Y += rng.Next(-MutationChangeMax, MutationChangeMax + 1);
            }
        }

        if (child.X < 0) child.X = 0;
        else if (child.X > maxGridSize.X) child.X = maxGridSize.X;

        if (child.Y < 0) child.Y = 0;
        else if (child.Y > maxGridSize.Y) child.Y = maxGridSize.Y;

        return child;
    }

	public static Dictionary<Vector2I, Agent> RunGA(List<Agent> agentsToBeMoved)
	{
		var newAgentPositions = new Dictionary<Vector2I, Agent>(GridManager.Instance.Agents);
		var agentMoves = new Dictionary<Agent, Vector2I>();
        var agentPopulations = new Dictionary<Agent, Dictionary<Vector2I, int>>();

        var maxGridX = GridManager.Instance.Tiles.Keys.Max(pos => pos.X);
        var maxGridY = GridManager.Instance.Tiles.Keys.Max(pos => pos.Y);
        var maxGridSize = new Vector2I(maxGridX, maxGridY);

		var maxMoveRange = agentsToBeMoved[0].MoveRange;

		GD.Print("Agents to be moved count: " + agentsToBeMoved.Count);
        for (int i = 1; i <= Generations; i++)
        {
			GD.Print("Generation " + i);

			if (i > 1)
			{
				// Reduce move range by distance from real location
				// Required for tile scoring checking for nearby friendlies that can form up
				foreach (var agent in agentsToBeMoved)
				{
					agent.CanMove = true;
					GridManager.Instance.GetPath(agent.GridPosition, agentMoves[agent], out var cost);
					agent.MoveRange -= cost;
				}
			}

			foreach (var agent in agentsToBeMoved)
            {
				agent.MoveRange = maxMoveRange;
                Dictionary<Vector2I, int> population;
				if (i == 1)
				{
					population = CreatePopulationAndGetFitness(agent);
                }
				else
				{
                    population = agentPopulations[agent];
                }
				var rng = new Random();
				var loopTimes = 0;

				// Create children until population cap is reached
				var tempPop = SelectElites(population);
				while (tempPop.Count < PopulationSize)
				{
					if (loopTimes >= PopulationSize*100) break;
					var count = population.Count;
					var val = rng.Next(count);
					var parent1 = TournamentSelection(population);
					val = rng.Next(count);
					var parent2 = TournamentSelection(population);
					while (parent2 == parent1)
					{
						val = rng.Next(count);
						parent2 = TournamentSelection(population);
						break;
					}
					var child = TryCrossoverAndMutate(parent1, parent2, maxGridSize);
					if (!tempPop.ContainsKey(child))
					{
						tempPop.Add(child, FindFitness(child, agent, newAgentPositions));
					}
					loopTimes++;
				}
				population = tempPop;
                Vector2I previousPos;
                if (i == 1)
                {
                    previousPos = agent.GridPosition;
                }
                else
                {
                    previousPos = agentMoves[agent];
                }

				// Should be made redundant by same line beneath, but prevents a rare exception within the dictionary
                newAgentPositions.Remove(previousPos);

                var (bestTile, bestTileScore) = population.MaxBy(kvp => kvp.Value);
                var tileScorer = new TileScorer(newAgentPositions);
				// Check if current tile has a higher score than tiles in population
				if (tileScorer.GetScoreForTile(agent.GridPosition, agent) > bestTileScore)
				{
					bestTile = agent.GridPosition;
				}
				else
				{
					var path = GridManager.Instance.GetPath(agent.GridPosition, bestTile, agent.MoveRange);
					var idx = path.Count - 1;
					// Loop to find a path to the best tile
					while (true)
					{
						if (idx < 0)
						{
							bestTile = previousPos;
							break;
						}
						var pathTile = path[idx];
                        GD.Print("Best tile: " + bestTile + " score " + bestTileScore);
                        GD.Print("Last tile in path: " + path.Last());
                        GD.Print("Path tile: " + pathTile);
                        GD.Print("try get: " + newAgentPositions.TryGetValue(pathTile, out _) + " path tile: " + pathTile);
						// If tile is not occupied
                        if (!newAgentPositions.TryGetValue(pathTile, out _))
						{
							if (agent.State == AgentState.Attacking)
							{
								var neighbouringEnemyPathTile = false;
								foreach (var tile in GridManager.Instance.GetNeighbourTiles(pathTile))
								{
									var otherAgent = GridManager.Instance.GetAgent(tile.Key);
                                    if (otherAgent != null && otherAgent.Team != agent.Team)
									{
										neighbouringEnemyPathTile = true;
										break;
									}
                                }

                                // If agent is pathing towards an enemy, but the final tile it goes to isn't next to an enemy
                                // Then try to find a tile along the path that still neighbours an enemy
                                if (!neighbouringEnemyPathTile)
                                {
                                    var pathIdx = path.Count - 1;
									while (pathIdx > 0)
                                    {
                                        pathIdx--; // skip last tile, already checked
                                        // If tile is occupied
                                        if (newAgentPositions.TryGetValue(path[pathIdx], out _))
										{
											pathIdx--;
											continue;
										}
										foreach (var tile in GridManager.Instance.GetNeighbourTiles(path[pathIdx]))
										{
                                            var otherAgent = GridManager.Instance.GetAgent(tile.Key);

                                            if (otherAgent != null && otherAgent.Team != agent.Team && !newAgentPositions.TryGetValue(path[pathIdx], out _))
                                            {
                                                pathTile = path[pathIdx];
                                                neighbouringEnemyPathTile = true;
												break;
                                            }
                                        }
                                    }

									// Check tile currently stood on if no suitable tiles were found along the path
									if (!neighbouringEnemyPathTile)
									{
                                        foreach (var tile in GridManager.Instance.GetNeighbourTiles(agent.GridPosition))
                                        {
                                            var otherAgent = GridManager.Instance.GetAgent(tile.Key);

                                            if (otherAgent != null && otherAgent.Team != agent.Team)
                                            {
                                                pathTile = agent.GridPosition;
                                            }
                                        }
                                    }
                                }
                            }
                            GD.Print(agent.Name + " Generation " + i + " best tile at " + bestTile + " with score " + bestTileScore + " pathing to " + pathTile);
                            bestTile = pathTile;
							break;
						}
						idx--;
					}
                }

                // If this is the final generation, add final score to ScoreManager
                if (i == Generations)
                {
					var tempScorer = new TileScorer(GridManager.Instance.Agents);
                    ScoreManager.Instance.AddScore(agent, bestTileScore);

					//var bestTileControl = tempScorer.FindBestTile(agent).GridPosition;
					//var controlPath = GridManager.Instance.GetPath(agent.GridPosition, bestTileControl, maxMoveRange);
     //               ScoreManager.Instance.AddVariance(bestTile, controlPath.Last(), agent);
                }

                GD.Print(agent.Name + "Adding best tile to dict: " + bestTile);
				newAgentPositions.Remove(previousPos);
				agentMoves.Remove(agent);

				agentMoves.Add(agent, bestTile);
				newAgentPositions.Add(bestTile, agent);

				agentPopulations.Remove(agent);
				agentPopulations.Add(agent, population);
				agent.CanMove = false;
            }
			// Clear dictionary of only the agents actually simulating moves, ie. the ones on the team whose turn it is currently
			foreach (var (pos, agent) in newAgentPositions)
			{
				if (agent.Team == agentsToBeMoved[0].Team)
				{
					newAgentPositions.Remove(pos);
				}
			}
			// Simulate each agent's move without actually moving them yet
			foreach (var (agent, move) in agentMoves)
			{
				newAgentPositions.Add(move, agent);
			}
		}

		foreach(var agent in agentsToBeMoved)
		{
			agent.MoveRange = maxMoveRange;
			agent.CanMove = true;
		}

        // Clear dictionary of the agents whose moves are not being calculated this turn
        foreach (var (pos, agent) in newAgentPositions)
        {
            if (agent.Team != agentsToBeMoved[0].Team)
            {
                newAgentPositions.Remove(pos);
            }
        }

        return newAgentPositions;
    }
}