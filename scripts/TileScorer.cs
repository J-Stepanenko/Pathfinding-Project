
using Godot;
using System.Collections.Generic;
using System.Linq;

public static class TileScorer
{
    public static Tile FindBestTile(Agent agent, AgentState state)
    {
        Dictionary<Vector2I, Tile> bestTiles = new Dictionary<Vector2I, Tile>();
        var targetPos = FindAttackTarget(agent).GridPosition;
        var bestScore = 0;
        switch (state)
        {
            case AgentState.Attacking:
                bestScore = ScoreTileAttacking(GridManager.Instance.GetTile(agent.GridPosition), agent, targetPos);
                bestTiles.Add(agent.GridPosition, GridManager.Instance.GetTile(agent.GridPosition));
                foreach (var tile in GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange))
                {
                    var score = ScoreTileAttacking(tile, agent, targetPos);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTiles.Clear();
                        bestTiles.Add(tile.GridPosition, tile);
                    }
                    else if (score == bestScore)
                    {
                        bestTiles.Add(tile.GridPosition, tile);
                    }
                }
                break;
            case AgentState.Forming_up:
                foreach (var tile in GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange))
                {
                    var score = ScoreTileFormingUp(tile, agent);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTiles.Clear();
                        bestTiles.Add(tile.GridPosition, tile);
                    }
                    else if (score == bestScore)
                    {
                        bestTiles.Add(tile.GridPosition, tile);
                    }
                }
                break;
            case AgentState.Chasing:
                foreach (var tile in GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange))
                {
                    var score = ScoreTileChasing(tile, agent, targetPos);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTiles.Clear();
                        bestTiles.Add(tile.GridPosition, tile);
                    }
                    else if (score == bestScore)
                    {
                        bestTiles.Add(tile.GridPosition, tile);
                    }
                }
                break;
        }
        foreach (var tile in bestTiles)
        {
            GD.Print(agent.Name + " Best tile is: " + tile.Key + " Score: " + bestScore);
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
                if (GridManager.Instance.GetTile(possibleTarget.Key + dir) == null) continue;
                // Get neighbours as tile with agent is disabled in A*
                GridManager.Instance.GetPath(agent.GridPosition, possibleTarget.Key + dir, out var cost);
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

    private static int ScoreTileFormingUp(Tile tile, Agent agent)
    {
        if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
        {
            return 0;
        }
        var tiles = GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange);

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
                if (neighbourAgent.Team == agent.Team && neighbourAgent != agent)
                {
                    if (neighbourAgent.InFormation)
                    {
                        score++;
                    }
                    else
                    {
                        score += 2;
                    }
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
            }
        }
        if (score > 0)
        {
            //GD.Print("Agent: " + agent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
        }
        return score;
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
                    score += (oldCost - newCost) * 6;
                }
                else
                {
                    score += (oldCost - newCost) * 3;
                }
            }
        }
        if (score > 0)
        {
            //GD.Print("Agent: " + thisAgent.Name + " score for tile: " + tile.GridPosition + " is: " + score);
        }
        return score;
    }
}
