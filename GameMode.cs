using Godot;
using System;
namespace T1;
/// <summary>
///  GameMode only run on server. response for receive login and spawn controller
/// GameMode will not be replicated to client
/// </summary>
public partial class GameMode : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}
		
	}

	private void SpawnController(int peerID)
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
