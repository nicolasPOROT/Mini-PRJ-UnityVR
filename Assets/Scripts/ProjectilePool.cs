using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared, expandable pool for fruit and bomb projectiles.</summary>
public static class ProjectilePool
{
    private sealed class Pool
    {
        public readonly Stack<GameObject> Inactive = new Stack<GameObject>();
        public Transform Root;
    }

    private static readonly Dictionary<GameObject, Pool> Pools = new Dictionary<GameObject, Pool>();
    private static Transform poolRoot;

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("A fruit or bomb prefab is missing from CanonShoot.");
            return null;
        }

        Pool pool = GetPool(prefab);
        GameObject instance = pool.Inactive.Count > 0
            ? pool.Inactive.Pop()
            : CreateInstance(prefab, pool);

        instance.transform.SetParent(null);
        instance.transform.SetPositionAndRotation(position, rotation);
        ResetPhysics(instance);
        instance.SetActive(true);
        return instance;
    }

    public static void Return(GameObject instance)
    {
        if (instance == null || !instance.TryGetComponent(out PooledProjectile pooled))
        {
            if (instance != null) DestroyObject(instance);
            return;
        }

        Pool pool = GetPool(pooled.Prefab);
        ResetPhysics(instance);
        instance.SetActive(false);
        instance.transform.SetParent(pool.Root);
        pool.Inactive.Push(instance);
    }

    private static Pool GetPool(GameObject prefab)
    {
        if (!Pools.TryGetValue(prefab, out Pool pool))
        {
            if (poolRoot == null)
            {
                GameObject root = new GameObject("Projectile Pool");
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

    private static GameObject CreateInstance(GameObject prefab, Pool pool)
    {
        GameObject instance = Object.Instantiate(prefab, pool.Root);
        instance.SetActive(false);
        PooledProjectile pooled = instance.GetComponent<PooledProjectile>();
        if (pooled == null) pooled = instance.AddComponent<PooledProjectile>();
        pooled.Prefab = prefab;
        return instance;
    }

    private static void ResetPhysics(GameObject instance)
    {
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }
    }

    private static void DestroyObject(GameObject instance)
    {
        if (Application.isPlaying) Object.Destroy(instance);
        else Object.DestroyImmediate(instance);
    }

}

public sealed class PooledProjectile : MonoBehaviour
{
    public GameObject Prefab;
}
