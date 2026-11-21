using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>Loads PCVR/Quest effects through Addressables and reuses their instances.</summary>
public static class TemporaryEffectPool
{
    private sealed class Pool
    {
        public readonly Stack<GameObject> Inactive = new Stack<GameObject>();
        public Transform Root;
    }

    private sealed class LoadEntry
    {
        public AsyncOperationHandle<GameObject> Handle;
        public bool Completed;
        public readonly List<PendingEffect> Pending = new List<PendingEffect>();
    }

    private struct PendingEffect
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Lifetime;
    }

    private static readonly Dictionary<GameObject, Pool> Pools = new Dictionary<GameObject, Pool>();
    private static readonly Dictionary<string, LoadEntry> Loads = new Dictionary<string, LoadEntry>();
    private static Transform poolRoot;

    public static void Play(AssetReferenceGameObject reference, Vector3 position, Quaternion rotation, float lifetime = 2f)
    {
        if (reference == null || !reference.RuntimeKeyIsValid())
        {
            Debug.LogError("A slice effect Addressable reference is missing or invalid.");
            return;
        }

        string key = reference.AssetGUID;
        PendingEffect request = new PendingEffect { Position = position, Rotation = rotation, Lifetime = lifetime };

        if (!Loads.TryGetValue(key, out LoadEntry entry))
        {
            entry = new LoadEntry();
            Loads.Add(key, entry);
            entry.Pending.Add(request);
            entry.Handle = Addressables.LoadAssetAsync<GameObject>(reference);
            entry.Handle.Completed += operation => CompleteLoad(key, operation);
            return;
        }

        if (!entry.Completed)
        {
            entry.Pending.Add(request);
            return;
        }

        if (entry.Handle.Status == AsyncOperationStatus.Succeeded)
            Spawn(entry.Handle.Result, request);
    }

    internal static void Return(PooledEffect pooledEffect)
    {
        GameObject effect = pooledEffect.gameObject;
        if (!effect.activeSelf) return;

        Pool pool = GetPool(pooledEffect.Prefab);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        effect.SetActive(false);
        effect.transform.SetParent(pool.Root);
        pool.Inactive.Push(effect);
    }

    internal static void ReleaseAddressables()
    {
        foreach (LoadEntry entry in Loads.Values)
        {
            if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
        }
        Loads.Clear();
    }

    private static void CompleteLoad(string key, AsyncOperationHandle<GameObject> operation)
    {
        if (!Loads.TryGetValue(key, out LoadEntry entry)) return;
        entry.Completed = true;

        if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
        {
            Debug.LogError($"Could not load slice effect Addressable '{key}': {operation.OperationException}");
            entry.Pending.Clear();
            return;
        }

        foreach (PendingEffect request in entry.Pending)
            Spawn(operation.Result, request);
        entry.Pending.Clear();
    }

    private static void Spawn(GameObject prefab, PendingEffect request)
    {
        Pool pool = GetPool(prefab);
        GameObject effect = pool.Inactive.Count > 0 ? pool.Inactive.Pop() : Create(prefab, pool);
        effect.transform.SetParent(null);
        effect.transform.SetPositionAndRotation(request.Position, request.Rotation);
        effect.GetComponent<PooledEffect>().Begin(request.Lifetime);
        effect.SetActive(true);

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Clear(true);
            particles.Play(true);
        }
    }

    private static Pool GetPool(GameObject prefab)
    {
        if (!Pools.TryGetValue(prefab, out Pool pool))
        {
            EnsurePoolRoot();
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

    private static void EnsurePoolRoot()
    {
        if (poolRoot != null) return;
        GameObject root = new GameObject("Temporary Effect Pool");
        poolRoot = root.transform;
        Object.DontDestroyOnLoad(root);
        root.AddComponent<AddressablePoolLifetime>();
    }
}
