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

    private List<Tile> reachableTiles = new();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        CanMove = true;
        var meshInstance = this.GetChild<MeshInstance2D>(0);
        area = meshInstance.GetChild<Area2D>(0);
        area.InputEvent += _on_mouse_press;

        GridPosition = Utilities.GetGridPosFromNode(this);

        GridManager.Instance.RegisterAgent(GridPosition, this);

        TurnManager.Instance.DoAITurn += DoAITurn;
        TurnManager.Instance.TurnEnded += OnTurnEnd;
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
        GridPosition = gridPos;
        // Remove old position from grid manager
        var oldPos = Utilities.GetGridPosFromVector(this.Position);
        GridManager.Instance.DeregisterAgent(oldPos);

        // Move agent and add new position to grid manager
        this.Position = Utilities.GetRealCoordinatesFromGridPos(gridPos);
        CanMove = false;
        GridManager.Instance.RegisterAgent(gridPos, this);
    }

    private void DoAITurn()
    {
        if (AIEnabled)
        {
            if (CanMove && TurnManager.Instance.TeamTurn == this.Team)
            {
                var bestTile = TileScorer.FindBestTile(this, AgentStateMachine.AgentStates.Attacking);
                var path = GridManager.Instance.GetPath(this.GridPosition, bestTile.GridPosition, MoveRange);
                if (path.Count == 0)
                {
                    return;
                }
                //var reachableTiles = GridManager.Instance.GetReachableTiles(this.GridPosition, MoveRange);
                //var nearestTileToBest = bestTile;
                //if (!reachableTiles.Contains(bestTile))
                //{
                //    for (int i = 0; i < path.Count - 2; i++)
                //    {
                //        if (!reachableTiles.Contains(
                //            GridManager.Instance.GetTile(path[i + 1])))
                //        {
                //            nearestTileToBest = GridManager.Instance.GetTile(path[i]);
                //            break;
                //        }
                //    }
                //}
                MoveAgent(path.Last());
            }
        }
    }

    private void OnTurnEnd()
    {
        CanMove = true;
    }
}
