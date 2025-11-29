using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.Models.Entities;
using realtime_game.Server.StreamingHubs;
using Shared.Interfaces.StreamingHubs;
using System.Numerics;
using UnityEngine;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Quaternion = UnityEngine.Quaternion;

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

        // ---------------------------------------------------------
        // ルームに入室（接続）する
        // ---------------------------------------------------------
        public async Task<JoinedUser[]> JoinAsync(string roomName, int userId)
        {
            // 複数ユーザーが同時に同じルームに入る時の競合を避けるためロック
            lock (roomContextRepos)
            {
                // 指定ルームを取得（無ければ null）
                this.roomContext = roomContextRepos.GetContext(roomName);

                // 無ければ新しく作成する
                if (this.roomContext == null)
                {
                    this.roomContext = roomContextRepos.CreateContext(roomName);
                    Console.WriteLine("Create Room:" + roomName);
                }
            }

            // ルーム内の MagicOnion グループに接続IDを追加
            // → クライアントにブロードキャストが送れるようになる
            this.roomContext.Group.Add(this.ConnectionId, Client);

            // DB からユーザー情報を取得
            GameDbContext context = new GameDbContext();
            User user = context.Users.Where(user => user.Id == userId).First();

            // このユーザーのルーム内情報を作成
            var joinedUser = new JoinedUser
            {
                ConnectionId = this.ConnectionId,
                UserData = user
            };

            // ルームのユーザー一覧に追加
            var roomUserData = new RoomUserData() { JoinedUser = joinedUser };
            this.roomContext.RoomUserDataList[ConnectionId] = roomUserData;

            // 自分以外のメンバーに「誰か入ってきたよ」と通知
            this.roomContext.Group.Except([this.ConnectionId]).OnJoin(joinedUser);

            // ログ出力
            Console.WriteLine($"[JOIN] User '{user.Name}' (ID={user.Id}) joined room '{roomName}'.");
            Console.WriteLine($"[ROOM STATUS] {roomName}: {this.roomContext.RoomUserDataList.Count} users now connected.");

            // 入室したユーザーに全参加者一覧を返す
            return this.roomContext.RoomUserDataList
                .Select(f => f.Value.JoinedUser)
                .ToArray();
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
            this.roomContext.RoomUserDataList.Remove(this.ConnectionId);

            // もし誰もいなくなったらルームごと削除
            if (this.roomContext.RoomUserDataList.Count == 0)
            {
                roomContextRepos.RemoveContext(this.roomContext.Name);
                Console.WriteLine($"Remove Room: {this.roomContext.Name}");
                roomContext = null;
            }

            return Task.CompletedTask;
        }

        public Task MoveAsync(UnityEngine.Vector3 pos, Quaternion rot)
        {
            // 位置情報を記録
            this.roomContext.RoomUserDataList[this.ConnectionId].pos = pos;

            this.roomContext.Group.Except(new[] { this.ConnectionId }).OnMove(this.ConnectionId, pos,rot);

            return Task.CompletedTask;

        }

    }
}
