
using Godot;

namespace  T1;

public partial class Track : Node2D
{
    [Export] private Line2D _leftLine2D;
    [Export] private Line2D _rightLine2D;

    
    public override void _Ready()
    {
        GenerateCollision();
        base._Ready();
    }

    private static void OnCollision(Node2D body)
    {
        if (body is not Player player) return;
        player.Modulate = Colors.Red;
        player.Cry("好疼");
    }

    private void GenerateCollision()
    {
        var area = new Area2D();
        area.CollisionLayer = 2;
        area.CollisionMask = 1;
        AddChild(area);
        TryGenerateCollisionFromLine(area, _leftLine2D);
        TryGenerateCollisionFromLine(area, _rightLine2D);
        area.BodyEntered += OnCollision;
    }

    private static void TryGenerateCollisionFromLine(Area2D area, Line2D sourceLine)
    {
        if (sourceLine == null)
        {
            return;
        }

        Vector2[] worldPoints = new Vector2[sourceLine.Points.Length];
        for (int i = 0; i < sourceLine.Points.Length; i++)
        {
            worldPoints[i] = area.ToLocal(sourceLine.ToGlobal(sourceLine.Points[i]));
        }

        for (int i = 0; i < sourceLine.Points.Length - 1; i++)
        {
            var collisionShape = new CollisionShape2D();
            var segmentShape = new SegmentShape2D()
            {
                A = worldPoints[i], B = worldPoints[i + 1]
            };
            collisionShape.Shape = segmentShape;
            area.AddChild(collisionShape);
        }
    }
};

