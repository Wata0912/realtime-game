using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.Models.Entities;
using realtime_game.Server.StreamingHubs;
using Shared.Interfaces.StreamingHubs;
using Shared.Models;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace realtime_game.Server.StreamingHubs
{
    // MagicOnion の StreamingHub。IRoomHub がクライアント⇄サーバーのメソッド定義。
    public class RoomHub(RoomContextRepository roomContextRepository)
        : StreamingHubBase<IRoomHub, IRoomHubReceiver>, IRoomHub
    {
        // ルーム管理するクラス（DI で渡される）
        private RoomContextRepository roomContextRepos;

        // 現在の接続が属しているルームの情報（部屋名・ユーザーリストなど）
        private RoomContext roomContext;

        private readonly object collisionSync = new();
        private readonly HashSet<string> collisionProcessing = new();



        // ---------------------------------------------------------
        // ルームに入室（接続）する
        // ---------------------------------------------------------
        public async Task<JoinedUser[]> JoinAsync(string roomName, int userId)
        {
            // ルーム取得 or 作成（ここは repos を lock）
            lock (roomContextRepos)
            {
                this.roomContext = roomContextRepos.GetContext(roomName);

                if (this.roomContext == null)
                {
                    this.roomContext = roomContextRepos.CreateContext(roomName);
                    Console.WriteLine("Create Room:" + roomName);
                }
            }

            // グループ追加（スレッド安全）
            this.roomContext.Group.Add(this.ConnectionId, Client);

            // DB 取得
            GameDbContext context = new GameDbContext();
            User user = context.Users.First(u => u.Id == userId);

            var joinedUser = new JoinedUser
            {
                ConnectionId = this.ConnectionId,
                UserData = user
            };

            var roomUserData = new RoomUserData
            {
                JoinedUser = joinedUser
            };

            JoinedUser[] result;

            // ★ ここが最重要：追加と列挙を同一 lock に入れる
            lock (this.roomContext)
            {
                this.roomContext.RoomUserDataList[this.ConnectionId] = roomUserData;

                result = this.roomContext.RoomUserDataList
                    .Values
                    .Where(v => v?.JoinedUser != null)
                    .Select(v => v.JoinedUser)
                    .ToArray();
            }

            // 通知（lock の外でOK）
            this.roomContext.Group.Except([this.ConnectionId]).OnJoin(joinedUser);

            Console.WriteLine($"[JOIN] User '{user.Name}' joined room '{roomName}'.");

            // イベント登録（多重登録防止は別途対応可）
            this.roomContext.OnAllReadyStateChanged += OnAllReadyStateChanged;

            return result;
        }

        // ---------------------------------------------------------
        // 接続時（クライアントが Hub に接続した瞬間）の処理
        // ---------------------------------------------------------
        protected override ValueTask OnConnected()
        {
            // DI の roomContextRepository をローカルに保持
            roomContextRepos = roomContextRepository;
            return default;
        }

        // ---------------------------------------------------------
        // 切断時（ネットワーク切断など）
        // ※ ここでは特に何もしない
        // ---------------------------------------------------------
        protected override ValueTask OnDisconnected()
        {
            return default;
        }

        // ---------------------------------------------------------
        // 接続ID をクライアントに返す
        // ---------------------------------------------------------
        public Task<Guid> GetConnectionId()
        {
            return Task.FromResult(this.ConnectionId);
        }

        // ---------------------------------------------------------
        // ルームから退室
        // ---------------------------------------------------------
        public Task LeaveAsync()
        {
            // roomContext が null → Join されていない
            if (roomContext == null)
            {
                Console.WriteLine("[LeaveAsync] roomContext が null のため処理を中断");
                return Task.CompletedTask;
            }

            // グループ未生成の可能性もあるためチェック
            if (roomContext.Group == null)
            {
                Console.WriteLine("[LeaveAsync] roomContext.Group が null のため処理を中断");
                return Task.CompletedTask;
            }


            // 全体に「退室したよ」と通知
            this.roomContext.Group.All.OnLeave(this.ConnectionId);

            // グループから自分を削除
            try
            {
                this.roomContext.Group.Remove(this.ConnectionId);
                Console.WriteLine($"[RoomHub] Removed from group: {this.ConnectionId}");
            }
            catch (ObjectDisposedException)
            {
                // 既に破棄されていたら無視
                Console.WriteLine("Group already disposed");
            }

            // ルーム内ユーザー一覧から削除
            roomContext.RemoveUser(this.ConnectionId);

            // もし誰もいなくなったらルームごと削除
            if (this.roomContext.RoomUserDataList.Count == 0)
            {
                roomContextRepos.RemoveContext(this.roomContext.Name);
                Console.WriteLine($"Remove Room: {this.roomContext.Name}");
                roomContext = null;
            }

            return Task.CompletedTask;
        }

        // ---------------------------------------------------------
        // 準備完了受信
        // ---------------------------------------------------------
        public async Task SetMyReadyAsync(bool isReady)
        {
            if (roomContext == null) return;

            // MagicOnion の接続ID
            Guid connectionId = Context.ContextId;

            Console.WriteLine(
            $"[SetMyReadyAsync] ConnectionId={connectionId}, isReady={isReady}"
   );
            // Ready状態の変更は RoomContext に任せる
            roomContext.UpdateReadyState(connectionId, isReady);

            await Task.CompletedTask;
        }


        private void OnAllReadyStateChanged(bool allReady)
        {
            if (roomContext == null) return;

            
            // ルーム全体にブロードキャスト
            roomContext.Group.All.OnAllReadyStateChanged(allReady);
        }

        public async Task SendSpawnPositionAsync(float x, float z)
        {
            var ctx = roomContext;

            ctx.SpawnPositions[Context.ContextId] = (x, z);

            // 全員分揃ったか？
            if (ctx.SpawnPositions.Count < ctx.RoomUserDataList.Count)
                return;

            // ===== カウントダウン =====
            for (int i = 3; i > 0; i--)
            {
                ctx.Group.All.OnSpawnCountdown(i);
                await Task.Delay(1000);
            }

            // ===== ベイ生成データ作成 =====
            var list = ctx.SpawnPositions.Select(p => new SpawnBayData
            {
                PlayerId = p.Key.ToString(),
                X = p.Value.x,
                Z = p.Value.z,
                BayType = 0

            }).ToArray();

            foreach(var item in list)
            {
                Console.WriteLine(item.PlayerId,item.X, item.Z);
            }

            ctx.Group.All.OnSpawnBays(list);
        }


        // ---------------------------------------------------------
        // ベイ移動受信
        // ---------------------------------------------------------
        public Task MoveAsync(UnityEngine.Vector3 pos, Quaternion rot, int seq)
        {
            if (roomContext == null)
                return Task.CompletedTask;

            // 必要なら記録（任意）
            this.roomContext.RoomUserDataList[this.ConnectionId].pos = pos;

            // ★ seq を必ず中継する
            this.roomContext.Group
                .Except(new[] { this.ConnectionId })
                .OnMove(this.ConnectionId, pos, rot, seq);

            return Task.CompletedTask;
        }

        public Task KnockbackAsync(Guid targetId, Vector3 dir, float force)
        {
            if (roomContext == null)
                return Task.CompletedTask;

            // 全員にノックバック通知（自分含む）
            roomContext.Group.All.OnKnockback(targetId, dir, force);

            return Task.CompletedTask;
        }

        public async Task ReportCollision(Guid a, Guid b)
        {
            if (a == b)
                return;

            string key = MakePairKey(a, b);

            // ★ 同時実行防止だけ
            lock (collisionSync)
            {
                if (collisionProcessing.Contains(key))
                    return;

                collisionProcessing.Add(key);
            }

            try
            {
                // ノックバック通知
                roomContext.Group.All.OnHit(a, b);              
            }
            finally
            {
                // ★ 処理終了後に必ず解放
                lock (collisionSync)
                {
                    collisionProcessing.Remove(key);
                }
            }

            await Task.CompletedTask;

        }


        // ---------------------------------------------------------
        // ベイ死亡受信
        // ---------------------------------------------------------
        public Task DeadAsync()
        {
            RoomUserData Winner;

            if (roomContext == null)
                return Task.CompletedTask;
            Console.WriteLine($"[Dead] User '{this.ConnectionId}");

            // ===== サーバー状態を更新 =====
            if (!roomContext.RoomUserDataList.TryGetValue(this.ConnectionId, out var bay))
                return Task.CompletedTask;

            if (!bay.IsAlive)
                return Task.CompletedTask;

            bay.IsAlive = false;

            // 全員に「このConnectionIdのベイが死亡した」と通知
            roomContext.Group.All.OnDead(this.ConnectionId);

            Winner =  roomContext.CheckGameEnd();

            if (Winner == null)
            {
                Console.WriteLine("勝者未確定");
                return Task.CompletedTask;
            }


            // 全クライアントに勝者通知
            roomContext.Group.All.OnGameEnd(Winner.JoinedUser.UserData.Id);

            return Task.CompletedTask;
        }

        //Guid ペアを一意キーにする関数
        private static string MakePairKey(Guid a, Guid b)
        {
            return string.CompareOrdinal(a.ToString(), b.ToString()) < 0
                ? $"{a}_{b}"
                : $"{b}_{a}";
        }

    }
}