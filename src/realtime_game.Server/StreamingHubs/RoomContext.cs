using Cysharp.Runtime.Multicast;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using realtime_game.Server.StreamingHubs;
using Shared.Interfaces.StreamingHubs;

namespace realtime_game.Server.StreamingHubs
{
    // ---------------------------------------------------------
    // RoomContext
    // ・1つのルーム（部屋）の状態を保持するクラス
    // ・RoomContextRepository により管理される
    // ・Hub（RoomHub）からユーザー入退室時に参照される
    // ---------------------------------------------------------
    public class RoomContext : IDisposable
    {
        // このルームを識別するための固有ID（内部管理用）
        public Guid Id { get; }

        // ルーム名（JoinAsync() で指定される roomName がそのまま使われる）
        public string Name { get; }

        // ルーム内のユーザーを管理する MagicOnion の同期マルチキャストグループ
        // ・OnJoin / OnLeave などをルーム内全員へ一斉通知できる
        // ・キーは接続ID（ConnectionId）、値は受信側 IRoomHubReceiver
        public IMulticastSyncGroup<Guid, IRoomHubReceiver> Group { get; }

        // ルーム内のユーザー情報を保持する辞書
        // ・Key: ConnectionId
        // ・Value: RoomUserData（JoinedUser や追加情報を含む）
        public Dictionary<Guid, RoomUserData> RoomUserDataList { get; }
            = new Dictionary<Guid, RoomUserData>();


      
        // -----------------------------
        // 全員Ready状態が変化した時に呼ばれるイベント
        // true  : 全員Readyになった
        // false : Ready解除などで全員Readyではなくなった
        // -----------------------------
        public event Action<bool>? OnAllReadyStateChanged;

        // 前回の「全員Ready判定結果」
        // 状態変化を検出するために保持する
        private bool lastAllReadyState = false;

        // ---------------------------------------------------------
        // コンストラクタ
        // ・ルーム生成時に RoomContextRepository から呼ばれる
        // ---------------------------------------------------------
        public RoomContext(IMulticastGroupProvider groupProvider, string roomName)
        {
            // ルーム毎に固有のIDを持たせておく
            Id = Guid.NewGuid();

            // ルーム名を保持（JoinAsync の引数と一致する）
            Name = roomName;

            // MagicOnion のグループを取得 or 生成
            // 第一引数：キー（ConnectionId の型）
            // 第二引数：受信用インターフェース（IRoomHubReceiver）
            Group = groupProvider.GetOrAddSynchronousGroup<Guid, IRoomHubReceiver>(roomName);
        }

        // ---------------------------------------------------------
        // ルーム削除時に呼ばれる Dispose
        // ・RoomContextRepository.RemoveContext() で発火
        // ・MagicOnion のグループ資源を解放
        // ---------------------------------------------------------
        public void Dispose()
        {
            // グループそのものを dispose → 内部のセッションや通知を破棄
            Group.Dispose();
        }


        public void UpdateReadyState(Guid connectionId, bool isReady)
        {
            // 対象ユーザーが存在しない場合は何もしない
            if (!RoomUserDataList.TryGetValue(connectionId, out var user))
            {
                Console.WriteLine(
          $"[RoomContext] UpdateReadyState failed. User not found: {connectionId}"
      );
                return;
            }

            // Ready状態を更新
            user.ToReady = isReady;

            Console.WriteLine(
        $"[RoomContext] Ready updated: {connectionId} => {isReady}"
    );

            // 現在の「全員Readyか？」を判定
            bool currentAllReady = IsAllUserReady();

            Console.WriteLine(
                $"[RoomContext] AllReady check: {currentAllReady} (last={lastAllReadyState})");

            Console.WriteLine("すべてのプレイヤーの準備完了");

            // 前回の状態と違う場合のみ通知する
            if (currentAllReady != lastAllReadyState)
            {
                lastAllReadyState = currentAllReady;

                Console.WriteLine(
           $"[RoomContext] AllReadyStateChanged fired: {currentAllReady}"
       );

                // GameDirector などへ通知
                OnAllReadyStateChanged?.Invoke(currentAllReady);
            }
        }

        // -----------------------------
        // 全員Readyかどうかの純粋な判定処理
        // -----------------------------
        public bool IsAllUserReady()
        {
            // 誰もいない場合は false
            if (RoomUserDataList.Count == 0)
            {
                Console.WriteLine("[RoomContext] IsAllUserReady: no users");
                return false;
            }

            // 1人でも Ready でなければ false
            foreach (var user in RoomUserDataList.Values)
            {
                if (!user.ToReady)
                {
                    Console.WriteLine(
              $"[RoomContext] Not ready: ConnectionId={user.JoinedUser.ConnectionId}"
          );
                    return false;
                }
            }
            foreach (var user in RoomUserDataList.Values)
            {
                user.IsAlive = true;
            }

            // 全員 Ready
            return true;
        }

        // 退出時にも再判定（重要）
        public void RemoveUser(Guid connectionId)
        {
            if (RoomUserDataList.Remove(connectionId))
            {
                bool currentAllReady = IsAllUserReady();

                if (currentAllReady != lastAllReadyState)
                {
                    lastAllReadyState = currentAllReady;
                    OnAllReadyStateChanged?.Invoke(currentAllReady);
                }
            }
        }
        // -----------------------------
        // 残っているベイが最後の1個なのか
        // -----------
        public RoomUserData CheckGameEnd()
        {
            var aliveBays = RoomUserDataList.Values
                .Where(b => b.IsAlive)
                .ToList();
            Console.WriteLine("勝敗判定中");
            if (aliveBays.Count != 1)
            {
                Console.WriteLine("勝者未確定");
                return null;
            }

            var winner = aliveBays[0];
            Console.WriteLine($"生存ベイ{winner.JoinedUser.UserData.Name}");

            foreach (var user in RoomUserDataList.Values)
            {
                if (user.ToReady)
                {
                    user.ToReady = false;

                }
            }

            lastAllReadyState = false;

            return winner;

        }



    }
}