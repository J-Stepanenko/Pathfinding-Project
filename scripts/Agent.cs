using Godot;
using System;
using System.Collections.Generic;

public partial class Agent : Node2D
{
    private Area2D area;

    [Export] public int MoveRange;
    public Vector2I GridPosition;

    private List<Tile> highlightedTiles = new();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        var meshInstance = this.GetChild<MeshInstance2D>(0);
        area = meshInstance.GetChild<Area2D>(0);
        area.InputEvent += _on_mouse_press;

        var x = this.Position.X - 50; // Offset by 50
        var y = this.Position.Y - 50;
        GridPosition = new Vector2I((int)x / 100, (int)y / 100);
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
                OnSelected();
            }
        }
    }

    public void OnSelected()
    {
        highlightedTiles = GridManager.Instance.GetReachableTiles(GridPosition, MoveRange);
        foreach (var tile in highlightedTiles)
        {
            tile.SetHighlight(true);
        }
    }

    public void OnDeselected()
    {
        foreach (var tile in highlightedTiles)
        {
            tile.SetHighlight(false);
        }
        highlightedTiles.Clear();
    }
}
