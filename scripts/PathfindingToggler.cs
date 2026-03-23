using Godot;
using System;

public partial class PathfindingToggler : Node
{
	[Export] bool BasicPathfinding = false;
	[Export] bool GeneticAlgorithm = false;
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
		if (BasicPathfinding)
		{
			EmitSignal(SignalName.BasicPathfindingEnabled);
		}
		if (GeneticAlgorithm)
		{
			EmitSignal(SignalName.GeneticAlgorithmEnabled);
		}
	}
    [Signal]
    public delegate void BasicPathfindingEnabledEventHandler();

    [Signal]
    public delegate void GeneticAlgorithmEnabledEventHandler();
}
