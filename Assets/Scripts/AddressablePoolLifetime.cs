using UnityEngine;

public sealed class AddressablePoolLifetime : MonoBehaviour
{
    private void OnApplicationQuit() => TemporaryEffectPool.ReleaseAddressables();
}
