using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    [Header("ベイプレハブ（複数登録可）")]
    public GameObject[] bayPrefabs;

    [Header("生成設定")]
    public int spawnCount = 4;
    public float spawnRadius = 3f;
    public Transform stageCenter;

    [Header("スタジアム範囲")]
    public float stadiumMinY = -2f;

    private List<PlayerTop> aliveBays = new List<PlayerTop>();

    private bool gameStarted = false;   // ← ★追加：ゲーム開始フラグ
    private bool gameEnded = false;

    void OnDestroy()
    {
        //PlayerTop.OnAnyBayDead -= OnBayDead;
    }

    void Update()
    {
        if (!gameStarted || gameEnded) return;

        CheckBayOutOfStage();
        CheckWinner();
    }

    // =================================================
    // ★ 外部(UIボタン)から呼ぶゲーム開始関数
    // =================================================
    public void StartGame()
    {
        if (gameStarted) return;

        Debug.Log("=== ゲーム開始 ===");

        gameStarted = true;
        gameEnded = false;

        aliveBays.Clear();

        // 死亡イベント登録
        //PlayerTop.OnAnyBayDead += OnBayDead;

        SpawnBays();     // ← ベイを生成
    }

    // =================================================
    // ベイ生成
    // =================================================
     public void SpawnBays()
    {
        aliveBays.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = bayPrefabs[Random.Range(0, bayPrefabs.Length)];

            Vector2 pos2D = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = stageCenter.position + new Vector3(pos2D.x, 0, pos2D.y);

            GameObject bayObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            PlayerTop bay = bayObj.GetComponent<PlayerTop>();
            aliveBays.Add(bay);
        }
    }

    // =================================================
    // 落下チェック
    // =================================================
    void CheckBayOutOfStage()
    {
        for (int i = aliveBays.Count - 1; i >= 0; i--)
        {
            PlayerTop bay = aliveBays[i];
            if (bay == null) continue;

            if (bay.transform.position.y < stadiumMinY)
            {
                //bay.Die();
                Destroy(bay.gameObject, 0.2f);
            }
        }
    }

    // =================================================
    // 死亡イベント
    // =================================================
    void OnBayDead(PlayerTop deadBay)
    {
        aliveBays.Remove(deadBay);
        Debug.Log(deadBay.name + " を生存リストから削除");
    }

    // =================================================
    // 勝者判定
    // =================================================
    void CheckWinner()
    {
        if (aliveBays.Count == 1)
        {
            PlayerTop winner = aliveBays[0];
            Debug.Log("勝者は: " + winner.name);
            OnGameEnd(winner);
        }
        else if (aliveBays.Count == 0)
        {
            Debug.Log("全滅！勝者なし");
            OnGameEnd(null);
        }
    }

    // =================================================
    // ゲーム終了
    // =================================================
    void OnGameEnd(PlayerTop winner)
    {
        gameEnded = true;

        if (winner == null)
            Debug.Log("=== GAME END ===\nWinner: なし（全滅）");
        else
            Debug.Log($"=== GAME END ===\nWinner: {winner.name}");
    }
}
