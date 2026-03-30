using Godot;
using System;

public partial class CombatManager : Node
{
	public static CombatManager Instance { get; private set; }

    const double PlainsDefense = 1;
    const double ForestDefense = 1.5;
    const double MountainDefense = 2;
    const double RiverDefense = 0.75;
    const double FormationAttBonus = 1.1;
    const double FormationDefBonus = 1.1;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Instance = this;
		GD.Print("CombatManager loaded");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ResolveCombat(Agent attacker, Agent defender)
	{
		var rng = new Random();
		double attackerDamage = Math.Max((attacker.Health / 2) * (attacker.CheckInFormation()? FormationAttBonus : 1), 5);
		double defenderDefense = defender.InFormation? FormationDefBonus : 1;
		switch (GridManager.Instance.GetTile(defender.GridPosition).Terrain)
		{
			case TileTerrain.Plains:
				defenderDefense *= PlainsDefense;
				break;
			case TileTerrain.Forest:
				defenderDefense *= ForestDefense;
				break;
			case TileTerrain.Mountain:
				defenderDefense *= MountainDefense;
				break;
			case TileTerrain.River:
				defenderDefense *= RiverDefense;
				break;
		}

        attackerDamage /= defenderDefense;
        attackerDamage = (int)Math.Round(attackerDamage);

        defender.HealthChanged(-(int)attackerDamage);
        double defenderDamage = 0;
		double attackerDefense = attacker.InFormation? FormationDefBonus : 1;

		if (defender.Health > 0)
		{
			defenderDamage = Math.Max((defender.Health / 2) * (defender.CheckInFormation() ? FormationAttBonus : 1), 5);
			switch (GridManager.Instance.GetTile(attacker.GridPosition).Terrain)
			{
				case TileTerrain.Plains:
                    attackerDefense *= PlainsDefense;
					break;
				case TileTerrain.Forest:
                    attackerDefense *= ForestDefense;
					break;
				case TileTerrain.Mountain:
                    attackerDefense *= MountainDefense;
					break;
				case TileTerrain.River:
                    attackerDefense *= RiverDefense;
					break;
			}

            defenderDamage /= attackerDefense;
            defenderDamage = (int)Math.Round(defenderDamage);

            attacker.HealthChanged(-(int)defenderDamage);
        }
        GD.Print(attacker.Name + " combat with " + defender.Name+", attacker damage = "+attackerDamage+" defender damage = "+defenderDamage+
			"\n Attacker formation bonus = "+attacker.InFormation+" Defender formation bonus = "+defender.InFormation);
    }

    /// <summary>
    /// Resolve combat without affecting either agents' health.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <returns>2D Array in form [AgentIndex[NewHealth, Damage]], in order of attacker then defender.</returns>
	public int[][] SimulateCombat(Agent attacker, Agent defender) 
	{
        var rng = new Random();
        double attackerDamage = Math.Max((attacker.Health / 2) * (attacker.CheckInFormation() ? FormationAttBonus : 1), 5);
        double defenderDefense = defender.InFormation ? FormationDefBonus : 1;
        switch (GridManager.Instance.GetTile(defender.GridPosition).Terrain)
        {
            case TileTerrain.Plains:
                defenderDefense *= PlainsDefense;
                break;
            case TileTerrain.Forest:
                defenderDefense *= ForestDefense;
                break;
            case TileTerrain.Mountain:
                defenderDefense *= MountainDefense;
                break;
            case TileTerrain.River:
                defenderDefense *= RiverDefense;
                break;
        }

        attackerDamage /= defenderDefense;
        attackerDamage = (int)Math.Round(attackerDamage);

        var simulatedDefenderHealth = defender.Health - attackerDamage;
        double defenderDamage = 0;
        double attackerDefense = attacker.InFormation ? FormationDefBonus : 1;

        if (simulatedDefenderHealth > 0)
        {
            defenderDamage = Math.Max((simulatedDefenderHealth / 2) * (defender.CheckInFormation() ? FormationAttBonus : 1), 5);
            switch (GridManager.Instance.GetTile(attacker.GridPosition).Terrain)
            {
                case TileTerrain.Plains:
                    attackerDefense *= PlainsDefense;
                    break;
                case TileTerrain.Forest:
                    attackerDefense *= ForestDefense;
                    break;
                case TileTerrain.Mountain:
                    attackerDefense *= MountainDefense;
                    break;
                case TileTerrain.River:
                    attackerDefense *= RiverDefense;
                    break;
            }

            defenderDamage /= attackerDefense;
            defenderDamage = (int)Math.Round(defenderDamage);
        }
        var simulatedAttackerHealth = (int)(attacker.Health - defenderDamage);
        simulatedDefenderHealth = (int)(defender.Health - attackerDamage);
        GD.Print(attacker.Name + " simulated combat with " + defender.Name + ", attacker damage = " + attackerDamage + " defender damage = " + defenderDamage +
            "\n Attacker formation bonus = " + attacker.InFormation + " Defender formation bonus = " + defender.InFormation);
        return new int[][]
            {
                new int[] { simulatedAttackerHealth, (int)attackerDamage },
                new int[] { (int)simulatedDefenderHealth, (int)defenderDamage }
            };
    }
}
