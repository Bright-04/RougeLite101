import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Modify] {msg}")

def ensure_stopped():
    state = unity_skills.call_skill('editor_get_state')
    if state.get('isPlaying'):
        log('Editor playing, stopping...')
        unity_skills.call_skill('editor_stop')
    else:
        log('Editor already stopped')

def modify_scene(scene_path, scene_name):
    log(f"--- Modifying {scene_name} ({scene_path}) ---")
    # Load scene
    load = unity_skills.call_skill('scene_load', scenePath=scene_path, additive=False)
    log(f"scene_load: {json.dumps(load)}")
    # Ensure stopped
    ensure_stopped()
    # Find Canvas_UI and GameManager
    canvas_res = unity_skills.call_skill('gameobject_find', name='Canvas_UI')
    gm_res = unity_skills.call_skill('gameobject_find', name='GameManager')
    if canvas_res.get('count',0)==0 or gm_res.get('count',0)==0:
        log('Missing Canvas_UI or GameManager, abort')
        return
    canvas_id = canvas_res['objects'][0]['instanceId']
    gm_id = gm_res['objects'][0]['instanceId']
    # Remove RunResultController from Canvas_UI if present
    comps = unity_skills.call_skill('component_list', instanceId=canvas_id)
    if 'RunResultController' in comps.get('components',[]):
        rem = unity_skills.call_skill('component_remove', instanceId=canvas_id, componentType='RunResultController')
        log(f"Removed from Canvas_UI: {rem}")
    else:
        log('Canvas_UI already has no RunResultController')
    # Add RunResultController to GameManager if missing
    comps_gm = unity_skills.call_skill('component_list', instanceId=gm_id)
    if 'RunResultController' not in comps_gm.get('components',[]):
        add = unity_skills.call_skill('component_add', instanceId=gm_id, componentType='RunResultController')
        log(f"Added to GameManager: {add}")
    else:
        log('GameManager already has RunResultController')
    # Wire resultUI property
    # Find EndGameResultUI component on Canvas_UI/RunResultOverlay
    endui_res = unity_skills.call_skill('gameobject_find', component='EndGameResultUI')
    # Should be one; get its GameObject path
    if endui_res.get('count',0)==0:
        log('EndGameResultUI not found')
    else:
        endui_path = endui_res['objects'][0]['path']  # e.g., Canvas_UI/RunResultOverlay
        # Set property on GameManager's RunResultController
        setprop = unity_skills.call_skill('component_set_property', instanceId=gm_id, componentType='RunResultController', propertyName='resultUI', referencePath=endui_path)
        log(f"Set resultUI reference: {setprop}")
    # Save scene
    unity_skills.call_skill('scene_save')
    log(f"--- Finished modification for {scene_name} ---\n")

# Apply to both scenes
modify_scene('Assets/Scenes/GameHome.unity', 'GameHome')
modify_scene('Assets/Scenes/Dungeon.unity', 'Dungeon')
print('[Modify] All done')
