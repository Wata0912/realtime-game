using Cysharp.Runtime.Multicast;
using System.Collections.Concurrent;

namespace realtime_game.Server.StreamingHubs
{
    // 各ルーム（部屋）の状態を管理するクラス
    // ・roomName（文字列）をキーに RoomContext を保持する
    // ・RoomHub から呼ばれ、ルームの生成/取得/削除を担当
    public class RoomContextRepository(IMulticastGroupProvider groupProvider)
    {
        // 複数スレッドから同時アクセスされるため ConcurrentDictionary を使用
        // 例：複数ユーザーが同時に JoinAsync を呼ぶなど
        private readonly ConcurrentDictionary<string, RoomContext> contexts =
            new ConcurrentDictionary<string, RoomContext>();

        // ---------------------------------------------------------
        // ルームコンテキストの作成
        // ・新しい RoomContext（ルーム情報 + グループ情報）を生成
        // ・同名ルームが既に存在する場合は上書きされる
        // ---------------------------------------------------------
        public RoomContext CreateContext(string roomName)
        {
            // RoomContext はルーム1つ分のデータ（ユーザー一覧など）を持つ
            var context = new RoomContext(groupProvider, roomName);

            // ConcurrentDictionary はスレッドセーフに書き込み可能
            contexts[roomName] = context;

            return context;
        }

        // ---------------------------------------------------------
        // ルームコンテキストの取得
        // ・指定ルーム名の RoomContext を返す
        // ・存在しない場合は null を返す
        // ---------------------------------------------------------
        public RoomContext GetContext(string roomName)
        {
            if (!contexts.ContainsKey(roomName))
            {
                return null;
            }

            return contexts[roomName];
        }

        // ---------------------------------------------------------
        // ルームコンテキストの削除
        // ・ルーム内に誰もいなくなった時に呼ばれる
        // ---------------------------------------------------------
        public void RemoveContext(string roomName)
        {
            // ConcurrentDictionary.Remove はスレッドセーフに削除可能
            if (contexts.Remove(roomName, out var roomContext))
            {
                // RoomContext.Dispose() 内でグループ破棄などを行う
                // （Remove は LeaveAsync 内で Group.Remove をすでに行っている）
                roomContext?.Dispose();
            }
        }
    }
}
