using Godot;
using System;

public partial class InputManager : Node
{
	public static InputManager Instance { get; private set; }

	private Agent selectedAgent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		GD.Print("InputManager loaded");
		CallDeferred(nameof(Init));
	}

	private void Init()
    {
        TurnManager.Instance.TurnEnded += DeselectAgent;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SelectAgent(Agent agent)
	{
		DeselectAgent();
		selectedAgent = agent;
		agent.OnSelected();
	}

	public void DeselectAgent()
	{
		if (selectedAgent != null)
		{
			selectedAgent.OnDeselected();
			selectedAgent = null;
			EmitSignal(SignalName.AgentDeselected);
		}
	}

	[Signal]
	public delegate void AgentDeselectedEventHandler();

	public void TileSelected(bool reachable, Tile tile)
	{
		if (reachable && selectedAgent != null)
		{
			if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition))
			{
				var agent = GridManager.Instance.GetAgent(tile.GridPosition);
				if (agent.Team != selectedAgent.Team)
				{
					CombatManager.Instance.ResolveCombat(selectedAgent, agent);
                    DeselectAgent();
                    return;
				}
			}
			var oldPos = Utilities.GetGridPosFromVector(selectedAgent.Position);
			var newPos = Utilities.GetGridPosFromNode(tile);

			var path = GridManager.Instance.GetPath(oldPos, newPos);
			selectedAgent.MoveAgent(newPos);

			DeselectAgent();
			foreach (var point in path)
			{
				GridManager.Instance.GetTile(point).ShowPath();
			}
		}
		else
		{
			DeselectAgent();
		}
	}
}
