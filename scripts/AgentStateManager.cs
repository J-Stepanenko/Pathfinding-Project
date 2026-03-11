
using Godot;
using Godot.NativeInterop;

public enum AgentState
{
	Attacking, // When enemy is in range
	Chasing, // No enemies in range
	Forming_up, // Getting into a formation
	Retreating // Retreating away from the enemy
}
public partial class AgentStateManager : Node
{
	public static AgentStateManager Instance { get; private set; }
	public override void _Ready()
	{
		Instance = this;

		GD.Print("AgentStateManager loaded");
	}

	public AgentState CalculateState(Agent agent)
	{
		if (agent.Health < 50)
		{
			return AgentState.Retreating;
		}
		// First check current tile's neighbours for enemies
		foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(agent.GridPosition))
		{
			var neighbourAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
			if (neighbourAgent != null && neighbourAgent.Team != agent.Team)
			{
                return AgentState.Attacking;
            }
		}

		// Then check all reachable tiles this turn
        AgentState? currentState = null;
        foreach (var tile in GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange))
		{
			foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
			{
				var tileAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
				if (tileAgent == null) continue;
				if (tileAgent == agent) continue;


                if (tileAgent.Team != TurnManager.Instance.TeamTurn)
				{
					return AgentState.Attacking;
				}
				else
				{
					if (!agent.InFormation)
					{
						currentState = AgentState.Forming_up;
					}
				}
			}
        }
        if (currentState != null) return (AgentState)currentState;
        // Then check all tiles reachable within 2 moves
        foreach (var tile in GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange * 2))
        {
			foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
			{
                var tileAgent = GridManager.Instance.GetAgent(neighbourTile.Key);
                if (tileAgent == null) continue;
                if (tileAgent == agent) continue;

                if (tileAgent.Team == TurnManager.Instance.TeamTurn)
                {
                    if (!tileAgent.InFormation && !agent.InFormation)
					{
						return AgentState.Forming_up;
					}
				}
            }
        }
        return AgentState.Chasing;
	}
}
