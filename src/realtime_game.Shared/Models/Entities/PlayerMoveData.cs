using MessagePack;

[MessagePackObject]
public class PlayerMoveData
{
    [Key(0)] public string PlayerId { get; set; }
    [Key(1)] public float PosX { get; set; }
    [Key(2)] public float PosY { get; set; }
    [Key(3)] public float PosZ { get; set; }
    [Key(4)] public float RotX { get; set; }
    [Key(5)] public float RotY { get; set; }
    [Key(6)] public float RotZ { get; set; }
    [Key(7)] public float RotW { get; set; }
    [Key(8)] public float Spin { get; set; } // y軸回転の補助
}
