using Godot;
namespace T1;
[GlobalClass]
public partial class PlayerHud : Control,IBaseWidget
{
	// Called when the node enters the scene tree for the first time.
	[Export] public Label HealthLabel;
	[Export] public TextureProgressBar HealthBar;
	private Tween _healthTween;
	public PlayerController Controller { get; set; }
	private Pawn _ownerPawn;
	public void OnAddToViewport()
	{
		if (Controller == null)
		{
			return;
		}
		Controller.OnPossessed += BindPlayer;
	}

	public void BindPlayer(Pawn p)
	{
		UnBind();
		p.attributes.GetAttribute("Health").OnChanged += UpdateHealthUI;
		_ownerPawn = p;
	}

	private void UpdateHealthUI(float currentVal, float baseVal)
	{
		float maxHealth = _ownerPawn.attributes.GetAttribute("MaxHealth");
		HealthLabel.Text = $"{currentVal:0} / {maxHealth:0}";
		HealthBar.MaxValue = maxHealth;
		if (_healthTween != null && _healthTween.IsValid())
		{
			_healthTween.Kill();
		}
		_healthTween = GetTree().CreateTween();
		_healthTween.TweenProperty(HealthBar, "value", (double)currentVal, 0.3f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		// 如果血量变红（低于 20%），可以顺便变个色（可选）
		float ratio = currentVal / maxHealth;
		HealthBar.TintProgress = ratio < 0.2f ? Colors.Red : Colors.Green;
	}

	public override void _ExitTree()
	{
		UnBind();
	}

	private void UnBind()
	{
		if (_ownerPawn != null)
		{
			_ownerPawn.attributes.GetAttribute("Health").OnChanged -= UpdateHealthUI;
		}
	}
}
