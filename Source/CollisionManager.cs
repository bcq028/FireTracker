using System;
namespace T1;

[Flags]
public enum GameLayer : uint
{
    None = 0,
    Track = 1 << 0,
    Player = 1 << 1,
    Pickable = 1 << 2
}