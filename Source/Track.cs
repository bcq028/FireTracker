using System.Collections.Generic;
using System.Linq;
using Godot;

namespace T1;

[Tool]
[GlobalClass]
public partial class Track : Path2D
{
    [ExportGroup("Settings")]
    [Export] public float TrackWidth = 100.0f;
    [Export] public float WallThickness = 10.0f;
    
    [ExportGroup("Runtime Generation")]
    [Export] public bool GenerateAtRuntime = false; // 运行时生成开关

    [ExportGroup("Editor Tools")]
    [Export]
    public bool BakeButton
    {
        get => false;
        set { if (value) BakeTrack(); }
    }

    public override void _Ready()
    {
        // 如果不是在编辑器里，且开启了运行时生成
        if (!Engine.IsEditorHint() && GenerateAtRuntime)
        {
            GenerateRandomCurve(); // (可选：如果是完全随机赛道)
            BakeTrack();
        }
    }

  private void BakeTrack()
    {
        if (Curve == null || Curve.PointCount < 2) return;

        // 1. 获取中心线
        var rawPoints = Curve.GetBakedPoints();
        var centerPoints = SanitizePoints(rawPoints);
        if (centerPoints.Length < 3) return; // 多边形至少要3个点

        float halfWidth = TrackWidth / 2f;

        // 2. 使用 Godot 内置几何库生成边缘
        // Geometry2D.OffsetPolygon 会自动处理自相交，把打结的地方剪掉
        // 注意：OffsetPolygon 返回的是由多个多边形组成的数组（以防断裂），通常我们取第一个 [0]
        
        // 生成赛道边缘
        // endType: 2 (JOIN_MITER) 或 0 (JOIN_SQUARE)
        // 假设点是顺时针排列，正数向外，负数向内（反之亦然，如果反了就调换正负）
        var leftPolys = Geometry2D.OffsetPolygon(centerPoints, halfWidth, Geometry2D.PolyJoinType.Square);
        var rightPolys = Geometry2D.OffsetPolygon(centerPoints, -halfWidth, Geometry2D.PolyJoinType.Square);

        // 防御性检查：如果偏移失败（比如路宽到了负数），直接返回
        if (leftPolys.Count == 0 || rightPolys.Count == 0) 
        {
            GD.PrintErr("Bake失败：赛道太窄或曲线太扭曲，无法生成几何体");
            return; 
        }

        Vector2[] trackLeft = leftPolys[0];
        Vector2[] trackRight = rightPolys[0];

        // 3. 生成墙壁外沿
        // 基于赛道边缘，再向外扩一个 WallThickness
        // 注意：我们需要根据之前的方向决定是扩还是缩
        // 这里为了简单稳健，我们再次基于中心线扩，宽度 = 半宽 + 墙厚
        var leftWallPolys = Geometry2D.OffsetPolygon(centerPoints, halfWidth + WallThickness, Geometry2D.PolyJoinType.Square);
        var rightWallPolys = Geometry2D.OffsetPolygon(centerPoints, -(halfWidth + WallThickness), Geometry2D.PolyJoinType.Square);

        if (leftWallPolys.Count == 0 || rightWallPolys.Count == 0) return;

        Vector2[] wallLeftOuter = leftWallPolys[0];
        Vector2[] wallRightOuter = rightWallPolys[0];

        // 4. 为了配合之前的 StitchPolygon 逻辑，我们需要把数组转回 List
        // 注意：Geometry2D 处理后的点数可能和 centerPoints 不一样（因为删掉了打结的点）
        // 但这对缝合函数没影响
        
        GenerateVisuals(trackLeft.ToList(), trackRight.ToList());
        GenerateCollision(trackLeft.ToList(), trackRight.ToList(), wallLeftOuter.ToList(), wallRightOuter.ToList());
    }

