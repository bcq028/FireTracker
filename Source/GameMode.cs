using System.Diagnostics;
using Godot;
using Godot.Collections;

namespace T1;
/// <summary>
///  GameMode only run on server. response for receive login and spawn controller
/// GameMode will not be replicated to client
/// </summary>
public partial class GameMode : Node
{
	// Called when the node enters the scene tree for the first time.
	private Dictionary<int,PlayerController> _playerControllers= new Dictionary<int, PlayerController>();
	[Export] public PackedScene PlayerControllerScene;
	[Export] public PackedScene PlayerPawnScene;
	
	private MultiplayerSpawner _multiplayerSpawner;
	public override void _Ready()
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}

		SetupSpawner();
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisConnected;
		// listen server下，延迟调用生成逻辑，防止破坏场景树
		if (!IsDedicatedServer())
		{
			CallDeferred(nameof(SpawnController), 1);
		}

	}

	private void SetupSpawner()
	{
		_multiplayerSpawner = new MultiplayerSpawner();
		AddChild(_multiplayerSpawner);
		_multiplayerSpawner.Name = "MultiplayerSpawner";
		_multiplayerSpawner.SpawnPath = GetParent().GetPath();
		if (PlayerPawnScene != null)
		{
			_multiplayerSpawner.AddSpawnableScene(PlayerPawnScene.ResourcePath);
		}
	}

	private void OnPeerConnected(long peerID)
	{
		SpawnController((int)peerID);
	}

	public static bool IsDedicatedServer()
	{
		return DisplayServer.GetName() == "headless";
	}

	private void OnPeerDisConnected(long peerID)
	{
		if (!_playerControllers.TryGetValue((int)peerID, out var controller)) return;
		controller.QueueFree();
		_playerControllers.Remove((int)peerID);
		GD.Print($"玩家{peerID} 已断开连接");
	}

	private void SpawnController(int peerID)
	{
		Debug.Assert(Multiplayer.IsServer());
		var controller = PlayerControllerScene.Instantiate<PlayerController>();
		controller.Name = peerID.ToString();
		controller.SetMultiplayerAuthority(peerID);
		AddChild(controller);
		_playerControllers[peerID] = controller;
		RestartPlayer(controller);
	}

	public void RestartPlayer(PlayerController controller)
	{
		int id = (int)controller.GetMultiplayerAuthority();
		var pawn = PlayerPawnScene.Instantiate<Pawn>();
		pawn.Name = $"Pawn_{id}";
		GetParent().AddChild(pawn);
		pawn.SetMultiplayerAuthority(id);
		// set transfrom
		var spawnPoints = GetTree().GetNodesInGroup("PlayerStart");
		if (spawnPoints.Count > 0)
		{
			if (spawnPoints[0] is Node2D spawnPoint)
			{
				pawn.GlobalTransform = spawnPoint.GlobalTransform;
			}
		}
		controller.Possess(pawn);
	}
}
