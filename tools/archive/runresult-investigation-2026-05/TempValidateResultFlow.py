import sys, time, json, os
# Add UnitySkills client library path
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[ResultFlowValidation] {msg}")

# Ensure editor is not playing, then start play mode
def ensure_play_mode():
    state = unity_skills.call_skill('editor_get_state')
    if not state.get('isPlaying'):
        unity_skills.call_skill('editor_play')
        time.sleep(2)  # give Unity time to enter play mode
    else:
        log('Already in Play Mode')

# Execute the validation menu command
def apply_fatal_damage():
    unity_skills.call_skill('editor_execute_menu', menuPath='Tools/Validation/Result Flow/Apply Fatal Player Damage')
    time.sleep(1)  # allow damage processing

    # After damage, query RunResultController details
    rr_props = query_run_result_controller()
    log(f"[ResultFlowDebug] RunResultController properties: {json.dumps(rr_props)}")
    # Inspect RunResultController.resultUI field
    result_controller_info = unity_skills.call_skill('gameobject_find', component='RunResultController')
    if result_controller_info['count'] > 0:
        obj = result_controller_info['objects'][0]
        rc_props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='RunResultController')
        rc_dict = {p['name']: p['value'] for p in rc_props.get('properties', [])}
        result_ui_ref = rc_dict.get('resultUI')
        log(f"[ResultFlowDebug] RunResultController.resultUI reference: {result_ui_ref}")
        if result_ui_ref:
            # result_ui_ref may be an object ID string; attempt to get its properties
            # Assuming we can query by object name if needed; here we try to find EndGameResultUI by type
            ui_find = unity_skills.call_skill('gameobject_find', component='EndGameResultUI')
            if ui_find['count'] > 0:
                ui_obj = ui_find['objects'][0]
                ui_props = unity_skills.call_skill('component_get_properties', instanceId=ui_obj['instanceId'], componentType='EndGameResultUI')
                ui_dict = {p['name']: p['value'] for p in ui_props.get('properties', [])}
                log(f"[ResultFlowDebug] EndGameResultUI.IsConfigured: {ui_dict.get('IsConfigured')}")
                # Log key serialized references presence
                for field in ['resultRoot', 'losePanel', 'titleText', 'summaryText', 'restartButton']:
                    log(f"[ResultFlowDebug] EndGameResultUI.{field} present: {ui_dict.get(field) is not None}")
            else:
                log("[ResultFlowDebug] EndGameResultUI component not found in scene")
    else:
        log("[ResultFlowDebug] RunResultController not found")

# Query RunResultController properties
def query_run_result_controller():
    # Find objects with RunResultController component
    result = unity_skills.call_skill('gameobject_find', component='RunResultController')
    if result['count'] == 0:
        log('RunResultController not found')
        return None
    obj = result['objects'][0]
    props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='RunResultController')
    # Convert list of property dicts to a name->value dict for easier access
    prop_list = props.get('properties', [])
    prop_dict = {p['name']: p['value'] for p in prop_list}
    return prop_dict

# Query RunResultOverlay active state (assumes a Canvas named 'RunResultOverlay')
def query_overlay_active():
    overlay = unity_skills.call_skill('gameobject_find', name='RunResultOverlay')
    if overlay['count'] == 0:
        log('RunResultOverlay not found')
        return None
    obj = overlay['objects'][0]
    canvas_props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='Canvas')
    go_props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='GameObject')
    # Convert property lists to dicts
    canvas_dict = {p['name']: p['value'] for p in canvas_props.get('properties', [])}
    go_dict = {p['name']: p['value'] for p in go_props.get('properties', [])}
    return {
        'canvasEnabled': canvas_dict.get('enabled'),
        'gameObjectActive': go_dict.get('active')
    }

# Perform validation steps
ensure_play_mode()
apply_fatal_damage()
rr_props = query_run_result_controller()
overlay_state = query_overlay_active()
log(f"RunResultController properties after damage: {json.dumps(rr_props)}")
log(f"Overlay state after damage: {json.dumps(overlay_state)}")
# Verify expected conditions
log(f"IsResultActive == true? {rr_props.get('IsResultActive')} ")
log(f"IsRunFinished == true? {rr_props.get('IsRunFinished')} ")
log(f"RunResultOverlay active? {overlay_state}")
# Restart to verify reset
unity_skills.call_skill('editor_stop')
time.sleep(1)
unity_skills.call_skill('editor_play')
time.sleep(2)
rr_props_reset = query_run_result_controller()
log(f"RunResultController after restart: {json.dumps(rr_props_reset)}")
# Cleanup temporary validator script
validator_path = r'e:/Repositories/RougeLite101/Assets/Editor/TempValidation/TempResultFlowValidator.cs'
if os.path.exists(validator_path):
    os.remove(validator_path)
    log('Deleted temporary validator script')
temp_dir = r'e:/Repositories/RougeLite101/Assets/Editor/TempValidation'
if os.path.isdir(temp_dir) and not os.listdir(temp_dir):
    os.rmdir(temp_dir)
    log('Removed empty TempValidation folder')
# Save scene
unity_skills.call_skill('scene_save')
# Check for compilation errors
comp = unity_skills.call_skill('debug_check_compilation')
log(f"Compilation status: {json.dumps(comp)}")
log('Result flow validation complete')
