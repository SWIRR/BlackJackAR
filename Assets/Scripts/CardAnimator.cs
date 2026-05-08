using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardAnimator : MonoBehaviour
{
    [Header("Animacja")]
    public float moveSpeed = 7f;
    public float flightDuration = 0.3f;
    public float elevationOnStart = 0.12f; // uniesienie nad talią przy starcie lotu

    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    public void StopAll()
    {
        foreach (var kvp in activeCoroutines)
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        activeCoroutines.Clear();
    }

    public void AnimateTo(GameObject obj, Vector3 targetPos, Quaternion targetRot)
    {
        if (obj == null) return;

        if (activeCoroutines.TryGetValue(obj, out var existing) && existing != null)
            StopCoroutine(existing);

        Coroutine c = StartCoroutine(AnimateObject(obj, targetPos, targetRot));
        activeCoroutines[obj] = c;
    }

    private IEnumerator AnimateObject(GameObject obj, Vector3 targetPos, Quaternion targetRot)
    {
        if (obj == null) yield break;

        Transform tr = obj.transform;

        Vector3 startPos = tr.position;
        Quaternion startRot = tr.rotation;

        float duration = 0.25f;
        float elapsed = 0f;

        // Delikatne uniesienie podczas przesuwania
        float arcHeight = 0.015f;

        // Lekki tilt w trakcie ruchu
        Quaternion tiltRot = startRot * Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f));

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth easing
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Podstawowy ruch
            Vector3 pos = Vector3.Lerp(startPos, targetPos, smoothT);

            // Mały łuk w górę
            pos += Vector3.up * Mathf.Sin(smoothT * Mathf.PI) * arcHeight;

            tr.position = pos;

            // Najpierw lekki tilt, potem powrót
            Quaternion currentRot = Quaternion.Slerp(
                tiltRot,
                targetRot,
                smoothT);

            tr.rotation = currentRot;

            yield return null;
        }

        tr.position = targetPos;
        tr.rotation = targetRot;

        activeCoroutines.Remove(obj);
    }
}