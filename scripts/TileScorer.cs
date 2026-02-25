
using Godot;
using System.Collections.Generic;
using System.Linq;
using static AgentStateMachine;

public static class TileScorer
{
    public static Tile FindBestTile(Agent agent, AgentStates state)
    {
        var tiles = GridManager.Instance.Tiles;
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        var bestScore = 0;
        switch (state)
        {
            case (AgentStates.Attacking):
                var targetPos = FindAttackTarget(agent).GridPosition;
                foreach (var tile in tiles)
                {
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
                break;
        }

        return GridManager.Instance.FindClosestTile(bestTiles, agent.GridPosition);

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
                // Get neighbours as tile with agent is disabled in A*
                GridManager.Instance.GetPath(agent.GridPosition, possibleTarget.Value.GridPosition + dir, out var cost);
                if (lowestCost == -1 || cost < lowestCost)
                {
                    lowestCost = cost;
                    target = possibleTarget.Value;
                }
            }
        }
        GD.Print("Agent:" + agent.Name + " targetting " + target.GridPosition+" cost: "+lowestCost);
        return target;
    }

    private static int ScoreTileAttacking(Tile tile, Agent agent, Vector2I targetPos)
    {
        var tiles = GridManager.Instance.Tiles;
        if (!tile
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
        if (score < 0)
        {
            score = 0;
        }
        if (score > 0)
        {
            GD.Print("Agent: " + agent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
        }
        return score;
    }
}
