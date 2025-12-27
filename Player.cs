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

    public void Cry(string message)
    {
        // 1. 创建一个 Label 节点 (UE: CreateWidget)
        Label label = new Label();
		
        // 2. 设置文字内容和样式
        label.Text = message;
        // 稍微美化一下：给文字加个背景色或修改颜色 (可选)
        label.AddThemeColorOverride("font_color", Colors.Yellow);
		
        // 3. 设置初始位置 (位于角色右上角)
        // 注意：如果 label 是 Player 的子节点，Position 是相对坐标
        // Vector2(50, -50) 表示向右偏 50，向上偏 50
        label.Position = new Vector2(50, -50);
        label.AddThemeFontSizeOverride("font_size",128);
        // 4. 将 Label 添加为 Player 的子节点
        // 这样 Label 会跟着 Player 一起移动
        AddChild(label);

        // 5. 使用 Tween (补间动画) 让文字向上飘动并逐渐消失
        // 这是 Godot 4 处理简单动画的“神技”
        Tween tween = GetTree().CreateTween();
		
        // 动画 A: 1.5秒内向上移动 100 像素 (相对当前位置)
        tween.TweenProperty(label, "position", label.Position + new Vector2(0, -100), 1.5f);
		
        // 动画 B: 同时进行淡出 (修改 Modulate 的 Alpha 值)
        // Parallel() 表示下一个动画与上一个同时开始
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 1.5f);

        // 6. 动画结束后自动销毁 Label (防止内存泄漏)
        // 相当于 UE 的 SetLifeSpan 或 DestroyActor
        tween.Finished += () => label.QueueFree();

    }
}