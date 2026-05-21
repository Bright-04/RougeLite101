using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WeaponRigAssetBuilder
{
    private const string SwordPrefabPath = "Assets/Prefabs/Sword.prefab";
    private const string BowPrefabPath = "Assets/Prefabs/bow.prefab";
    private const string TestScenePath = "Assets/Scenes/WeaponAlignmentTest.unity";
    private const string TestPrefabPath = "Assets/Prefabs/WeaponAlignmentTest.prefab";
    private const string DefaultWeaponPath = "Assets/ScriptableObjects/Weapons/Definitions/sword_basic.asset";
    private const string SecondaryWeaponPath = "Assets/ScriptableObjects/Weapons/Definitions/bow_basic.asset";

    [MenuItem("Tools/Weapons/Rebuild Weapon Rig Assets")]
    public static void RebuildWeaponRigAssets()
    {
        EnsureWeaponRigPrefab(SwordPrefabPath, true);
        EnsureWeaponRigPrefab(BowPrefabPath, false);
        EnsureWeaponAlignmentTestPrefab();
        EnsureWeaponAlignmentTestScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureWeaponRigPrefab(string prefabPath, bool melee)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            WeaponRig rig = root.GetComponent<WeaponRig>();
            if (rig == null)
            {
                rig = root.AddComponent<WeaponRig>();
            }

            Transform grip = EnsureChild(root.transform, WeaponRig.GripPointName);
            Transform tip = EnsureChild(root.transform, WeaponRig.TipPointName);
            Transform projectile = EnsureChild(root.transform, WeaponRig.ProjectileSpawnPointName);
            Transform slashOrigin = EnsureChild(root.transform, WeaponRig.SlashOriginName);
            Transform slashStart = EnsureChild(root.transform, WeaponRig.SlashArcStartName);
            Transform slashEnd = EnsureChild(root.transform, WeaponRig.SlashArcEndName);

            if (melee)
            {
                grip.localPosition = new Vector3(-0.09f, 0f, 0f);
                tip.localPosition = new Vector3(0.28f, 0f, 0f);
                projectile.localPosition = tip.localPosition;
                slashOrigin.localPosition = new Vector3(0.12f, 0f, 0f);
                slashStart.localPosition = new Vector3(0.18f, -0.28f, 0f);
                slashEnd.localPosition = new Vector3(0.18f, 0.28f, 0f);
            }
            else
            {
                grip.localPosition = new Vector3(0.24f, 0f, 0f);
                tip.localPosition = new Vector3(0.46f, 0f, 0f);
                projectile.localPosition = tip.localPosition;
                slashOrigin.localPosition = Vector3.zero;
                slashStart.localPosition = new Vector3(0.2f, -0.18f, 0f);
                slashEnd.localPosition = new Vector3(0.2f, 0.18f, 0f);
            }

            ResetMarkerTransform(grip);
            ResetMarkerTransform(tip);
            ResetMarkerTransform(projectile);
            ResetMarkerTransform(slashOrigin);
            ResetMarkerTransform(slashStart);
            ResetMarkerTransform(slashEnd);
            rig.AutoBindRequiredPoints(true);
            rig.ValidateRequiredPoints();
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(name);
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void ResetMarkerTransform(Transform marker)
    {
        marker.localRotation = Quaternion.identity;
        marker.localScale = Vector3.one;
    }

    private static GameObject EnsureWeaponAlignmentTestPrefab()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabPath);
        if (root != null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(TestPrefabPath);
            try
            {
                ConfigureTestHarness(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, TestPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return root;
        }

        GameObject instance = new GameObject("WeaponAlignmentTest");
        instance.AddComponent<WeaponAlignmentTestHarness>();
        ConfigureTestHarness(instance);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, TestPrefabPath);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void ConfigureTestHarness(GameObject root)
    {
        WeaponAlignmentTestHarness harness = root.GetComponent<WeaponAlignmentTestHarness>();
        if (harness == null)
        {
            harness = root.AddComponent<WeaponAlignmentTestHarness>();
        }

        harness.SelectedWeapon = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(DefaultWeaponPath);
        SerializedObject serializedHarness = new SerializedObject(harness);
        serializedHarness.FindProperty("secondaryWeapon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinitionSO>(SecondaryWeaponPath);
        serializedHarness.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureWeaponAlignmentTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject prefab = EnsureWeaponAlignmentTestPrefab();
        GameObject instance = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)
            : new GameObject("WeaponAlignmentTest");
        instance.transform.position = Vector3.zero;

        Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 4f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        EditorSceneManager.SaveScene(scene, TestScenePath);
        EditorSceneManager.CloseScene(scene, true);
    }
}
