using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ObjectAudioManager : MonoBehaviour
{
    private AudioSource audioSource;
    public float smoothSpeed = 5f;
    private Coroutine fadeCoroutine;
    private Coroutine timedLoopCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EnsureAudioListenerExists();
    }

    // === THE RESTORED ONE-SHOT CODE ===
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
        AudioListener listener = Object.FindAnyObjectByType<AudioListener>();

        if (listener == null)
        {
            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
                Debug.LogWarning("[Audio Manager] No Audio Listener found! Automatically added one to the Main Camera.");
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
                Debug.LogError("[Audio Manager] No Audio Listener or Main Camera found! Added Audio Listener to this character as a fallback.");
            }
        }
    }
}
