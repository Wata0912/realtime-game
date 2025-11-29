using Shared.Interfaces.StreamingHubs;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs
{
    // ルーム内のユーザー単体の情報
    public class RoomUserData
    {
        public JoinedUser JoinedUser;
        internal Vector3 pos;
    }
}