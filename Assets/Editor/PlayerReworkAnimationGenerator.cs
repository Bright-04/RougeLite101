using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerReworkAnimationGenerator
{
    private const string ReworkFolder = "Assets/Animation/Player/Rework";
    private const string ControllerPath = "Assets/Resources/Player.controller";
    private const string SwordOverrideControllerPath = "Assets/Resources/Player_Sword.overrideController";
    private const string BowOverrideControllerPath = "Assets/Resources/Player_Bow.overrideController";
    private const string IdleSheetPath = ReworkFolder + "/adventurer_idle.png";
    private const string WalkSheetPath = ReworkFolder + "/adventurer_walk.png";
    private const string CastSheetPath = ReworkFolder + "/adventurer_cast.png";
    private const string SwordIdleSheetPath = ReworkFolder + "/Sword_idle.png";
    private const string SwordWalkSheetPath = ReworkFolder + "/Sword_walk.png";
    private const string SwordCastSheetPath = ReworkFolder + "/Sword_cast.png";
    private const string SwordSlashSheetPath = ReworkFolder + "/Sword_slash.png";
    private const string BowIdleSheetPath = ReworkFolder + "/bow_idle.png";
    private const string BowWalkSheetPath = ReworkFolder + "/bow_walk.png";
    private const string BowCastSheetPath = ReworkFolder + "/bow_cast.png";
    private const string BowShootSheetPath = ReworkFolder + "/bow_shoot.png";
    private const float IdleFrameRate = 4f;
    private const float WalkFrameRate = 12f;
    private const float CastFrameRate = 12f;
    private const float AttackFrameRate = 12f;
    private const float ShootFrameRate = 12f;

    private static readonly Direction[] Directions =
    {
        Direction.Up,
        Direction.Right,
        Direction.Left,
        Direction.Down
    };

    private enum Direction
    {
        Up,
        Right,
        Left,
        Down
    }

    [MenuItem("Tools/Player/Generate Rework Animator")]
    public static void Generate()
    {
        Directory.CreateDirectory(ReworkFolder);

        Sprite[] idleSprites = LoadSprites(IdleSheetPath);
        Sprite[] walkSprites = LoadSprites(WalkSheetPath);
        Sprite[] castSprites = LoadSprites(CastSheetPath);

        AnimationClip[] idleClips = CreateDirectionalClips("Player_Rework_Idle", idleSprites, 2, IdleFrameRate, true);
        AnimationClip[] walkClips = CreateDirectionalClips("Player_Rework_Walk", walkSprites, 8, WalkFrameRate, true);
        AnimationClip[] castClips = CreateDirectionalClips("Player_Rework_Cast", castSprites, 6, CastFrameRate, false);
        AnimationClip[] attackClips = CreateDirectionalClips("Player_Rework_Attack", castSprites, 6, AttackFrameRate, false);
        AnimationClip[] shootClips = CreateDirectionalClips("Player_Rework_Shoot", castSprites, 6, ShootFrameRate, false);

        AnimatorController controller = CreateController();
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = Array.Empty<ChildAnimatorState>();
        stateMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300f, 120f, 0f));
        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(300f, 260f, 0f));
        AnimatorState castState = stateMachine.AddState("Cast", new Vector3(560f, 120f, 0f));
        AnimatorState attackState = stateMachine.AddState("Attack", new Vector3(560f, 260f, 0f));
        AnimatorState shootState = stateMachine.AddState("Shoot", new Vector3(820f, 260f, 0f));

        idleState.motion = CreateDirectionalBlendTree(controller, "BT_Idle", idleClips);
        walkState.motion = CreateDirectionalBlendTree(controller, "BT_Walk", walkClips);
        castState.motion = CreateDirectionalBlendTree(controller, "BT_Cast", castClips);
        attackState.motion = CreateDirectionalBlendTree(controller, "BT_Attack", attackClips);
        shootState.motion = CreateDirectionalBlendTree(controller, "BT_Shoot", shootClips);
        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");

        AnimatorStateTransition anyToCast = stateMachine.AddAnyStateTransition(castState);
        anyToCast.hasExitTime = false;
        anyToCast.duration = 0f;
        anyToCast.canTransitionToSelf = false;
        anyToCast.AddCondition(AnimatorConditionMode.If, 0f, "Cast");

        AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0f;
        anyToAttack.canTransitionToSelf = false;
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition anyToShoot = stateMachine.AddAnyStateTransition(shootState);
        anyToShoot.hasExitTime = false;
        anyToShoot.duration = 0f;
        anyToShoot.canTransitionToSelf = false;
        anyToShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

        AnimatorStateTransition castToIdle = castState.AddTransition(idleState);
        castToIdle.hasExitTime = true;
        castToIdle.exitTime = 1f;
        castToIdle.duration = 0f;
        castToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");

        AnimatorStateTransition castToWalk = castState.AddTransition(walkState);
        castToWalk.hasExitTime = true;
        castToWalk.exitTime = 1f;
        castToWalk.duration = 0f;
        castToWalk.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0f;
        attackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");

        AnimatorStateTransition attackToWalk = attackState.AddTransition(walkState);
        attackToWalk.hasExitTime = true;
        attackToWalk.exitTime = 1f;
        attackToWalk.duration = 0f;
        attackToWalk.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");

        AnimatorStateTransition shootToIdle = shootState.AddTransition(idleState);
        shootToIdle.hasExitTime = true;
        shootToIdle.exitTime = 1f;
        shootToIdle.duration = 0f;
        shootToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");

        AnimatorStateTransition shootToWalk = shootState.AddTransition(walkState);
        shootToWalk.hasExitTime = true;
        shootToWalk.exitTime = 1f;
        shootToWalk.duration = 0f;
        shootToWalk.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");

        CreateSwordOverrideController(controller, idleClips, walkClips, castClips, attackClips);
        CreateBowOverrideController(controller, idleClips, walkClips, castClips, shootClips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated player rework clips, Player.controller, and weapon override controllers.");
    }

    private static AnimatorController CreateController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("moveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("moveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("lookX", AnimatorControllerParameterType.Float);
        controller.AddParameter("lookY", AnimatorControllerParameterType.Float);
        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        return controller;
    }

    private static BlendTree CreateDirectionalBlendTree(AnimatorController controller, string name, Motion[] clips)
    {
        if (clips.Length != Directions.Length)
        {
            throw new InvalidOperationException($"{name} expected {Directions.Length} clips but found {clips.Length}.");
        }

        BlendTree blendTree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "lookX",
            blendParameterY = "lookY",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);
        blendTree.AddChild(clips[0], Vector2.up);
        blendTree.AddChild(clips[1], Vector2.right);
        blendTree.AddChild(clips[2], Vector2.left);
        blendTree.AddChild(clips[3], Vector2.down);
        return blendTree;
    }

    private static AnimationClip[] CreateDirectionalClips(
        string clipPrefix,
        Sprite[] sprites,
        int framesPerDirection,
        float frameRate,
        bool loop)
    {
        return Directions
            .Select(direction => CreateDirectionalClip($"{clipPrefix}_{direction}", sprites, direction, framesPerDirection, frameRate, loop))
            .ToArray();
    }

    private static AnimationClip CreateDirectionalClip(
        string clipName,
        Sprite[] sprites,
        Direction direction,
        int framesPerDirection,
        float frameRate,
        bool loop)
    {
        int row = RowForDirection(direction);
        Sprite[] frames = sprites
            .Skip(row * framesPerDirection)
            .Take(framesPerDirection)
            .ToArray();

        if (frames.Length != framesPerDirection)
        {
            throw new InvalidOperationException($"{clipName} expected {framesPerDirection} frames but found {frames.Length}.");
        }

        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            frameRate = frameRate
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            },
            keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.startTime = 0f;
        settings.stopTime = frames.Length / frameRate;
        settings.loopTime = loop;
        settings.keepOriginalPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = $"{ReworkFolder}/{clipName}.anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
        {
            AssetDatabase.DeleteAsset(clipPath);
        }

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static Sprite[] LoadSprites(string path)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => ExtractNumericSuffix(sprite.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            throw new InvalidOperationException($"No sliced sprites found at {path}.");
        }

        return sprites;
    }

    private static void CreateSwordOverrideController(
        AnimatorController baseController,
        AnimationClip[] baseIdleClips,
        AnimationClip[] baseWalkClips,
        AnimationClip[] baseCastClips,
        AnimationClip[] baseAttackClips)
    {
        Sprite[] swordIdleSprites = LoadSprites(SwordIdleSheetPath);
        Sprite[] swordWalkSprites = LoadSprites(SwordWalkSheetPath);
        Sprite[] swordCastSprites = LoadSprites(SwordCastSheetPath);
        Sprite[] swordSlashSprites = LoadSprites(SwordSlashSheetPath);

        AnimationClip[] swordIdleClips = CreateDirectionalClips("Player_Sword_Idle", swordIdleSprites, 2, IdleFrameRate, true);
        AnimationClip[] swordWalkClips = CreateDirectionalClips("Player_Sword_Walk", swordWalkSprites, 8, WalkFrameRate, true);
        AnimationClip[] swordCastClips = CreateDirectionalClips("Player_Sword_Cast", swordCastSprites, 6, CastFrameRate, false);
        AnimationClip[] swordAttackClips = CreateDirectionalClips("Player_Sword_Attack", swordSlashSprites, 5, AttackFrameRate, false);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController)
        {
            name = "Player_Sword"
        };

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        ApplyDirectionalOverrides(overrides, baseIdleClips, swordIdleClips);
        ApplyDirectionalOverrides(overrides, baseWalkClips, swordWalkClips);
        ApplyDirectionalOverrides(overrides, baseCastClips, swordCastClips);
        ApplyDirectionalOverrides(overrides, baseAttackClips, swordAttackClips);
        overrideController.ApplyOverrides(overrides);

        if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(SwordOverrideControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(SwordOverrideControllerPath);
        }

        AssetDatabase.CreateAsset(overrideController, SwordOverrideControllerPath);
    }

    private static void CreateBowOverrideController(
        AnimatorController baseController,
        AnimationClip[] baseIdleClips,
        AnimationClip[] baseWalkClips,
        AnimationClip[] baseCastClips,
        AnimationClip[] baseShootClips)
    {
        Sprite[] bowIdleSprites = LoadSprites(BowIdleSheetPath);
        Sprite[] bowWalkSprites = LoadSprites(BowWalkSheetPath);
        Sprite[] bowCastSprites = LoadSprites(BowCastSheetPath);
        Sprite[] bowShootSprites = LoadSprites(BowShootSheetPath);

        AnimationClip[] bowIdleClips = CreateDirectionalClips("Player_Bow_Idle", bowIdleSprites, 2, IdleFrameRate, true);
        AnimationClip[] bowWalkClips = CreateDirectionalClips("Player_Bow_Walk", bowWalkSprites, 8, WalkFrameRate, true);
        AnimationClip[] bowCastClips = CreateDirectionalClips("Player_Bow_Cast", bowCastSprites, 6, CastFrameRate, false);
        AnimationClip[] bowShootClips = CreateDirectionalClips("Player_Bow_Shoot", bowShootSprites, 12, ShootFrameRate, false);

        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController)
        {
            name = "Player_Bow"
        };

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        ApplyDirectionalOverrides(overrides, baseIdleClips, bowIdleClips);
        ApplyDirectionalOverrides(overrides, baseWalkClips, bowWalkClips);
        ApplyDirectionalOverrides(overrides, baseCastClips, bowCastClips);
        ApplyDirectionalOverrides(overrides, baseShootClips, bowShootClips);
        overrideController.ApplyOverrides(overrides);

        if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(BowOverrideControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(BowOverrideControllerPath);
        }

        AssetDatabase.CreateAsset(overrideController, BowOverrideControllerPath);
    }

    private static void ApplyDirectionalOverrides(
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides,
        AnimationClip[] baseClips,
        AnimationClip[] replacementClips)
    {
        if (baseClips.Length != replacementClips.Length)
        {
            throw new InvalidOperationException("Base and replacement clip arrays must have the same length.");
        }

        for (int i = 0; i < baseClips.Length; i++)
        {
            int overrideIndex = overrides.FindIndex(item => item.Key == baseClips[i]);
            if (overrideIndex < 0)
            {
                throw new InvalidOperationException($"Could not find override slot for {baseClips[i].name}.");
            }

            overrides[overrideIndex] = new KeyValuePair<AnimationClip, AnimationClip>(baseClips[i], replacementClips[i]);
        }
    }

    private static int ExtractNumericSuffix(string spriteName)
    {
        int separator = spriteName.LastIndexOf('_');
        return separator >= 0 && int.TryParse(spriteName.Substring(separator + 1), out int number)
            ? number
            : 0;
    }

    private static int RowForDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return 0;
            case Direction.Right:
                return 3;
            case Direction.Left:
                return 1;
            case Direction.Down:
                return 2;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
