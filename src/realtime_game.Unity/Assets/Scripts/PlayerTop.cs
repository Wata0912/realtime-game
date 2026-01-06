using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using DG.Tweening;
using System.Diagnostics;


[RequireComponent(typeof(Rigidbody))]
public class PlayerTop : MonoBehaviour
{
    public Transform stageCenter;

    // =========================
    // 識別
    // =========================
    public int userId;
    public bool isLocalPlayer;
    public Guid Guid;

    // =========================
    // 通信
    // =========================
    public RoomModel roomModel;
    private int seq = 0;


    // =========================
    // 移動設定
    // =========================
    public float moveForce = 8f;
    public float spinSpeed = 20f;
    public float centerForce = 5f;
    // 追加（クラス内）
    Tween moveTween;
    Tween rotTween;


    // =========================
    // 衝突
    // =========================
    public float pushForce = 8f;
    public float collisionCooldown = 0.2f;
    private float collisionTimer;
    public int maxHP = 40;
    public int currentHP = 40;
    public int currentDMG = 2;
    bool isKnockback;
    float knockbackTimer;
    [SerializeField] float knockbackLockTime = 0.15f;

    // =========================
    // 同期
    // =========================
    public float sendInterval = 0.03f;
    private float sendTimer;

    public float lerpSpeed = 12f;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private int lastSeq = -1;
    public bool isDead;

    Rigidbody rb;

    [Header("Cursor")]
    public GameObject cursorObject;
    [SerializeField] private Transform cursorTransform;
    [SerializeField] float stageRadius = 9f;

    // =========================
    //初期化
    // =========================
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetupCursor(); 

        if (isLocalPlayer)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        else
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // =========================
    // カーソル初期化
    // =========================
    void SetupCursor()
    {
        if (cursorObject == null) return;
        cursorObject.SetActive(isLocalPlayer && !isDead);
    }


    // =========================
    // 自身のベイ移動
   // =========================
    void FixedUpdate()
    {
        if (isDead) return;  

        if (isLocalPlayer)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 dir = new Vector3(h, 0, v);
            rb.AddForce(dir * moveForce, ForceMode.Acceleration);

            // ===== 中心へ向かう力 =====
            Vector3 toCenter = stageCenter.transform.position - rb.position;
            toCenter.y = 0f;

            rb.AddForce(toCenter.normalized * centerForce, ForceMode.Acceleration);

            rb.angularVelocity = Vector3.up * spinSpeed;
        }
       
    }

    // =========================
    // 毎フレーム
    // =========================
    void Update()
    {
        //死亡判定
        if (isDead) return;

        if (isKnockback)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockback = false;
            }
        }

        Vector3 pos = transform.position;
        pos.y = 0f;

        if (pos.magnitude > stageRadius)
        {
            Die();
            return;
        }

        if (collisionTimer > 0f)
            collisionTimer -= Time.deltaTime;

        if (isLocalPlayer)
        {
            SendSync();
        }
    }

    // =========================
    // カーソル
    // =========================
    void LateUpdate()
    {
        if (cursorTransform == null) return;

        // 親（ベイ）の回転を打ち消す
        cursorTransform.rotation = Quaternion.identity;
    }


    // =========================
    // 同期送信
    // =========================
    void SendSync()
    {
        if (isDead) return;
            if (isKnockback) return;

      
            sendTimer += Time.deltaTime;
            if (sendTimer < sendInterval) return;
            sendTimer = 0f;

            Quaternion safeRot = Quaternion.Normalize(rb.rotation);
            roomModel.MoveAsync(rb.position, safeRot, seq++).Forget();
        
       
    }

    // =========================
    // サーバーから他ベイの座標受信
    // =========================
    public void SetRemoteState(Vector3 pos, Quaternion rot, int seq)
    {
        if (seq <= lastSeq) return;
        lastSeq = seq;

        // ===== 位置 =====
        if (moveTween != null && moveTween.IsActive())
            moveTween.Kill();

        moveTween = transform.DOMove(pos, sendInterval)
            .SetEase(Ease.OutQuad);

        // ===== 回転 =====
        if (rot.x == 0 && rot.y == 0 && rot.z == 0 && rot.w == 0)
            return;

        rot = Quaternion.Normalize(rot);

        if (rotTween != null && rotTween.IsActive())
            rotTween.Kill();

        rotTween = transform.DORotateQuaternion(rot, sendInterval)
            .SetEase(Ease.OutQuad);
    }


    // =========================
    // 衝突処理
    // =========================
  
    void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer) return;
        if (!collision.gameObject.CompareTag("Bay")) return;

        PlayerTop other = collision.gameObject.GetComponent<PlayerTop>();
        if (other == null) return;

        roomModel.ReportCollision(Guid, other.Guid).Forget();
    }

    public void Hit(PlayerTop enemy)
    {
        if (!isLocalPlayer) return;
        if (enemy == null) return;

        SetupCursor();

        Vector3 dir = transform.position - enemy.transform.position;
        dir.y = 0f;
        dir.Normalize();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(dir * enemy.pushForce, ForceMode.Impulse);

        UnityEngine.Debug.Log($"{enemy.pushForce}: {enemy.Guid}");

        ForceSendSync();

        ApplyDamage(enemy.currentDMG);
 
    }

    // =========================
    // ダメージ処理
    // =========================
    void ApplyDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // =========================
    // 死亡処理
    // =========================
    public void Die()
    {
        
        if (isDead) return;
        isDead = true;

        currentHP = 0;

        if (moveTween != null) moveTween.Kill();
        if (rotTween != null) rotTween.Kill();

        // 自分のベイの死亡通知
        if (isLocalPlayer && roomModel != null)
        {
            roomModel.DeadAsync().Forget();
        }

        
        Destroy(gameObject, 0.1f);
    }

    public void ApplyRemoteDead()
    {
        
        if (isDead) return;
        isDead = true;

        if (moveTween != null) moveTween.Kill();
        if (rotTween != null) rotTween.Kill();
      
        //this.gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
        
    }


   

    // =========================
    //生成時代入
    // =========================
    public void Initialize(Guid id, int userId, bool isLocal)
    {
        this.userId = userId;
        this.isLocalPlayer = isLocal;
        this.Guid = id;

        SetupCursor();
    }

    void ForceSendSync()
    {
        sendTimer = 0f;
        Quaternion safeRot = Quaternion.Normalize(rb.rotation);
        roomModel.MoveAsync(rb.position, safeRot, seq++).Forget();
    }


}
