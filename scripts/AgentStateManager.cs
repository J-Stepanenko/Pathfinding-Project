
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
        Vector2I[] directions =
        {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };
        foreach (var tile in GridManager.Instance.GetReachableTiles(agent.GridPosition, agent.MoveRange))
        {
            foreach (var dir in directions) 
            {
                var neighbourPos = tile.GridPosition + dir;
                var neighbourTile = GridManager.Instance.GetTile(neighbourPos); 

                if (GridManager.Instance.CheckTileHasAgent(neighbourPos))
                {
                    var tileAgent = GridManager.Instance.GetAgent(neighbourPos);
                    if (tileAgent.Team != TurnManager.Instance.TeamTurn)
                    {
                        return AgentState.Attacking;
                    }
                    else
                    {
                        if (!agent.InFormation)
                        {
                            return AgentState.Forming_up;
                        }
                    }
                }
            }
        }
        return AgentState.Chasing;
    }
}
