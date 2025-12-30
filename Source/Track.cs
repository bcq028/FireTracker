
using System.Linq;
using Godot;

namespace  T1;

[Tool]
[GlobalClass]
public partial class Track : Node2D
{
    [Export] public Line2D LeftLine2D;
    [Export] public Line2D RightLine2D;
    [Export] public TrackData TrackData;

    // 编辑器按钮
    [Export] public bool BakeButton {
        get => false;
        set { if (value) Bake(); }
    }

    public override void _Notification(int what)
    {
        if (!IsInsideTree())
        {
            return;
        }
        bool bIsInsideOwnScene = !string.IsNullOrEmpty(SceneFilePath) && GetTree()?.EditedSceneRoot == this;
        if (what == (int)NotificationEditorPreSave)
        {
            if (bIsInsideOwnScene)
            {
                GD.Print($"自动保存...");
                Bake();
            }
        }
        base._Notification(what);
    }


    private void Bake()
    {
        var globalLeftPoints = LeftLine2D.Points.Select(p => ToLocal(LeftLine2D.ToGlobal(p))).ToArray();
        var globalRightPoints = RightLine2D.Points.Select(p => ToLocal(RightLine2D.ToGlobal(p))).ToArray();
        if (TrackData != null)
        {
            TrackData.NormalizeAndBake(globalLeftPoints, globalRightPoints);
        }
    }

    
    public override void _Ready()
    {
        if (TrackData == null || LeftLine2D == null || RightLine2D == null)
        {
            GD.PrintErr("TrackData is null. ");
        }
        else
        {
            GenerateCollision();
        }
        base._Ready();
    }

    private static void OnCollision(Node2D body)
    {
        if (body is not Pawn player) return;
        player.Modulate = Colors.Red;
        player.Cry("好疼");
    }

    private void GenerateCollision()
    {
        var area = new Area2D();
        area.CollisionLayer = 2;
        area.CollisionMask = 1;
        AddChild(area);
        if (!TrackData.IsValid())
        {
            GD.PrintErr("TrackData is invalid");
        }
        TryGenerateCollisionFromLine(area, TrackData.LeftSegmentsCache);
        TryGenerateCollisionFromLine(area, TrackData.RightSegmentsCache);
        area.BodyEntered += OnCollision;
    }

    private static void TryGenerateCollisionFromLine(Area2D area, Vector2[] segments)
    {
        var shape = new ConcavePolygonShape2D();
        shape.Segments = segments;
        var col = new CollisionShape2D();
        col.Shape = shape;
        area.AddChild(col);
    }
};

