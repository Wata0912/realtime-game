using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject topPrefab;              // ← ベイのPrefabをInspectorから指定
    public Transform[] spawnPoints;           // ← スポーン位置

    [Header("Match Settings")]
    public float respawnDelay = 2f;

    private List<PlayerTop> activeTops = new List<PlayerTop>();

    void Start()
    {
        SpawnAll();
    }

    // ------------------------------------------------------
    // すべてのベイをPrefabから生成
    // ------------------------------------------------------
    public void SpawnAll()
    {
        ClearExisting();

        foreach (Transform sp in spawnPoints)
        {
            GameObject t = Instantiate(topPrefab, sp.position, sp.rotation);
            PlayerTop top = t.GetComponent<PlayerTop>();

            if (top != null)
            {
                activeTops.Add(top);
            }
            else
            {
                Debug.LogError("Top Prefab に PlayerTop.cs が付いていません！");
            }
        }
    }

    // ------------------------------------------------------
    // 生成済みベイの破棄（ラウンドリセット）
    // ------------------------------------------------------
    void ClearExisting()
    {
        foreach (var top in activeTops)
        {
            if (top != null)
                Destroy(top.gameObject);
        }
        activeTops.Clear();
    }

    // ------------------------------------------------------
    // KnockOutチェック + 勝敗判定
    // ------------------------------------------------------
    void Update()
    {
        for (int i = activeTops.Count - 1; i >= 0; i--)
        {
            var top = activeTops[i];
            if (top == null)
            {
                activeTops.RemoveAt(i);
                continue;
            }

            // 落下判定（Y が -5 など）
            if (top.transform.position.y < -1f || top.IsKnockedOut())
            {
                Destroy(top.gameObject);
                activeTops.RemoveAt(i);
            }
        }

        // 勝者が1体になったらリセット
        if (activeTops.Count <= 1)
        {
            Invoke(nameof(SpawnAll), respawnDelay);
        }
    }
}
