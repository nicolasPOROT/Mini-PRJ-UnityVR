using UnityEngine;

public abstract class SliceableObject : MonoBehaviour
{
    [SerializeField] private GameObject pcvrSliceEffect;
    [SerializeField] private GameObject questSliceEffect;
    public AudioClip sliceAudioClip;

    public GameObject GetSliceEffect()
    {
        GameObject sliceEffect = Application.platform == RuntimePlatform.Android
            ? questSliceEffect
            : pcvrSliceEffect;

        if (sliceEffect == null)
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
            Destroy(gameObject);
        }
    }

    // Méthodes virtuelles à override
    protected virtual void OnMissed() { }
    public virtual void IsSliced() { }
}
