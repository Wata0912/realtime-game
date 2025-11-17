using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static bool Tojoin = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

   
    public float moveSpeed = 5f;

    void Update()
    {
        if(!Tojoin)
        {
            return;
        }
        // WASDキーの入力を取得
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        // 移動方向を計算
        Vector3 direction = new Vector3(horizontal, 0, vertical);

        // 移動を適用
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

}
