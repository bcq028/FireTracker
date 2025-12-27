using System;
using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float FollowSpeed = 10.0f; // 跟随鼠标的灵敏度 (类似插值 Alpha)

    private bool _isDragging = false; // 是否正在被拖拽
    private Vector2 _dragOffset;      // 鼠标点击位置和角色中心的偏移量

    public override void _Ready()
    {
        // UE: BeginPlay
        // 这里我们用代码绑定输入信号，不用编辑器面板，显得更 Pro 一点
        // 当鼠标在这个物体范围内按下/松开时触发
        this.InputEvent += OnInputEvent;
        GD.Print("_Ready called");
    }

    // 1. 检测鼠标点击角色 (类似于 UE 的 OnClicked)
    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    // 鼠标按下：开始拖拽
                    _isDragging = true;
                    // 记录偏移量，这样点击角色的边缘时，角色不会瞬间跳到鼠标中心
                    _dragOffset = GlobalPosition - GetGlobalMousePosition();
                }
                else
                {
                    // 鼠标松开：停止拖拽
                    _isDragging = false;
                }
            }
        }
    }

    // 2. 处理全局鼠标释放 (防止玩家拖得太快，鼠标移出了角色范围才松开)
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

    // 3. 物理帧更新 (类似于 UE 的 Tick + Physics)
    public override void _PhysicsProcess(double delta)
    {
        if (_isDragging)
        {
            // 获取鼠标当前位置
            Vector2 targetPos = GetGlobalMousePosition() + _dragOffset;

            // --- 核心手感算法 ---
            // 不直接设置 Position，而是使用 Lerp (线性插值) 制造平滑的“惯性”感
            // GlobalPosition 会逐渐逼近 targetPos
            GlobalPosition = GlobalPosition.Lerp(targetPos, (float)delta * FollowSpeed);
			
            // 如果你希望有碰撞体积阻挡 (比如撞墙停下)，请使用 MoveAndSlide 方案：
            // Vector2 direction = (targetPos - GlobalPosition).Normalized();
            // float distance = GlobalPosition.DistanceTo(targetPos);
            // Velocity = direction * distance * FollowSpeed * 50; // 简单的 P 控制器
            // MoveAndSlide(); 
            // (目前为了演示最顺滑的拖拽，我们先用直接修改 Position + Lerp)
        }
    }
}