using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SlimeForestAnimatorBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/Slime.prefab";
    private const string TargetPrefabPath = "Assets/Prefabs/Enemies/Slime_Forest.prefab";
    private const string ControllerFolder = "Assets/Animation/Monster/Slime_Forest";
    private const string ControllerPath = "Assets/Animation/Monster/Slime_Forest/SlimeF_Anim.controller";
    private const float SpeedThreshold = 0.1f;
    private const float ReturnExitTime = 0.95f;

    private static readonly string[] StateGroups = { "Idle", "Walk", "Attack", "Hurt", "Death" };
    private static readonly string[] Directions = { "Down", "Left", "Right", "Up" };
    private static readonly string[] ParameterNames = { "Speed", "Facing", "Attack", "Hurt", "Dead" };
    private static readonly AnimatorControllerParameterType[] ParameterTypes =
    {
        AnimatorControllerParameterType.Float,
        AnimatorControllerParameterType.Int,
        AnimatorControllerParameterType.Trigger,
        AnimatorControllerParameterType.Trigger,
        AnimatorControllerParameterType.Bool
    };

    [MenuItem("Tools/Enemies/Slime Forest/Rebuild Animator And Prefab")]
    public static void BuildAndValidateMenu()
    {
        Debug.Log(BuildAndValidateInternal());
    }

    public static void BuildAndValidate()
    {
        string report = BuildAndValidateInternal();
        Debug.Log(report);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(report.Contains("VALIDATION: PASS", StringComparison.Ordinal) ? 0 : 2);
        }
    }

    private static string BuildAndValidateInternal()
    {
        var changedFiles = new HashSet<string>(StringComparer.Ordinal);
        var createdParameters = new List<string>();
        var foundStates = new List<string>();
        var missingStates = new List<string>();
        var validation = new StringBuilder();

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            throw new InvalidOperationException($"Animator controller not found at {ControllerPath}");
        }

        changedFiles.Add(ControllerPath);

        EnsureParameters(controller, createdParameters);
        Dictionary<string, AnimationClip> clips = LoadClips();
        Dictionary<string, AnimatorState> states = EnsureStates(controller, clips, foundStates, missingStates);
        NormalizeController(controller, states);
        NormalizeClipLoopSettings(clips);
        UpgradePrefab(controller, changedFiles);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool success = Validate(controller, missingStates, validation);
        string summary = BuildSummary(changedFiles, createdParameters, foundStates, missingStates);
        return summary + validation + Environment.NewLine + (success ? "VALIDATION: PASS" : "VALIDATION: FAIL");
    }

    private static string BuildSummary(
        HashSet<string> changedFiles,
        List<string> createdParameters,
        List<string> foundStates,
        List<string> missingStates)
    {
        var summary = new StringBuilder();
        summary.AppendLine("Slime_Forest Animator Builder Report");
        summary.AppendLine("FILES CHANGED:");
        foreach (string path in changedFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            summary.AppendLine($"- {path}");
        }

        summary.AppendLine("PARAMETERS FOUND/CREATED:");
        foreach (string parameter in ParameterNames)
        {
            string status = createdParameters.Contains(parameter) ? "created" : "found";
            summary.AppendLine($"- {parameter} ({status})");
        }

        summary.AppendLine("STATES FOUND:");
        foreach (string state in foundStates.OrderBy(name => name, StringComparer.Ordinal))
        {
            summary.AppendLine($"- {state}");
        }

        summary.AppendLine("MISSING STATES/CLIPS:");
        if (missingStates.Count == 0)
        {
            summary.AppendLine("- None");
        }
        else
        {
            foreach (string item in missingStates.OrderBy(name => name, StringComparer.Ordinal))
            {
                summary.AppendLine($"- {item}");
            }
        }

        return summary.ToString();
    }

    private static void EnsureParameters(AnimatorController controller, List<string> createdParameters)
    {
        for (int i = 0; i < ParameterNames.Length; i++)
        {
            AnimatorControllerParameter existing = controller.parameters.FirstOrDefault(parameter => parameter.name == ParameterNames[i]);
            if (existing != null && existing.type == ParameterTypes[i])
            {
                continue;
            }

            if (existing != null)
            {
                controller.RemoveParameter(existing);
            }

            controller.AddParameter(ParameterNames[i], ParameterTypes[i]);
            createdParameters.Add(ParameterNames[i]);
        }
    }

    private static Dictionary<string, AnimationClip> LoadClips()
    {
        var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ControllerFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                clips[clip.name] = clip;
            }
        }

        return clips;
    }

    private static Dictionary<string, AnimatorState> EnsureStates(
        AnimatorController controller,
        IReadOnlyDictionary<string, AnimationClip> clips,
        List<string> foundStates,
        List<string> missingStates)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        var states = stateMachine.states.ToDictionary(child => child.state.name, child => child.state, StringComparer.Ordinal);

        foreach (string group in StateGroups)
        {
            foreach (string direction in Directions)
            {
                string clipName = ResolveAssetName(clips.Keys, group, direction);
                if (clipName == null)
                {
                    missingStates.Add($"{group}_{direction} clip");
                    continue;
                }

                string stateName = ResolveAssetName(states.Keys, group, direction) ?? clipName;
                if (!states.TryGetValue(stateName, out AnimatorState state))
                {
                    state = stateMachine.AddState(stateName, GetStatePosition(group, direction));
                    states[stateName] = state;
                }

                state.motion = clips[clipName];
                foundStates.Add(stateName);
            }
        }

        return states;
    }

    private static void NormalizeController(AnimatorController controller, IReadOnlyDictionary<string, AnimatorState> states)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }

        foreach (string group in StateGroups)
        {
            foreach (string direction in Directions)
            {
                AnimatorState state = GetState(states, group, direction);
                if (state == null)
                {
                    continue;
                }

                foreach (AnimatorStateTransition transition in state.transitions.ToArray())
                {
                    state.RemoveTransition(transition);
                }
            }
        }

        AnimatorState idleDown = GetState(states, "Idle", "Down");
        if (idleDown != null)
        {
            stateMachine.defaultState = idleDown;
        }

        for (int direction = 0; direction < Directions.Length; direction++)
        {
            string directionName = Directions[direction];
            AnimatorState idle = GetState(states, "Idle", directionName);
            AnimatorState walk = GetState(states, "Walk", directionName);
            AnimatorState attack = GetState(states, "Attack", directionName);
            AnimatorState hurt = GetState(states, "Hurt", directionName);
            AnimatorState death = GetState(states, "Death", directionName);

            if (idle != null && walk != null)
            {
                AddTransition(idle, walk, false, 0f, false, MakeCondition(AnimatorConditionMode.Greater, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
                AddTransition(walk, idle, false, 0f, false, MakeCondition(AnimatorConditionMode.Less, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
            }

            if (attack != null)
            {
                if (idle != null)
                {
                    AddTransition(attack, idle, true, ReturnExitTime, false, MakeCondition(AnimatorConditionMode.Less, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
                }

                if (walk != null)
                {
                    AddTransition(attack, walk, true, ReturnExitTime, false, MakeCondition(AnimatorConditionMode.Greater, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
                }

                AddAnyStateTransition(stateMachine, attack, MakeCondition(AnimatorConditionMode.If, 0f, "Attack"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"), MakeCondition(AnimatorConditionMode.IfNot, 0f, "Dead"));
            }

            if (hurt != null)
            {
                if (idle != null)
                {
                    AddTransition(hurt, idle, true, ReturnExitTime, false, MakeCondition(AnimatorConditionMode.Less, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
                }

                if (walk != null)
                {
                    AddTransition(hurt, walk, true, ReturnExitTime, false, MakeCondition(AnimatorConditionMode.Greater, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
                }

                AddAnyStateTransition(stateMachine, hurt, MakeCondition(AnimatorConditionMode.If, 0f, "Hurt"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"), MakeCondition(AnimatorConditionMode.IfNot, 0f, "Dead"));
            }

            if (death != null)
            {
                AddAnyStateTransition(stateMachine, death, MakeCondition(AnimatorConditionMode.If, 0f, "Dead"), MakeCondition(AnimatorConditionMode.Equals, direction, "Facing"));
            }
        }

        AddDirectionalSwitches(states, "Idle", AnimatorConditionMode.Less);
        AddDirectionalSwitches(states, "Walk", AnimatorConditionMode.Greater);
    }

    private static void AddDirectionalSwitches(IReadOnlyDictionary<string, AnimatorState> states, string group, AnimatorConditionMode speedMode)
    {
        for (int fromIndex = 0; fromIndex < Directions.Length; fromIndex++)
        {
            AnimatorState fromState = GetState(states, group, Directions[fromIndex]);
            if (fromState == null)
            {
                continue;
            }

            for (int toIndex = 0; toIndex < Directions.Length; toIndex++)
            {
                if (fromIndex == toIndex)
                {
                    continue;
                }

                AnimatorState toState = GetState(states, group, Directions[toIndex]);
                if (toState == null)
                {
                    continue;
                }

                AddTransition(fromState, toState, false, 0f, false, MakeCondition(speedMode, SpeedThreshold, "Speed"), MakeCondition(AnimatorConditionMode.Equals, toIndex, "Facing"));
            }
        }
    }

    private static void NormalizeClipLoopSettings(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        foreach (AnimationClip clip in clips.Values)
        {
            bool shouldLoop = clip.name.Contains("_Idle_", StringComparison.Ordinal)
                || clip.name.Contains("_Walk_", StringComparison.Ordinal)
                || clip.name.Contains("_Run_", StringComparison.Ordinal);

            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
            SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
            if (loopTime == null || loopTime.boolValue == shouldLoop)
            {
                continue;
            }

            loopTime.boolValue = shouldLoop;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clip);
        }
    }

    private static void UpgradePrefab(AnimatorController controller, HashSet<string> changedFiles)
    {
        GameObject source = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        GameObject target = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
        try
        {
            CopyRootSettings(source, target);
            CopyCollider(source, target);
            CopyOrAddComponent<Rigidbody2D>(source, target);
            CopyOrAddComponent<SlimePathFinding>(source, target);
            CopyOrAddComponent<SlimeAI>(source, target);
            CopyOrAddComponent<SlimeHealth>(source, target);
            CopyOrAddComponent<Flash>(source, target);
            CopyOrAddComponent<Knockback>(source, target);
            CopyOrAddComponent<EnemyDamageSource>(source, target);
            CopyOrAddComponent<EnemyDeathNotifier>(source, target);
            CopyOrAddComponent<EnemyDeathAnimation>(source, target);

            Animator animator = target.GetComponent<Animator>() ?? target.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            if (target.GetComponent<SlimeForestAnimatorDriver>() == null)
            {
                target.AddComponent<SlimeForestAnimatorDriver>();
            }

            ConfigureFlash(target);
            ConfigureDeathAnimation(target);

            EditorUtility.SetDirty(target);
            PrefabUtility.SaveAsPrefabAsset(target, TargetPrefabPath);
            changedFiles.Add(TargetPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
            PrefabUtility.UnloadPrefabContents(target);
        }
    }

    private static void CopyRootSettings(GameObject source, GameObject target)
    {
        target.layer = source.layer;
        target.tag = source.tag;
    }

    private static void CopyCollider(GameObject source, GameObject target)
    {
        if (source == null || target == null)
        {
            Debug.LogWarning("CopyCollider skipped because source or target GameObject is null.");
            return;
        }

        Collider2D sourceCollider = source.GetComponent<Collider2D>();
        if (sourceCollider == null)
        {
            Debug.LogWarning($"CopyCollider skipped because source '{source.name}' does not have a Collider2D.");
            return;
        }

        Collider2D targetCollider = target.GetComponent(sourceCollider.GetType()) as Collider2D;
        if (targetCollider == null)
        {
            targetCollider = target.AddComponent(sourceCollider.GetType()) as Collider2D;
        }

        if (targetCollider != null)
        {
            EditorUtility.CopySerialized(sourceCollider, targetCollider);
            EditorUtility.SetDirty(target);
        }
    }

    private static T CopyOrAddComponent<T>(GameObject source, GameObject target) where T : Component
    {
        if (source == null || target == null)
        {
            Debug.LogWarning($"CopyOrAddComponent<{typeof(T).Name}> skipped because source or target GameObject is null.");
            return null;
        }

        T sourceComponent = source.GetComponent<T>();
        if (sourceComponent == null)
        {
            Debug.LogWarning($"CopyOrAddComponent<{typeof(T).Name}> skipped because source '{source.name}' does not have the component.");
            return null;
        }

        T targetComponent = target.GetComponent<T>();
        if (targetComponent == null)
        {
            targetComponent = target.AddComponent<T>();
        }

        EditorUtility.CopySerialized(sourceComponent, targetComponent);
        EditorUtility.SetDirty(target);
        return targetComponent;
    }

    private static void ConfigureFlash(GameObject target)
    {
        Flash flash = target.GetComponent<Flash>();
        SpriteRenderer bodyRenderer = FindChildRenderer(target.transform, "BodyRenderer");
        if (flash == null || bodyRenderer == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(flash);
        serialized.FindProperty("targetRenderer").objectReferenceValue = bodyRenderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDeathAnimation(GameObject target)
    {
        EnemyDeathAnimation deathAnimation = target.GetComponent<EnemyDeathAnimation>();
        if (deathAnimation == null)
        {
            return;
        }

        var renderers = new List<SpriteRenderer>();
        AddRendererIfPresent(renderers, FindChildRenderer(target.transform, "BodyRenderer"));
        AddRendererIfPresent(renderers, FindChildRenderer(target.transform, "ShadowRenderer"));
        AddRendererIfPresent(renderers, FindChildRenderer(target.transform, "VfxRenderer"));

        SerializedObject serialized = new SerializedObject(deathAnimation);
        serialized.FindProperty("disableAnimatorOnDeath").boolValue = false;
        serialized.FindProperty("deathDelay").floatValue = 0.35f;
        SerializedProperty fadeRenderers = serialized.FindProperty("fadeRenderers");
        fadeRenderers.arraySize = renderers.Count;
        for (int i = 0; i < renderers.Count; i++)
        {
            fadeRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool Validate(AnimatorController controller, List<string> missingStates, StringBuilder validation)
    {
        bool success = true;
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
        try
        {
            if (prefabRoot == null)
            {
                validation.AppendLine("ERROR: Prefab missing.");
                return false;
            }

            Animator animator = prefabRoot.GetComponent<Animator>();
            if (animator == null)
            {
                success = false;
                validation.AppendLine("ERROR: Animator missing on Slime_Forest prefab.");
            }
            else if (animator.runtimeAnimatorController != controller)
            {
                success = false;
                validation.AppendLine("ERROR: RuntimeAnimatorController is not SlimeF_Anim.controller.");
            }

            int missingScripts = CountMissingScripts(prefabRoot);
            int missingReferences = CountMissingReferences(prefabRoot, validation);
            if (missingScripts > 0)
            {
                success = false;
                validation.AppendLine($"ERROR: Missing scripts = {missingScripts}");
            }

            if (missingReferences > 0)
            {
                success = false;
                validation.AppendLine($"ERROR: Missing references = {missingReferences}");
            }

            foreach (string parameterName in ParameterNames)
            {
                AnimatorControllerParameter parameter = controller.parameters.FirstOrDefault(item => item.name == parameterName);
                if (parameter == null)
                {
                    success = false;
                    validation.AppendLine($"ERROR: Missing parameter {parameterName}");
                }
            }

            AnimatorState defaultState = controller.layers[0].stateMachine.defaultState;
            if (defaultState == null || !defaultState.name.EndsWith("_Idle_Down", StringComparison.Ordinal))
            {
                success = false;
                validation.AppendLine("ERROR: Default state is not Idle_Down");
            }

            foreach (AnimatorState state in controller.layers[0].stateMachine.states.Select(child => child.state))
            {
                if (state.name.Contains("_Death_", StringComparison.Ordinal) && state.transitions.Length > 0)
                {
                    success = false;
                    validation.AppendLine($"ERROR: Death state has outgoing transitions: {state.name}");
                }
            }

            foreach (AnimationClip clip in LoadClips().Values)
            {
                bool shouldLoop = clip.name.Contains("_Idle_", StringComparison.Ordinal)
                    || clip.name.Contains("_Walk_", StringComparison.Ordinal)
                    || clip.name.Contains("_Run_", StringComparison.Ordinal);
                bool actualLoop = GetLoopTime(clip);
                if (actualLoop != shouldLoop)
                {
                    success = false;
                    validation.AppendLine($"ERROR: Loop mismatch on clip {clip.name}");
                }
            }

            if (missingStates.Count > 0)
            {
                success = false;
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return success;
    }

    private static int CountMissingScripts(GameObject root)
    {
        int total = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            total += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
        }

        return total;
    }

    private static int CountMissingReferences(GameObject root, StringBuilder validation)
    {
        int total = 0;
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            var serializedObject = new SerializedObject(component);
            SerializedProperty iterator = serializedObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyPath == "m_Script" || iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceInstanceIDValue == 0 || iterator.objectReferenceValue != null)
                {
                    continue;
                }

                total++;
                validation.AppendLine($"MISSING_REF {component.GetType().Name} :: {iterator.propertyPath}");
            }
        }

        return total;
    }

    private static bool GetLoopTime(AnimationClip clip)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        return settings != null && settings.FindPropertyRelative("m_LoopTime").boolValue;
    }

    private static AnimatorCondition MakeCondition(AnimatorConditionMode mode, float threshold, string parameter)
    {
        return new AnimatorCondition
        {
            mode = mode,
            threshold = threshold,
            parameter = parameter
        };
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, bool hasExitTime, float exitTime, bool canTransitionToSelf, params AnimatorCondition[] conditions)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = canTransitionToSelf;
        foreach (AnimatorCondition condition in conditions)
        {
            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState to, params AnimatorCondition[] conditions)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
        foreach (AnimatorCondition condition in conditions)
        {
            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }
    }

    private static AnimatorState GetState(IReadOnlyDictionary<string, AnimatorState> states, string group, string direction)
    {
        string stateName = ResolveAssetName(states.Keys, group, direction);
        return stateName != null ? states[stateName] : null;
    }

    private static string ResolveAssetName(IEnumerable<string> names, string group, string direction)
    {
        return names.FirstOrDefault(name => name.EndsWith($"_{group}_{direction}", StringComparison.Ordinal));
    }

    private static Vector3 GetStatePosition(string group, string direction)
    {
        int row = Array.IndexOf(StateGroups, group);
        int column = Array.IndexOf(Directions, direction);
        return new Vector3(column * 250f, row * 100f, 0f);
    }

    private static SpriteRenderer FindChildRenderer(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        return child != null ? child.GetComponent<SpriteRenderer>() : null;
    }

    private static void AddRendererIfPresent(List<SpriteRenderer> renderers, SpriteRenderer renderer)
    {
        if (renderer != null)
        {
            renderers.Add(renderer);
        }
    }
}
