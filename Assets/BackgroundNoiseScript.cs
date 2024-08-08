using UnityEngine;

public class SoundEffectPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.05f;
        audioSource.Play();
    }
}