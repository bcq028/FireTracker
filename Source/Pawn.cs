using System;
using Godot;

namespace T1;

public class GameplayAttribute(float value)
{
    public float BaseValue = value;
    private float _currentValue = value;
    private float CurrentValue
    {
        get => _currentValue;
        set
        {
            if (!Mathf.IsEqualApprox(value, _currentValue))
            {
                _currentValue = value;
                OnChanged?.Invoke(_currentValue,BaseValue);
            }
        }
    }
    public event Action<float, float> OnChanged;
    
    public static implicit operator float(GameplayAttribute attribute) => attribute.CurrentValue;
}

public partial class PlayerAttributeSet: GodotObject
{
    public GameplayAttribute Health = new(100);
    public GameplayAttribute FollowSpeed = new(10);
}
[GlobalClass]
public partial class Pawn : CharacterBody2D
{
    public PlayerAttributeSet attributes = new();
    private bool _isDragging = false; 
    private Vector2 _dragOffset;     
    public override void _Ready()
    {
        this.InputEvent += OnInputEvent;
    }

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    _isDragging = true;
                    _dragOffset = GlobalPosition - GetGlobalMousePosition();
                }
                else
                {
                    _isDragging = false;
                }
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            // 如果左键松开，且当前正在拖拽，强制停止
            if (mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
            {
                _isDragging = false;
            }
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_isDragging)
        {
            Vector2 targetPos = GetGlobalMousePosition() + _dragOffset;

            GlobalPosition = GlobalPosition.Lerp(targetPos, (float)delta * attributes.FollowSpeed);
        }
    }

    public void Cry(string message)
    {
        Label label = new Label();
		
        label.Text = message;
        label.AddThemeColorOverride("font_color", Colors.Yellow);
        label.Position = new Vector2(50, -50);
        label.AddThemeFontSizeOverride("font_size",128);
        AddChild(label);

        Tween tween = GetTree().CreateTween();
		
        tween.TweenProperty(label, "position", label.Position + new Vector2(0, -100), 1.5f);
		
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 1.5f);
        tween.Finished += () => label.QueueFree();
    }
}