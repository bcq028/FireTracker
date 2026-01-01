using System;
using Godot;

namespace T1;

public interface IBaseWidget
{
    PlayerController Controller { get; set; }
    virtual void OnAddToViewport()
    {
    }
}

public class HUDManager
{
    public static HUDManager Instance { get; private set; } = new();
    private CanvasLayer _globalCanvasLayer;
    public T CreateWidget<T>(PackedScene scene, PlayerController owner) where T : Control
    {
        var widget = scene.Instantiate<T>();
        if (widget is IBaseWidget baseWidget)
        {
            baseWidget.Controller = owner;
        }

        return widget;
    }

    public void AddToViewport(Node context, Control widget)
    {
        if (_globalCanvasLayer == null || !GodotObject.IsInstanceValid(_globalCanvasLayer))
        {
            _globalCanvasLayer = new CanvasLayer();
            _globalCanvasLayer.Name = "GlobalCanvasLayer";
            _globalCanvasLayer.Layer = 100;
            context.GetTree().Root.AddChild(_globalCanvasLayer);
        }
        if (widget is IBaseWidget baseWidget)
        {
            baseWidget.OnAddToViewport();
        }

        _globalCanvasLayer.AddChild(widget);
    }
}