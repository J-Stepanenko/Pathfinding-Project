using Godot;
using System;

public partial class UI : Node
{

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var endTurnButton = GetNode<Button>("End Turn Button");
        endTurnButton.Pressed += TurnManager.Instance.EndTurn;

        var showScoreButton = GetNode<Button>("Show Final Scores Button");
        showScoreButton.Pressed += ScoreManager.Instance.DisplayFinalScores;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
