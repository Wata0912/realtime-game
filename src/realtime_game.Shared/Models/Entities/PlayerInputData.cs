// Shared
using MessagePack;

[MessagePackObject]
public struct PlayerInputData
{
    [Key(0)] public int userId;
    [Key(1)] public float h;
    [Key(2)] public float v;
}
