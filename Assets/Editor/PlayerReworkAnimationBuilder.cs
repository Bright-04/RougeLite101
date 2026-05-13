using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerReworkAnimationBuilder
{
    private const string SourceFolder = "Assets/Sprites/Player/Player Rework";
    private const string OutputFolder = "Assets/Animation/Player/Rework";
    private const string ControllerPath = OutputFolder + "/Player_Rework_4Dir_Walk.controller";
    private const string RuntimeControllerPath = "Assets/Resources/Player_Rework_4Dir_Walk.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Scenes Management/Player.prefab";

    private const float FrameStep = 1f / 12f;
    private const float ReworkPixelsPerUnit = 200f;
    // Row convention observed in the Player Rework idle/walk sheets:
    // Row0 = Up/Back, Row1 = Left, Row2 = Down/Front, Row3 = Right.
    private const int UpRowIndex = 0;
    private const int LeftRowIndex = 1;
    private const int DownRowIndex = 2;
    private const int RightRowIndex = 3;

    private static readonly HashSet<string> LoopingSheets = new HashSet<string>
    {
        "idle",
        "run",
        "walk",
        "combat",
        "sit"
    };

    [MenuItem("Tools/Player Rework/1. Generate row animation clips")]
    public static void GenerateRowAnimationClips()
    {
        EnsureOutputFolder();
        ConfigureReworkTextureImportSettings();

        int clipCount = 0;
        foreach (string texturePath in Directory.GetFiles(SourceFolder, "*.png").Select(NormalizePath))
        {
            List<Sprite> sprites = LoadSprites(texturePath);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"No sprites found in {texturePath}. Open the texture in Unity and slice it first.");
                continue;
            }

            string sheetName = Path.GetFileNameWithoutExtension(texturePath);
            List<List<Sprite>> rows = GroupSpritesByRows(sprites);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                string clipPath = $"{OutputFolder}/Player_{SanitizeName(sheetName)}_Row{rowIndex}.anim";
                CreateSpriteClip(clipPath, rows[rowIndex], LoopingSheets.Contains(sheetName.ToLowerInvariant()));
                clipCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated {clipCount} Player Rework animation clips in {OutputFolder}.");
    }

    [MenuItem("Tools/Player Rework/2. Generate 4-direction walk controller")]
    public static void GenerateSimpleMovementController()
    {
        GenerateRowAnimationClips();

        AnimationClip idleDownClip = LoadRequiredClip("idle", DownRowIndex);
        AnimationClip idleLeftClip = LoadRequiredClip("idle", LeftRowIndex);
        AnimationClip idleRightClip = LoadRequiredClip("idle", RightRowIndex);
        AnimationClip idleUpClip = LoadRequiredClip("idle", UpRowIndex);

        AnimationClip walkDownClip = LoadRequiredClip("walk", DownRowIndex);
        AnimationClip walkLeftClip = LoadRequiredClip("walk", LeftRowIndex);
        AnimationClip walkRightClip = LoadRequiredClip("walk", RightRowIndex);
        AnimationClip walkUpClip = LoadRequiredClip("walk", UpRowIndex);

        if (idleDownClip == null || idleLeftClip == null || idleRightClip == null || idleUpClip == null ||
            walkDownClip == null || walkLeftClip == null || walkRightClip == null || walkUpClip == null)
        {
            Debug.LogError("Could not find generated idle/walk clips. Check Player Rework slicing and regenerate row animation clips.");
            return;
        }

        if (File.Exists(ControllerPath))
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AddFloatParameter(controller, "moveX");
        AddFloatParameter(controller, "moveY");
        AddFloatParameter(controller, "lastMoveX");
        AddFloatParameter(controller, "lastMoveY");
        AddTriggerParameter(controller, "Dash");
        SetFloatParameterDefault(controller, "lastMoveY", -1f);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(280, 80, 0));
        AnimatorState runState = stateMachine.AddState("Runniing", new Vector3(520, 80, 0));
        idleState.motion = CreateDirectionalBlendTree(
            controller,
            "Idle_4Dir",
            "lastMoveX",
            "lastMoveY",
            idleDownClip,
            idleLeftClip,
            idleRightClip,
            idleUpClip);
        runState.motion = CreateDirectionalBlendTree(
            controller,
            "Walk_4Dir",
            "moveX",
            "moveY",
            walkDownClip,
            walkLeftClip,
            walkRightClip,
            walkUpClip);
        stateMachine.defaultState = idleState;

        AddMoveTransition(idleState, runState, "moveX", AnimatorConditionMode.Greater, 0.1f);
        AddMoveTransition(idleState, runState, "moveX", AnimatorConditionMode.Less, -0.1f);
        AddMoveTransition(idleState, runState, "moveY", AnimatorConditionMode.Greater, 0.1f);
        AddMoveTransition(idleState, runState, "moveY", AnimatorConditionMode.Less, -0.1f);

        AnimatorStateTransition stopTransition = runState.AddTransition(idleState);
        stopTransition.hasExitTime = false;
        stopTransition.duration = 0f;
        stopTransition.AddCondition(AnimatorConditionMode.Less, 0.1f, "moveX");
        stopTransition.AddCondition(AnimatorConditionMode.Greater, -0.1f, "moveX");
        stopTransition.AddCondition(AnimatorConditionMode.Less, 0.1f, "moveY");
        stopTransition.AddCondition(AnimatorConditionMode.Greater, -0.1f, "moveY");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CopyControllerToResources();
        Debug.Log($"Generated 4-direction walk controller: {ControllerPath}");
    }

    [MenuItem("Tools/Player Rework/3. Assign 4-direction controller to Player prefab")]
    public static void AssignSimpleControllerToPlayerPrefab()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            GenerateSimpleMovementController();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        if (controller == null)
        {
            Debug.LogError($"Controller not found at {ControllerPath}.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Animator animator = prefabRoot.GetComponent<Animator>();
            SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
            AnimationClip idleClip = LoadRequiredClip("idle", 0);

            if (animator == null)
            {
                Debug.LogError("Player prefab does not have an Animator component.");
                return;
            }

            animator.runtimeAnimatorController = controller;
            ConfigurePlayerMovementForDirectionalSprites(prefabRoot);
            if (spriteRenderer != null && idleClip != null)
            {
                Sprite firstSprite = GetFirstSpriteFromClip(idleClip);
                if (firstSprite != null)
                {
                    spriteRenderer.sprite = firstSprite;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            Debug.Log($"Assigned {ControllerPath} to {PlayerPrefabPath} and disabled body sprite flip for directional animations.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void AddFloatParameter(AnimatorController controller, string name)
    {
        if (controller.parameters.Any(parameter => parameter.name == name))
        {
            return;
        }

        controller.AddParameter(name, AnimatorControllerParameterType.Float);
    }

    private static void SetFloatParameterDefault(AnimatorController controller, string name, float defaultValue)
    {
        AnimatorControllerParameter parameter = controller.parameters.FirstOrDefault(item => item.name == name);
        if (parameter == null)
        {
            return;
        }

        parameter.defaultFloat = defaultValue;
    }

    private static void AddTriggerParameter(AnimatorController controller, string name)
    {
        if (controller.parameters.Any(parameter => parameter.name == name))
        {
            return;
        }

        controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    private static void AddMoveTransition(AnimatorState source, AnimatorState destination, string parameter, AnimatorConditionMode mode, float threshold)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static BlendTree CreateDirectionalBlendTree(
        AnimatorController controller,
        string treeName,
        string parameterX,
        string parameterY,
        AnimationClip downClip,
        AnimationClip leftClip,
        AnimationClip rightClip,
        AnimationClip upClip)
    {
        BlendTree blendTree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = parameterX,
            blendParameterY = parameterY,
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        blendTree.AddChild(downClip, new Vector2(0f, -1f));
        blendTree.AddChild(leftClip, new Vector2(-1f, 0f));
        blendTree.AddChild(rightClip, new Vector2(1f, 0f));
        blendTree.AddChild(upClip, new Vector2(0f, 1f));
        EditorUtility.SetDirty(blendTree);
        return blendTree;
    }

    private static void CreateSpriteClip(string clipPath, List<Sprite> sprites, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = 60f;

        ObjectReferenceKeyframe[] keyframes = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index * FrameStep,
                value = sprite
            })
            .ToArray();

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.stopTime = Mathf.Max(FrameStep, keyframes.Length * FrameStep);
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
    }

    private static void ConfigureReworkTextureImportSettings()
    {
        foreach (string texturePath in Directory.GetFiles(SourceFolder, "*.png").Select(NormalizePath))
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool changed = false;
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, ReworkPixelsPerUnit))
            {
                importer.spritePixelsPerUnit = ReworkPixelsPerUnit;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static void CopyControllerToResources()
    {
        EnsureFolder("Assets/Resources");

        if (File.Exists(RuntimeControllerPath))
        {
            AssetDatabase.DeleteAsset(RuntimeControllerPath);
        }

        if (!AssetDatabase.CopyAsset(ControllerPath, RuntimeControllerPath))
        {
            Debug.LogWarning($"Could not copy {ControllerPath} to {RuntimeControllerPath} for runtime fallback loading.");
            return;
        }

        AssetDatabase.ImportAsset(RuntimeControllerPath);
    }

    private static Sprite GetFirstSpriteFromClip(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (EditorCurveBinding binding in bindings)
        {
            ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (frames.Length > 0)
            {
                return frames[0].value as Sprite;
            }
        }

        return null;
    }

    private static AnimationClip LoadClip(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{OutputFolder}/{fileName}");
    }

    private static AnimationClip LoadRequiredClip(string sheetName, int rowIndex)
    {
        return LoadClip($"Player_{SanitizeName(sheetName)}_Row{rowIndex}.anim");
    }

    private static void ConfigurePlayerMovementForDirectionalSprites(GameObject prefabRoot)
    {
        PlayerMovement playerMovement = prefabRoot.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(playerMovement);
        SerializedProperty flipSpriteWithAim = serializedObject.FindProperty("flipSpriteWithAim");
        if (flipSpriteWithAim != null)
        {
            flipSpriteWithAim.boolValue = false;
        }

        SerializedProperty fallbackAnimatorController = serializedObject.FindProperty("fallbackAnimatorController");
        if (fallbackAnimatorController != null)
        {
            fallbackAnimatorController.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Sprite> LoadSprites(string texturePath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => ExtractTrailingNumber(sprite.name))
            .ToList();
    }

    private static List<List<Sprite>> GroupSpritesByRows(List<Sprite> sprites)
    {
        return sprites
            .GroupBy(sprite => Mathf.RoundToInt(sprite.rect.y / 8f))
            .OrderByDescending(group => group.Average(sprite => sprite.rect.y))
            .Select(group => group.OrderBy(sprite => sprite.rect.x).ToList())
            .ToList();
    }

    private static int ExtractTrailingNumber(string value)
    {
        int underscoreIndex = value.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex == value.Length - 1)
        {
            return 0;
        }

        return int.TryParse(value.Substring(underscoreIndex + 1), out int result) ? result : 0;
    }

    private static void EnsureOutputFolder()
    {
        EnsureFolder("Assets/Animation");
        EnsureFolder("Assets/Animation/Player");
        EnsureFolder(OutputFolder);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }

    private static string SanitizeName(string name)
    {
        return name.Replace(" ", "_").Replace("-", "_").ToLowerInvariant();
    }
}
