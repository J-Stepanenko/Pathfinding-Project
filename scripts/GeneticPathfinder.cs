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
	const int Generations = 10;

	private static Dictionary<Vector2I, int> CreatePopulationAndGetFitness(Agent agent)
	{
		var selectedTiles = new Dictionary<Vector2I, int>();
		var tilesArray = new List<Vector2I>();
		var indexes = new List<int>();
		foreach (var tile in GridManager.Instance.Tiles)
		{
			tilesArray.Add(tile.Key);
		}
		for (var i = 1; i <= PopulationSize; i++)
		{
			var nextTileFound = false;
			while (!nextTileFound)
			{
				var rng = new Random();
				var idx = rng.Next(0, tilesArray.Count);
				if (!indexes.Contains(idx))
				{
					indexes.Add(idx);
					// Make sure tile isnt occupied
					if (GridManager.Instance.GetAgent(tilesArray[indexes.Last()]) != null) continue;
					nextTileFound = true;
				}
			}
			var tile = tilesArray[indexes.Last()];
			selectedTiles.Add(tile, FindFitness(tile, agent));
		}
		return selectedTiles;
	}

	private static int FindFitness(Vector2I tile, Agent agent)
	{
		return TileScorer.GetScoreForTile(tile, agent);
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

		GD.Print("Crossover between: " + parent1 + " and " + parent2 + " max grid size: " + maxGridSize + " child: " + child);

        return child;
    }

	public static Tile RunGA(Agent agent)
    {
		var maxGridX = GridManager.Instance.Tiles.Keys.Max(pos => pos.X);
        var maxGridY = GridManager.Instance.Tiles.Keys.Max(pos => pos.Y);
        var maxGridSize = new Vector2I(maxGridX, maxGridY);
        var population = CreatePopulationAndGetFitness(agent);
		for (int i = 1; i <= Generations; i++)
		{
			GD.Print(agent.Name + " Generation " + i);
			var parents = TournamentSelection(population);
			population.Clear();
			foreach (var parent in parents)
			{
				population.Add(parent.Key, parent.Value);
			}
			var rng = new Random();
			GD.Print("pop size: " + population.Count);
			while (population.Count < PopulationSize)
			{
                var parentList = parents.Keys.ToList();
				var count = parentList.Count;
                var val = rng.Next(count);
                GD.Print("list size: " + count);
                GD.Print("rng value: " + val);
                var parent1 = parentList[val];
                val = rng.Next(count);
                GD.Print("rng value: " + val);
                var parent2 = parentList[val];
                while (parent2 == parent1)
                {
                    val = rng.Next(count);
                    GD.Print("rng value: " + val);
                    parent2 = parentList[val];
                }
				var child = CrossoverAndMutate(parent1, parent2, maxGridSize);
				if (!population.ContainsKey(child))
				{
					population.Add(child, FindFitness(child, agent));
				}
			}
		}
        var bestTile = population.MaxBy(kvp => kvp.Value).Key;
		return GridManager.Instance.GetTile(bestTile);
	}
}
