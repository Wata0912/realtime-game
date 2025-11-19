using Cysharp.Runtime.Multicast;
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
    }
}
