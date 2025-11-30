using UnityEngine;

public class PlayerTop : MonoBehaviour
{
    [Header("回転速度")]
    public float spinSpeed = 720f; // 1秒間で何度回るか

    [Header("回転減衰（0 = 減衰なし）")]
    public float spinDamping = 0f;

    private float currentSpinSpeed;

    void Start()
    {
        currentSpinSpeed = spinSpeed;
    }

    void Update()
    {
        // Y軸回転（ベイブレードの回転軸）
        transform.Rotate(0, currentSpinSpeed * Time.deltaTime, 0, Space.Self);

        // 回転の減衰
        if (spinDamping > 0f)
        {
            currentSpinSpeed = Mathf.Max(0, currentSpinSpeed - spinDamping * Time.deltaTime);
        }
    }

    /// <summary>
    /// 回転速度を変更したい時に呼ぶ
    /// </summary>
    public void SetSpinSpeed(float speed)
    {
        currentSpinSpeed = speed;
    }
}
