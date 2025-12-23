using Cysharp.Threading.Tasks;
using MagicOnion;
using MagicOnion.Client;
using Shared.Interfaces.StreamingHubs;
using Shared.Models;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

public class RoomModel : BaseModel, IRoomHubReceiver
{
    private GrpcChannelx channel;
    private IRoomHub roomHub;
    [SerializeField] public Button LeaveButton;    // 退室ボタン
    [SerializeField] public Button JoinButton;     // 入室ボタン
    [SerializeField] public GameObject lobbyPanel;
    [SerializeField] public GameObject WinnerPanel;
    [SerializeField] GameObject Userprefub; //ユーザー情報所持の空オブジェクト
    RoomUser roomUser;                      //ユーザー情報所持スクリプト
    bool GameReady  = false;
    [SerializeField] public Button ReadyButton;
    [SerializeField] GameDirector gameDirector;
    bool isConnected;


    public Guid ConnectionId { get; set; }
    // 他ユーザー入室通知（GameDirector が購読する）
    public Action<JoinedUser> OnJoinedUser { get; set; }
    // 他ユーザー退室通知（GameDirector が購読する）
    public Action<Guid> OnLeftUser { get; set; }
    // ベイ移動通知（GameDirector が購読する）
    public Action<Guid, Vector3, UnityEngine.Quaternion, int> OnMoveBay { get; set; }
    //全ユーザー準備完了通知
    public Action<bool> OnAllReadyStateChangedEvent;
    public Action<Guid> OnDeadBay { get; set; }
    public Action<int> OnEnd { get; set; }
    public Action<Guid, Vector3, float> OnKnockbackEvent;


    private void Start()
    {
        // 初期状態では接続していない
        roomHub = null;
        ReadyButton.interactable = false;
    }

    public async UniTask ConnectAsync()
    {
        channel = GrpcChannelx.ForAddress(ServerURL);

        roomHub = await StreamingHubClient.ConnectAsync<IRoomHub, IRoomHubReceiver>(
            channel, this
        );

        ConnectionId = await roomHub.GetConnectionId();
        isConnected = true;
    }

    //========================================s
    // 入室処理
    //========================================
    public async UniTask JoinAsync(string room, int userId)
    {

        if (!isConnected)
        {
            Debug.LogError("未接続のため Join できません");
            return;
        }

        JoinedUser[] users = await roomHub.JoinAsync(room, userId);
        foreach (var u in users)
        {
            OnJoinedUser?.Invoke(u);

            Debug.Log($"接続ID:{ConnectionId} ユーザーID:{u.UserData.Id} ユーザーネーム:{u.UserData.Name}");

            // UI の状態更新
            JoinButton.interactable = false;
            LeaveButton.interactable = true;
            ReadyButton.interactable = true;
        }
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
    //========================================s
    // 準備完了送信
    //========================================
    public async void OnReadyButtonClicked()
    {
        Debug.Log("[Unity] Ready button clicked");
        await roomHub.SetMyReadyAsync(true);
        ReadyButton.interactable = false;
    }

    public void OnReadyStateChanged(Guid connectionId, bool isReady)
    {
        // ① 各ユーザーの Ready 状態更新
        gameDirector.OnUserReadyChanged(connectionId, isReady);

        // ② 自分自身の Ready 状態
        if (connectionId == ConnectionId)
        {
            GameReady = isReady;
        }
    }
    //========================================s
    // ルームのユーザー全員の準備完了
    //========================================
    public void OnAllReadyStateChanged(bool allReady)
    {

        Debug.Log($"[RoomModel] AllReady received = {allReady}");
        OnAllReadyStateChangedEvent?.Invoke(allReady);

        // UI の状態更新
        JoinButton.interactable = false;
        LeaveButton.interactable = false;
        ReadyButton.interactable = false;
        lobbyPanel.SetActive(false);

    }

    //========================================s
    // 退出
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
            ReadyButton.interactable = false;
        }
    }

    
    //========================================
    // 自分のベイの移動送信
    //========================================
    public async UniTask MoveAsync(Vector3 pos, UnityEngine.Quaternion rot, int seq)
    {
        if (roomHub != null)
            await roomHub.MoveAsync(pos, rot, seq);
    }

    public async UniTask KnockbackAsync(Guid targetId, Vector3 dir, float force)
    {
        if (roomHub == null)
            return;

        // 念のため正規化（安全）
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();

        await roomHub.KnockbackAsync(targetId, dir, force);
    }

    public Task SendSpawnPositionAsync(float x, float z)
    {
        return roomHub.SendSpawnPositionAsync(x, z);
    }


    //========================================
    // 自分のベイの死亡送信
    //========================================
    public async UniTask DeadAsync()
    {
        Debug.Log($"死亡");
        await roomHub.DeadAsync();
    }

    //========================================
    // 衝突判定(別方法での実装のため未実装)
    //========================================
    public void OnKnockback(Guid targetId, Vector3 dir, float force)
    {
        OnKnockbackEvent?.Invoke(targetId, dir, force);
    }


    public event Action<int> OnSpawnCountdown;
    public event Action<SpawnBayData[]> OnSpawnBays;

    void IRoomHubReceiver.OnSpawnCountdown(int sec)
    {
        OnSpawnCountdown?.Invoke(sec);
    }

    void IRoomHubReceiver.OnSpawnBays(SpawnBayData[] bays)
    {
        OnSpawnBays?.Invoke(bays);
    }

    // === StreamingHub コールバック ===

    //========================================
    // 他ユーザー入室通知（サーバー → クライアント）
    //========================================
    public void OnJoin(JoinedUser user) => OnJoinedUser?.Invoke(user);

    //========================================
    // 他ユーザーが退室したときの通知（サーバー → クライアント）
    //========================================
    public void OnLeave(Guid id) => OnLeftUser?.Invoke(id);
    //========================================
    // 自身の移動以外でベイが動いたときの通知
    //========================================
    public void OnMove(Guid id, Vector3 pos, UnityEngine.Quaternion rot, int seq) => OnMoveBay?.Invoke(id, pos, rot,seq);
    //========================================
    // 自身以外のベイの死亡通知
    //========================================
    public void OnDead(Guid id) => OnDeadBay?.Invoke(id);

    public void OnGameEnd(int id) =>  OnEnd?.Invoke(id);

}
