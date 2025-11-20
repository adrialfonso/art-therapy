using UnityEngine;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip ambientMusic;
    [SerializeField] private AudioClip drawSound;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource drawSource;

    [Header("Brush Controller Reference")]
    [SerializeField] private BrushController brushController;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float ambientVolume = 0.5f;
    [Range(0f, 1f)] public float drawVolume = 0.8f;

    private void Awake()
    {
        // Ambient music setup
        if (ambientSource == null)
            ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = ambientMusic;
        ambientSource.loop = true;
        ambientSource.volume = ambientVolume;
        ambientSource.playOnAwake = false;
        ambientSource.Play();

        // Draw sound setup
        if (drawSource == null)
            drawSource = gameObject.AddComponent<AudioSource>();
        drawSource.clip = drawSound;
        drawSource.loop = true; 
        drawSource.volume = drawVolume;
        drawSource.playOnAwake = false;
    }

    private void Update()
    {
        if (brushController == null) return;

        // Detect if any controller is drawing
        bool isDrawing = (brushController.leftState != null && brushController.leftState.isDrawing) ||
                         (brushController.rightState != null && brushController.rightState.isDrawing);

        if (isDrawing)
        {
            if (!drawSource.isPlaying)
                drawSource.Play();
        }
        else
        {
            if (drawSource.isPlaying)
                drawSource.Stop();
        }
    }
}
