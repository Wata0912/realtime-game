using UnityEngine;

public class TitleDirecter : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip BGM;
    [SerializeField] AudioClip advanceSE;

    bool isTransitioning = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isTransitioning = false;
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (isTransitioning) return;

        if (Input.GetMouseButtonDown(0))
        {
            isTransitioning = true;
            Initiate.Fade("GameScene", Color.black, 2.0f);
            audioSource.PlayOneShot(advanceSE);
        }
    }
}
