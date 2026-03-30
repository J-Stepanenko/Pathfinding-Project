using Godot;
using System;

public partial class PathfindingToggler : Node
{
	[Export] bool Team1BasicPathfinding = false;
    [Export] bool Team1GeneticAlgorithm = false;
    [Export] bool Team2BasicPathfinding = false;
    [Export] bool Team2GeneticAlgorithm = false;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		CallDeferred(nameof(Init));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
    }

	private void Init()
	{
		if (Team1BasicPathfinding)
		{
			EmitSignal(SignalName.Team1BasicPathfindingEnabled);
        }
        if (Team2BasicPathfinding)
        {
            EmitSignal(SignalName.Team2BasicPathfindingEnabled);
        }
        if (Team1GeneticAlgorithm)
		{
			EmitSignal(SignalName.Team1GeneticAlgorithmEnabled);
        }
        if (Team2GeneticAlgorithm)
        {
            EmitSignal(SignalName.Team2GeneticAlgorithmEnabled);
        }
    }
    [Signal]
    public delegate void Team1BasicPathfindingEnabledEventHandler();

    [Signal]
    public delegate void Team2BasicPathfindingEnabledEventHandler();

    [Signal]
    public delegate void Team1GeneticAlgorithmEnabledEventHandler();

    [Signal]
    public delegate void Team2GeneticAlgorithmEnabledEventHandler();
}
