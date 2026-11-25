using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CanonShoot : MonoBehaviour
{
    public Transform shootPos;
    private float shootForce = 10f;
    [SerializeField] private float bombProb = 0.2f; // Probability of bomb spawn
    [SerializeField] private AssetReferenceGameObject[] pcvrFruits;
    [SerializeField] private AssetReferenceGameObject[] questFruits;
    [SerializeField] private GameObject[] bombs;
    private readonly List<GameObject> loadedFruits = new List<GameObject>();
    private readonly List<AsyncOperationHandle<GameObject>> fruitLoadHandles = new List<AsyncOperationHandle<GameObject>>();

    private void Start()
    {
        StartCoroutine(LoadPlatformFruits());
    }

    private IEnumerator LoadPlatformFruits()
    {
        AssetReferenceGameObject[] fruitReferences = Application.platform == RuntimePlatform.Android
            ? questFruits
            : pcvrFruits;

        foreach (AssetReferenceGameObject fruitReference in fruitReferences)
        {
            if (fruitReference == null || !fruitReference.RuntimeKeyIsValid())
            {
                Debug.LogError("A platform fruit Addressable reference is missing or invalid.", this);
                continue;
            }

            AsyncOperationHandle<GameObject> handle = fruitReference.LoadAssetAsync<GameObject>();
            fruitLoadHandles.Add(handle);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedFruits.Add(handle.Result);
            }
            else
            {
                Debug.LogError($"Could not load fruit Addressable '{fruitReference.AssetGUID}'.", this);
            }
        }
    }


    public void Shoot()
    {
        GameObject projectile;

        // Random fruit or bomb
        float randomFloat = Random.Range(0f, 1f);

        //random rotation
        float randomX = Random.Range(0f, 360f);
        float randomY = Random.Range(0f, 360f);
        float randomZ = Random.Range(0f, 360f);
        Quaternion randomRotation = Quaternion.Euler(randomX, randomY, randomZ);

        if (randomFloat <= bombProb) // Shoot Random Bomb with probability
        {
            int randomBomb = Random.Range(0, bombs.Length);
            projectile = ProjectilePool.Get(bombs[randomBomb], shootPos.position, randomRotation);
        }
        else // Shoot Random Fruit
        {
            if (loadedFruits.Count == 0) return;

            int randomFruit = Random.Range(0, loadedFruits.Count);
            projectile = ProjectilePool.Get(loadedFruits[randomFruit], shootPos.position, randomRotation);
        }

        if (projectile == null) return;

        Rigidbody body = projectile.GetComponent<Rigidbody>();
        if (body == null)
        {
            Debug.LogError($"Projectile prefab {projectile.name} has no Rigidbody.", projectile);
            ProjectilePool.Return(projectile);
            return;
        }

        body.AddForce(shootPos.up * shootForce, ForceMode.Impulse);
    }

    private void OnDestroy()
    {
        foreach (AsyncOperationHandle<GameObject> handle in fruitLoadHandles)
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }
}
