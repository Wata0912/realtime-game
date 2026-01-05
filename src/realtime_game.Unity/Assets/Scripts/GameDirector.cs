using Cysharp.Threading.Tasks;
using Shared.Interfaces.StreamingHubs;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameDirector : MonoBehaviour
{
    [SerializeField] GameObject Userprefub;
    RoomUser roomUser;
    [SerializeField] InputField RoomID;
    [SerializeField] InputField UserID;
    [SerializeField] Text WinnerName;
    [SerializeField] GameObject Bayprefub;

    RoomModel roomModel;
    PlayerTop myPlayer;
    [SerializeField] public Transform stageCenter;
    
    int myUserId;
    public Dictionary<Guid, RoomUser> players = new Dictionary<Guid, RoomUser>();

    [SerializeField] GameObject spawnCursorPrefab;
    [SerializeField] GameObject spawnButton;
    SpawnCursorController cursor;
    bool selectingSpawn;

    [SerializeField] GameObject spawnCursor;

    void Start()
    {
        roomModel = GetComponent<RoomModel>();

        roomModel.OnJoinedUser += OnJoinedUser;
        roomModel.OnMoveBay += OnMoveUser;
        roomModel.OnLeftUser += OnLeaveUser;
        roomModel.OnDeadBay += OnDead;
        roomModel.OnEnd += OnGameEnd;
        //roomModel.OnKnockbackEvent += OnKnockback;
        roomModel.OnSpawnBays += SpawnBays;

        roomModel.ConnectAsync().Forget();
        roomModel.OnAllReadyStateChangedEvent += OnAllReadyStateChanged;
        spawnCursor.SetActive(false);
    }

    void Update()
    {
       
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
       
        userModel.ConnectionId = user.ConnectionId;
        userModel.userId = user.UserData.Id;
        userModel.userName = user.UserData.Name;
        userModel.userObject = User;

        Debug.Log($"Join→{userModel.userId}:{userModel.userName}:{userModel.ConnectionId}");

        players.Add(user.ConnectionId,userModel);

        foreach (var player in players)
        {
            Debug.Log($"player:{player.Value.userName}");
        }
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
                    if (obj.bay != null)
                        Destroy(obj.bay.gameObject);

                    if (obj.userObject != null)
                        Destroy(obj.userObject);
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
            Debug.Log($"ユーザー退室→{players[connectionId].userId}:{players[connectionId].userName}");
            if (obj.bay != null)
            {
                Destroy(obj.bay.gameObject);
            }

            // ★ユーザー表示削除
            if (obj.userObject != null)
            {
                Destroy(obj.userObject);
            }

            players.Remove(connectionId);  // 管理リストから削除
            //Destroy(obj);                 // 画面から削除

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
        StartSpawnSelect();
        Debug.Log($"ベイ生成座標指定開始");
        
    }

    //ベイ生成位置設定開始
    void StartSpawnSelect()
    {
        //spawnCursor = Instantiate(spawnCursor);
        spawnCursor.SetActive(true);
        spawnButton.SetActive(true);
        //selectingSpawn = true;
    }


    public void ConfirmSpawn()
    {
        //selectingSpawn = false;

        Vector3 pos = spawnCursor.transform.position;
        spawnCursor.SetActive(false);
        spawnButton.SetActive(false);
        //Destroy(spawnCursor);
        Debug.Log($"POST X:{pos.x} Z:{pos.z}");
        roomModel.SendSpawnPositionAsync(pos.x, pos.z);
    }

    

//ベイ生成位置取得
public Vector3 GetCursorWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hit = ray.GetPoint(distance);
            hit.y = 0f;
            return hit;
        }
        return Vector3.zero;
    }

    //========================================
    //ベイの生成
    //========================================
    void SpawnBays(SpawnBayData[] list)
    {
        foreach (var data in list)
        {
            Guid id = Guid.Parse(data.PlayerId);

            // 対応するユーザーを探す
            if (!players.TryGetValue(id, out var user))
                continue;

            // すでにベイがあるなら生成しない
            if (user.bay != null)
                continue;

            Vector3 pos = new Vector3(data.X, 0f, data.Z);
            Debug.Log($"Spawn {id} at {pos}");

            GameObject bayObj = Instantiate(Bayprefub, pos, Quaternion.identity);
            PlayerTop bay = bayObj.GetComponent<PlayerTop>();

            bay.stageCenter = stageCenter;

            bool isLocal = id == roomModel.ConnectionId;

            bay.Initialize(id, user.userId, isLocal);
            bay.roomModel = roomModel;

            user.bay = bay;
        }
    }

  

    //========================================
    //ベイの死亡
    //========================================
    public void OnDead(Guid connectionId)
    {
        if (!players.TryGetValue(connectionId, out var user))
            //return;

        if (user.bay != null) return;

        // 自分のベイ → 何もしない（すでにDie済み）
        if (user.bay.isLocalPlayer)
            return;

        

        user.bay.ApplyRemoteDead(); // Fixed: Use ApplyRemoteDead
        Debug.Log($"Remote Die: {user.userName}");
    }

    void OnGameEnd(int winnerUserId)
    {
        roomModel.ReadyButton.interactable = true;
        roomModel.LeaveButton.interactable = true;
        
        string winner = null;
        
       foreach (var player in players)
        {
            if(player.Value .userId == winnerUserId)
            {
                Debug.Log($"Winner:{player.Value.userId}_{player.Value.userName}");
                winner = player.Value.userName;
            }
        }

       WinnerName.text = "Winner " + winner;
        roomModel.WinnerPanel.SetActive(true);
    }

    public void Reset()
    {

        foreach (var obj in players.Values)
        {
            if (obj.bay != null)
                Destroy(obj.bay.gameObject);

            if (obj.userObject != null)
                Destroy(obj.userObject);
        }

        roomModel.WinnerPanel.SetActive(false);
        roomModel.lobbyPanel.SetActive(true);
       
    }


}


