using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class ConfigureSliceEffectAddressables
{
    private const string GroupName = "Slice Effects PCVR and Quest";
    private const string PcvrJuiceGuid = "3feb62d2399935142a62ddd260bb13c1";
    private const string QuestJuiceGuid = "4194f05acd5106640aaaeafae837d5bf";
    private const string PcvrExplosionGuid = "9a8b5d54e8752f9469607e8c0701d666";
    private const string QuestExplosionGuid = "2327d7d6fc774cd4db2409c85ea85ef4";

    [MenuItem("Tools/Addressables/Configure Slice Effects")]
    public static void Configure()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null) throw new InvalidOperationException("Could not create Addressables settings.");
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                GroupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        AddAsset(settings, group, PcvrJuiceGuid, "SliceEffect_Juice_PC");
        AddAsset(settings, group, QuestJuiceGuid, "SliceEffect_Juice_Quest");
        AddAsset(settings, group, PcvrExplosionGuid, "SliceEffect_Explosion_PC");
        AddAsset(settings, group, QuestExplosionGuid, "SliceEffect_Explosion_Quest");

        SetPrefabReferences("Assets/Prefabs/Fruits", PcvrJuiceGuid, QuestJuiceGuid);
        SetPrefabReferences("Assets/Prefabs/Bombs", PcvrExplosionGuid, QuestExplosionGuid);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured four PCVR/Quest slice effects as Addressables and assigned prefab references.");
    }

    private static void AddAsset(AddressableAssetSettings settings, AddressableAssetGroup group, string guid, string address)
    {
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
        entry.address = address;
    }

    private static void SetPrefabReferences(string folder, string pcvrGuid, string questGuid)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SliceableObject sliceable = prefab.GetComponent<SliceableObject>();
                if (sliceable == null) continue;

                SerializedObject serialized = new SerializedObject(sliceable);
                SetReferenceGuid(serialized.FindProperty("pcvrSliceEffect"), pcvrGuid, path);
                SetReferenceGuid(serialized.FindProperty("questSliceEffect"), questGuid, path);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
    }

    private static void SetReferenceGuid(SerializedProperty property, string guid, string path)
    {
        if (property == null) throw new InvalidOperationException($"Addressable field was not found on {path}.");
        SerializedProperty assetGuid = property.FindPropertyRelative("m_AssetGUID");
        if (assetGuid == null) throw new InvalidOperationException($"AssetReference GUID field was not found on {path}.");
        assetGuid.stringValue = guid;
    }
}
