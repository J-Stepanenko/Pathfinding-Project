using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static partial class GeneticPathfinder
{
	const int PopulationSize = 20;
	const int TournamentSelectionAmount = 10;
	const double MutationChance = 0.05;
	const int MutationChangeMax = 3;
	const int Generations = 5;

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

	private static Dictionary<Vector2I, int> TournamentSelection(Dictionary<Vector2I, int> scores)
	{
		var selectedTiles = new Dictionary<Vector2I, int>();
		// Doesn't randomly select tiles, so will always select tiles in same order if there are multiple of same score
		foreach(var tile in scores)
		{
			if (selectedTiles.Count < TournamentSelectionAmount)
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

	private static Vector2I CrossoverAndMutate(Vector2I parent1, Vector2I parent2, Vector2I maxGridSize)
	{
		var combinedX = (int)Math.Round((double)(parent1.X + parent2.X) / 2);
        var combinedY = (int)Math.Round((double)(parent1.Y + parent2.Y) / 2);
		var child = new Vector2I(combinedX, combinedY);
		var rng = new Random();
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

	public static Dictionary<Vector2I, Agent> RunGA(List<Agent> agents)
	{
		var newAgentPositions = new Dictionary<Vector2I, Agent>(GridManager.Instance.Agents);
		var agentMoves = new Dictionary<Agent, Vector2I>();
        var agentPopulations = new Dictionary<Agent, Dictionary<Vector2I, int>>();

        var maxGridX = GridManager.Instance.Tiles.Keys.Max(pos => pos.X);
        var maxGridY = GridManager.Instance.Tiles.Keys.Max(pos => pos.Y);
        var maxGridSize = new Vector2I(maxGridX, maxGridY);

        for (int i = 1; i <= Generations; i++)
        {
			//agentMoves.Clear();
			foreach (var agent in agents) agent.CanMove = true;

			foreach (var agent in agents)
            {
                Dictionary<Vector2I, int> population;
				if (i == 1)
				{
					population = CreatePopulationAndGetFitness(agent);
                }
				else
				{
                    population = agentPopulations[agent];
                }
				var parents = TournamentSelection(population);
				population.Clear();
				foreach (var parent in parents)
				{
					population.Add(parent.Key, parent.Value);
				}
				var rng = new Random();
				var loopTimes = 0;
				while (population.Count < PopulationSize)
                {
                    if (loopTimes >= 100) break;
                    var parentList = parents.Keys.ToList();
					var count = parentList.Count;
					var val = rng.Next(count);
					var parent1 = parentList[val];
					val = rng.Next(count);
					var parent2 = parentList[val];
					while (parent2 == parent1)
					{
						val = rng.Next(count);
						parent2 = parentList[val];
					}
					var child = CrossoverAndMutate(parent1, parent2, maxGridSize);
					if (!population.ContainsKey(child))
					{
						population.Add(child, 0);
					}
					loopTimes++;
				}
				var tempPop = new Dictionary<Vector2I, int>();
				foreach (var pop in population)
				{
					tempPop.Add(pop.Key, FindFitness(pop.Key, agent, newAgentPositions));
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
                newAgentPositions.Remove(previousPos);

                var (bestTile, bestTileScore) = population.MaxBy(kvp => kvp.Value);
				var tileScorer = new TileScorer(newAgentPositions);
				if (tileScorer.GetScoreForTile(agent.GridPosition, agent) > bestTileScore)
				{
					bestTile = agent.GridPosition;
				}
				else
				{
					var path = GridManager.Instance.GetPath(agent.GridPosition, bestTile, agent.MoveRange);
					var idx = path.Count - 1;
					while (true)
					{
						if (idx < 0)
						{
							bestTile = previousPos;
							break;
						}
						var pathTile = path[idx];
						GD.Print("try get: " + newAgentPositions.TryGetValue(pathTile, out _) + " path tile: " + pathTile);
						if (!newAgentPositions.TryGetValue(pathTile, out _))
						{
							GD.Print("Best tile: " + bestTile + " score " + bestTileScore);
							GD.Print("Last tile in path: " + path.Last());
							GD.Print("Path tile: " + pathTile);
							if (agent.State == AgentState.Attacking)
							{
								var neighbouringEnemyCurrTile = false;
								var neighbouringEnemyPathTile = false;
								foreach (var tile in GridManager.Instance.GetNeighbourTiles(agent.GridPosition))
								{
									var otherAgent = GridManager.Instance.GetAgent(tile.Key);

                                    if (otherAgent != null && otherAgent.Team != agent.Team)
									{
										neighbouringEnemyCurrTile = true;
										break;
									}
								}
								foreach (var tile in GridManager.Instance.GetNeighbourTiles(pathTile))
								{
									var otherAgent = GridManager.Instance.GetAgent(tile.Key);

                                    if (otherAgent != null && otherAgent.Team != agent.Team)
									{
										neighbouringEnemyPathTile = true;
										break;
									}
								}

                                // If agent is currently next to an enemy, but the tile it is trying to path towards is not next to an enemy
                                // Then try to find a tile along the path that still neighbours an enemy
                                if (neighbouringEnemyCurrTile && !neighbouringEnemyPathTile)
								{
									var pathIdx = path.Count - 2; // skip last tile, already checked
									var foundTile = false;
									while (pathIdx > 0)
									{
										// If tile is occupied
										if (GridManager.Instance.GetAgent(path[pathIdx], newAgentPositions) != null)
										{
											pathIdx--;
											continue;
										}
										foreach (var tile in GridManager.Instance.GetNeighbourTiles(path[pathIdx]))
										{
                                            var otherAgent = GridManager.Instance.GetAgent(tile.Key);

                                            if (otherAgent != null && otherAgent.Team != agent.Team)
                                            {
												foundTile = true;
												break;
                                            }
                                        }
										if (foundTile)
										{
											pathTile = path[pathIdx];
										}
										pathIdx--;
									}
                                }
							}
							bestTile = pathTile;
							break;
						}
						idx--;
					}
				}
				newAgentPositions.Remove(agent.GridPosition);
				agentMoves.Remove(agent);

				agentMoves.Add(agent, bestTile);
				newAgentPositions.Add(bestTile, agent);

				agentPopulations.Remove(agent);
				agentPopulations.Add(agent, population);
				agent.CanMove = false;
				GD.Print("Generation " + i + " best tile at " + bestTile + " with score " + bestTileScore);
            }
			// Clear dictionary of only the agents actually simulating moves, ie. the ones on the team whose turn it is currently
			foreach (var (pos, agent) in newAgentPositions)
			{
				if (agent.Team == agents[0].Team)
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
		return newAgentPositions;
    }
}