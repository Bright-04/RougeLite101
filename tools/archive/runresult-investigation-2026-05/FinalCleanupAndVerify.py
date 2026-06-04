import sys, os, json
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Final] {msg}")

def delete_file(rel_path):
    full = os.path.join('e:/Repositories/RougeLite101', rel_path)
    if os.path.exists(full):
        os.remove(full)
        log(f"Deleted {rel_path}")
    else:
        log(f"File {rel_path} not found")

def list_dir_check(path):
    res = unity_skills.call_skill('list_dir', DirectoryPath=path)
    log(f"Contents of {path}: {res.get('children', [])}")

# 1. Delete automation script
delete_file('AutomateFixAndValidate.py')

# 2. Ensure temp directories are empty
list_dir_check('Assets/Editor/TempTools')
list_dir_check('Assets/Editor/TempValidation')

# Helper to audit a scene
def audit_scene(scene_path, scene_name):
    log(f"--- Auditing {scene_name}")
    unity_skills.call_skill('scene_load', scenePath=scene_path, additive=False)
    # Compile check
    comp = unity_skills.call_skill('debug_check_compilation')
    log(f"Compilation: {comp}")
    # Find RunResultController
    rrc = unity_skills.call_skill('gameobject_find', component='RunResultController')
    log(f"RunResultController count: {rrc.get('count',0)}")
    if rrc.get('count',0):
        rrc_obj = rrc['objects'][0]
        log(f"RRC on: {rrc_obj.get('path')}")
        # Get its resultUI field
        props = unity_skills.call_skill('component_get_properties', instanceId=rrc_obj['instanceId'], componentType='RunResultController')
        result_ui = next((p for p in props.get('properties',[]) if p['name']=='resultUI'), None)
        log(f"resultUI reference: {result_ui.get('value') if result_ui else 'None'}")
    # Find EndGameResultUI
    endui = unity_skills.call_skill('gameobject_find', component='EndGameResultUI')
    log(f"EndGameResultUI count: {endui.get('count',0)}")
    if endui.get('count',0):
        end_obj = endui['objects'][0]
        log(f"EndGameResultUI path: {end_obj.get('path')}")
        props_end = unity_skills.call_skill('component_get_properties', instanceId=end_obj['instanceId'], componentType='EndGameResultUI')
        cfg = next((p for p in props_end.get('properties',[]) if p['name']=='IsConfigured'), None)
        log(f"IsConfigured: {cfg.get('value') if cfg else 'N/A'}")
    # Verify Canvas_UI has no RunResultController
    canvas = unity_skills.call_skill('gameobject_find', name='Canvas_UI')
    if canvas.get('count',0):
        cid = canvas['objects'][0]['instanceId']
        comps = unity_skills.call_skill('component_list', instanceId=cid)
        has = 'RunResultController' in comps.get('components',[])
        log(f"Canvas_UI has RunResultController: {has}")
    else:
        log("Canvas_UI not found")
    # Verify GameManager has RunResultController
    gm = unity_skills.call_skill('gameobject_find', name='GameManager')
    if gm.get('count',0):
        gid = gm['objects'][0]['instanceId']
        comps_gm = unity_skills.call_skill('component_list', instanceId=gid)
        has_gm = 'RunResultController' in comps_gm.get('components',[])
        log(f"GameManager has RunResultController: {has_gm}")
    else:
        log('GameManager not found')
    log(f"--- End audit {scene_name} ---\n")

# 3. Re‑audit both scenes
audit_scene('Assets/Scenes/GameHome.unity', 'GameHome')
audit_scene('Assets/Scenes/Dungeon.unity', 'Dungeon')

# 4. Create temporary validator script (will be deleted later)
validator_path = 'Assets/Editor/TempValidation/ApplyFatalDamageValidator.cs'
validator_code = '''using UnityEngine;
using UnityEditor;

public static class ApplyFatalDamageValidator
{
    [MenuItem("Tools/Validation/Result Flow/Apply Fatal Player Damage")]
    public static void ApplyFatalDamage()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogError("[ResultFlow] Player not found"); return; }
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) { Debug.LogError("[ResultFlow] PlayerStats missing"); return; }
        stats.TakeDamage(9999f);
        Debug.Log("[ResultFlow] Applied fatal damage");
    }
}
'''
# Write the file
full_path = os.path.join('e:/Repositories/RougeLite101', validator_path)
os.makedirs(os.path.dirname(full_path), exist_ok=True)
with open(full_path, 'w', encoding='utf-8') as f:
    f.write(validator_code)
log('Created temporary validator script')

# Helper to run lose flow validation for a scene
def validate_lose_flow(scene_path, scene_name):
    log(f"--- Lose flow validation {scene_name} ---")
    unity_skills.call_skill('scene_load', scenePath=scene_path, additive=False)
    unity_skills.call_skill('editor_play')
    # Execute validator menu
    exec_res = unity_skills.call_skill('editor_execute_menu', menuPath='Tools/Validation/Result Flow/Apply Fatal Player Damage')
    log(f"Validator executed: {exec_res}")
    # Query RunResultController
    rrc = unity_skills.call_skill('gameobject_find', component='RunResultController')
    if rrc.get('count',0):
        rrc_id = rrc['objects'][0]['instanceId']
        props = unity_skills.call_skill('component_get_properties', instanceId=rrc_id, componentType='RunResultController')
        act = next((p for p in props.get('properties',[]) if p['name']=='IsResultActive'), None)
        fin = next((p for p in props.get('properties',[]) if p['name']=='IsRunFinished'), None)
        log(f"IsResultActive: {act.get('value') if act else 'N/A'}")
        log(f"IsRunFinished: {fin.get('value') if fin else 'N/A'}")
    else:
        log('RunResultController not found during validation')
    unity_skills.call_skill('editor_stop')
    log(f"--- End validation {scene_name} ---\n")

validate_lose_flow('Assets/Scenes/GameHome.unity', 'GameHome')
validate_lose_flow('Assets/Scenes/Dungeon.unity', 'Dungeon')

# 5. Delete temporary validator script
delete_file('Assets/Editor/TempValidation/ApplyFatalDamageValidator.cs')

# 6. Final compile and console checks (editor mode)
comp_final = unity_skills.call_skill('debug_check_compilation')
log(f"Final compilation check: {comp_final}")
console_logs = unity_skills.call_skill('console_get_logs', logLevel='Error')
log(f"Console error logs: {len(console_logs.get('logs',[]))} entries")

log('Final verification completed')
''
