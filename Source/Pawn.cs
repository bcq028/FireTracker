using System;
using Godot;
using Godot.Collections;

namespace T1;

public partial class PlayerAttributeSet: AttributeSet
{
    public PlayerAttributeSet()
    {
        RegisterAttribute("Health",100);
        RegisterAttribute("FollowSpeed",100);
        RegisterAttribute("MaxHealth",100);
    }
}
[GlobalClass]
public partial class Pawn : CharacterBody2D
{
    public PlayerAttributeSet attributes = new();
    private bool _isDragging = false; 
    private Vector2 _dragOffset;     
    
    private MultiplayerSynchronizer _synchronizer = new MultiplayerSynchronizer();
    public override void _Ready()
    {
        InputPickable = true;
        this.InputEvent += OnInputEvent;
        _synchronizer.Name = "PawnSynchronizer";
        var config = new SceneReplicationConfig();
        NodePath posPath = $".:{nameof(GlobalPosition)}"; 
        config.AddProperty(posPath);
        config.PropertySetReplicationMode(posPath, SceneReplicationConfig.ReplicationMode.Always);
        _synchronizer.ReplicationConfig = config;
        AddChild(_synchronizer);
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
            Vector2 screenSize = GetViewportRect().Size;
            float margin = 20.0f;
            Vector2 targetPos = GetGlobalMousePosition() + _dragOffset;
            targetPos.X = Mathf.Clamp(targetPos.X, margin, screenSize.X - margin);
            targetPos.Y = Mathf.Clamp(targetPos.Y, margin, screenSize.Y - margin);
            Vector2 nextStep = targetPos - GlobalPosition;
            Velocity = nextStep / (float)delta;
            bool hasCollision = MoveAndSlide();
            if (hasCollision)
            {
                Cry("好疼");
            }
        }
        else
        {
            Velocity = Vector2.Zero; 
            // MoveAndSlide(); // 可选：如果你希望非拖拽状态也能被别人推走
        }
    }

    public void Cry(string message)
    {
        int collisionCount = GetSlideCollisionCount();
        for (int i = 0; i < collisionCount; i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            GodotObject collider = collision.GetCollider();
        }
        Label label = new Label();
        label.Text = message;
        label.AddThemeColorOverride("font_color", Colors.Yellow);
        label.Position = new Vector2(50, -50);
        label.AddThemeFontSizeOverride("font_size", 128);
        AddChild(label);

        Tween tween = GetTree().CreateTween();

        tween.TweenProperty(label, "position", label.Position + new Vector2(0, -100), 1.5f);

        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 1.5f);
        tween.Finished += () => label.QueueFree();
        var buff = new GameplayEffect
        {
            DurationType = EffectDurationType.Instant,
            Modifiers = [new("Health", -10, ModifierOp.Add)]
        };
        attributes.ApplyEffect(buff, this);
    }

    public override void _ExitTree()
    {
        GD.Print($"Exit Tree");
    }
}