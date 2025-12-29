
using Godot;
namespace T1;
public partial class PlayerController : Node
{
    [Export] public Pawn PossessedPawn { get; private set; }
    public static string GroupName = "LocalPlayerController";
    [Signal]
    public delegate void OnPossessedEventHandler(Pawn pawn);

    public bool IsLocalController => IsMultiplayerAuthority();

    public override void _Ready()
    {
        if (IsLocalController)
        {
            AddToGroup(GroupName);
        }
    }

    public void Possess(Pawn pawn)
    {
        PossessedPawn = pawn;
        EmitSignal(SignalName.OnPossessed, pawn);
    }

    public override void _Input(InputEvent @event)
    {
        
    }
}