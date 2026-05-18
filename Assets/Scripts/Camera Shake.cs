using UnityEngine;
using System;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;


    private Coroutine shakeCoroutine;
    // The camera listens to this event to receive the offset
    public static event Action<Vector3> OnShakeOffset;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }






    public void TriggerShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }


    public void CancelShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // Force the camera back to zero offset immediately
        OnShakeOffset?.Invoke(Vector3.zero);
    }




    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentMag = Mathf.Lerp(magnitude, 0f, elapsed / duration);

            // Send the offset directly to the camera script
            OnShakeOffset?.Invoke(UnityEngine.Random.insideUnitSphere * currentMag);
            yield return null;
        }

        // Return a final zero offset to reset the camera position perfectly
        OnShakeOffset?.Invoke(Vector3.zero);
        shakeCoroutine = null; // Clear reference when finished naturally
    }
}
