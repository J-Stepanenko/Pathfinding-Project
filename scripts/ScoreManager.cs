using Godot;
using Godot.Collections;
using System;
using static System.Formats.Asn1.AsnWriter;

public partial class ScoreManager : Node
{
	public static ScoreManager Instance;
	private Dictionary<Agent, int> Scores;
	private Dictionary<Agent, int> Variances;

	const double AttackMultiplier = 5;
	const double FormingUpMultiplier = 1;
	const double ChasingMultiplier = 2;
	const double RetreatingMultiplier = 0.5;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		Scores = new Dictionary<Agent, int>();
		Variances = new Dictionary<Agent, int>();
		GD.Print("ScoreManager loaded");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void AddScore(Agent agent, int score)
	{
		if (score < 0) score = 0;
		if (Scores.ContainsKey(agent))
		{
			Scores.TryGetValue(agent, out var currScore);
			var tempScore = score * GetMultiplier(agent);
			currScore += (int)Math.Round(tempScore);
			Scores.Remove(agent);
			Scores.Add(agent, currScore);
		}
		else
		{
			Scores.Add(agent, score);
		}
	}

	public void DisplayFinalScores()
	{
		GD.Print("Displaying Final scores and variances");
		foreach (var (agent, score) in Scores)
		{
			GD.Print(agent.Name + " score: " + score);
        }
        foreach (var (agent, variance) in Variances)
        {
            GD.Print(agent.Name + " variance: " + variance);
        }
        GD.Print("Final turn: " + TurnManager.Instance.Turn);
	}

	private double GetMultiplier(Agent agent)
	{
		double mult = 1;
		switch (agent.State)
		{
			case (AgentState.Attacking):
				mult = AttackMultiplier;
				break;
			case (AgentState.Forming_up):
				mult = FormingUpMultiplier;
                break;
            case (AgentState.Chasing):
				mult = ChasingMultiplier;
                break;
            case (AgentState.Retreating):
				mult = RetreatingMultiplier;
                break;
        }
		return mult;
	}

	/// <summary>
	/// Adds and stores the difference between the test tile (the tile that actually has been selected) against the control tile (the ideal tile to move to according to TileScorer)
	/// </summary>
	/// <param name="testTile"></param>
	/// <param name="controlTile"></param>
	/// <param name="agent"></param>
	public void AddVariance(Vector2I testTile, Vector2I controlTile, Agent agent)
	{
		var variance = Math.Abs(testTile.X - controlTile.X) + Math.Abs(testTile.Y - controlTile.Y);
		GD.Print(agent.Name + " Variance from test: " + testTile + " control: " + controlTile + " is: " + variance);
        if (Variances.ContainsKey(agent))
        {
            Variances.TryGetValue(agent, out var currVariance);
			currVariance += variance;
            Variances.Remove(agent);
            Variances.Add(agent, currVariance);
        }
        else
        {
            Variances.Add(agent, variance);
        }
    }
}
