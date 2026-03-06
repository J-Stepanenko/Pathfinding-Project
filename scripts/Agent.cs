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
	public Vector2I GridPosition;
	public bool CanMove;
	public bool CanAttack;
	public bool InFormation;
	public AgentState State;
	public int Health;

	private List<Tile> reachableTiles = new();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Health = 100;
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
		Vector2I[] directions =
		{
			Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
		};

		foreach (var dir in directions)
		{
			var neighbourPos = GridPosition + dir;
			if (GridManager.Instance.CheckTileHasAgent(neighbourPos))
			{
				var agent = GridManager.Instance.GetAgent(neighbourPos);
				if (agent.Team == Team)
				{
					InFormation = true;
				}
			}
		}

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
        Vector2I[] directions =
            {
                Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
            };
        if (CanMove)
		{
			reachableTiles = GridManager.Instance.GetReachableTiles(GridPosition, MoveRange);
			GridManager.Instance.HighlightTiles(reachableTiles, true);

			foreach (var tile in reachableTiles)
			{
				foreach (var dir in directions)
				{
					if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition + dir))
					{
						var agent = GridManager.Instance.GetAgent(tile.GridPosition + dir);
						if (agent.Team != Team && CanAttack)
						{
							GD.Print("Move highlighting enemy tile");
							GridManager.Instance.GetTile(tile.GridPosition + dir).HighlightEnemy();
						}
					}
				}
			}
		}
		if (CanAttack)
		{
			var tile = GridManager.Instance.GetTile(GridPosition);
			foreach (var dir in directions)
			{
                if (GridManager.Instance.CheckTileHasAgent(tile.GridPosition + dir))
                {
                    var agent = GridManager.Instance.GetAgent(tile.GridPosition + dir);
                    if (agent.Team != Team)
                    {
                        GD.Print("Attack highlighting enemy tile");
                        GridManager.Instance.GetTile(tile.GridPosition + dir).HighlightEnemy();
                    }
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

	public void MoveAgent(Vector2I gridPos)
	{
		GD.Print(Name + " moving from " + GridPosition + " to " + gridPos);
		GridPosition = gridPos;
		// Remove old position from grid manager
		var oldPos = Utilities.GetGridPosFromVector(this.Position);
		GridManager.Instance.DeregisterAgent(oldPos);

		// Move agent and add new position to grid manager
		this.Position = Utilities.GetRealCoordinatesFromGridPos(gridPos);
		CanMove = false;
		GridManager.Instance.RegisterAgent(gridPos, this);
	}

	public bool CheckInFormation()
	{
        Vector2I[] directions =
        {
            Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
        };
		foreach(var dir in directions)
		{
			if (GridManager.Instance.GetTile(GridPosition + dir) == null) continue;
			else if (GridManager.Instance.CheckTileHasAgent(GridPosition + dir))
			{
				var agent = GridManager.Instance.GetAgent(GridPosition + dir);
				if (agent.Team == Team)
				{
					InFormation = true;
					GD.Print(Name + " is in formation");
					return InFormation;
				}
			}
        }
        GD.Print(Name + " is not in formation");
        InFormation = false;
		return InFormation;
    }

	public void DoAIMove()
	{
		if (AIEnabled)
		{
			if (CanMove && TurnManager.Instance.TeamTurn == this.Team)
			{
				State = AgentStateManager.Instance.CalculateState(this);
				if (State == AgentState.Retreating)
				{
					var tile = GridManager.Instance.GetTile(GridPosition);

                    if (tile.IsBase && tile.BaseTeam == Team)
					{
						Health = 100;
						HealthChanged();
						return;
					}
				}
				GD.Print("Agent: " + Name + " is in state: " + State + " at position: " + GridPosition);
				var bestTile = TileScorer.FindBestTile(this, State);
				var path = GridManager.Instance.GetPath(this.GridPosition, bestTile.GridPosition, MoveRange);
                if (path.Count != 0)
                {
                    MoveAgent(path.Last());
                }
				CheckInFormation();
			}
		}
	}

	public void DoAICombat()
	{
		if (AIEnabled)
        {
            var tiles = GridManager.Instance.Tiles;
            Vector2I[] directions =
            {
                Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right
            };
            foreach (var dir in directions)
            {
                var neighbourPos = GridPosition + dir;

                if (!tiles.ContainsKey(neighbourPos)) continue;

                tiles.TryGetValue(neighbourPos, out Tile neighbour);

                if (GridManager.Instance.CheckTileHasAgent(neighbourPos))
                {
                    var neighbourAgent = GridManager.Instance.GetAgent(neighbourPos);
                    if (neighbourAgent.Team != this.Team)
                    {
                        if (CanAttack)
                        {
                            CombatManager.Instance.ResolveCombat(this, neighbourAgent);
                        }
                    }
                }
            }
        }
	}

	public void HealthChanged()
	{
		var label = this.GetChild<Label>(1);
		label.Text = Name + "\n" + Team + "\n" + Health;
		if (Health < 0)
		{
			this.Visible = false;
			GridManager.Instance.DeregisterAgent(GridPosition);
		}
	}

	public void Attack(Agent defender)
	{
		CombatManager.Instance.ResolveCombat(this, defender);
		CanMove = false;
		CanAttack = false;
	}
	private void OnTurnEnd()
    {
        CheckInFormation();
        CanMove = true;
		CanAttack = true;
	}
}
