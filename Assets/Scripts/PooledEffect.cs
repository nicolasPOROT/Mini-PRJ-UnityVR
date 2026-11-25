using UnityEngine;

public sealed class PooledEffect : MonoBehaviour
{
    public GameObject Prefab { get; private set; }
    private float remainingLifetime;

    internal void Initialize(GameObject prefab) => Prefab = prefab;
    internal void Begin(float lifetime) => remainingLifetime = Mathf.Max(0.01f, lifetime);

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f) TemporaryEffectPool.Return(this);
    }
}
