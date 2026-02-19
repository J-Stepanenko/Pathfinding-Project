using Godot;
using System;

public partial class Tile : Node2D
{
	private Area2D area;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var meshInstance = this.GetChild<MeshInstance2D>(0);
        area = meshInstance.GetChild<Area2D>(0);
		area.InputEvent += _on_mouse_press;
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
                GD.Print("pressed");
            }
		}
	}
}
