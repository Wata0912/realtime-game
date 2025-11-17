using MagicOnion;
using System;
using System.Threading.Tasks;
namespace Shared.Interfaces.StreamingHubs
{
    public interface IRoomHub : IStreamingHub<IRoomHub, IRoomHubReceiver>
    {
        Task<Guid> GetConnectionId();
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);
    }
}