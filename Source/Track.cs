using System.Collections.Generic;
using System.Linq;
using Godot;

namespace T1;

[Tool]
[GlobalClass]
public partial class Track : Path2D
{
    [Export] public float TrackWidth = 100.0f;
    [Export] public float WallThickness = 10.0f;

    [Export]
    public bool BakeButton
    {
        get => false;
        set { if (value) BakeTrack(); }
    }

    private void BakeTrack()
    {
        if (Curve == null || Curve.PointCount < 2) return;

        var rawPoints = Curve.GetBakedPoints();
        var centerPoints = SanitizePoints(rawPoints);

        if (centerPoints.Length < 2) return;

        var trackLeft = new List<Vector2>();      // 赛道左边缘 (视觉边缘 + 左墙内侧)
        var trackRight = new List<Vector2>();     // 赛道右边缘 (视觉边缘 + 右墙内侧)
        var leftWallOuter = new List<Vector2>();  // 左墙外侧
        var rightWallOuter = new List<Vector2>(); // 右墙外侧

        float halfWidth = TrackWidth / 2f;

        for (int i = 0; i < centerPoints.Length; i++)
        {
            Vector2 current = centerPoints[i];
            
            Vector2 next = (i < centerPoints.Length - 1) 
                ? centerPoints[i + 1] 
                : centerPoints[i] + (centerPoints[i] - centerPoints[i - 1]);
                
            Vector2 dir = (next - current).Normalized();
            Vector2 normal = new Vector2(-dir.Y, dir.X); // 左手法线

            trackLeft.Add(current + normal * halfWidth);
            trackRight.Add(current - normal * halfWidth);
            leftWallOuter.Add(current + normal * (halfWidth + WallThickness));
            rightWallOuter.Add(current - normal * (halfWidth + WallThickness));
        }

        // 4. 分发数据进行生成
        GenerateVisuals(trackLeft, trackRight);
        GenerateCollision(trackLeft, trackRight, leftWallOuter, rightWallOuter);
    }

    // --- 生成逻辑 ---

    private void GenerateVisuals(List<Vector2> left, List<Vector2> right)
    {
        var poly = GetOrCreateChild<Polygon2D>(this, "VisualPoly");
        poly.Polygon = StitchPolygon(left, right);
        poly.Color = new Color(0.2f, 0.8f, 1.0f, 0.5f);
    }

    private void GenerateCollision(
        List<Vector2> trackLeft, List<Vector2> trackRight, 
        List<Vector2> wallLeftOuter, List<Vector2> wallRightOuter)
    {
        var staticBody = GetOrCreateChild<StaticBody2D>(this, "TrackWalls");
        staticBody.Position = Vector2.Zero;
        staticBody.CollisionLayer = 2;
        staticBody.CollisionMask = 1;

        var leftCol = GetOrCreateChild<CollisionPolygon2D>(staticBody, "LeftWall");
        leftCol.BuildMode = CollisionPolygon2D.BuildModeEnum.Solids;
        leftCol.Polygon = StitchPolygon(wallLeftOuter, trackLeft);

        var rightCol = GetOrCreateChild<CollisionPolygon2D>(staticBody, "RightWall");
        rightCol.BuildMode = CollisionPolygon2D.BuildModeEnum.Solids;
        rightCol.Polygon = StitchPolygon(trackRight, wallRightOuter);
    }

    // --- 通用工具函数 ---

    /// <summary>
    /// 将两条线缝合成一个封闭的多边形 (LineA + LineB_Reversed)
    /// </summary>
    private Vector2[] StitchPolygon(List<Vector2> lineA, List<Vector2> lineB)
    {
        var combined = new List<Vector2>(lineA);
        var reversedB = new List<Vector2>(lineB);
        reversedB.Reverse();
        combined.AddRange(reversedB);
        return combined.ToArray();
    }

    private Vector2[] SanitizePoints(Vector2[] points)
    {
        if (points.Length < 2) return System.Array.Empty<Vector2>();
        var result = new List<Vector2> { points[0] };
        for (int i = 1; i < points.Length; i++)
        {
            // 过滤掉距离过近的点
            if (points[i].DistanceSquaredTo(result[^1]) > 1.0f)
                result.Add(points[i]);
        }
        return result.ToArray();
    }

    private T GetOrCreateChild<T>(Node parent, string name) where T : Node, new()
    {
        if (parent == null) return null;
        var node = parent.GetNodeOrNull(name);

        if (node != null)
        {
            if (node is T typedNode) return typedNode;
            
            // 类型不匹配时销毁旧的
            node.QueueFree();
            node.Name = name + "_Deleting"; 
        }

        var newNode = new T { Name = name };
        parent.AddChild(newNode);

        if (Engine.IsEditorHint() && parent.IsInsideTree())
        {
            var tree = parent.GetTree();
            if (tree != null && tree.EditedSceneRoot != null)
                newNode.Owner = tree.EditedSceneRoot;
        }
        return newNode;
    }
}