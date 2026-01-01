
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace  T1;

[Tool]
[GlobalClass]
public partial class Track : Path2D
{
    [Export] public float TrackWidth = 100.0f;
    [Export] public float Precision = 10.0f;

    [Export]
    public bool BakeButton
    {
        get => false;
        set
        {
            if (value) BakeTrack();
        }
    }

    private void BakeTrack()
    {
        if (Curve == null || Curve.PointCount < 2) return;
        // 根据贝塞尔曲线生成track
        var points = Curve.GetBakedPoints();
        List<Vector2> leftEdge = new();
        List<Vector2> rightEdge = new();
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 current = points[i];
            Vector2 next = (i < points.Length - 1) ? points[i + 1] : points[i] + (points[i] - points[i - 1]);
            Vector2 dir = (next - current).Normalized();
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            leftEdge.Add(current + normal * TrackWidth / 2);
            rightEdge.Add(current - normal * TrackWidth / 2);
        }

        GenerateVisuals(leftEdge, rightEdge);
        GenerateCollision(leftEdge, rightEdge);
    }

    private void GenerateVisuals(List<Vector2> leftEdge, List<Vector2> rightEdge)
    {
        var poly = GetNodeOrNull<Polygon2D>("VisualPoly") ?? new Polygon2D { Name = "VisualPoly" };
        if (poly.GetParent() == null)
        {
            AddChild(poly);
        }

        if (Engine.IsEditorHint())
        {
            poly.Owner = GetTree().EditedSceneRoot;
        }
        var combined = new List<Vector2>(leftEdge);
        var rightReverse = new List<Vector2>(rightEdge);
        rightReverse.Reverse();
        combined.AddRange(rightReverse);
        poly.Polygon = combined.ToArray();
        poly.Color = Colors.Aqua;
    }

    private void GenerateCollision(List<Vector2> left, List<Vector2> right)
    {
        var staticBody = GetOrCreateChild<StaticBody2D>(this, "TrackWalls");
        staticBody.Position = Vector2.Zero; 
        staticBody.CollisionLayer = 2; 
        staticBody.CollisionMask = 1;

        var leftCol = GetOrCreateChild<CollisionPolygon2D>(staticBody, "LeftWall");
        leftCol.BuildMode = CollisionPolygon2D.BuildModeEnum.Segments;
        leftCol.Polygon = left.ToArray();
        var rightCol =GetOrCreateChild<CollisionPolygon2D>(staticBody, "RightWall");
        rightCol.BuildMode = CollisionPolygon2D.BuildModeEnum.Segments;
        rightCol.Polygon = right.ToArray();
    }

    private T GetOrCreateChild<T>(Node parent, string name) where T : Node, new()
    {
        if (parent == null)
        {
            return null;
        }
        var node = parent.GetNodeOrNull(name);
        if (node != null)
        {
            if (node is not T)
            {
                node.QueueFree();
                node.Name = name + "_Deleting";
            }

            return (T)node;
        }
        var newNode = new T { Name = name };
        parent.AddChild(newNode);
        if (Engine.IsEditorHint())
        {
            newNode.Owner = GetTree().EditedSceneRoot;
        }
        return newNode;
    }
};