    private void GenerateVisuals(List<Vector2> left, List<Vector2> right)
    {
        var poly = GetOrCreateChild<Polygon2D>(this, "VisualPoly");
        poly.Polygon = StitchPolygon(left, right);
        poly.Color = new Color(0.2f, 0.8f, 1.0f, 0.5f);
        poly.ZIndex = -1; 
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
    
   [ExportGroup("Random Generation")]
    [Export] public int GenPoints = 12;
    [Export] public float GenSmoothness = 0.3f;
    [Export] public int GenSeed = 0;
    // 移除 GenMinRadius/MaxRadius，因为现在由屏幕大小动态决定
    [Export] public float RadiusJitter = 0.5f; // 半径随机抖动幅度 (0~1)

    public void GenerateRandomCurve()
    {
        var rng = new RandomNumberGenerator();
        if (GenSeed != 0) rng.Seed = (ulong)GenSeed;
        else rng.Randomize();

        // 1. 获取屏幕安全范围 (Local Coordinates)
        Rect2 viewportRect = GetViewportRect();
        
        // 安全边距 = 赛道半宽 + 墙厚 + 一点缓冲(例如20px)
        float safeMargin = (TrackWidth / 2) + WallThickness + 20.0f;
        
        // 计算安全矩形 (在这个矩形内的点，生成赛道绝对不会出界)
        Rect2 safeRect = viewportRect.Grow(-safeMargin);

        // 如果屏幕太小连赛道都放不下，直接报错
        if (safeRect.Size.X <= 0 || safeRect.Size.Y <= 0)
        {
            GD.PrintErr("屏幕太小，无法容纳该宽度的赛道！");
            return;
        }

        // 2. 确定圆心 (使用屏幕中心作为生成的参考重心)
        // 注意：这是相对于 Track 节点的本地坐标
        Vector2 screenCenterLocal = ToLocal(GetViewportTransform().AffineInverse() * (viewportRect.Position + viewportRect.Size / 2));
        
        // 修正：上面的 ToLocal 计算可能受父级影响，最稳妥是用 ViewportRect 的中心转 Local
        // 简单做法：假设 Track 在 (0,0) 或者我们直接基于 Track 的 Local 坐标系算 SafeRect
        // 让我们重新计算相对于 Track 节点的 SafeRect
        Transform2D invTrans = GlobalTransform.AffineInverse();
        Vector2 topLeft = invTrans.BasisXform(Vector2.Zero) + invTrans.Origin; // 粗略估算，还是直接用 ViewportRect 比较好
        
        // === 重新定义坐标系策略 ===
        // 我们直接在 Track 的局部空间操作。
        // 获取 Viewport 在 Track 局部空间的矩形：
        Rect2 localViewportRect = new Rect2(ToLocal(Vector2.Zero), Vector2.Zero); // Hacky init
        // 正确获取 Viewport 的四个角转 Local
        Vector2 vSize = GetViewportRect().Size;
        Vector2 p1 = ToLocal(Vector2.Zero); // 左上
        Vector2 p2 = ToLocal(new Vector2(vSize.X, 0)); // 右上
        Vector2 p3 = ToLocal(vSize); // 右下
        Vector2 p4 = ToLocal(new Vector2(0, vSize.Y)); // 左下
        
        // 构建包含这4个点的 AABB (Axis Aligned Bounding Box)
        float minX = Mathf.Min(p1.X, Mathf.Min(p2.X, Mathf.Min(p3.X, p4.X)));
        float maxX = Mathf.Max(p1.X, Mathf.Max(p2.X, Mathf.Max(p3.X, p4.X)));
        float minY = Mathf.Min(p1.Y, Mathf.Min(p2.Y, Mathf.Min(p3.Y, p4.Y)));
        float maxY = Mathf.Max(p1.Y, Mathf.Max(p2.Y, Mathf.Max(p3.Y, p4.Y)));
        
        Rect2 localScreenRect = new Rect2(minX, minY, maxX - minX, maxY - minY);
        Rect2 localSafeRect = localScreenRect.Grow(-safeMargin);
        Vector2 center = localScreenRect.GetCenter();

        // 3. 获取并钳制起点 (PlayerStart)
        Vector2 startPos = Vector2.Zero;
        var playerStart = GetTree().GetFirstNodeInGroup("PlayerStart") as Node2D;
        if (playerStart != null)
        {
            startPos = ToLocal(playerStart.GlobalPosition);
            
            // 【关键】如果 PlayerStart 本身就在屏幕边缘，强制把它移进来
            // 否则起点处一定会出界
            if (!localSafeRect.HasPoint(startPos))
            {
                startPos.X = Mathf.Clamp(startPos.X, localSafeRect.Position.X, localSafeRect.End.X);
                startPos.Y = Mathf.Clamp(startPos.Y, localSafeRect.Position.Y, localSafeRect.End.Y);
                GD.Print("PlayerStart 过于靠近屏幕边缘，生成的赛道起点已被修正到安全区内。");
            }
        }
        else
        {
            startPos = center + new Vector2(0, 200); // 默认起点
        }

        // 4. 生成锚点
        List<Vector2> anchors = new List<Vector2>();
        
        // 计算起点的角度 (相对于中心)
        float startAngle = (startPos - center).Angle();

        for (int i = 0; i < GenPoints; i++)
        {
            Vector2 point;

            if (i == 0)
            {
                // 第一个点强制为起点
                point = startPos;
            }
            else
            {
                // 均匀分布角度
                float angleStep = Mathf.Tau / GenPoints;
                // 从 StartAngle 开始转圈，保证 i=0 对应 StartPos
                float targetAngle = startAngle + i * angleStep;
                
                // 角度随机抖动
                float angleJitter = rng.RandfRange(-angleStep * 0.2f, angleStep * 0.2f);
                float finalAngle = targetAngle + angleJitter;

                // 【核心算法】计算该角度下，离中心最远能多远 (射线与矩形求交)
                float maxRadius = GetRayToRectDistance(center, finalAngle, localSafeRect);
                
                // 在 [Min, Max] 之间随机
                // 最小半径设为 Max 的 30%，保证赛道不会缩成一团
                float minRadius = maxRadius * 0.3f; 
                
                // 应用 RadiusJitter (0~1)
                // Jitter 越大，越容易取到 minRadius，路越曲折
                float t = rng.RandfRange(0, 1.0f); 
                // 让分布稍微倾向于外圈 (Mathf.Sqrt)，这样赛道更大气
                float randomRadius = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt(t));

                point = center + new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle)) * randomRadius;
            }

