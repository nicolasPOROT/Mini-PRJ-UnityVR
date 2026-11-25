using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class ConfigurePlatformFruitAddressables
{
    private const string SharedGroupName = "Fruit Projectiles Shared";
    private const string PcvrGroupName = "Fruit Projectiles PCVR";
    private const string AppleGuid = "91bb87ba73f041a48aa034a969119026";
    private const string BananaGuid = "828e284ef166fcb4abda6ee2acc62096";
    private const string PineappleGuid = "260e9f39b0527244e92b77a3f1ec4339";

    [MenuItem("Tools/Addressables/Configure Platform Fruits")]
    public static void Configure()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null) throw new InvalidOperationException("Could not create Addressables settings.");
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

        AddressableAssetGroup sharedGroup = GetOrCreateGroup(settings, SharedGroupName);
        AddressableAssetGroup pcvrGroup = GetOrCreateGroup(settings, PcvrGroupName);

        AddAsset(settings, sharedGroup, AppleGuid, "Fruit_Apple");
        AddAsset(settings, sharedGroup, BananaGuid, "Fruit_Banana");
        AddAsset(settings, pcvrGroup, PineappleGuid, "Fruit_Pineapple_PC");
        RemoveEmptyLegacyGroup(settings);

        foreach (string prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            ConfigureCanons(AssetDatabase.GUIDToAssetPath(prefabGuid));
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured platform-specific fruit Addressables: PCVR uses apple, banana and pineapple; Quest uses apple and banana.");
    }

    private static void AddAsset(AddressableAssetSettings settings, AddressableAssetGroup group, string guid, string address)
    {
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
        entry.address = address;
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        AddressableAssetGroup group = settings.FindGroup(groupName);
        return group ?? settings.CreateGroup(
            groupName,
            false,
            false,
            false,
            null,
            typeof(BundledAssetGroupSchema),
            typeof(ContentUpdateGroupSchema));
    }

    private static void RemoveEmptyLegacyGroup(AddressableAssetSettings settings)
    {
        AddressableAssetGroup legacyGroup = settings.FindGroup("Fruit Projectiles");
        if (legacyGroup != null && legacyGroup.entries.Count == 0)
            settings.RemoveGroup(legacyGroup);
    }

    private static void ConfigureCanons(string path)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            bool changed = false;
            foreach (CanonShoot canon in prefab.GetComponentsInChildren<CanonShoot>(true))
            {
                SerializedObject serializedCanon = new SerializedObject(canon);
                SetReferences(serializedCanon.FindProperty("pcvrFruits"), AppleGuid, BananaGuid, PineappleGuid);
                SetReferences(serializedCanon.FindProperty("questFruits"), AppleGuid, BananaGuid);
                serializedCanon.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void SetReferences(SerializedProperty array, params string[] assetGuids)
    {
        if (array == null || !array.isArray)
            throw new InvalidOperationException("The platform fruit Addressable field was not found.");

        array.arraySize = assetGuids.Length;
        for (int i = 0; i < assetGuids.Length; i++)
        {
            SerializedProperty assetGuid = array.GetArrayElementAtIndex(i).FindPropertyRelative("m_AssetGUID");
            if (assetGuid == null)
                throw new InvalidOperationException("The AssetReference GUID field was not found.");
            assetGuid.stringValue = assetGuids[i];
        }
    }
}
