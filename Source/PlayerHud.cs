using Godot;
namespace T1;
[GlobalClass]
public partial class PlayerHud : Control,IBaseWidget
{
	// Called when the node enters the scene tree for the first time.
	[Export] public Label HealthLabel;
	public PlayerController Controller { get; set; }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Ready()
	{
		if (Controller == null)
		{
			return;
		}
		Controller.OnPossessed += BindPlayer;
	}

	public void BindPlayer(Pawn p)
	{
		p.attributes.Health.OnChanged += (currentVal, baseVal) =>
		{
			UpdateHealthUI(currentVal,baseVal);
		};
	}

	private void UpdateHealthUI(float currentVal, float baseVal)
	{
		HealthLabel.Text = $"{currentVal:0} / {baseVal:0}";
	}
}
