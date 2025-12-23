using MagicOnion;
using System;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
namespace Shared.Interfaces.StreamingHubs
{
    /// <summary>
    /// クライアントからサーバーの通知関連
    /// </summary>
    public interface IRoomHub : IStreamingHub<IRoomHub, IRoomHubReceiver>
    {   //ID取得
        Task<Guid> GetConnectionId();

        //入出
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);

        //退出
        Task LeaveAsync();

        //準備完了
        Task SetMyReadyAsync(bool isReady);

        //位置同期
        Task MoveAsync(Vector3 pos, Quaternion quaternion, int seq);

        Task SendSpawnPositionAsync(float x, float z);

        Task KnockbackAsync(Guid targetId, Vector3 dir, float force);

        //ベイ死亡
        Task DeadAsync();

    }
}