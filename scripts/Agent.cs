using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Agent : Node2D
{
	private Area2D area;

	[Export] public int MoveRange;
	[Export] public int Team;
	[Export] public bool AIEnabled;
    [Export] public int Health;
    public Vector2I GridPosition;
	public bool CanMove;
	public bool CanAttack;
	public bool InFormation;
	public AgentState State;

	private List<Tile> reachableTiles = new();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		State = AgentState.Forming_up;
		CanMove = true;
		CanAttack = true;
		InFormation = false;
		var meshInstance = this.GetChild<MeshInstance2D>(0);
		var label = this.GetChild<Label>(1);
		label.Text = Name + "\n" + Team + "\n" + Health;
		area = meshInstance.GetChild<Area2D>(0);
		area.InputEvent += _on_mouse_press;

		GridPosition = Utilities.GetGridPosFromNode(this);

		GridManager.Instance.RegisterAgent(GridPosition, this);

		TurnManager.Instance.DoAITurn += DoAIMove;
		TurnManager.Instance.TurnEnded += OnTurnEnd;
	}

	public void Init()
	{
		InFormation = CheckInFormation();

		if (Team == 2)
		{
            var meshInstance = this.GetChild<MeshInstance2D>(0);
			meshInstance.Modulate = Colors.DarkBlue;
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void _on_mouse_press(Node viewport, InputEvent @event, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && Team == TurnManager.Instance.TeamTurn)
			{
				GD.Print(Name+ " pressed");
				InputManager.Instance.SelectAgent(this);
			}
		}
	}

	public void OnSelected()
	{
		GD.Print(Name + " can move: " + CanMove + " can attack: " + CanAttack);
		// Check if it is this agent's turn
		if (TurnManager.Instance.TeamTurn != Team)
		{
			return;
		}
        if (CanMove)
		{
			reachableTiles = GridManager.Instance.GetReachableTiles(GridPosition, MoveRange);
			GridManager.Instance.HighlightTiles(reachableTiles, true);

			foreach (var tile in reachableTiles)
			{
				foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
				{
					var agent = GridManager.Instance.GetAgent(neighbourTile.Key);
					if (agent != null && agent.Team != Team && CanAttack)
					{
                        GridManager.Instance.GetTile(neighbourTile.Key).HighlightEnemy();
                    }
				}
			}
		}
		if (CanAttack)
		{
			var tile = GridManager.Instance.GetTile(GridPosition);
			foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(tile.GridPosition))
			{
                var agent = GridManager.Instance.GetAgent(neighbourTile.Key);
                if (agent != null && agent.Team != Team && CanAttack)
                {
                    GridManager.Instance.GetTile(neighbourTile.Key).HighlightEnemy();
                }
            }
		}
	}

	public void OnDeselected()
	{
		GridManager.Instance.HighlightTiles(reachableTiles, false);
		reachableTiles.Clear();
		GD.Print(Name + " Deselected");
	}

	private void MoveAgent(Vector2I newPos)
	{
		GD.Print(Name + " moving from " + GridPosition + " to " + newPos);
        var oldPos = GridPosition;
        GridPosition = newPos;
		// Remove old position from grid manager
		GridManager.Instance.DeregisterAgent(oldPos);

        foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(oldPos))
        {
            var agent = GridManager.Instance.GetAgent(neighbourTile.Key);
            if (agent == null) continue;
            if (agent.Team == Team)
            {
                agent.InFormation = agent.CheckInFormation();
            }
        }

        // Move agent and add new position to grid manager
        this.Position = Utilities.GetRealCoordinatesFromGridPos(newPos);
		CanMove = false;
		InFormation = CheckInFormation();
        GridManager.Instance.RegisterAgent(newPos, this);

        foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(newPos))
		{
			var agent = GridManager.Instance.GetAgent(neighbourTile.Key);
			if (agent == null) continue;
            if (agent.Team == Team)
            {
                agent.InFormation = agent.CheckInFormation();
			}
		}
    }

	public bool CheckInFormation()
	{
		foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(GridPosition))
		{
			var agent = GridManager.Instance.GetAgent(neighbourTile.Key);
			if (agent != null && agent.Team == Team) 
			{
                return true;
            }
		}
		return false;
    }

	public void DoAIMove()
	{
		TryHeal();
		if (AIEnabled)
        {
            if (CanMove && TurnManager.Instance.TeamTurn == this.Team)
			{
				InFormation = CheckInFormation();

                var tileScorer = new TileScorer(GridManager.Instance.Agents);
                var bestTile = tileScorer.FindBestTile(this);
				var score = tileScorer.GetScoreForTile(bestTile.GridPosition, this);
				ScoreManager.Instance.AddScore(this, score);
				PathTowards(bestTile.GridPosition);
				CanMove = false;
			}
        }
        InFormation = CheckInFormation();
    }

	public void TryHeal()
	{
        if (State == AgentState.Retreating && CanMove && Health != 100 && TurnManager.Instance.TeamTurn == Team)
        {
            var tile = GridManager.Instance.GetTile(GridPosition);

            if (tile.IsBase && tile.BaseTeam == Team)
            {
                // Heal and end turn
                HealthChanged(100);
                CanMove = false;
                return;
            }
        }
    }

	public void DoAICombat()
	{
		if (AIEnabled)
        {
			if (CanAttack && TurnManager.Instance.TeamTurn == Team)
			{
				foreach (var neighbourTile in GridManager.Instance.GetNeighbourTiles(GridPosition))
				{
					var agent = GridManager.Instance.GetAgent(neighbourTile.Key);
					if (agent != null && agent.Team != Team)
                    {
						if (State == AgentState.Retreating)
						{
							var doCombat = false;

							var combatResult = CombatManager.Instance.SimulateCombat(this, agent);

							// Check if this agent will not die and will do more than 10 damage in combat
							if (combatResult[0][0] > 0 && combatResult[0][1] >= 10)
							{
								doCombat = true;
							}

                            doCombat = true;
                            if (doCombat)
							{
								CombatManager.Instance.ResolveCombat(this, agent);
								CanAttack = false;
							}
						}
						else 
						{
							CombatManager.Instance.ResolveCombat(this, agent);
							CanAttack = false; 
						}
					}
				}
			}
        }
	}

	public void HealthChanged(int amount)
	{
		Health += amount;
		if (Health > 100) Health = 100;

		var label = this.GetChild<Label>(1);
		label.Text = Name + "\n" + Team + "\n" + Health;
		if (Health <= 0)
		{
			// Kill agent
			this.Visible = false;
			GridManager.Instance.DeregisterAgent(GridPosition);
		}
	}

	/// <summary>
	/// Moves the agent towards a new position by its movement range. Will not move if CanMove is false.
	/// </summary>
	/// <param name="newPos"></param>
	/// <returns>True if it has successfully moved or is unable to move due to CanMove being false, false if not (ie. blocked by another agent)</returns>
	public bool PathTowards(Vector2I newPos)
	{
		if (newPos == GridPosition)
		{
			CanMove = false;
			return true;
		}
        var path = GridManager.Instance.GetPath(this.GridPosition, newPos, MoveRange);
        if (path.Count != 0 && CanMove)
        {
			var hasMoved = false;
            for (var i = 0; i < path.Count; i++)
            {
                var tile = path[path.Count - 1 - i];
                if (GridManager.Instance.CheckTileHasAgent(tile))
                {
                    continue;
                }
                else
                {
					hasMoved = true;
                    MoveAgent(tile);
                    break;
                }
            }
			return hasMoved;
        }
		else if (CanMove)
        {
            return false;
        }
        return true;
    }

	public AgentState CheckState()
	{
		var state = AgentStateManager.Instance.CalculateState(this);
        GD.Print("Agent: " + Name + " is in state: " + state + " at position: " + GridPosition);
        return state;
	}

	public void Attack(Agent defender)
	{
		CombatManager.Instance.ResolveCombat(this, defender);
		CanMove = false;
		CanAttack = false;
	}
	private void OnTurnEnd()
    {
        InFormation = CheckInFormation();
        CanMove = true;
		CanAttack = true;
	}
}
