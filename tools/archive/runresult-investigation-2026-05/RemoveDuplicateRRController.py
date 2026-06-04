import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Action] {msg}")

# Find Canvas_UI GameObject
canvas_res = unity_skills.call_skill('gameobject_find', name='Canvas_UI')
if not canvas_res.get('count'):
    log('Canvas_UI not found')
    raise SystemExit
canvas_obj = canvas_res['objects'][0]
canvas_id = canvas_obj['instanceId']
log(f"Canvas_UI ID: {canvas_id}")
# Verify it has RunResultController
components = unity_skills.call_skill('gameobject_get_components', instanceId=canvas_id)
comp_names = [c['type'] for c in components.get('components', [])]
if 'RunResultController' in comp_names:
    # Remove it
    rm_res = unity_skills.call_skill('component_remove', instanceId=canvas_id, componentType='RunResultController')
    log(f"Remove result: {rm_res}")
else:
    log('RunResultController not present on Canvas_UI')

# Find GameRoot
root_res = unity_skills.call_skill('gameobject_find', name='GameRoot')
if not root_res.get('count'):
    log('GameRoot not found')
    raise SystemExit
root_obj = root_res['objects'][0]
root_id = root_obj['instanceId']
log(f"GameRoot ID: {root_id}")
# Ensure RunResultController exists on GameRoot
components_root = unity_skills.call_skill('gameobject_get_components', instanceId=root_id)
comp_names_root = [c['type'] for c in components_root.get('components', [])]
if 'RunResultController' not in comp_names_root:
    log('RunResultController missing on GameRoot')
    # Could add if needed, but assume exists
else:
    # Find EndGameResultUI on Canvas_UI/RunResultOverlay
    overlay_res = unity_skills.call_skill('gameobject_find', name='RunResultOverlay')
    if not overlay_res.get('count'):
        log('RunResultOverlay not found')
        raise SystemExit
    overlay_obj = overlay_res['objects'][0]
    overlay_id = overlay_obj['instanceId']
    # List components on overlay to find EndGameResultUI
    comps_overlay = unity_skills.call_skill('gameobject_get_components', instanceId=overlay_id)
    end_ui_type = None
    for c in comps_overlay.get('components', []):
        if c['type'].endswith('EndGameResultUI'):
            end_ui_type = c['type']
            break
    if not end_ui_type:
        log('EndGameResultUI component not found on overlay')
        raise SystemExit
    # Set resultUI reference on GameRoot's RunResultController
    set_res = unity_skills.call_skill(
        'component_set_property',
        instanceId=root_id,
        componentType='RunResultController',
        propertyName='resultUI',
        referencePath=overlay_obj['path']
    )
    log(f"Set resultUI property result: {set_res}")

print('[Done]')
