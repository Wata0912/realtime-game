using UnityEngine;

public class TitleBay : MonoBehaviour
{
    [Header("‰ñ“]Ý’è")]
    [SerializeField] float baseSpeed = 180f;     // ’Êí‰ñ“]‘¬“xi“x/•bj
    [SerializeField] float boostSpeed = 360f;    // ‰Á‘¬Žž‚Ì‘¬“x
    [SerializeField] float boostDuration = 0.4f; // ‰Á‘¬ŽžŠÔ
    [SerializeField] float boostInterval = 5f;   // ‰Á‘¬ŠÔŠu

    float timer;
    float currentSpeed;

    void Start()
    {
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        // ‰ñ“]
        transform.Rotate(Vector3.up * currentSpeed * Time.deltaTime, Space.World);

        // ƒ^ƒCƒ}[XV
        timer += Time.deltaTime;

        // ˆê’èŠÔŠu‚Å‰Á‘¬
        if (timer >= boostInterval)
        {
            timer = 0f;
            StopAllCoroutines();
            StartCoroutine(Boost());
        }
    }

    System.Collections.IEnumerator Boost()
    {
        currentSpeed = boostSpeed;
        yield return new WaitForSeconds(boostDuration);
        currentSpeed = baseSpeed;
    }

}
