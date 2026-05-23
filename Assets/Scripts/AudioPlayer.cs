using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ObjectAudioManager : MonoBehaviour
{
    // Global Singleton Instance
    public static ObjectAudioManager Instance { get; private set; }

    private AudioSource audioSource;

    [Header("Audio Settings")]
    public float smoothSpeed = 5f;

    private Coroutine fadeCoroutine;
    private Coroutine timedLoopCoroutine;

    void Awake()
    {
        // Enforce the Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Keeps audio running cleanly between scene loads
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EnsureAudioListenerExists();
    }

    // === ONE-SHOT CODE === 
    // Use this for instant, overlapping sounds (like jumping, landing, or clicking buttons)
    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    // === TIMED LOOPING CODE === 
    // Use this for continuous sounds (like your thruster engine)
    public void PlayLoopTimed(AudioClip clip, float durationInSeconds, bool fadeOutAtEnd = true, float pitch = 1f)
    {
        if (clip == null) return;

        ResetActiveCoroutines(stopTimedLoop: true);
        timedLoopCoroutine = StartCoroutine(TimedLoopSequence(clip, durationInSeconds, fadeOutAtEnd, pitch));
    }

    // === STOP AUDIO CODE === 
    public void StopAudio(bool fade = false)
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        ResetActiveCoroutines(stopTimedLoop: true);

        if (fade)
        {
            fadeCoroutine = StartCoroutine(FadeOutAndStop());
        }
        else
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    // === INTERNAL CORE LOGIC === 
    private IEnumerator TimedLoopSequence(AudioClip clip, float duration, bool fadeOut, float pitch)
    {
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.pitch = pitch;
        audioSource.volume = 1f;
        audioSource.Play();

        yield return new WaitForSeconds(duration);

        if (fadeOut)
        {
            fadeCoroutine = StartCoroutine(FadeOutAndStop());
        }
        else
        {
            audioSource.Stop();
        }

        timedLoopCoroutine = null;
    }

    private IEnumerator FadeOutAndStop()
    {
        while (audioSource.volume > 0.01f)
        {
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0f, Time.deltaTime * smoothSpeed);
            yield return null;
        }

        audioSource.Stop();
        fadeCoroutine = null;
    }

    private void ResetActiveCoroutines(bool stopTimedLoop)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (stopTimedLoop && timedLoopCoroutine != null)
        {
            StopCoroutine(timedLoopCoroutine);
            timedLoopCoroutine = null;
        }
    }

    private void EnsureAudioListenerExists()
    {
        // Character cameras already own AudioListeners — only add one if nothing exists
        if (Object.FindAnyObjectByType<AudioListener>() != null) return;
        StartCoroutine(DeferredListenerCheck());
    }

    private IEnumerator DeferredListenerCheck()
    {
        // Wait for the end of the initialization frame so characters can spawn their cameras
        yield return new WaitForEndOfFrame();

        // Re-check for any listener created by the character initialize blocks
        AudioListener listener = Object.FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            // Try searching for any camera built dynamically during initialization
            Camera activeCam = Object.FindAnyObjectByType<Camera>();
            if (activeCam != null)
            {
                activeCam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[Audio Manager] Dynamic frame loaded. Attached Listener to: " + activeCam.gameObject.name);
            }
            else
            {
                // Absolute safety fallback
                GameObject listenerObj = new GameObject("GlobalAudioListener");
                listenerObj.AddComponent<AudioListener>();
                Debug.Log("[Audio Manager] Fallback state active. Created a standalone Global Audio Listener.");
            }
        }
    }

}
