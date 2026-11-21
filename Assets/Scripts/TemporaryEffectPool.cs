using System.Collections.Generic;
using UnityEngine;

/// <summary>Reuses short-lived particle effects instead of instantiating and destroying them.</summary>
public static class TemporaryEffectPool
{
    private sealed class Pool
    {
        public readonly Stack<GameObject> Inactive = new Stack<GameObject>();
        public Transform Root;
    }

    private static readonly Dictionary<GameObject, Pool> Pools = new Dictionary<GameObject, Pool>();
    private static Transform poolRoot;

    public static GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 2f)
    {
        if (prefab == null) return null;

        Pool pool = GetPool(prefab);
        GameObject effect = pool.Inactive.Count > 0 ? pool.Inactive.Pop() : Create(prefab, pool);
        effect.transform.SetParent(null);
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.GetComponent<PooledEffect>().Begin(lifetime);
        effect.SetActive(true);

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Clear(true);
            particles.Play(true);
        }

        return effect;
    }

    internal static void Return(PooledEffect pooledEffect)
    {
        GameObject effect = pooledEffect.gameObject;
        if (!effect.activeSelf) return;

        Pool pool = GetPool(pooledEffect.Prefab);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        effect.SetActive(false);
        effect.transform.SetParent(pool.Root);
        pool.Inactive.Push(effect);
    }

    private static Pool GetPool(GameObject prefab)
    {
        if (!Pools.TryGetValue(prefab, out Pool pool))
        {
            if (poolRoot == null)
            {
                GameObject root = new GameObject("Temporary Effect Pool");
                poolRoot = root.transform;
                Object.DontDestroyOnLoad(root);
            }

            GameObject rootObject = new GameObject(prefab.name + " Pool");
            rootObject.transform.SetParent(poolRoot);
            pool = new Pool { Root = rootObject.transform };
            Pools.Add(prefab, pool);
        }

        return pool;
    }

    private static GameObject Create(GameObject prefab, Pool pool)
    {
        GameObject effect = Object.Instantiate(prefab, pool.Root);
        effect.SetActive(false);
        PooledEffect pooledEffect = effect.GetComponent<PooledEffect>();
        if (pooledEffect == null) pooledEffect = effect.AddComponent<PooledEffect>();
        pooledEffect.Initialize(prefab);
        return effect;
    }
}

public sealed class PooledEffect : MonoBehaviour
{
    public GameObject Prefab { get; private set; }
    private float remainingLifetime;

    internal void Initialize(GameObject prefab)
    {
        Prefab = prefab;
    }

    internal void Begin(float lifetime)
    {
        remainingLifetime = Mathf.Max(0.01f, lifetime);
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f) TemporaryEffectPool.Return(this);
    }
}
