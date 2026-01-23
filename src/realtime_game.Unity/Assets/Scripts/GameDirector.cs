using Cysharp.Threading.Tasks;
using Shared.Interfaces.StreamingHubs;
using Shared.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;



public class GameDirector : MonoBehaviour
{


    [SerializeField] GameObject Userprefub;
    RoomUser roomUser;
    [SerializeField] InputField RoomID;
    [SerializeField] InputField UserID;
    [SerializeField] Text WinnerName;
    [SerializeField] GameObject[] Bayprefub;
    [SerializeField] private TMP_Dropdown bayDropdown;
    [SerializeField] UnityEngine.UI.Slider HPBer;
    [SerializeField] Text UsersText;

    [SerializeField] GameObject playerNamePrefab;
    [SerializeField] Transform contentTransform;

    [SerializeField] Text readyText;
    [SerializeField] Text goText;
    [SerializeField] GameObject ReadyButton;


    // 表示済み管理（超重要）
    Dictionary<Guid, GameObject> playerNameItems = new();

    RoomModel roomModel;
    PlayerTop myPlayer;
    [SerializeField] public Transform stageCenter;
    
    int myUserId;
    public int SelectedBayType { get; private set; }
    public Dictionary<Guid, RoomUser> players = new Dictionary<Guid, RoomUser>();

    [SerializeField] GameObject spawnCursorPrefab;
    [SerializeField] GameObject spawnButton;
    SpawnCursorController cursor;
    bool selectingSpawn;
    [SerializeField] GameObject spawnCursor;

    [SerializeField] GameObject CreateUserPanel;
    [SerializeField] GameObject CreatedUserPanel;

    [Header("Sound")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip[] bgmClips;  //0:ロビー   1:ゲーム     2:リザルト
    [SerializeField] float fadeTime = 1.0f;
    [SerializeField] AudioClip hitSE;
    [SerializeField] AudioClip advanceSE;
    [SerializeField] AudioClip returnSE;

    int currentIndex = -1;
    Coroutine fadeCoroutine;

    bool isTransitioning = false;
    [SerializeField] float showTimeGoText = 0.3f; // 表示時間（0.2〜0.4がおすすめ）

    void Start()
    {
        roomModel = GetComponent<RoomModel>();

        roomModel.OnJoinedUser += OnJoinedUser;
        roomModel.OnMoveBay += OnMoveUser;
        roomModel.OnLeftUser += OnLeaveUser;
        roomModel.OnDeadBay += OnDead;
        roomModel.OnEnd += OnGameEnd;
        roomModel.OnHitBay += OnHitBay;
        //roomModel.OnKnockbackEvent += OnKnockback;
        roomModel.OnSpawnBays += SpawnBays;

        roomModel.ConnectAsync().Forget();
        roomModel.OnAllReadyStateChangedEvent += OnAllReadyStateChanged;
        spawnCursor.SetActive(false);
        UsersText.gameObject.SetActive(false);
        PlayBGM(0);
        isTransitioning = false;
        readyText.gameObject.SetActive(false);
        goText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (myPlayer == null)
            return;

        HPBer.value = myPlayer.currentHP;
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
        UsersText.gameObject.SetActive(true);
        checksolo();
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

      
        RefreshPlayerNameList();
        checksolo();
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

                // --- データ削除 ---
                players.Remove(roomModel.ConnectionId);

                // 🔴 ここが重要：UIを全消し
                foreach (var ui in playerNameItems.Values)
                {
                    Destroy(ui);
                }

                playerNameItems.Clear();
                //players.Clear();

                // 全キャラクターを削除（自分以外）
                foreach (var obj in players.Values)
                {
                    if (obj.bay != null)
                    {
                       
                        Destroy(obj.bay.gameObject);
                    }
                   

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

           
            UsersText.gameObject.SetActive(false);
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
                //players[connectionId].bay.pushForce;
            }

            // ★ユーザー表示削除
            if (obj.userObject != null)
            {
                Destroy(obj.userObject);
            }

            players.Remove(connectionId);  // 管理リストから削除
                                           //Destroy(obj);                 // 画面から削除

           
            if (playerNameItems.TryGetValue(connectionId, out var item))
            {
                Destroy(item);
                playerNameItems.Remove(connectionId);
            }

        }
        // 存在しなければ何もしない
        checksolo();
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
        PlayBGM(1);

    }

    //ベイ生成位置設定開始
    void StartSpawnSelect()
    {
        //spawnCursor = Instantiate(spawnCursor);
        spawnCursor.SetActive(true);
        spawnButton.SetActive(true);
        bayDropdown.gameObject.SetActive(true);
        // 初期値
        SelectedBayType = bayDropdown.value;
        //selectingSpawn = true;
    }


