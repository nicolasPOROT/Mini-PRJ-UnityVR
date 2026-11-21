using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class SliceableObject : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject pcvrSliceEffect;
    [SerializeField] private AssetReferenceGameObject questSliceEffect;
    public AudioClip sliceAudioClip;

    public AssetReferenceGameObject GetSliceEffect()
    {
        AssetReferenceGameObject sliceEffect = Application.platform == RuntimePlatform.Android
            ? questSliceEffect
            : pcvrSliceEffect;

        if (sliceEffect == null || !sliceEffect.RuntimeKeyIsValid())
        {
            Debug.LogError($"No {(Application.platform == RuntimePlatform.Android ? "Quest" : "PCVR")} slice effect is configured on {name}.", this);
        }

        return sliceEffect;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("fruitKillZone"))
        {
            OnMissed();
            ProjectilePool.Return(gameObject);
        }
    }

    // Méthodes virtuelles à override
    protected virtual void OnMissed() { }
    public virtual void IsSliced() { }
}
