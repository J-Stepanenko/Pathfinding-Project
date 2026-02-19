using Godot;
using System;

public partial class Tile : Node2D
{
	private Area2D area;

	[Export] public int MoveCost = 1;
	[Export] public bool IsWalkable = true;

	public Vector2I GridPosition;
	public bool Highlighted = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var meshInstance = this.GetChild<MeshInstance2D>(0);
        area = meshInstance.GetChild<Area2D>(0);
		area.InputEvent += _on_mouse_press;

		GridPosition = Utilities.GetGridPosFromNode(this);

        GridManager.Instance.RegisterTile(GridPosition, this);
        TurnManager.Instance.TurnEnded += OnTurnEnd;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void SetHighlight(bool highlighted)
	{
		this.Highlighted = highlighted;
		var mesh = this.GetChild<MeshInstance2D>(0);
		mesh.SelfModulate = highlighted ? Colors.Green : Colors.White;
    }

	public void _on_mouse_press(Node viewport, InputEvent @event, long shapeIdx) 
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				var hasAgent = GridManager.Instance.CheckTileHasAgent(GridPosition);
				if (hasAgent)
				{
					return;
				}
				InputManager.Instance.TileSelected(Highlighted, this);
            }
			else if (mouseEvent.ButtonIndex == MouseButton.Right && Highlighted)
			{

			}
		}
	}

	private void OnTurnEnd()
	{
		if (GridManager.Instance.CheckTileHasAgent(GridPosition))
		{
			var agent = GridManager.Instance.GetAgent(GridPosition);
			if (agent.Team != TurnManager.Instance.TeamTurn)
			{
				IsWalkable = false;
			}
			else
			{
				IsWalkable = true;
			}
		}
		else
		{
			IsWalkable = true;
		}
	}
}
