using Godot;
using T1;
public partial class PlayerHud : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export] public Label HealthLabel;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
