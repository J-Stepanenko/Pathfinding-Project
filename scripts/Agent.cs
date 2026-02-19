using Godot;
using System;
using System.Collections.Generic;

public partial class Agent : Node2D
{
    private Area2D area;

    [Export] public int MoveRange;
    [Export] public int Team;
    public Vector2I GridPosition;
    public bool CanMove;

    private List<Tile> reachableTiles = new();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        CanMove = true;
        var meshInstance = this.GetChild<MeshInstance2D>(0);
        area = meshInstance.GetChild<Area2D>(0);
        area.InputEvent += _on_mouse_press;

        GridPosition = Utilities.GetGridPosFromNode(this);

        GridManager.Instance.RegisterAgent(GridPosition, this);

        TurnManager.Instance.TurnEnded += OnTurnEnd;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void _on_mouse_press(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                InputManager.Instance.SelectAgent(this);
            }
        }
    }

    public void OnSelected()
    {
        // Check if it is this agent's turn
        if (TurnManager.Instance.TeamTurn != Team)
        {
            return;
        }

        GridPosition = Utilities.GetGridPosFromNode(this);
        if (CanMove)
        {
            reachableTiles = GridManager.Instance.GetReachableTiles(GridPosition, MoveRange);
            GridManager.Instance.HighlightTiles(reachableTiles, true);
        }
    }

    public void OnDeselected()
    {
        GridManager.Instance.HighlightTiles(reachableTiles, false);
        reachableTiles.Clear();
    }

    public void MoveAgent(Vector2I gridPos)
    {
        GD.Print(gridPos);
        this.Position = Utilities.GetRealCoordinatesFromGridPos(gridPos);
        CanMove = false;
    }

    private void OnTurnEnd()
    {
        CanMove = true;
    }
}
