using System.Collections;
using UnityEngine;

public sealed class LootDropMotion : MonoBehaviour
{
    [SerializeField] private LootPickup lootPickup;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float arcHeight = 1.25f;

    private Coroutine motionCoroutine;

    private void Awake()
    {
        lootPickup ??= GetComponent<LootPickup>();
    }

    public void Play(Vector3 start, Vector3 end)
    {
        if (motionCoroutine != null)
            StopCoroutine(motionCoroutine);

        transform.position = start;
        lootPickup?.SetLootable(false);

        if (duration <= 0f)
        {
            transform.position = end;
            lootPickup?.SetLootable(true);
            motionCoroutine = null;
            return;
        }

        motionCoroutine = StartCoroutine(PlayRoutine(start, end));
    }

    private IEnumerator PlayRoutine(Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = position;

            yield return null;
        }

        transform.position = end;
        lootPickup?.SetLootable(true);
        motionCoroutine = null;
    }
}
