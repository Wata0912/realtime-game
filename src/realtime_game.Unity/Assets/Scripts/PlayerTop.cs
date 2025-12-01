using UnityEngine;

public class PlayerTop : MonoBehaviour
{
    [Header("回転設定")]
    public float spinSpeed = 900f;   // 初期回転速度
    public float spinDamping = 0f;   // 回転減衰（0なら減衰なし）

    private float currentSpinSpeed;
    private Rigidbody rb;

    [Header("慣性ベース移動")]
    public Transform stageCenter;   // ステージ中心
    public float centerForce = 20f; // 中心へ戻す最大補正力（小さめ）
    public float springFactor = 2f; // 距離に比例する強さ（バネの硬さ）

    [Header("操作")]
    public float controlForce = 15f; // 慣性方向を少しだけ変える
    public float maxSpeed = 20f;    // 最大速度

    [Header("衝突反応")]
    public float knockbackForce = 15f; // ベイを弾く力

    // ===============================
    // ベイの体力システム
    // ===============================
    [Header("体力")]
    public float maxHP = 1000f;
    public float currentHP;
    public float passiveDrain = 1f;        // 自然減少（1秒あたり）
    public float collisionDamage = 4f;    // 衝突ダメージ

    [Header("性能低下カーブ")]
    public float minSpinMultiplier = 0.2f;     // HP0%時の回転力倍率
    public float minSpeedMultiplier = 0.3f;    // HP0%時の移動速度倍率
    public float minControlMultiplier = 0.3f;  // HP0%時の操作力倍率

    private bool isDead = false;

    // ■ 基礎性能保持用（累積しないため）
    private float baseSpinSpeed;
    private float baseMaxSpeed;
    private float baseControlForce;


    void Start()
    {
        currentSpinSpeed = spinSpeed;
        rb = GetComponent<Rigidbody>();
        currentSpinSpeed = spinSpeed;
        rb = GetComponent<Rigidbody>();

        // HP 初期化
        currentHP = maxHP;

        // 基礎性能を保存（弱体化で累積しないため）
        baseSpinSpeed = spinSpeed;
        baseMaxSpeed = maxSpeed;
        baseControlForce = controlForce;
    }
    void Awake()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        if (isDead) return;

        // ---------------------------
        // 自転（ベイブレードの回転）
        // ---------------------------
        transform.Rotate(0, currentSpinSpeed * Time.deltaTime, 0, Space.Self);

        // 回転減衰
        if (spinDamping > 0f)
        {
            currentSpinSpeed = Mathf.Max(0, currentSpinSpeed - spinDamping * Time.deltaTime);
        }

        // --- 常時体力減少 ---
        currentHP -= passiveDrain * Time.deltaTime;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // --- HP による性能低下反映 ---
        ApplyStatusByHP();

        // --- 死亡判定 ---
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    void FixedUpdate()
    {
        ApplySpringCentering();
        ApplyControl();
        LimitSpeed();
    }


    // ============================================
    // ベイブレードらしい「弱いバネで中心へ戻る」
    // ============================================
    void ApplySpringCentering()
    {
        if (stageCenter == null) return;

        // 中心との距離ベクトル
        Vector3 toCenter = stageCenter.position - transform.position;
        float distance = toCenter.magnitude;

        // 距離に応じて力が強くなる（バネ）
        float force = Mathf.Clamp(distance * springFactor, 0f, centerForce);

        Vector3 dir = toCenter.normalized;

        // バネ力を加える
        rb.AddForce(dir * force, ForceMode.Acceleration);
    }


    // ============================================
    // プレイヤーによる慣性の微調整
    // ============================================
    void ApplyControl()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(h, 0, v);

        if (inputDir.magnitude > 0.1f)
        {
            // 強すぎない慣性補正
            rb.AddForce(inputDir.normalized * controlForce, ForceMode.Acceleration);
        }
    }


    // ============================================
    // 衝突時にベイを弾く処理
    // ============================================
    void OnCollisionEnter(Collision col)
    {

        if (isDead) return;

        if (col.gameObject.CompareTag("Bay"))
        {
            Rigidbody otherRb = col.rigidbody;
            if (otherRb == null) return;

            Vector3 dir = (col.transform.position - transform.position).normalized;

            // 相手を弾く
            otherRb.AddForce(dir * knockbackForce, ForceMode.Impulse);

            // 自分にも反動
            rb.AddForce(-dir * (knockbackForce * 0.5f), ForceMode.Impulse);

            // ダメージ
            currentHP -= collisionDamage;
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        }

    }


    // ============================================
    // 最大速度の制限
    // ============================================
    void LimitSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    // ============================================
    // 回転速度を変更する外部呼び出し用
    // ============================================
    public void SetSpinSpeed(float speed)
    {
        currentSpinSpeed = speed;
    }

    // ===============================
    // HPによる性能低下
    // ===============================
    void ApplyStatusByHP()
    {
        float hpRate = currentHP / maxHP; // 0〜1

        // 元の性能 × HP補正（累積しない）
        currentSpinSpeed = baseSpinSpeed * Mathf.Lerp(minSpinMultiplier, 1f, hpRate);
        maxSpeed = baseMaxSpeed * Mathf.Lerp(minSpeedMultiplier, 1f, hpRate);
        controlForce = baseControlForce * Mathf.Lerp(minControlMultiplier, 1f, hpRate);
    }

    

    // ===============================
    // HPが尽きたら停止
    // ===============================
    void Die()
    {
        isDead = true;

        // 完全停止
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 回転も停止
        currentSpinSpeed = 0;

        // 操作不能
        controlForce = 0;
        maxSpeed = 0;

        Debug.Log("ベイが停止しました");
    }
}
