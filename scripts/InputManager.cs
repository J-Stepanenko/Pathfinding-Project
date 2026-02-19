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
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SelectAgent(Agent agent)
	{
		selectedAgent = agent;
		agent.OnSelected();
	}

	public void DeselectAgent()
	{
		if (selectedAgent != null)
		{
			selectedAgent.OnDeselected();
			selectedAgent = null;
		}
	}

	public void TileSelected(bool reachable, Tile tile)
	{
		if (reachable && selectedAgent != null)
		{
			// Remove old position from grid manager
			var oldPos = Utilities.GetGridPosFromVector(selectedAgent.Position);
            GridManager.Instance.DeregisterAgent(oldPos);

			// Move agent and add new position to grid manager
			var newPos = Utilities.GetGridPosFromNode(tile);
			var path = GridManager.Instance.GetPath(oldPos, newPos);
			selectedAgent.MoveAgent(newPos);
			GridManager.Instance.RegisterAgent(newPos, selectedAgent);

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
