
using System.Diagnostics;
using Godot;
namespace T1;
/// <summary>
/// this will only run at server and autonomous
/// </summary>
[GlobalClass]
public partial class PlayerController : Node
{
    public Pawn PossessedPawn { get; private set; }
    [Export] public PackedScene HudScene;

    private MultiplayerSynchronizer _synchronizer;
    public static readonly string GroupName = "LocalPlayerController";
    [Signal]
    public delegate void OnPossessedEventHandler(Pawn pawn);

    public bool IsLocalController => IsMultiplayerAuthority();

    public override void _Ready()
    {
        SetupSynchronizer();
    }

    public void Possess(Pawn pawn)
    {
        Debug.Assert(Multiplayer.IsServer());
        PossessedPawn = pawn;
        EmitSignal(SignalName.OnPossessed, pawn);
        // listen server跳过自身
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId())
        {
            RpcId(GetMultiplayerAuthority(), nameof(ClientPossess), pawn.GetPath());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientPossess(NodePath pawnPath)
    {
        var pawn = GetNodeOrNull<Pawn>(pawnPath);
        PossessedPawn = pawn;
        EmitSignal(SignalName.OnPossessed, pawn);
    }

    public void SetupSynchronizer()
    {
        _synchronizer = new MultiplayerSynchronizer();
        _synchronizer.Name = "ControllerSynchronizer";
        AddChild(_synchronizer);
        var config = new SceneReplicationConfig();
        NodePath propPath = $".:{nameof(PossessedPawn)}";
        config.AddProperty(propPath);
        config.PropertySetReplicationMode(propPath, SceneReplicationConfig.ReplicationMode.Always);
        _synchronizer.ReplicationConfig = config;
    }
}