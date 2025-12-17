using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerTop : MonoBehaviour
{
    public Transform stageCenter;

    // =========================
    // 識別
    // =========================
    public int userId;
    public bool isLocalPlayer;

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

    // =========================
    // 衝突
    // =========================
    public float pushForce = 8f;
    public float collisionCooldown = 0.2f;
    private float collisionTimer;

    // =========================
    // 同期
    // =========================
    public float sendInterval = 0.03f;
    private float sendTimer;

    public float lerpSpeed = 12f;
    private Vector3 targetPos;
    private Quaternion targetRot;
    private int lastSeq = -1;

    
    Rigidbody rb;


    
 

    // =========================
    //初期化
    // =========================
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (isLocalPlayer)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        else
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // =========================
    // 物理処理（ローカルのみ）
    // =========================
    void FixedUpdate()
    {
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
        else
        {
            rb.MovePosition(
                Vector3.Lerp(rb.position, targetPos, Time.fixedDeltaTime * lerpSpeed)
            );

            rb.MoveRotation(
              Quaternion.RotateTowards(
                 rb.rotation,
                targetRot,
                720f * Time.fixedDeltaTime));
        }
    }


    // =========================
    // 毎フレーム
    // =========================
    void Update()
    {
        if (collisionTimer > 0f)
            collisionTimer -= Time.deltaTime;

        if (isLocalPlayer)
        {
            SendSync();
        }
    }


    // =========================
    // 同期送信
    // =========================
    void SendSync()
    {
        sendTimer += Time.deltaTime;
        if (sendTimer < sendInterval) return;
        sendTimer = 0f;

        Quaternion safeRot = Quaternion.Normalize(rb.rotation);
        roomModel.MoveAsync(rb.position, safeRot, seq++).Forget();
    }

    // =========================
    // サーバーから受信
    // =========================
    public void SetRemoteState(Vector3 pos, Quaternion rot, int seq)
    {
        if (seq <= lastSeq) return;
        lastSeq = seq;

        targetPos = pos;
        targetRot = Quaternion.Normalize(rot);
    }

    // =========================
    // 衝突処理（ローカルのみ）
    // =========================
    void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer) return;
        if (collisionTimer > 0f) return;
        if (!collision.gameObject.CompareTag("Bay")) return;

        PlayerTop other = collision.gameObject.GetComponent<PlayerTop>();
        if (other == null) return;

        collisionTimer = collisionCooldown;

        Vector3 dir = transform.position - collision.transform.position;
        dir.y = 0f;
        dir.Normalize();

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ★相手の pushForce を受け取る
        rb.AddForce(dir * other.pushForce, ForceMode.Impulse);
    }
    public void Initialize(int userId, bool isLocal)
    {
        this.userId = userId;
        this.isLocalPlayer = isLocal;
       
    }
}
