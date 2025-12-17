using MessagePack;

namespace Shared.Models
{
    [MessagePackObject]
    public class SpawnBayData
    {
        [Key(0)] public string PlayerId { get; set; }
        [Key(1)] public float X { get; set; }
        [Key(2)] public float Y { get; set; }   // ← 必須！
        [Key(3)] public float Z { get; set; }
        [Key(4)] public int BayType { get; set; } // optional
    }
}
