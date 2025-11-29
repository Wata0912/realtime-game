using Cysharp.Threading.Tasks;
using DG.Tweening;
using MagicOnion;
using MagicOnion.Client;
using Shared.Interfaces.StreamingHubs;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RoomModel : BaseModel, IRoomHubReceiver
{
    private GrpcChannelx channel;     // MagicOnion の gRPC 通信チャネル
    private IRoomHub roomHub;         // StreamingHub クライアント（サーバーと双方向通信）

    [SerializeField] Button LeaveButton;   // 退室ボタン
    [SerializeField] Button JoinButton;    // 入室ボタン

    //========================================
    // 接続ID（StreamingHub につながるとサーバーが発行する）
    //========================================
    public Guid ConnectionId { get; set; }

    //========================================
    // イベントコールバック
    //========================================

    // 他ユーザー入室通知（GameDirector が購読する）
    public Action<JoinedUser> OnJoinedUser { get; set; }

    // 他ユーザー退室通知（GameDirector が購読する）
    public Action<Guid> OnLeftUser { get; set; }


    //移動通知
    // 変更後（3 引数：Quaternion を追加）
    public Action<Guid, Vector3, Quaternion> OnMoveUser { get; set; }


    private void Start()
    {
        // 初期状態では接続していない
        roomHub = null;
    }

    //========================================
    // MagicOnion サーバーに接続
    //========================================
    public async UniTask ConnectAsync()
    {
        // サーバーのアドレスに接続
        channel = GrpcChannelx.ForAddress(ServerURL);

        // StreamingHub 接続（this = IRoomHubReceiver の実装としてコールバック受取）
        roomHub = await StreamingHubClient
            .ConnectAsync<IRoomHub, IRoomHubReceiver>(channel, this);

        // 接続ID取得（サーバー側で生成される）
        this.ConnectionId = await roomHub.GetConnectionId();
    }

    //========================================
    // MagicOnion 切断処理
    //========================================
    public async UniTask DisconnectAsync()
    {
        // Hub の Dispose → サーバーへの接続を解除
        if (roomHub != null) await roomHub.DisposeAsync();

        // gRPC チャネルをシャットダウン
        if (channel != null) await channel.ShutdownAsync();

        roomHub = null;
        channel = null;
    }

    //========================================
    // Unity オブジェクト破棄時（シーン切替など）
    //========================================
    async void OnDestroy()
    {
        if (roomHub != null)
        {
            await DisconnectAsync();

            // 退室フラグをクリア（PlayerController 側の状態管理用）
            PlayerController.Tojoin = false;
        }
    }

    //========================================
    // 入室処理（Join ボタンから呼ばれる）
    //========================================
    public async UniTask JoinAsync(string roomName, int userId)
    {
        // サーバーの Join を呼び出し → すでに入室しているユーザー一覧が返る
        JoinedUser[] users = await roomHub.JoinAsync(roomName, userId);

        // 受け取った既存ユーザー情報をクライアント側に通知
        foreach (var user in users)
        {
            OnJoinedUser?.Invoke(user);

            Debug.Log($"接続ID:{ConnectionId} ユーザーID:{user.UserData.Id} ユーザーネーム:{user.UserData.Name}");

            // 自分が入室中であることを記録
            PlayerController.Tojoin = true;

            // UI の状態更新
            JoinButton.interactable = false;
            LeaveButton.interactable = true;
        }
    }

    //========================================
    // 他ユーザー入室通知（サーバー → クライアント）
    //========================================
    public void OnJoin(JoinedUser user)
    {
        // 登録されているコールバックがあれば呼ぶ
        OnJoinedUser?.Invoke(user);
    }

    //========================================
    // 自分がルームから退室する（Leave ボタンから呼ばれる）
    //========================================
    public async UniTask LeaveAsync()
    {
        if (roomHub != null)
        {
            // サーバーに退室を通知（RoomHub.LeaveAsync が呼ばれる）
            await roomHub.LeaveAsync();

            // 自分はもうルームに入っていない
            PlayerController.Tojoin = false;

            // ボタン状態を切り替え
            JoinButton.interactable = true;
            LeaveButton.interactable = false;
        }
    }

    //========================================
    // 他ユーザーが退室したときの通知（サーバー → クライアント）
    //========================================
    public void OnLeave(Guid connectionId)
    {
        // 登録されている処理を呼ぶ
        // GameDirector ではこれを受けてキャラ削除が行われる
        OnLeftUser?.Invoke(connectionId);
    }

    // サーバーからの移動通知を受け取る
    // クライアント → サーバー に位置を送る
    public async UniTask MoveAsync(Vector3 pos,Quaternion rot )
    {
        if (roomHub != null)
        {
            await roomHub.MoveAsync(pos,rot);
        }
    }

    // サーバー → クライアント から位置通知を受ける
    public void OnMove(Guid connectionId, Vector3 pos, Quaternion rot)
    {
        // 登録されたコールバック(API使用側)を呼ぶ
        OnMoveUser?.Invoke(connectionId, pos, rot);
    }

}
