import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Automation] {msg}")

def wire_scene(scene_path, scene_name):
    log(f"Loading {scene_name} for wiring")
    load = unity_skills.call_skill('scene_load', scenePath=scene_path, additive=False)
    log(f"scene_load: {load}")
    # Execute the temporary wiring menu item
    exec_res = unity_skills.call_skill('editor_execute_menu', menuPath='Tools/Temp/Wire RunResultController')
    log(f"execute wiring menu: {exec_res}")
    # Save scene explicitly
    save = unity_skills.call_skill('scene_save')
    log(f"scene_save: {save}")

def validate_lose_flow(scene_path, scene_name):
    log(f"Loading {scene_name} for validation")
    load = unity_skills.call_skill('scene_load', scenePath=scene_path, additive=False)
    log(f"scene_load: {load}")
    # Ensure compile is clean
    comp = unity_skills.call_skill('debug_check_compilation')
    log(f"compilation check: {comp}")
    # Enter Play mode
    play = unity_skills.call_skill('editor_play')
    log(f"enter play mode: {play}")
    # Run the fatal damage validator menu
    validator = unity_skills.call_skill('editor_execute_menu', menuPath='Tools/Validation/Result Flow/Apply Fatal Player Damage')
    log(f"run validator: {validator}")
    # Give a short frame for UI updates (no real wait needed)
    # Query RunResultController properties
    rrc = unity_skills.call_skill('gameobject_find', component='RunResultController')
    if rrc.get('count',0)==0:
        log('RunResultController not found after play')
        return
    rrc_id = rrc['objects'][0]['instanceId']
    props = unity_skills.call_skill('component_get_properties', instanceId=rrc_id, componentType='RunResultController')
    log(f"RunResultController properties: {props.get('properties',{})}")
    # Query EndGameResultUI IsConfigured
    endui = unity_skills.call_skill('gameobject_find', component='EndGameResultUI')
    if endui.get('count',0):
        endui_id = endui['objects'][0]['instanceId']
        end_props = unity_skills.call_skill('component_get_properties', instanceId=endui_id, componentType='EndGameResultUI')
        log(f"EndGameResultUI properties: {end_props.get('properties',{})}")
    # Exit Play mode
    stop = unity_skills.call_skill('editor_stop')
    log(f"exit play mode: {stop}")

# Process both scenes
wire_scene('Assets/Scenes/GameHome.unity', 'GameHome')
wire_scene('Assets/Scenes/Dungeon.unity', 'Dungeon')
# Validate lose flow in each scene
validate_lose_flow('Assets/Scenes/GameHome.unity', 'GameHome')
validate_lose_flow('Assets/Scenes/Dungeon.unity', 'Dungeon')

# Cleanup temporary scripts
for path in [
    'Assets/Editor/TempTools/WireRunResultController.cs',
    'Assets/Editor/TempValidation/ApplyFatalDamageValidator.cs'
]:
    full = os.path.join('e:/Repositories/RougeLite101', path)
    if os.path.exists(full):
        os.remove(full)
        log(f"Deleted temporary script {path}")
    else:
        log(f"Temporary script {path} not found for deletion")

print('[Automation] Done')
