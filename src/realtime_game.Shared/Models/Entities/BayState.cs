using MessagePack;

namespace Shared.Models
{
    [MessagePackObject]
    public class BayState
    {
        [Key(0)] public string PlayerId { get; set; }
        [Key(1)] public float X { get; set; }
        [Key(2)] public float Y { get; set; }
        [Key(3)] public float Z { get; set; }

        [Key(4)] public float VelX { get; set; }
        [Key(5)] public float VelY { get; set; }
        [Key(6)] public float VelZ { get; set; }

        [Key(7)] public float Spin { get; set; }
        [Key(8)] public float HP { get; set; }
        [Key(9)] public bool IsDead { get; set; }
        [Key(10)] public int BayType { get; set; }
    }
}