    public void ConfirmSpawn()
    {
        //selectingSpawn = false;

        Vector3 pos = spawnCursor.transform.position;
        spawnCursor.SetActive(false);
        spawnButton.SetActive(false);    
        //Destroy(spawnCursor);
        //仮置き　
        SelectedBayType = bayDropdown.value;
        bayDropdown.gameObject.SetActive(false);
        Debug.Log($"POST X:{pos.x} Z:{pos.z} BayType:{SelectedBayType}");

        
        roomModel.SendSpawnPositionAsync(pos.x, pos.z, SelectedBayType);      

        readyText.gameObject.SetActive(true);

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
            Debug.Log($"Spawn {id} at BayType{data.BayType}:{pos}");

            GameObject bayObj = Instantiate(Bayprefub[data.BayType], pos, Quaternion.identity);
            PlayerTop bay = bayObj.GetComponent<PlayerTop>();

            bay.stageCenter = stageCenter;

            bool isLocal = id == roomModel.ConnectionId;

            bay.Initialize(id, user.userId, isLocal);
            bay.roomModel = roomModel;

            user.bay = bay;
          
        }

        if (players.TryGetValue(roomModel.ConnectionId ,out var Player))
        {
            myPlayer = Player.bay;
        }

        HPBer.gameObject.SetActive(true);
        HPBer.maxValue = myPlayer.maxHP;
        HPBer.value = myPlayer.currentHP;
        StartCoroutine(ShowGo()); // ⭕

    }


    public void OnHitBay(Guid a,Guid b )
    {
        PlayerTop LocalBay = null;
        PlayerTop RemoteBay = null;

        PlayHitSE();

        if (players.TryGetValue(a, out var A))
        {
            if (A.bay != null)
            {
                if (A.bay.isLocalPlayer == true)
                {
                    LocalBay = A.bay;
                }else
                {
                    RemoteBay = A.bay;
                }
                
            }
        }

        if (players.TryGetValue(b, out var B))
        {
            if (B.bay != null)
            {
                if (B.bay.isLocalPlayer == true)
                {
                    LocalBay = B.bay;
                }
                else
                {
                    RemoteBay = B.bay;
                }
            }
        }

        if(roomModel.ConnectionId == LocalBay.Guid)
        {
            LocalBay.Hit(RemoteBay);
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
        HPBer.gameObject.SetActive(false);

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
        PlayBGM(2);
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
        PlayBGM(0);     
    }

    void RefreshPlayerNameList()
    {
        if (players == null) return;

        foreach (var pair in players)
        {
            var id = pair.Key;
            var user = pair.Value;

            if (playerNameItems.ContainsKey(id)) continue;

            GameObject item = Instantiate(playerNamePrefab, contentTransform);

            Text text = item.GetComponent<Text>();
            if (text == null)
            {
                Debug.LogError("playerNamePrefab に Text コンポーネントがありません");
                Destroy(item);
                continue;
            }

            text.text = user.userName;

            // ローカルプレイヤーだけ色分け
            text.color = user.ConnectionId == roomModel.ConnectionId? Color.yellow : Color.white;

            playerNameItems[id] = item;
        }
    }

    public void Exit()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        Initiate.Fade("TitleScene", Color.black, 2.0f);
        ReturnSE();
    }

    public void CreateUser()
    {
        CreateUserPanel.SetActive(true);
        AdvanceSE();
    }

    public void CreatedUser()
    {
        CreateUserPanel.SetActive(false);
        CreatedUserPanel.SetActive(false);
        AdvanceSE();
    }

    public void checksolo()
    {
        if(players.Count < 2)
        {
            ReadyButton.SetActive(false);
        }else
        {
            ReadyButton.SetActive(true) ;
        }
    }


    public void PlayHitSE()
    {
        if (audioSource == null || hitSE == null) return;
        audioSource.PlayOneShot(hitSE);
    }
    public void AdvanceSE()
    {
        if (audioSource == null || hitSE == null) return;
        audioSource.PlayOneShot(advanceSE);
    }
    public void ReturnSE()
    {
        if (audioSource == null || hitSE == null) return;
        audioSource.PlayOneShot(returnSE);
    }

    // =========================
    // BGM 再生
    // =========================
    public void PlayBGM(int index)
    {
        if (index < 0 || index >= bgmClips.Length) return;
        if (currentIndex == index) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeAndPlay(index));
    }

    // =========================
    // BGM 停止
    // =========================
    public void StopBGM()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOut());
        currentIndex = -1;
    }

    // =========================
    // フェード付き再生
    // =========================
    IEnumerator FadeAndPlay(int index)
    {
        // フェードアウト
        yield return FadeOut();

        audioSource.clip = bgmClips[index];
        audioSource.volume = 0f;
        audioSource.loop = true;
        audioSource.Play();

        currentIndex = index;

        // フェードイン
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeTime);
            yield return null;
        }

        audioSource.volume = 1f;
    }

    IEnumerator FadeOut()
    {
        if (!audioSource.isPlaying)
            yield break;

        float startVol = audioSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = 1f;
    }

    IEnumerator ShowGo()
    {
        readyText.gameObject.SetActive(false);
        goText.gameObject.SetActive(true);
        yield return new WaitForSeconds(showTimeGoText);
        goText.gameObject.SetActive(false);
    }
}


