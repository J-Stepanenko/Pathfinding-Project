using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AIManager : Node
{
	public static AIManager Instance { get; private set; }

    private bool Team1BasicPathfindingEnabled = false;
    private bool Team2BasicPathfindingEnabled = false;
    private bool Team1GeneticAlgorithmEnabled = false;
    private bool Team2GeneticAlgorithmEnabled = false;
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
        pathfindingNode.Team1BasicPathfindingEnabled += SetPathfindingToBasicTeam1;
        pathfindingNode.Team2BasicPathfindingEnabled += SetPathfindingToBasicTeam2;
        pathfindingNode.Team1GeneticAlgorithmEnabled += SetGeneticAlgorithmEnabledTeam1;
        pathfindingNode.Team2GeneticAlgorithmEnabled += SetGeneticAlgorithmEnabledTeam2;
    }

    private void SetPathfindingToBasicTeam1()
    {
        GD.Print("Team 1 basic pathfinding");
        Team1BasicPathfindingEnabled = true;
    }
    private void SetPathfindingToBasicTeam2()
    {
        GD.Print("Team 2 basic pathfinding");
        Team2BasicPathfindingEnabled = true;
    }
    private void SetGeneticAlgorithmEnabledTeam1()
    {
        GD.Print("Team 1 genetic pathfinding");
        Team1GeneticAlgorithmEnabled = true;
    }
    private void SetGeneticAlgorithmEnabledTeam2()
    {
        GD.Print("Team 2 genetic pathfinding");
        Team2GeneticAlgorithmEnabled = true;
    }

    public void DoAITurns()
    {
        var agentsDict = GridManager.Instance.Agents;
        var agentsValues = new List<Agent>();
        foreach (var agent in agentsDict)
        {
            agentsValues.Add(agent.Value);
            if (agent.Value.Team == TurnManager.Instance.TeamTurn)
            {
                agent.Value.State = agent.Value.CheckState();
            }
        }

        bool basicPathfinding = false;
        bool geneticAlgorithm = false;

        var team = TurnManager.Instance.TeamTurn;
        if (team == 1)
        {
            if (Team1BasicPathfindingEnabled) basicPathfinding = true;
            if (Team1GeneticAlgorithmEnabled) geneticAlgorithm = true;
        }
        else if (team == 2)
        {

            if (Team2BasicPathfindingEnabled) basicPathfinding = true;
            if (Team2GeneticAlgorithmEnabled) geneticAlgorithm = true;
        }

        // Basic pathfinding using A* to go directly to closest enemy
        if (basicPathfinding)
        {
            foreach (var agent in agentsValues)
            {
                if (agent.AIEnabled && agent.Team == TurnManager.Instance.TeamTurn)
                {
                    var tileScorer = new TileScorer(GridManager.Instance.Agents);
                    var target = tileScorer.FindAttackTarget(agent);
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
                    agent.PathTowards(closest);
                    agent.DoAICombat();
                }
            }
        }
        else if (geneticAlgorithm)
        {
            var agentsToBeMoved = new List<Agent>();
            foreach (var agent in agentsValues)
            {
                agent.TryHeal();
                if (agent.AIEnabled && agent.CanMove && agent.Team == TurnManager.Instance.TeamTurn)
                {
                    agentsToBeMoved.Add(agent);
                }
            }
            if (agentsToBeMoved.Count > 0)
            {
                var tileScorer = new TileScorer(GridManager.Instance.Agents);
                var result = GeneticPathfinder.RunGA(agentsToBeMoved);
                var loopTimesMax = 10;
                int i = 0;

                while (agentsToBeMoved.Count > 0)
                {
                    i++;
                    foreach (var (pos, agent) in result)
                    {
                        var oldPos = agent.GridPosition;
                        var deterministicBest = tileScorer.FindBestTile(agent);
                        var pathToBest = GridManager.Instance.GetPath(oldPos, deterministicBest.GridPosition, agent.MoveRange);
                        var pathTile = pathToBest.Count > 0 ? pathToBest.Last() : agent.GridPosition;
                        if (agent.PathTowards(pos))
                        {
                            agentsToBeMoved.Remove(agent);
                            ScoreManager.Instance.AddVariance(agent.GridPosition, pathTile, agent);
                        }
                    }
                    if (i >= loopTimesMax) break;
                }

                // Remaining agents that failed to move 
                foreach(var agent in agentsToBeMoved)
                {
                    var deterministicBest = tileScorer.FindBestTile(agent);
                    var pathToBest = GridManager.Instance.GetPath(agent.GridPosition, deterministicBest.GridPosition, agent.MoveRange);
                    var pathTile = pathToBest.Count > 0 ? pathToBest.Last() : agent.GridPosition;
                    ScoreManager.Instance.AddVariance(agent.GridPosition, pathTile, agent);
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
                else
                {
                    foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(agent.GridPosition))
                    {
                        if (!GridManager.Instance.CheckForFriendlyAgentsThatCanMoveHere(neighbourTile.Value, agent, true))
                        {
                            // Do combat early if no friendly agents are capable of forming up, allowing other agents to go for different targets if enemy is killed
                            agent.DoAICombat();
                        }
                    }
                }
            }
        }
        // Only do combat after all agents move (unless specific exceptions) to benefit off formation bonuses as much as possible
        foreach (var agent in agentsValues)
        {
            agent.DoAICombat();
        }
        foreach (var agent in agentsDict)
        {
            GD.Print(agent.Value.Name + " at " + agent.Key);
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
