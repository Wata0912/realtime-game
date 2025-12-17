using MagicOnion;
using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Shared.Interfaces.StreamingHubs
{
    /// <summary>
    /// サーバーからクライアントへの通知関連
    /// </summary>
    public interface IRoomHubReceiver
    {
        // [クライアントに実装]
        // [サーバーから呼び出す]

        // ユーザーの入室通知
        void OnJoin(JoinedUser user);

        //ユーザーの退出通知
        void OnLeave(Guid connectionId);

        public void OnReadyStateChanged(Guid connectionId, bool isReady);

        void OnAllReadyStateChanged(bool allReady);

        //ユーザーの位置通知
        void OnMove(Guid connectionId, Vector3 pos, Quaternion quaternion, int seq);
    }
}