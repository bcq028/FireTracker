using Godot;
namespace T1;

public partial class MainMenuView : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export] public string WorldScenePath = "res://World.tscn";
	[Export] public int DefaultPort = 7001;

	private Label _statusLabel;

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatusLabel");

		// 绑定按钮事件
		GetNode<Button>("MarginContainer/VBoxContainer/HostButton").Pressed += OnHostPressed;
		GetNode<Button>("MarginContainer/VBoxContainer/JoinButton").Pressed += OnJoinPressed;

		// 监听连接结果
		Multiplayer.ConnectedToServer += () => _statusLabel.Text = "连接成功！等待服务器加载...";
		Multiplayer.ConnectionFailed += () => _statusLabel.Text = "连接失败，请检查 IP 或防火墙";
		Multiplayer.ServerDisconnected += () => _statusLabel.Text = "与服务器断开连接";
	}

	private void OnHostPressed()
	{
		var peer = new ENetMultiplayerPeer();
		Error err = peer.CreateServer(DefaultPort, 32);
        
		if (err != Error.Ok)
		{
			_statusLabel.Text = $"创建服务器失败: {err}";
			return;
		}

		Multiplayer.MultiplayerPeer = peer;
		_statusLabel.Text = "服务器已启动，正在进入世界...";
        
		// 服务器负责切换场景
		ChangeToWorldScene();
	}

	private void OnJoinPressed()
	{
		var ip = "127.0.0.1";

		var peer = new ENetMultiplayerPeer();
		Error err = peer.CreateClient(ip, DefaultPort);

		if (err != Error.Ok)
		{
			_statusLabel.Text = $"无法发起连接: {err}";
			return;
		}

		Multiplayer.MultiplayerPeer = peer;
		_statusLabel.Text = $"正在连接到 {ip}...";
        
		// 注意：客户端不需要手动调用 ChangeToWorldScene
		// 如果你配置了 MultiplayerSpawner，服务器切换场景时，客户端会自动跟着切换
	}

	private void ChangeToWorldScene()
	{
		// 传统的场景切换
		GetTree().ChangeSceneToFile(WorldScenePath);
	}
}
