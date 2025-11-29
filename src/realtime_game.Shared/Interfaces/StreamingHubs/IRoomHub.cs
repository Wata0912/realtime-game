using MagicOnion;
using System;
using System.Numerics;
using System.Threading.Tasks;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
namespace Shared.Interfaces.StreamingHubs
{
    public interface IRoomHub : IStreamingHub<IRoomHub, IRoomHubReceiver>
    {   //ID取得
        Task<Guid> GetConnectionId();

        //入出
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);

        //退出
        Task LeaveAsync();

        //位置同期
        Task MoveAsync(Vector3 pos, Quaternion quaternion);
    }
}