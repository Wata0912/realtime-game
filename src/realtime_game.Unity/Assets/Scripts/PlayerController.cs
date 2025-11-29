using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float moveSpeed = 5f;
    public static bool Tojoin = false; // 入室中フラグ
    public RoomModel roomModel;          // RoomModel を Inspector でアタッチ

    private Vector3 lastSentPos;
    private float sendInterval = 0.05f; // 座標送信間隔
    private float timer = 0f;

    void Update()
    {
        if (!Tojoin || roomModel == null) return;

        // 入力を取得
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 移動情報送信
        timer += Time.deltaTime;
        if ((transform.position - lastSentPos).sqrMagnitude > 0.001f && timer >= sendInterval)
        {
            lastSentPos = transform.position;
            timer = 0f;
            roomModel.MoveAsync(transform.position,transform.rotation).Forget();
        }
    }
}


    
