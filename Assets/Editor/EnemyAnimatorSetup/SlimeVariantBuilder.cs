using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SlimeVariantBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/Enemies/Slime_Forest.prefab";
    private const string SourceControllerPath = "Assets/Animation/Monster/Slime_Forest/SlimeF_Anim.controller";
    private const string SourceClipFolder = "Assets/Animation/Monster/Slime_Forest";
    private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
    private const string AnimationRootFolder = "Assets/Animation/Monster";

    private static readonly string[] ParameterNames = { "Speed", "Facing", "Attack", "Hurt", "Dead" };
    private static readonly AnimatorControllerParameterType[] ParameterTypes =
    {
        AnimatorControllerParameterType.Float,
        AnimatorControllerParameterType.Int,
        AnimatorControllerParameterType.Trigger,
        AnimatorControllerParameterType.Trigger,
        AnimatorControllerParameterType.Bool
    };

    private static readonly VariantSpec[] Variants =
    {
        new VariantSpec(
            variantName: "Slime_Ice",
            spritePrefix: "Slime2",
            spriteFolder: "Assets/Sprites/Monster/Slime(2.0)/Slime2",
            prefabPath: "Assets/Prefabs/Enemies/Slime_Ice.prefab",
            controllerPath: "Assets/Animation/Monster/Slime_Ice/Slime_Ice_Anim.controller"),
        new VariantSpec(
            variantName: "Slime_Magma",
            spritePrefix: "Slime3",
            spriteFolder: "Assets/Sprites/Monster/Slime(2.0)/Slime3",
            prefabPath: "Assets/Prefabs/Enemies/Slime_Magma.prefab",
            controllerPath: "Assets/Animation/Monster/Slime_Magma/Slime_Magma_Anim.controller")
    };

    [MenuItem("Tools/Enemies/Slime Variants/Build Remaining Variants")]
    public static void BuildRemainingVariantsMenu()
    {
        Debug.Log(BuildRemainingVariants());
    }

    public static string BuildRemainingVariants()
    {
        BuildReport report = BuildRemainingVariantsInternal();
        string text = report.ToText();
        Debug.Log(text);

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(report.Success ? 0 : 2);
        }

        return text;
    }

    private static BuildReport BuildRemainingVariantsInternal()
    {
        var report = new BuildReport();
        report.Info("BuilderVersion: variable-frame-v4");
        report.Info("FrameMode: body-shadow-variable-counts; vfx-regenerate-or-fallback");
        report.Info($"Source prefab: {SourcePrefabPath}");
        report.Info($"Source controller: {SourceControllerPath}");

        GameObject sourcePrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        AnimatorController sourceController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SourceControllerPath);
        if (sourcePrefabRoot == null)
        {
            report.Error($"Source prefab missing: {SourcePrefabPath}");
            return report;
        }

        if (sourceController == null)
        {
            report.Error($"Source controller missing: {SourceControllerPath}");
            return report;
        }

        PrefabShape sourceShape;
        try
        {
            sourceShape = InspectPrefabShape(SourcePrefabPath);
        }
        catch (Exception ex)
        {
            report.Error($"Failed to inspect source prefab: {ex.Message}");
            return report;
        }

        report.Info($"Source prefab root: {sourceShape.RootName}");
        report.Info($"Source prefab children: {string.Join(", ", sourceShape.ChildNames)}");
        report.Info($"Source prefab component count: {sourceShape.RootComponentTypes.Count}");

        Dictionary<string, ClipSpec> sourceClips;
        try
        {
            sourceClips = LoadSourceClipSpecs();
        }
        catch (Exception ex)
        {
            report.Error($"Failed to inspect source clips: {ex.Message}");
            return report;
        }

        foreach (ClipSpec clip in sourceClips.Values.OrderBy(item => item.Clip.name, StringComparer.Ordinal))
        {
            string bindingSummary = string.Join(", ", clip.Bindings.Select(binding => $"{binding.Path}:{binding.Keyframes.Length}"));
            report.Info($"Source clip {clip.Clip.name} [{clip.Group}/{clip.Direction}] sample={clip.Clip.frameRate} loop={clip.Loops} bindings={bindingSummary}");
        }

        if (!ValidateSourceController(sourceController, sourceClips, report))
        {
            return report;
        }

        var plans = new List<VariantBuildPlan>();
        bool preparationFailed = false;
        foreach (VariantSpec variant in Variants)
        {
            try
            {
                VariantBuildPlan plan = PrepareVariantPlan(variant, sourceClips, sourceShape, report);
                if (plan == null)
                {
                    preparationFailed = true;
                    continue;
                }

                plans.Add(plan);
            }
            catch (Exception ex)
            {
                report.Error($"{variant.VariantName}: preparation failed: {ex.Message}");
                preparationFailed = true;
            }
        }

        if (preparationFailed)
        {
            return report;
        }

        foreach (VariantBuildPlan plan in plans)
        {
            try
            {
                ApplyVariantPlan(plan, sourceController, sourceShape, report);
            }
            catch (Exception ex)
            {
                report.Error($"{plan.Variant.VariantName}: build failed: {ex.Message}");
                return report;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (VariantBuildPlan plan in plans)
        {
            ValidateBuiltVariant(plan, sourceController, sourceClips, sourceShape, report);
        }

        ValidateSourceStillIntact(sourceController, sourceShape, report);
        return report;
    }

    private static bool ValidateSourceController(
        AnimatorController sourceController,
        IReadOnlyDictionary<string, ClipSpec> sourceClips,
        BuildReport report)
    {
        if (sourceController.layers.Length == 0)
        {
            report.Error("Source controller has no layers.");
            return false;
        }

        for (int i = 0; i < ParameterNames.Length; i++)
        {
            AnimatorControllerParameter parameter = sourceController.parameters.FirstOrDefault(item => item.name == ParameterNames[i]);
            if (parameter == null || parameter.type != ParameterTypes[i])
            {
                report.Error($"Source controller parameter mismatch: expected {ParameterNames[i]} ({ParameterTypes[i]}).");
                return false;
            }
        }

        HashSet<string> clipNames = new HashSet<string>(sourceClips.Keys, StringComparer.Ordinal);
        foreach (ChildAnimatorState childState in sourceController.layers[0].stateMachine.states)
        {
            AnimationClip clip = childState.state.motion as AnimationClip;
            if (clip == null)
            {
                report.Error($"Source controller state {childState.state.name} does not reference an AnimationClip.");
                return false;
            }

            if (!clipNames.Contains(clip.name))
            {
                report.Error($"Source controller state {childState.state.name} references clip outside source set: {clip.name}");
                return false;
            }
        }

        return true;
    }

    private static VariantBuildPlan PrepareVariantPlan(
        VariantSpec variant,
        IReadOnlyDictionary<string, ClipSpec> sourceClips,
        PrefabShape sourceShape,
        BuildReport report)
    {
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(variant.PrefabPath) != null;
        bool controllerExists = AssetDatabase.LoadAssetAtPath<AnimatorController>(variant.ControllerPath) != null;
        report.Info($"{variant.VariantName}: prefab {(prefabExists ? "update-in-place" : "create-new")} -> {variant.PrefabPath}");
        report.Info($"{variant.VariantName}: controller {(controllerExists ? "update-in-place" : "create-new")} -> {variant.ControllerPath}");

        var clipPlans = new List<VariantClipPlan>();
        bool hasBlockingIssue = false;
        foreach (ClipSpec sourceClip in sourceClips.Values.OrderBy(item => item.Clip.name, StringComparer.Ordinal))
        {
            string targetClipName = BuildTargetClipName(variant.VariantName, sourceClip.Group, sourceClip.Direction);
            string targetFolder = GetControllerFolder(variant.ControllerPath);
            string targetClipPath = $"{targetFolder}/{targetClipName}.anim";
            bool clipExists = AssetDatabase.LoadAssetAtPath<AnimationClip>(targetClipPath) != null;
            report.Info($"{variant.VariantName}: clip {(clipExists ? "update-in-place" : "create-new")} -> {targetClipPath}");

            var replacements = new List<BindingReplacement>();
            foreach (ClipBindingSpec binding in sourceClip.Bindings)
            {
                try
                {
                    string spriteAssetPath = ResolveVariantSpriteAssetPath(variant, sourceClip.Group, binding.Path);
                    if (binding.Path == "VfxRenderer" && AssetDatabase.LoadMainAssetAtPath(spriteAssetPath) == null)
                    {
                        report.Info($"{variant.VariantName}: fallback VFX for {sourceClip.Clip.name} because {spriteAssetPath} does not exist.");
                        replacements.Add(new BindingReplacement(binding, spriteAssetPath, Array.Empty<Sprite>(), binding.Keyframes, binding.Keyframes.Length, 0, ReplacementMode.FallbackSource, 0, "missing-vfx-sheet"));
                        continue;
                    }

                    SpriteSheetInfo sheet = LoadSpriteSheetInfo(spriteAssetPath);
                    report.Info($"{variant.VariantName}: {sourceClip.Clip.name} binding {binding.Path} totalFrames={sheet.TotalFrames} perDirection={sheet.FramesPerDirection} possibleEmpty={sheet.PossibleEmptyCount}");
                    if (!string.IsNullOrEmpty(sheet.Warning))
                    {
                        report.Info($"{variant.VariantName}: {sourceClip.Clip.name} binding {binding.Path} warning={sheet.Warning}");
                    }

                    if (sourceClip.Group == "Attack" && (binding.Path == "BodyRenderer" || binding.Path == "ShadowRenderer"))
                    {
                        report.Info($"{variant.VariantName}: attack frames for {sourceClip.Direction} -> {binding.Path} uses {sheet.FramesPerDirection} per direction from {sheet.TotalFrames} total slices.");
                    }

                    BindingReplacement replacement = BuildBindingReplacement(variant, sourceClip, binding, sheet, spriteAssetPath, report);
                    if (replacement == null)
                    {
                        hasBlockingIssue = true;
                        continue;
                    }

                    replacements.Add(replacement);
                }
                catch (Exception ex)
                {
                    report.Error($"{variant.VariantName}: failed to inspect binding {binding.Path} for {sourceClip.Clip.name}: {ex.Message}");
                    hasBlockingIssue = true;
                }
            }

            if (HasRequiredBindingReplacements(sourceClip, replacements))
            {
                clipPlans.Add(new VariantClipPlan(sourceClip, targetClipName, targetClipPath, clipExists, replacements));
            }
        }

        if (prefabExists)
        {
            PrefabShape targetShape = InspectPrefabShape(variant.PrefabPath);
            if (!sourceShape.Matches(targetShape))
            {
                report.Error($"{variant.VariantName}: existing prefab structure differs from authoritative source prefab. " +
                    $"Will not recreate automatically to preserve GUID.");
                report.Info($"{variant.VariantName}: source children = {string.Join(", ", sourceShape.ChildNames)}");
                report.Info($"{variant.VariantName}: target children = {string.Join(", ", targetShape.ChildNames)}");
                hasBlockingIssue = true;
            }
        }

        if (hasBlockingIssue)
        {
            return null;
        }

        return new VariantBuildPlan(variant, prefabExists, controllerExists, clipPlans);
    }

    private static void ApplyVariantPlan(
        VariantBuildPlan plan,
        AnimatorController sourceController,
        PrefabShape sourceShape,
        BuildReport report)
    {
        EnsureFolder(GetControllerFolder(plan.Variant.ControllerPath));
        EnsureFolder(EnemyPrefabFolder);

        foreach (VariantClipPlan clipPlan in plan.Clips)
        {
            EnsureClipAsset(clipPlan, report);
            ApplyClipSprites(clipPlan);
            report.Changed(clipPlan.TargetClipPath);
        }

        EnsureControllerAsset(plan, report);
        UpdateControllerMotions(plan, sourceController);
        report.Changed(plan.Variant.ControllerPath);

        EnsurePrefabAsset(plan, report);
        UpdatePrefabAsset(plan, sourceShape);
        report.Changed(plan.Variant.PrefabPath);
    }

    private static void EnsureClipAsset(VariantClipPlan clipPlan, BuildReport report)
    {
        if (clipPlan.Exists)
        {
            return;
        }

        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(clipPlan.Source.Clip), clipPlan.TargetClipPath))
        {
            throw new InvalidOperationException($"Failed to duplicate clip to {clipPlan.TargetClipPath}");
        }

        report.Info($"Created clip by duplication: {clipPlan.TargetClipPath}");
    }

    private static void ApplyClipSprites(VariantClipPlan clipPlan)
    {
        AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPlan.TargetClipPath);
        if (targetClip == null)
        {
            throw new InvalidOperationException($"Target clip missing: {clipPlan.TargetClipPath}");
        }

        EditorCurveBinding[] targetBindings = AnimationUtility.GetObjectReferenceCurveBindings(targetClip);
        Dictionary<(string path, string property), EditorCurveBinding> bindingMap = targetBindings.ToDictionary(
            item => (item.path, item.propertyName),
            item => item);

        foreach (BindingReplacement replacement in clipPlan.Replacements)
        {
            if (!replacement.ShouldReplace)
            {
                continue;
            }

            EditorCurveBinding sourceBinding = replacement.SourceBinding.Binding;
            if (!bindingMap.TryGetValue((sourceBinding.path, sourceBinding.propertyName), out EditorCurveBinding targetBinding))
            {
                throw new InvalidOperationException($"Target clip {clipPlan.TargetClipPath} is missing binding {sourceBinding.path}:{sourceBinding.propertyName}");
            }

            AnimationUtility.SetObjectReferenceCurve(targetClip, targetBinding, replacement.TargetKeyframes);
        }

        UpdateClipDuration(targetClip, clipPlan);
        EditorUtility.SetDirty(targetClip);
    }

    private static void EnsureControllerAsset(VariantBuildPlan plan, BuildReport report)
    {
        if (plan.ControllerExists)
        {
            return;
        }

        if (!AssetDatabase.CopyAsset(SourceControllerPath, plan.Variant.ControllerPath))
        {
            throw new InvalidOperationException($"Failed to duplicate controller to {plan.Variant.ControllerPath}");
        }

        report.Info($"Created controller by duplication: {plan.Variant.ControllerPath}");
    }

    private static void UpdateControllerMotions(VariantBuildPlan plan, AnimatorController sourceController)
    {
        AnimatorController targetController = AssetDatabase.LoadAssetAtPath<AnimatorController>(plan.Variant.ControllerPath);
        if (targetController == null)
        {
            throw new InvalidOperationException($"Target controller missing: {plan.Variant.ControllerPath}");
        }

        ValidateControllerContract(targetController, sourceController);

        Dictionary<string, AnimationClip> targetClips = plan.Clips.ToDictionary(
            item => item.TargetClipName,
            item => AssetDatabase.LoadAssetAtPath<AnimationClip>(item.TargetClipPath),
            StringComparer.Ordinal);

        foreach (ChildAnimatorState childState in targetController.layers[0].stateMachine.states)
        {
            string stateName = childState.state.name;
            string sourceMotionName = ((AnimationClip)FindSourceState(sourceController, stateName).motion).name;
            ClipSpec sourceClip = plan.Clips.First(item => item.Source.Clip.name == sourceMotionName).Source;
            string expectedTargetClipName = BuildTargetClipName(plan.Variant.VariantName, sourceClip.Group, sourceClip.Direction);
            if (!targetClips.TryGetValue(expectedTargetClipName, out AnimationClip targetClip) || targetClip == null)
            {
                throw new InvalidOperationException($"No target clip found for state {stateName} in controller {plan.Variant.ControllerPath}");
            }

            childState.state.motion = targetClip;
        }

        EditorUtility.SetDirty(targetController);
    }

    private static void EnsurePrefabAsset(VariantBuildPlan plan, BuildReport report)
    {
        if (plan.PrefabExists)
        {
            return;
        }

        if (!AssetDatabase.CopyAsset(SourcePrefabPath, plan.Variant.PrefabPath))
        {
            throw new InvalidOperationException($"Failed to duplicate prefab to {plan.Variant.PrefabPath}");
        }

        report.Info($"Created prefab by duplication: {plan.Variant.PrefabPath}");
    }

    private static void UpdatePrefabAsset(VariantBuildPlan plan, PrefabShape sourceShape)
    {
        GameObject sourceRoot = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        GameObject targetRoot = PrefabUtility.LoadPrefabContents(plan.Variant.PrefabPath);
        try
        {
            PrefabShape targetShape = PrefabShape.FromLoadedPrefab(targetRoot);
            if (!sourceShape.Matches(targetShape))
            {
                throw new InvalidOperationException("Target prefab structure differs from source prefab.");
            }

            SyncPrefabObjects(sourceRoot, targetRoot);
            targetRoot.name = plan.Variant.VariantName;

            Animator animator = targetRoot.GetComponent<Animator>();
            AnimatorController targetController = AssetDatabase.LoadAssetAtPath<AnimatorController>(plan.Variant.ControllerPath);
            if (animator == null || targetController == null)
            {
                throw new InvalidOperationException("Animator or target controller missing during prefab update.");
            }

            animator.runtimeAnimatorController = targetController;

            SpriteRenderer bodyRenderer = FindRenderer(targetRoot.transform, "BodyRenderer");
            SpriteRenderer shadowRenderer = FindRenderer(targetRoot.transform, "ShadowRenderer");
            SpriteRenderer vfxRenderer = FindRenderer(targetRoot.transform, "VfxRenderer");

            bodyRenderer.sprite = FindInitialSprite(plan, "Idle", "Down", "BodyRenderer");
            shadowRenderer.sprite = FindInitialSprite(plan, "Idle", "Down", "ShadowRenderer");
            if (vfxRenderer != null)
            {
                vfxRenderer.sprite = null;
            }

            ConfigureFlash(targetRoot, bodyRenderer);
            ConfigureDeathAnimation(targetRoot, bodyRenderer, shadowRenderer, vfxRenderer);

            EditorUtility.SetDirty(targetRoot);
            PrefabUtility.SaveAsPrefabAsset(targetRoot, plan.Variant.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(sourceRoot);
            PrefabUtility.UnloadPrefabContents(targetRoot);
        }
    }

    private static void ValidateBuiltVariant(
        VariantBuildPlan plan,
        AnimatorController sourceController,
        IReadOnlyDictionary<string, ClipSpec> sourceClips,
        PrefabShape sourceShape,
        BuildReport report)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(plan.Variant.ControllerPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(plan.Variant.PrefabPath);
        if (controller == null)
        {
            report.Error($"{plan.Variant.VariantName}: missing controller after build.");
            return;
        }

        if (prefab == null)
        {
            report.Error($"{plan.Variant.VariantName}: missing prefab after build.");
            return;
        }

        try
        {
            ValidateControllerContract(controller, sourceController);
        }
        catch (Exception ex)
        {
            report.Error($"{plan.Variant.VariantName}: controller validation failed: {ex.Message}");
        }

        foreach (VariantClipPlan clipPlan in plan.Clips)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPlan.TargetClipPath);
            if (clip == null)
            {
                report.Error($"{plan.Variant.VariantName}: missing clip {clipPlan.TargetClipPath}");
                continue;
            }

            ValidateClipMatchesPlan(clipPlan, clip, report, plan.Variant.VariantName);
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(plan.Variant.PrefabPath);
        try
        {
            PrefabShape targetShape = PrefabShape.FromLoadedPrefab(prefabRoot);
            if (!sourceShape.Matches(targetShape))
            {
                report.Error($"{plan.Variant.VariantName}: prefab shape no longer matches source.");
            }

            Animator animator = prefabRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                report.Error($"{plan.Variant.VariantName}: prefab animator/controller mismatch.");
            }

            int missingScripts = CountMissingScripts(prefabRoot);
            int missingReferences = CountMissingReferences(prefabRoot, report, plan.Variant.VariantName);
            if (missingScripts > 0)
            {
                report.Error($"{plan.Variant.VariantName}: missing scripts = {missingScripts}");
            }

            if (missingReferences > 0)
            {
                report.Error($"{plan.Variant.VariantName}: missing references = {missingReferences}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ValidateSourceStillIntact(AnimatorController sourceController, PrefabShape sourceShape, BuildReport report)
    {
        GameObject sourcePrefabRoot = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        try
        {
            PrefabShape currentShape = PrefabShape.FromLoadedPrefab(sourcePrefabRoot);
            if (!sourceShape.Matches(currentShape))
            {
                report.Error("Source prefab changed during variant build.");
            }

            Animator animator = sourcePrefabRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != sourceController)
            {
                report.Error("Source prefab animator/controller changed during variant build.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(sourcePrefabRoot);
        }
    }

    private static void ValidateClipMatchesPlan(VariantClipPlan plan, AnimationClip target, BuildReport report, string variantName)
    {
        ClipSpec source = plan.Source;
        EditorCurveBinding[] sourceBindings = AnimationUtility.GetObjectReferenceCurveBindings(source.Clip);
        EditorCurveBinding[] targetBindings = AnimationUtility.GetObjectReferenceCurveBindings(target);
        if (sourceBindings.Length != targetBindings.Length)
        {
            report.Error($"{variantName}: binding count mismatch in {target.name}");
            return;
        }

        foreach (ClipBindingSpec sourceBinding in source.Bindings)
        {
            bool foundTargetBinding = false;
            EditorCurveBinding targetBinding = default;
            foreach (EditorCurveBinding candidate in targetBindings)
            {
                if (candidate.path == sourceBinding.Path &&
                    candidate.propertyName == sourceBinding.Binding.propertyName &&
                    candidate.type == sourceBinding.Binding.type)
                {
                    targetBinding = candidate;
                    foundTargetBinding = true;
                    break;
                }
            }

            if (!foundTargetBinding)
            {
                report.Error($"{variantName}: missing binding {sourceBinding.Path} in {target.name}");
                continue;
            }

            ObjectReferenceKeyframe[] targetKeys = AnimationUtility.GetObjectReferenceCurve(target, targetBinding);
            BindingReplacement replacement = plan.Replacements.First(item => item.SourceBinding.Path == sourceBinding.Path);
            ObjectReferenceKeyframe[] expectedKeys = replacement.ShouldReplace ? replacement.TargetKeyframes : sourceBinding.Keyframes;
            if (targetKeys.Length != expectedKeys.Length)
            {
                report.Error($"{variantName}: keyframe count mismatch for {target.name} binding {sourceBinding.Path}");
                continue;
            }

            for (int i = 0; i < targetKeys.Length; i++)
            {
                if (!Mathf.Approximately(targetKeys[i].time, expectedKeys[i].time))
                {
                    report.Error($"{variantName}: keyframe time mismatch for {target.name} binding {sourceBinding.Path} at index {i}");
                    break;
                }

                if (targetKeys[i].value != expectedKeys[i].value)
                {
                    report.Error($"{variantName}: sprite value mismatch for {target.name} binding {sourceBinding.Path} at index {i}");
                    break;
                }
            }
        }

        if (!Mathf.Approximately(target.frameRate, source.Clip.frameRate))
        {
            report.Error($"{variantName}: sample rate mismatch for {target.name}");
        }

        if (GetLoopTime(target) != source.Loops)
        {
            report.Error($"{variantName}: loop setting mismatch for {target.name}");
        }
    }

    private static PrefabShape InspectPrefabShape(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            return PrefabShape.FromLoadedPrefab(root);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Dictionary<string, ClipSpec> LoadSourceClipSpecs()
    {
        var specs = new Dictionary<string, ClipSpec>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { SourceClipFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                continue;
            }

            ParseSourceClipName(clip.name, out string group, out string direction);
            List<ClipBindingSpec> bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .Where(binding => binding.propertyName == "m_Sprite" && binding.type == typeof(SpriteRenderer))
                .Select(binding => new ClipBindingSpec(binding, AnimationUtility.GetObjectReferenceCurve(clip, binding)))
                .OrderBy(binding => binding.Path, StringComparer.Ordinal)
                .ToList();

            specs[clip.name] = new ClipSpec(clip, group, direction, bindings, GetLoopTime(clip));
        }

        return specs;
    }

    private static void ParseSourceClipName(string clipName, out string group, out string direction)
    {
        Match match = Regex.Match(clipName, @"^[^_]+_(?<group>Idle|Walk|Run|Attack|Hurt|Death)_(?<direction>Down|Left|Right|Up)$");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Unexpected source clip name format: {clipName}");
        }

        group = match.Groups["group"].Value;
        direction = match.Groups["direction"].Value;
    }

    private static string BuildTargetClipName(string variantName, string group, string direction)
    {
        return $"{variantName}_{group}_{direction}";
    }

    private static string ResolveVariantSpriteAssetPath(VariantSpec variant, string group, string bindingPath)
    {
        string suffix = bindingPath switch
        {
            "BodyRenderer" => "_body",
            "ShadowRenderer" => "_shadow",
            "VfxRenderer" => string.Empty,
            _ => throw new InvalidOperationException($"Unsupported renderer path in source clip: {bindingPath}")
        };

        return $"{variant.SpriteFolder}/{variant.SpritePrefix}_{group}{suffix}.png";
    }

    private static SpriteSheetInfo LoadSpriteSheetInfo(string spriteAssetPath)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(spriteAssetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, SpriteNameComparer.Instance)
            .ToArray();

        if (sprites.Length == 0)
        {
            throw new InvalidOperationException($"No sliced sprites found at {spriteAssetPath}");
        }

        int possibleEmptyCount = sprites.Count(IsPossiblyEmptySprite);
        return new SpriteSheetInfo(
            spriteAssetPath,
            sprites,
            possibleEmptyCount,
            possibleEmptyCount > 0 ? "possible-empty-slices-detected" : null);
    }

    private static BindingReplacement BuildBindingReplacement(
        VariantSpec variant,
        ClipSpec sourceClip,
        ClipBindingSpec binding,
        SpriteSheetInfo sheet,
        string spriteAssetPath,
        BuildReport report)
    {
        bool isBodyOrShadow = binding.Path == "BodyRenderer" || binding.Path == "ShadowRenderer";
        bool isVfx = binding.Path == "VfxRenderer";
        if (isBodyOrShadow)
        {
            if (sheet.TotalFrames % 4 != 0)
            {
                report.Error($"{variant.VariantName}: {sourceClip.Clip.name} binding {binding.Path} cannot partition {sheet.TotalFrames} frames into 4 directions.");
                return null;
            }

            int framesPerDirection = sheet.TotalFrames / 4;
            if (framesPerDirection <= 0)
            {
                report.Error($"{variant.VariantName}: {sourceClip.Clip.name} binding {binding.Path} has no frames per direction.");
                return null;
            }

            Sprite[] directionSprites = TakeDirectionSprites(sheet.Sprites, sourceClip.Direction, framesPerDirection);
            ObjectReferenceKeyframe[] keyframes = BuildObjectReferenceKeyframes(directionSprites, sourceClip.Clip.frameRate);
            report.Info($"{variant.VariantName}: replace {sourceClip.Clip.name} {binding.Path} with {framesPerDirection} frames for {sourceClip.Direction} at {sourceClip.Clip.frameRate} fps");
            return new BindingReplacement(binding, spriteAssetPath, directionSprites, keyframes, framesPerDirection, sheet.TotalFrames, ReplacementMode.Replace, sheet.PossibleEmptyCount, sheet.Warning);
        }

        if (isVfx)
        {
            if (sheet.TotalFrames % 4 != 0)
            {
                report.Info($"{variant.VariantName}: fallback VFX for {sourceClip.Clip.name} because {spriteAssetPath} has ambiguous frame count {sheet.TotalFrames}.");
                return new BindingReplacement(binding, spriteAssetPath, Array.Empty<Sprite>(), binding.Keyframes, binding.Keyframes.Length, sheet.TotalFrames, ReplacementMode.FallbackSource, sheet.PossibleEmptyCount, "ambiguous-vfx-frame-count");
            }

            int framesPerDirection = sheet.TotalFrames / 4;
            if (framesPerDirection <= 0)
            {
                report.Info($"{variant.VariantName}: fallback VFX for {sourceClip.Clip.name} because {spriteAssetPath} has zero directional frames.");
                return new BindingReplacement(binding, spriteAssetPath, Array.Empty<Sprite>(), binding.Keyframes, binding.Keyframes.Length, sheet.TotalFrames, ReplacementMode.FallbackSource, sheet.PossibleEmptyCount, "no-vfx-frames");
            }

            Sprite[] directionSprites = TakeDirectionSprites(sheet.Sprites, sourceClip.Direction, framesPerDirection);
            ObjectReferenceKeyframe[] keyframes = BuildObjectReferenceKeyframes(directionSprites, sourceClip.Clip.frameRate);
            report.Info($"{variant.VariantName}: regenerate VFX for {sourceClip.Clip.name} with {framesPerDirection} frames for {sourceClip.Direction} at {sourceClip.Clip.frameRate} fps");
            return new BindingReplacement(binding, spriteAssetPath, directionSprites, keyframes, framesPerDirection, sheet.TotalFrames, ReplacementMode.Replace, sheet.PossibleEmptyCount, sheet.Warning);
        }

        report.Error($"{variant.VariantName}: unsupported binding path {binding.Path} in {sourceClip.Clip.name}");
        return null;
    }

    private static bool HasRequiredBindingReplacements(ClipSpec sourceClip, List<BindingReplacement> replacements)
    {
        foreach (ClipBindingSpec binding in sourceClip.Bindings)
        {
            BindingReplacement replacement = replacements.FirstOrDefault(item => item.SourceBinding.Path == binding.Path);
            if (replacement == null)
            {
                return false;
            }

            if ((binding.Path == "BodyRenderer" || binding.Path == "ShadowRenderer") && !replacement.ShouldReplace)
            {
                return false;
            }
        }

        return true;
    }

    private static Sprite[] TakeDirectionSprites(IReadOnlyList<Sprite> sprites, string direction, int framesPerDirection)
    {
        int startIndex = GetDirectionIndex(direction) * framesPerDirection;
        return sprites.Skip(startIndex).Take(framesPerDirection).ToArray();
    }

    private static ObjectReferenceKeyframe[] BuildObjectReferenceKeyframes(IReadOnlyList<Sprite> directionSprites, float sampleRate)
    {
        var keyframes = new ObjectReferenceKeyframe[directionSprites.Count];
        for (int i = 0; i < directionSprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / sampleRate,
                value = directionSprites[i]
            };
        }

        return keyframes;
    }

    private static void UpdateClipDuration(AnimationClip targetClip, VariantClipPlan clipPlan)
    {
        float maxStopTime = 0f;
        foreach (BindingReplacement replacement in clipPlan.Replacements)
        {
            ObjectReferenceKeyframe[] keys = replacement.ShouldReplace ? replacement.TargetKeyframes : replacement.SourceBinding.Keyframes;
            if (keys.Length == 0)
            {
                continue;
            }

            float clipStopTime = keys.Length / targetClip.frameRate;
            if (clipStopTime > maxStopTime)
            {
                maxStopTime = clipStopTime;
            }
        }

        if (maxStopTime <= 0f)
        {
            return;
        }

        SerializedObject serializedClip = new SerializedObject(targetClip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty stopTime = settings?.FindPropertyRelative("m_StopTime");
        SerializedProperty startTime = settings?.FindPropertyRelative("m_StartTime");
        if (settings == null || stopTime == null || startTime == null)
        {
            return;
        }

        startTime.floatValue = 0f;
        stopTime.floatValue = maxStopTime;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool IsPossiblyEmptySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return true;
        }

        if (sprite.rect.width <= 1f || sprite.rect.height <= 1f)
        {
            return true;
        }

        return sprite.name.IndexOf("empty", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int GetDirectionIndex(string direction)
    {
        return direction switch
        {
            "Down" => 0,
            "Left" => 1,
            "Right" => 2,
            "Up" => 3,
            _ => throw new InvalidOperationException($"Unknown direction: {direction}")
        };
    }

    private static string GetControllerFolder(string controllerPath)
    {
        int slashIndex = controllerPath.LastIndexOf('/');
        return slashIndex >= 0 ? controllerPath.Substring(0, slashIndex) : controllerPath;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath.Replace('\\', '/'))?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        if (!string.IsNullOrEmpty(parent))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static AnimatorState FindSourceState(AnimatorController controller, string stateName)
    {
        ChildAnimatorState childState = controller.layers[0].stateMachine.states.FirstOrDefault(item => item.state.name == stateName);
        if (childState.state == null)
        {
            throw new InvalidOperationException($"Source controller is missing state {stateName}");
        }

        return childState.state;
    }

    private static void ValidateControllerContract(AnimatorController targetController, AnimatorController sourceController)
    {
        if (targetController.layers.Length != sourceController.layers.Length)
        {
            throw new InvalidOperationException("Layer count mismatch.");
        }

        for (int i = 0; i < ParameterNames.Length; i++)
        {
            AnimatorControllerParameter parameter = targetController.parameters.FirstOrDefault(item => item.name == ParameterNames[i]);
            if (parameter == null || parameter.type != ParameterTypes[i])
            {
                throw new InvalidOperationException($"Parameter mismatch for {ParameterNames[i]}.");
            }
        }

        HashSet<string> sourceStateNames = sourceController.layers[0].stateMachine.states
            .Select(item => item.state.name)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> targetStateNames = targetController.layers[0].stateMachine.states
            .Select(item => item.state.name)
            .ToHashSet(StringComparer.Ordinal);
        if (!sourceStateNames.SetEquals(targetStateNames))
        {
            throw new InvalidOperationException("State set mismatch.");
        }

        string sourceDefaultState = sourceController.layers[0].stateMachine.defaultState?.name;
        string targetDefaultState = targetController.layers[0].stateMachine.defaultState?.name;
        if (!string.Equals(sourceDefaultState, targetDefaultState, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Default state mismatch.");
        }
    }

    private static void SyncPrefabObjects(GameObject sourceRoot, GameObject targetRoot)
    {
        SyncGameObjectRecursive(sourceRoot.transform, targetRoot.transform);
    }

    private static void SyncGameObjectRecursive(Transform source, Transform target)
    {
        target.gameObject.layer = source.gameObject.layer;
        target.gameObject.tag = source.gameObject.tag;
        target.gameObject.SetActive(source.gameObject.activeSelf);
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        CopyComponentSet(source.gameObject, target.gameObject, typeof(Transform));

        if (source.childCount != target.childCount)
        {
            throw new InvalidOperationException($"Child count mismatch on {source.name}");
        }

        for (int i = 0; i < source.childCount; i++)
        {
            Transform sourceChild = source.GetChild(i);
            Transform targetChild = target.GetChild(i);
            if (!string.Equals(sourceChild.name, targetChild.name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Child name mismatch: {sourceChild.name} != {targetChild.name}");
            }

            SyncGameObjectRecursive(sourceChild, targetChild);
        }
    }

    private static void CopyComponentSet(GameObject sourceObject, GameObject targetObject, Type excludedType)
    {
        Component[] sourceComponents = sourceObject.GetComponents<Component>();
        Component[] targetComponents = targetObject.GetComponents<Component>();
        if (sourceComponents.Length != targetComponents.Length)
        {
            throw new InvalidOperationException($"Component count mismatch on {sourceObject.name}");
        }

        for (int i = 0; i < sourceComponents.Length; i++)
        {
            Component sourceComponent = sourceComponents[i];
            Component targetComponent = targetComponents[i];
            if (sourceComponent == null || targetComponent == null)
            {
                throw new InvalidOperationException($"Missing component on {sourceObject.name}");
            }

            if (sourceComponent.GetType() != targetComponent.GetType())
            {
                throw new InvalidOperationException($"Component type mismatch on {sourceObject.name}: {sourceComponent.GetType().Name} != {targetComponent.GetType().Name}");
            }

            if (sourceComponent.GetType() == excludedType)
            {
                continue;
            }

            EditorUtility.CopySerialized(sourceComponent, targetComponent);
        }
    }

    private static Sprite FindInitialSprite(VariantBuildPlan plan, string group, string direction, string bindingPath)
    {
        VariantClipPlan clipPlan = plan.Clips.First(item => item.Source.Group == group && item.Source.Direction == direction);
        BindingReplacement replacement = clipPlan.Replacements.First(item => item.SourceBinding.Path == bindingPath);
        return replacement.DirectionSprites[0];
    }

    private static SpriteRenderer FindRenderer(Transform root, string name)
    {
        Transform child = root.Find(name);
        if (child == null)
        {
            throw new InvalidOperationException($"Renderer child missing: {name}");
        }

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            throw new InvalidOperationException($"SpriteRenderer missing on child: {name}");
        }

        return renderer;
    }

    private static void ConfigureFlash(GameObject targetRoot, SpriteRenderer bodyRenderer)
    {
        Flash flash = targetRoot.GetComponent<Flash>();
        if (flash == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(flash);
        SerializedProperty targetRenderer = serialized.FindProperty("targetRenderer");
        if (targetRenderer != null)
        {
            targetRenderer.objectReferenceValue = bodyRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureDeathAnimation(GameObject targetRoot, SpriteRenderer bodyRenderer, SpriteRenderer shadowRenderer, SpriteRenderer vfxRenderer)
    {
        EnemyDeathAnimation deathAnimation = targetRoot.GetComponent<EnemyDeathAnimation>();
        if (deathAnimation == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(deathAnimation);
        SerializedProperty fadeRenderers = serialized.FindProperty("fadeRenderers");
        if (fadeRenderers != null)
        {
            var renderers = new List<SpriteRenderer> { bodyRenderer, shadowRenderer };
            if (vfxRenderer != null)
            {
                renderers.Add(vfxRenderer);
            }

            fadeRenderers.arraySize = renderers.Count;
            for (int i = 0; i < renderers.Count; i++)
            {
                fadeRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
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

    private static int CountMissingReferences(GameObject root, BuildReport report, string owner)
    {
        int total = 0;
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty iterator = serialized.GetIterator();
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
                report.Info($"{owner}: missing ref {component.GetType().Name}.{iterator.propertyPath}");
            }
        }

        return total;
    }

    private static bool GetLoopTime(AnimationClip clip)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        SerializedProperty loopTime = settings?.FindPropertyRelative("m_LoopTime");
        return loopTime != null && loopTime.boolValue;
    }

    private sealed class VariantSpec
    {
        public VariantSpec(string variantName, string spritePrefix, string spriteFolder, string prefabPath, string controllerPath)
        {
            VariantName = variantName;
            SpritePrefix = spritePrefix;
            SpriteFolder = spriteFolder;
            PrefabPath = prefabPath;
            ControllerPath = controllerPath;
        }

        public string VariantName { get; }
        public string SpritePrefix { get; }
        public string SpriteFolder { get; }
        public string PrefabPath { get; }
        public string ControllerPath { get; }
    }

    private sealed class ClipSpec
    {
        public ClipSpec(AnimationClip clip, string group, string direction, List<ClipBindingSpec> bindings, bool loops)
        {
            Clip = clip;
            Group = group;
            Direction = direction;
            Bindings = bindings;
            Loops = loops;
        }

        public AnimationClip Clip { get; }
        public string Group { get; }
        public string Direction { get; }
        public List<ClipBindingSpec> Bindings { get; }
        public bool Loops { get; }
    }

    private sealed class ClipBindingSpec
    {
        public ClipBindingSpec(EditorCurveBinding binding, ObjectReferenceKeyframe[] keyframes)
        {
            Binding = binding;
            Keyframes = keyframes;
        }

        public EditorCurveBinding Binding { get; }
        public ObjectReferenceKeyframe[] Keyframes { get; }
        public string Path => Binding.path;
    }

    private sealed class BindingReplacement
    {
        public BindingReplacement(
            ClipBindingSpec sourceBinding,
            string spriteAssetPath,
            Sprite[] directionSprites,
            ObjectReferenceKeyframe[] targetKeyframes,
            int framesPerDirection,
            int totalFrames,
            ReplacementMode mode,
            int possibleEmptyCount,
            string note)
        {
            SourceBinding = sourceBinding;
            SpriteAssetPath = spriteAssetPath;
            DirectionSprites = directionSprites;
            TargetKeyframes = targetKeyframes;
            FramesPerDirection = framesPerDirection;
            TotalFrames = totalFrames;
            Mode = mode;
            PossibleEmptyCount = possibleEmptyCount;
            Note = note;
        }

        public ClipBindingSpec SourceBinding { get; }
        public string SpriteAssetPath { get; }
        public Sprite[] DirectionSprites { get; }
        public ObjectReferenceKeyframe[] TargetKeyframes { get; }
        public int FramesPerDirection { get; }
        public int TotalFrames { get; }
        public ReplacementMode Mode { get; }
        public int PossibleEmptyCount { get; }
        public string Note { get; }
        public bool ShouldReplace => Mode == ReplacementMode.Replace;
    }

    private sealed class SpriteSheetInfo
    {
        public SpriteSheetInfo(string assetPath, Sprite[] sprites, int possibleEmptyCount, string warning)
        {
            AssetPath = assetPath;
            Sprites = sprites;
            PossibleEmptyCount = possibleEmptyCount;
            Warning = warning;
        }

        public string AssetPath { get; }
        public Sprite[] Sprites { get; }
        public int TotalFrames => Sprites.Length;
        public int FramesPerDirection => TotalFrames % 4 == 0 ? TotalFrames / 4 : 0;
        public int PossibleEmptyCount { get; }
        public string Warning { get; }
    }

    private enum ReplacementMode
    {
        Replace,
        FallbackSource
    }

    private sealed class VariantClipPlan
    {
        public VariantClipPlan(ClipSpec source, string targetClipName, string targetClipPath, bool exists, List<BindingReplacement> replacements)
        {
            Source = source;
            TargetClipName = targetClipName;
            TargetClipPath = targetClipPath;
            Exists = exists;
            Replacements = replacements;
        }

        public ClipSpec Source { get; }
        public string TargetClipName { get; }
        public string TargetClipPath { get; }
        public bool Exists { get; }
        public List<BindingReplacement> Replacements { get; }
    }

    private sealed class VariantBuildPlan
    {
        public VariantBuildPlan(VariantSpec variant, bool prefabExists, bool controllerExists, List<VariantClipPlan> clips)
        {
            Variant = variant;
            PrefabExists = prefabExists;
            ControllerExists = controllerExists;
            Clips = clips;
        }

        public VariantSpec Variant { get; }
        public bool PrefabExists { get; }
        public bool ControllerExists { get; }
        public List<VariantClipPlan> Clips { get; }
    }

    private sealed class PrefabShape
    {
        private PrefabShape(string rootName, List<string> childNames, List<string> rootComponentTypes, Dictionary<string, List<string>> componentMap)
        {
            RootName = rootName;
            ChildNames = childNames;
            RootComponentTypes = rootComponentTypes;
            ComponentMap = componentMap;
        }

        public string RootName { get; }
        public List<string> ChildNames { get; }
        public List<string> RootComponentTypes { get; }
        public Dictionary<string, List<string>> ComponentMap { get; }

        public static PrefabShape FromLoadedPrefab(GameObject root)
        {
            var childNames = new List<string>();
            var componentMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (Transform child in root.transform)
            {
                childNames.Add(child.name);
            }

            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                string path = GetTransformPath(root.transform, transform);
                componentMap[path] = transform.gameObject.GetComponents<Component>()
                    .Select(component => component?.GetType().FullName ?? "<missing>")
                    .ToList();
            }

            return new PrefabShape(
                root.name,
                childNames,
                componentMap[string.Empty],
                componentMap);
        }

        public bool Matches(PrefabShape other)
        {
            if (other == null)
            {
                return false;
            }

            if (ComponentMap.Count != other.ComponentMap.Count)
            {
                return false;
            }

            foreach ((string path, List<string> components) in ComponentMap)
            {
                if (!other.ComponentMap.TryGetValue(path, out List<string> otherComponents))
                {
                    return false;
                }

                if (components.Count != otherComponents.Count)
                {
                    return false;
                }

                for (int i = 0; i < components.Count; i++)
                {
                    if (!string.Equals(components[i], otherComponents[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            return ChildNames.SequenceEqual(other.ChildNames, StringComparer.Ordinal);
        }

        private static string GetTransformPath(Transform root, Transform current)
        {
            if (current == root)
            {
                return string.Empty;
            }

            var parts = new Stack<string>();
            Transform walker = current;
            while (walker != null && walker != root)
            {
                parts.Push(walker.name);
                walker = walker.parent;
            }

            return string.Join("/", parts);
        }
    }

    private sealed class BuildReport
    {
        private readonly List<string> _infos = new List<string>();
        private readonly List<string> _changed = new List<string>();
        private readonly List<string> _errors = new List<string>();

        public bool Success => _errors.Count == 0;

        public void Info(string line) => _infos.Add(line);
        public void Changed(string path)
        {
            if (!_changed.Any(item => string.Equals(item, path, StringComparison.Ordinal)))
            {
                _changed.Add(path);
            }
        }

        public void Error(string line) => _errors.Add(line);

        public string ToText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Slime Variant Builder Report");
            builder.AppendLine("INFO:");
            foreach (string line in _infos)
            {
                builder.AppendLine($"- {line}");
            }

            builder.AppendLine("CHANGED FILES:");
            if (_changed.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (string path in _changed.OrderBy(item => item, StringComparer.Ordinal))
                {
                    builder.AppendLine($"- {path}");
                }
            }

            builder.AppendLine("ERRORS:");
            if (_errors.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                foreach (string line in _errors)
                {
                    builder.AppendLine($"- {line}");
                }
            }

            builder.AppendLine(Success ? "VALIDATION: PASS" : "VALIDATION: FAIL");
            return builder.ToString();
        }
    }

    private sealed class SpriteNameComparer : IComparer<string>
    {
        public static readonly SpriteNameComparer Instance = new SpriteNameComparer();
        private static readonly Regex SuffixRegex = new Regex(@"^(.*?)(\d+)$", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            Match xMatch = SuffixRegex.Match(x);
            Match yMatch = SuffixRegex.Match(y);
            if (xMatch.Success && yMatch.Success &&
                string.Equals(xMatch.Groups[1].Value, yMatch.Groups[1].Value, StringComparison.Ordinal))
            {
                int xNumber = int.Parse(xMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                int yNumber = int.Parse(yMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                return xNumber.CompareTo(yNumber);
            }

            return string.CompareOrdinal(x, y);
        }
    }
}
