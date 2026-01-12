using UnityEngine;
using System.Linq;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource soundEffectSource;

    [Header("Ambient Music Options")]
    [SerializeField] private AudioClip[] ambientMusicOptions;

    [Header("Sound Effect Options")]
    [SerializeField] private AudioClip[] soundEffectOptions;

    private void Awake()
    {
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
        }

        if (soundEffectSource == null)
        {
            soundEffectSource = gameObject.AddComponent<AudioSource>();
            soundEffectSource.loop = false;
            soundEffectSource.playOnAwake = false;
            soundEffectSource.spatialBlend = 0f;
        }
    }

    // Play random ambient music from the available options
    public void PlayRandomAmbientMusic()
    {
        if (ambientMusicOptions == null || ambientMusicOptions.Length == 0)
            return;

        int index = Random.Range(0, ambientMusicOptions.Length);
        ambientSource.clip = ambientMusicOptions[index];
        ambientSource.volume = 0.1f;
        ambientSource.Play();
    }

    // Play a specific sound effect by name
    public void PlaySoundEffect(string clipName)
    {
        if (string.IsNullOrEmpty(clipName) || soundEffectSource == null)
            return;

        AudioClip clip = soundEffectOptions.FirstOrDefault(c => c != null && c.name == clipName);
        soundEffectSource.PlayOneShot(clip, 0.5f);
    }

    public void SetAmbientVolume(float volume)
    {
        if (ambientSource != null)
            ambientSource.volume = volume;
    }
}
