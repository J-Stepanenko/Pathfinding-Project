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
    public bool InFormation;
    public AgentState State;

    private List<Tile> reachableTiles = new();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        State = AgentState.Forming_up;
        CanMove = true;
        InFormation = false;
        var meshInstance = this.GetChild<MeshInstance2D>(0);
        var label = this.GetChild<Label>(1);
        label.Text = Name + "\n" + Team;
        area = meshInstance.GetChild<Area2D>(0);
        area.InputEvent += _on_mouse_press;

        GridPosition = Utilities.GetGridPosFromNode(this);

        GridManager.Instance.RegisterAgent(GridPosition, this);

        TurnManager.Instance.DoAITurn += DoAITurn;
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
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void _on_mouse_press(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                InputManager.Instance.SelectAgent(this);
            }
        }
    }

    public void OnSelected()
    {
        // Check if it is this agent's turn
        if (TurnManager.Instance.TeamTurn != Team)
        {
            return;
        }

        GridPosition = Utilities.GetGridPosFromNode(this);
        if (CanMove)
        {
            reachableTiles = GridManager.Instance.GetReachableTiles(GridPosition, MoveRange);
            GridManager.Instance.HighlightTiles(reachableTiles, true);
        }
    }

    public void OnDeselected()
    {
        GridManager.Instance.HighlightTiles(reachableTiles, false);
        reachableTiles.Clear();
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

    public void DoAITurn()
    {
        if (AIEnabled)
        {
            if (CanMove && TurnManager.Instance.TeamTurn == this.Team)
            {
                State = AgentStateManager.Instance.CalculateState(this);
                GD.Print("Agent: " + Name + " is in state: " + State + " at position: " + GridPosition);
                var bestTile = TileScorer.FindBestTile(this, State);
                var path = GridManager.Instance.GetPath(this.GridPosition, bestTile.GridPosition, MoveRange);
                if (State == AgentState.Forming_up)
                {
                    InFormation = true;
                }
                else
                {
                    InFormation = false;
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
                            if (neighbourAgent.Team  == this.Team)
                            {
                                InFormation = true;
                            }
                        }
                    }
                }
                if (path.Count == 0)
                {
                    return;
                }
                MoveAgent(path.Last());
            }
        }
    }

    private void OnTurnEnd()
    {
        CanMove = true;
    }
}
