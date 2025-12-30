using System.Linq;
using Godot;

namespace T1;

[Tool]
[GlobalClass]
public partial class TrackData : Resource
{
    [Export] public Vector2[] LeftPoints;

    [Export] public Vector2[] RightPoints;

    // Cache
    [Export] public Vector2[] LeftSegmentsCache;
    [Export] public Vector2[] RightSegmentsCache;

    public bool IsValid()
    {
        return LeftSegmentsCache != null && RightSegmentsCache != null;
    }


    /// <summary>
    ///  重新构造资源数据
    /// </summary>
    /// <param name="rawLeft"></param>
    /// <param name="rawRight"></param>
    public void NormalizeAndBake(Vector2[] rawLeft, Vector2[] rawRight)
    {
        if (rawLeft.Length < 2 || rawRight.Length < 2)
        {
            GD.PrintErr("Normalize and Bake requires two points");
            return;
        }

        var offset = rawLeft[0];
        LeftPoints = rawLeft.Select(p => p - offset).ToArray();
        RightPoints = rawRight.Select(p => p - offset).ToArray();
        LeftSegmentsCache = CreateSegments(LeftPoints);
        RightSegmentsCache = CreateSegments(RightPoints);
        var err = ResourceSaver.Save(this);
        if (err == Error.Ok)
        {
            GD.Print($"成功：资源已保存至 {ResourcePath}");
        }
        else
        {
            GD.PrintErr($"TrackData bake失败：{err}");
        }
    }

    private static Vector2[] CreateSegments(Vector2[] points)
    {
        var segments = new Vector2[(points.Length - 1) * 2];
        for (int i = 0; i < points.Length - 1; i++)
        {
            segments[i * 2] = points[i];
            segments[i * 2 + 1] = points[i + 1];
        }

        return segments;
    }
}