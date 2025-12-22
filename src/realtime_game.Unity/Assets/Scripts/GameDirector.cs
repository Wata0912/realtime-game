using Cysharp.Threading.Tasks;
using Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameDirector : MonoBehaviour
{
    [SerializeField] GameObject Userprefub;
    RoomUser roomUser;
    [SerializeField] InputField RoomID;
    [SerializeField] InputField UserID;

    [SerializeField] GameObject Bayprefub;

    RoomModel roomModel;
    PlayerTop myPlayer;
    [SerializeField] public Transform stageCenter;
    


    int myUserId;
    public Dictionary<Guid, RoomUser> players = new Dictionary<Guid, RoomUser>();

    void Start()
    {
        roomModel = GetComponent<RoomModel>();

        roomModel.OnJoinedUser += OnJoinedUser;
        roomModel.OnMoveBay += OnMoveUser;
        roomModel.OnLeftUser += OnLeaveUser;
        roomModel.OnDeadBay += OnDead;
        roomModel.OnEnd += OnGameEnd;
        //roomModel.OnKnockbackEvent += OnKnockback;

        roomModel.ConnectAsync().Forget();
        roomModel.OnAllReadyStateChangedEvent += OnAllReadyStateChanged;


    }

    // =========================================================
    // 入室処理
    // =========================================================
    public async void JoinRoom()
    {
        // 入力チェック：空文字なら入室しない
        if (string.IsNullOrEmpty(RoomID.text)) return;
        if (string.IsNullOrEmpty(UserID.text)) return;

        if (!int.TryParse(UserID.text, out myUserId)) return;

        await roomModel.JoinAsync(RoomID.text, myUserId);
        // キャラクター生成


    }

    // =========================================================
    // ユーザーが入室してきた
    // =========================================================
    private void OnJoinedUser(JoinedUser user)
    {
        // すでに表示されているユーザーなら何もしない（重複生成防止）
        if (players.ContainsKey(user.ConnectionId))
            return;


        // キャラクター生成
        GameObject User = Instantiate(Userprefub);
        var userModel = User.AddComponent<RoomUser>();

        userModel.userId = user.UserData.Id;
        userModel.userName = user.UserData.Name;


        Debug.Log($"Join→{userModel.userId}:{userModel.userName}");
        // 接続IDをキーとして保持
        players[user.ConnectionId] = userModel;
    }

    //========================================
    // 退室処理
    //========================================
    public async void LeaveRoom()
    {
        if (roomModel != null)
        {
            try
            {
                // サーバーへ退室通知
                await roomModel.LeaveAsync();
                Debug.Log("ルーム退室完了");

                // 全キャラクターを削除（自分以外）
                foreach (var obj in players.Values)
                {
                    Destroy(obj);
                }

                // ローカルの一覧もクリア
                players.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError("LeaveRoom failed: " + e);
            }
        }
    }


    //========================================
    //ほかのユーザーが退出した
    //========================================
    private void OnLeaveUser(Guid connectionId)
    {

        // 該当ユーザーが一覧に存在すれば削除
        if (players.TryGetValue(connectionId, out var obj))
        {
            Destroy(obj);                 // 画面から削除
            players.Remove(connectionId);  // 管理リストから削除

            Debug.Log($"ユーザー退室→{players[connectionId].userId}:{players[connectionId].userName}");
        }
        // 存在しなければ何もしない
    }
    
  

    // RoomModel から呼ばれる入口
    public void OnUserReadyChanged(Guid connectionId, bool isReady)
    {
        if (!players.TryGetValue(connectionId, out var player))
            return;

        //player.SetReady(isReady);
    }

    //========================================
    //リモートベイの位置同期
    //========================================
    private void OnMoveUser(Guid id, Vector3 pos, Quaternion rot, int seq)
    {
        if (players.TryGetValue(id, out var user))
        {
            if (user.bay != null)
            {
                user.bay.SetRemoteState(pos, rot,seq);
            }
        }        
    }

    //========================================
    //ゲームの開始準備ができた
    //========================================
    public void OnAllReadyStateChanged(bool allReady)
    {
        Debug.Log($"[GameDirector] AllReady = {allReady}");

        if (!allReady)
            return;

        OnAllPlayerReady();
    }

    //========================================
    //ゲーム開始処理
    //========================================
    public void OnAllPlayerReady()
    {
        Debug.Log("[GameDirector] 全員Ready → ゲーム開始");

        // フェーズ遷移・カウントダウン・操作解放など
        SpawnBays();
    }

    //========================================
    //ベイの生成
    //========================================
    public void SpawnBays()
    {
        foreach (var pair in players)
        {
            RoomUser user = pair.Value;

            // すでにベイがあるなら作らない
            if (user.bay != null) continue;

            Vector3 spawnPos = new Vector3(
            UnityEngine.Random.Range(-3f, 3f),
            0f,
            UnityEngine.Random.Range(-3f, 3f)
            );

            GameObject bayObj = Instantiate(Bayprefub, spawnPos, Quaternion.identity);
            PlayerTop bay = bayObj.GetComponent<PlayerTop>();
            bay.stageCenter = stageCenter;

            // ローカル / リモート判定
            bay.Initialize(roomModel.ConnectionId,user.userId, user.userId == myUserId);

            // RoomModel を渡す（同期用）
            bay.roomModel = roomModel;

            // RoomUser に紐づける
            user.bay = bay;
        }
    }

    //========================================
    //ベイの死亡
    //========================================
    public void OnDead(Guid connectionId)
    {
        if (!players.TryGetValue(connectionId, out var user))
            return;

        if (user.bay != null) return;

        // 自分のベイ → 何もしない（すでにDie済み）
        if (user.bay.isLocalPlayer)
            return;

        // リモートベイ専用処理
        user.bay.ApplyRemoteDead();

        Debug.Log($"Remote Die: {user.userName}");
    }

    void OnGameEnd(int winnerUserId)
    {
     
        if (winnerUserId == myUserId)
        {
            Debug.Log("YOU WIN");
        }
        else
        {
            Debug.Log("YOU LOSE");
        }
    }

}
