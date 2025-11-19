using realtime_game.Server.Models.Entities;
using Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameDirector : MonoBehaviour
{
    [SerializeField] GameObject characterPrefab;                // 生成するキャラクターのプレハブ
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>(); // 接続IDとキャラオブジェクトの対応表

    [SerializeField] InputField RoomID;                        // 入室するルームID入力欄
    [SerializeField] InputField UserID;                        // 自分のユーザーID入力欄

    private bool OnAdmissionStatus = false;                    // 未使用（入室状態フラグ？）

    RoomModel roomModel;                                       // ルーム通信モデル
    UserModels userModels;                                     // ユーザー管理モデル

    int myUserId;                                              // 自分のユーザーID
    User myself;                                               // 自分のユーザーデータ（未使用状態）

    //========================================
    // 初期化処理
    //========================================
    async void Start()
    {
        roomModel = GetComponent<RoomModel>();
        userModels = GetComponent<UserModels>();

        // ルーム内イベントの購読（ユーザー入室 / 退室）
        roomModel.OnJoinedUser += this.OnJoinedUser;
        roomModel.OnLeftUser += this.OnLeaveUser;

        // MagicOnion サーバーとの接続
        await roomModel.ConnectAsync();
        try
        {
            // 本来はここでユーザーデータ取得などを行う
            // myself = await userModels.GetUser(myUserId);
        }
        catch (Exception e)
        {
            Debug.Log("RegistUser failed");
            Debug.LogException(e);
        }
    }

    //========================================
    // 入室処理
    //========================================
    public async void JoinRoom()
    {
        // 入力チェック：空文字なら入室しない
        if (string.IsNullOrEmpty(RoomID.text)) return;
        if (string.IsNullOrEmpty(UserID.text)) return;

        // ユーザーID文字列 → int に変換
        if (!int.TryParse(UserID.text, out int userid))
        {
            Debug.Log("数字ではありません");
            return;
        }

        Debug.Log("変換成功: " + userid);
        myUserId = userid;

        // ルームに入室
        await roomModel.JoinAsync(RoomID.text, myUserId);
        Debug.Log(RoomID.text);
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
                foreach (var obj in characterList.Values)
                {
                    Destroy(obj);
                }

                // ローカルの一覧もクリア
                characterList.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError("LeaveRoom failed: " + e);
            }
        }
    }

    //========================================
    // ほかのユーザーが入室してきた時の処理
    //========================================
    private void OnJoinedUser(JoinedUser user)
    {
        // すでに表示されているユーザーなら何もしない（重複生成防止）
        if (characterList.ContainsKey(user.ConnectionId))
            return;

        // 自分自身ならキャラ生成しない
        if (user.UserData.Id == myUserId)
            return;

        // キャラクター生成
        GameObject characterObject = Instantiate(characterPrefab);
        characterObject.transform.position = new Vector3(0, 0, 0);

        // 接続IDをキーとして保持
        characterList[user.ConnectionId] = characterObject;
    }

    //========================================
    // ほかのユーザーが退室した時の処理
    //========================================
    private void OnLeaveUser(Guid connectionId)
    {
        // 該当ユーザーが一覧に存在すれば削除
        if (characterList.TryGetValue(connectionId, out var obj))
        {
            Destroy(obj);                 // 画面から削除
            characterList.Remove(connectionId);  // 管理リストから削除

            Debug.Log($"ユーザー退室: {connectionId}");
        }
        // 存在しなければ何もしない
    }
}