            anchors.Add(point);
        }

        // 5. 构建平滑曲线 (Catmull-Rom logic)
        var newCurve = new Curve2D();
        newCurve.BakeInterval = 20.0f;

        for (int i = 0; i < anchors.Count; i++)
        {
            Vector2 current = anchors[i];
            Vector2 prev = anchors[(i - 1 + anchors.Count) % anchors.Count];
            Vector2 next = anchors[(i + 1) % anchors.Count];

            Vector2 tangentDir = (next - prev).Normalized();
            float distToNext = current.DistanceTo(next);
            float distToPrev = current.DistanceTo(prev);

            // 限制控制柄长度，防止贝塞尔曲线由于控制柄太长而“甩”出屏幕
            // 0.3 是个比较安全的系数，0.5 可能会导致过冲
            float smoothFactor = GenSmoothness;
            
            // 额外的过冲保护：如果控制点离边界很近，减小曲率
            // (这里简化处理，直接用较保守的 Smoothness)
            
            Vector2 inDir = -tangentDir * (distToPrev * smoothFactor);
            Vector2 outDir = tangentDir * (distToNext * smoothFactor);

            newCurve.AddPoint(current, inDir, outDir);
        }

        // 6. 闭合
        Vector2 firstPos = newCurve.GetPointPosition(0);
        Vector2 firstIn = newCurve.GetPointIn(0);
        Vector2 firstOut = newCurve.GetPointOut(0);
        newCurve.AddPoint(firstPos, firstIn, firstOut);

        this.Curve = newCurve;
        BakeTrack();
    }

    /// <summary>
    /// 计算从 center 发出的射线，与 rect 边界的交点距离
    /// </summary>
    private float GetRayToRectDistance(Vector2 center, float angle, Rect2 rect)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        // 防止除以 0
        if (Mathf.Abs(cos) < 0.001f) cos = 0.001f;
        if (Mathf.Abs(sin) < 0.001f) sin = 0.001f;

        // 计算碰到 X 轴边界的距离 (左边或右边)
        float xDist = (cos > 0) 
            ? (rect.End.X - center.X) / cos 
            : (rect.Position.X - center.X) / cos;

        // 计算碰到 Y 轴边界的距离 (上边或下边)
        float yDist = (sin > 0) 
            ? (rect.End.Y - center.Y) / sin 
            : (rect.Position.Y - center.Y) / sin;

        // 取正值中的最小值
        // 射线只会撞到一个边，那个更近的就是实际距离
        return Mathf.Min(xDist, yDist);
    }
    
    /// <summary>
    /// Helper Functions
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
    
    public void ClearTrack()
    {
        GetNodeOrNull("VisualPoly")?.QueueFree();
        GetNodeOrNull("TrackWalls")?.QueueFree();
    }
}