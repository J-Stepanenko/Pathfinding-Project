
using Godot;
using System.Collections.Generic;
using System.Linq;
using static AgentStateMachine;

public static class TileScorer
{
    public static Tile FindBestTile(Agent agent, AgentStates state)
    {
        var tiles = GridManager.Instance.tiles;
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        var bestScore = 0;
        switch (state)
        {
            case (AgentStates.Attacking):
                foreach (var tile in tiles)
                {
                    var score = ScoreTileAttacking(tile.Key, agent, tiles);
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
                break;
        }

        return GridManager.Instance.FindClosestTile(bestTiles, agent.GridPosition);

    }

    private static int ScoreTileAttacking(Vector2I tilePos, Agent agent, Dictionary<Vector2I, Tile> tiles)
    {
        if (!GridManager.Instance.GetTile(tilePos)
            .CanPassThisTurn)
        {
            return 0;
        }

        var score = 0;
        var enemies = 0;
        var friendlies = 0;
        Vector2I[] directions =
        {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };

        foreach (var dir in directions)
        {
            var neighborPos = tilePos + dir;

            if (!tiles.ContainsKey(neighborPos)) continue;

            tiles.TryGetValue(neighborPos, out Tile neighbour);
            if (GridManager.Instance.CheckTileHasAgent(neighborPos))
            {
                var neighbourAgent = GridManager.Instance.GetAgent(neighborPos);
                if (neighbourAgent.Team != agent.Team)
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
            score++;
        }

        // Incentivise attacking enemies together with teammates
        if (enemies > 0 && friendlies > 0)
        {
            for (int i = 1; i <= friendlies; i++)
            {
                score++;
            }
        }
        return score;
    }
}
