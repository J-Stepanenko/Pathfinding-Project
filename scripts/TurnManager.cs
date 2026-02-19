using Godot;
using System;

public partial class TurnManager : Node
{
    public static TurnManager Instance { get; private set; }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
        GD.Print("TurnManager loaded");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    public void EndTurn()
    {
        EmitSignal(SignalName.TurnEnded);
    }

    [Signal]
    public delegate void TurnEndedEventHandler();
}
