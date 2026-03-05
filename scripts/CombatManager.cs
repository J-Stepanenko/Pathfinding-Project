using Godot;
using System;

public partial class CombatManager : Node
{
	public static CombatManager Instance { get; private set; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ResolveCombat(Agent attacker, Agent defender)
	{
		const double PlainsDefense = 1;
		const double ForestDefense = 1.5;
		const double MountainDefense = 2;
		const double RiverDefense = 0.75;

		var rng = new Random();
		double attackerDamage = (attacker.Health / 2) + rng.Next(-5, 6);
		double defenseValue = 1;
		switch (GridManager.Instance.GetTile(defender.GridPosition).Terrain)
		{
			case TileTerrain.Plains:
				defenseValue = PlainsDefense;
				break;
			case TileTerrain.Forest:
				defenseValue = ForestDefense;
				break;
			case TileTerrain.Mountain:
				defenseValue = MountainDefense;
				break;
			case TileTerrain.River:
				defenseValue = RiverDefense;
				break;
		}

		attackerDamage /= defenseValue;

		defender.Health = (int)Math.Round(defender.Health - attackerDamage);

		if (defender.Health > 0)
		{
			double defenderDamage = (defender.Health / 2) + rng.Next(-5, 6);
			switch (GridManager.Instance.GetTile(attacker.GridPosition).Terrain)
			{
				case TileTerrain.Plains:
					defenseValue = PlainsDefense;
					break;
				case TileTerrain.Forest:
					defenseValue = ForestDefense;
					break;
				case TileTerrain.Mountain:
					defenseValue = MountainDefense;
					break;
				case TileTerrain.River:
					defenseValue = RiverDefense;
					break;
			}

			defenderDamage /= defenseValue;
			attacker.Health = (int)Math.Round(attacker.Health - defenderDamage);
			attacker.TookDamage();
		}
		defender.TookDamage();
	}
}
