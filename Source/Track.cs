using System.Collections.Generic;
using Godot;

namespace T1;

[Tool]
[GlobalClass]
public partial class Track : Path2D
{
    [Export] public float TrackWidth = 100.0f;
    [Export] public float WallThickness = 10.0f; // 新增：墙壁厚度

    [Export]
    public bool BakeButton
    {
        get => false;
        set { if (value) BakeTrack(); }
    }

    private void BakeTrack()
    {
        if (Curve == null || Curve.PointCount < 2) return;

        var points = Curve.GetBakedPoints();
        List<Vector2> leftEdge = new();
        List<Vector2> rightEdge = new();

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 current = points[i];
            Vector2 next = (i < points.Length - 1) 
                ? points[i + 1] 
                : points[i] + (points[i] - points[i - 1]);
                
            Vector2 dir = (next - current).Normalized();
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            
            leftEdge.Add(current + normal * TrackWidth / 2);
            rightEdge.Add(current - normal * TrackWidth / 2);
        }

        GenerateVisuals(leftEdge, rightEdge);
        GenerateCollision(leftEdge, rightEdge);
    }

    private void GenerateCollision(List<Vector2> left, List<Vector2> right)
    {
        var staticBody = GetOrCreateChild<StaticBody2D>(this, "TrackWalls");
        staticBody.Position = Vector2.Zero;
        staticBody.CollisionLayer = 2; // 建议用 Layer 2 作为墙壁层
        staticBody.CollisionMask = 1;

        // 生成左墙 (向外扩充)
        GenerateWallStrip(staticBody, "LeftWall", SanitizePoints(left), true);
        
        // 生成右墙 (向外扩充)
        GenerateWallStrip(staticBody, "RightWall", SanitizePoints(right), false);
    }

    private void GenerateWallStrip(StaticBody2D parent, string name, Vector2[] innerPoints, bool isLeft)
    {
        if (innerPoints.Length < 2) return;

        var colPoly = GetOrCreateChild<CollisionPolygon2D>(parent, name);
        
        // 1. 计算外圈的点 (Outer Edge)
        List<Vector2> outerPoints = new List<Vector2>();
        
        for (int i = 0; i < innerPoints.Length; i++)
        {
            // 计算当前点的法线方向
            // 为了平滑，处理首尾逻辑
            Vector2 current = innerPoints[i];
            Vector2 next = (i < innerPoints.Length - 1) ? innerPoints[i+1] : innerPoints[i] + (innerPoints[i] - innerPoints[i-1]);
            Vector2 dir = (next - current).Normalized();
            Vector2 normal = new Vector2(-dir.Y, dir.X);

            // 挤出厚度
            // 如果是左墙，Normal 指向赛道内，所以我们要减去 Normal * Thickness 往外挤
            // 或者根据你 Normal 的具体计算逻辑调整符号
            float offset = isLeft ? WallThickness : -WallThickness;
            
            outerPoints.Add(current + normal * offset);
        }

        // 2. 缝合内圈和外圈，形成封闭多边形
        var combined = new List<Vector2>(innerPoints);
        var outerReversed = new List<Vector2>(outerPoints);
        outerReversed.Reverse(); // 倒序回来
        combined.AddRange(outerReversed);

        // 3. 设置为实心模式
        colPoly.BuildMode = CollisionPolygon2D.BuildModeEnum.Solids;
        colPoly.Polygon = combined.ToArray();
    }

    // --- 辅助函数保持不变 ---
    
    private void GenerateVisuals(List<Vector2> leftEdge, List<Vector2> rightEdge)
    {
        var poly = GetOrCreateChild<Polygon2D>(this, "VisualPoly");
        var combined = new List<Vector2>(leftEdge);
        var rightReverse = new List<Vector2>(rightEdge);
        rightReverse.Reverse();
        combined.AddRange(rightReverse);
        poly.Polygon = combined.ToArray();
        poly.Color = new Color(0.2f, 0.8f, 1.0f, 0.5f);
    }

    private Vector2[] SanitizePoints(List<Vector2> points)
    {
        if (points.Count < 2) return System.Array.Empty<Vector2>();
        var result = new List<Vector2> { points[0] };
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].DistanceSquaredTo(result[^1]) > 0.5f)
                result.Add(points[i]);
        }
        return result.ToArray();
    }

    private T GetOrCreateChild<T>(Node parent, string name) where T : Node, new()
    {
        var node = parent.GetNodeOrNull(name);
        if (node is T typedNode) return typedNode;
        
        node?.QueueFree();
        var newNode = new T { Name = name };
        parent.AddChild(newNode);
        
        if (Engine.IsEditorHint() && IsInsideTree())
            newNode.Owner = GetTree().EditedSceneRoot;
            
        return newNode;
    }
}