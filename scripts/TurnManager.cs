using Godot;
using System;

public partial class TurnManager : Node
{
    public static TurnManager Instance { get; private set; }
    public int Turn;
    public int TeamTurn;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;

        Turn = 1;
        TeamTurn = 1;
        GD.Print("TurnManager loaded");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    public void EndTurn()
    {
        EmitSignal(SignalName.DoAITurn);
        if (TeamTurn+1 > 2)
        {
            TeamTurn = 1;
            Turn++;
        }
        else
        {
            TeamTurn++;
        }
        GD.Print("Turn: "+Turn);
        GD.Print("Team: "+TeamTurn);
        EmitSignal(SignalName.TurnEnded);
    }

    [Signal]
    public delegate void TurnEndedEventHandler();

    [Signal]
    public delegate void DoAITurnEventHandler();
}
