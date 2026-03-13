using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AIManager : Node
{
	public static AIManager Instance { get; private set; }

    private bool BasicPathfindingEnabled = false;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Instance = this;
		CallDeferred(nameof(Init));
		GD.Print("AI Manager loaded");
	}

	private void Init()
	{
        var pathfindingNode = (PathfindingToggler)GetNode("/root/Scene/PathfindingToggler");
        pathfindingNode.BasicPathfindingEnabled += SetPathfindingToBasic;
    }

    private void SetPathfindingToBasic()
    {
        BasicPathfindingEnabled = true;
    }

    public void DoAITurns()
    {
        var agentsDict = GridManager.Instance.Agents;
        var agentsValues = new List<Agent>();
        foreach (var agent in agentsDict)
        {
            agentsValues.Add(agent.Value);
        }

        // Basic pathfinding using A* to go directly to closest enemy
        if (BasicPathfindingEnabled)
		{
            foreach (var agent in agentsValues)
            {
                if (agent.AIEnabled && agent.Team == TurnManager.Instance.TeamTurn)
                {
                    var target = TileScorer.FindAttackTarget(agent);
                    Vector2I closest = agent.GridPosition;
                    int lowestCost = -1;
                    foreach (var neighbour in GridManager.Instance.GetNeighbourTiles(target.GridPosition))
                    {
                        if (neighbour.Key == agent.GridPosition)
                        {
                            closest = agent.GridPosition;
                            break;
                        }
                        GridManager.Instance.GetPath(agent.GridPosition, neighbour.Key, out var cost);
                        var tile = GridManager.Instance.GetPath(agent.GridPosition, neighbour.Key, agent.MoveRange).LastOrDefault();

                        // If tile is default value for Vector2I
                        if (tile == new Vector2I(0, 0)) continue;

                        if (lowestCost == -1 || cost < lowestCost && !GridManager.Instance.CheckTileHasAgent(tile))
                        {
                            closest = tile;
                        }
                    }
                    agent.MoveAgent(closest);
                    agent.DoAICombat();
                }
            }
		}
		else
		{
            foreach (var agent in agentsValues)
            {
                agent.DoAIMove();

                // Try to attack sooner in case formation is broken on the next agent's move
                if (agent.InFormation)
                {
                    agent.DoAICombat();
                }
            }
            foreach (var agent in agentsValues)
            {
                agent.DoAICombat();
            }
            foreach (var agent in agentsDict)
            {
                GD.Print(agent.Value.Name + " at " + agent.Key);
            }
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
