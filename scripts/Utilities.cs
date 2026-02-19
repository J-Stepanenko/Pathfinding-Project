using Godot;
using System;

public static class Utilities
{
	private static int offset = 50;
	public static Vector2I GetGridPosFromNode(Node2D thing)
	{
		var x = thing.Position.X - offset;
		var y = thing.Position.Y - offset;
		return new Vector2I((int)x / 100, (int)y / 100);
	}

	public static Vector2I GetGridPosFromVector(Vector2 pos)
	{
		var x = pos.X - offset;
		var y = pos.Y - offset;
		return new Vector2I((int)x/100, (int)y / 100);
	}

	public static Vector2 GetRealCoordinatesFromGridPos(Vector2I pos)
	{
		var x = pos.X*100 + offset;
		var y = pos.Y*100 + offset;
		return new Vector2(x, y);
	}
}
