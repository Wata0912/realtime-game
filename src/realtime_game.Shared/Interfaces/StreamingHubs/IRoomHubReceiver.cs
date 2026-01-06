using MagicOnion;
using Shared.Models;
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

        //ユーザーの準備完了通知
        public void OnReadyStateChanged(Guid connectionId, bool isReady);

        //すべてのユーザーの準備完了通知
        void OnAllReadyStateChanged(bool allReady);

        //ベイの位置通知
        void OnMove(Guid connectionId, Vector3 pos, Quaternion quaternion, int seq);
        //ベイ生成カウントダウン
        void OnSpawnCountdown(int sec);
        //ベイの生成位置通知
        void OnSpawnBays(SpawnBayData[] bays);
        void OnKnockback(Guid targetId, Vector3 dir, float force);
        //衝突処理
        void OnHit(Guid connectionId1,Guid connectionId2);

        //ベイのの死亡通知
        void OnDead(Guid connectionId);

        //ゲームの終了通知
        void OnGameEnd(int id);

    }
}