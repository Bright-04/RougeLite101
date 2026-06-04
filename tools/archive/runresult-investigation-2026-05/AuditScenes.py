import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Audit] {msg}")

def dump(obj, limit=500):
    s = json.dumps(obj, indent=2)
    return s[:limit]

# 1. Ensure editor is not in play mode
state = unity_skills.call_skill('editor_get_state')
if state.get('isPlaying'):
    log('Editor is playing, stopping...')
    unity_skills.call_skill('editor_stop')
else:
    log('Editor already stopped')

# Helper to load a scene and perform checks
def audit_scene(scene_path, scene_name):
    log(f"--- Auditing scene {scene_name} ({scene_path}) ---")
    # Load scene (non-additive)
    load_res = unity_skills.call_skill('scene_load', scenePath=scene_path, additive=False)
    log(f"scene_load result: {dump(load_res)}")
    # Compile check
    compile_res = unity_skills.call_skill('debug_check_compilation')
    log(f"compile check: {dump(compile_res)}")
    # Query RunResultController components
    rr = unity_skills.call_skill('gameobject_find', component='RunResultController')
    log(f"RunResultController count: {rr.get('count',0)}")
    for obj in rr.get('objects', []):
        log(f"RRC object: name={obj.get('name')}, id={obj.get('instanceId')}, path={obj.get('path')}")
        # Get its components list
        comps = unity_skills.call_skill('gameobject_get_components', instanceId=obj['instanceId'])
        log(f"Components: {[c['type'] for c in comps.get('components',[])]}")
    # Query EndGameResultUI components
    ui = unity_skills.call_skill('gameobject_find', component='EndGameResultUI')
    log(f"EndGameResultUI count: {ui.get('count',0)}")
    for obj in ui.get('objects', []):
        log(f"EndGameResultUI object: name={obj.get('name')}, id={obj.get('instanceId')}, path={obj.get('path')}")
    # Query Canvas_UI object
    canvas = unity_skills.call_skill('gameobject_find', name='Canvas_UI')
    log(f"Canvas_UI found: {canvas.get('count',0)}")
    if canvas.get('count',0):
        cobj = canvas['objects'][0]
        cid = cobj['instanceId']
        comps = unity_skills.call_skill('gameobject_get_components', instanceId=cid)
        log(f"Canvas_UI components: {[c['type'] for c in comps.get('components',[])]}")
        # Check if Canvas_UI is a prefab instance
        info = unity_skills.call_skill('gameobject_get_info', instanceId=cid)
        prefab = info.get('prefabParent')
        log(f"Canvas_UI prefabParent: {prefab}")
    # Query GameManager object
    gm = unity_skills.call_skill('gameobject_find', name='GameManager')
    log(f"GameManager found: {gm.get('count',0)}")
    if gm.get('count',0):
        gobj = gm['objects'][0]
        gid = gobj['instanceId']
        comps = unity_skills.call_skill('gameobject_get_components', instanceId=gid)
        log(f"GameManager components: {[c['type'] for c in comps.get('components',[])]}")
        info = unity_skills.call_skill('gameobject_get_info', instanceId=gid)
        log(f"GameManager prefabParent: {info.get('prefabParent')}")
    # Save scene to ensure state persisted (optional)
    unity_skills.call_skill('scene_save')
    log(f"--- Finished audit for {scene_name} ---\n")

# Audit GameHome
audit_scene('Assets/Scenes/GameHome.unity', 'GameHome')
# Audit Dungeon (assuming path)
audit_scene('Assets/Scenes/Dungeon.unity', 'Dungeon')

print('[Audit] All done')
