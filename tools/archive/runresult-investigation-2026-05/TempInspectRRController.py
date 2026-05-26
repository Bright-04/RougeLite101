import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Inspect] {msg}")

# Find all RunResultController components
res = unity_skills.call_skill('gameobject_find', component='RunResultController')
log(f"Found {res.get('count',0)} RunResultController objects")
if res.get('count',0) > 0:
    for obj in res['objects']:
        log(f"Object ID: {obj.get('instanceId')}, Name: {obj.get('name')}, Path: {obj.get('path')}")
        # Get its parent hierarchy to see if under Canvas
        parent = unity_skills.call_skill('gameobject_get_hierarchy', instanceId=obj['instanceId'])
        log(f"Hierarchy: {parent.get('hierarchy')}")
        # Get component properties (including resultUI reference)
        props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='RunResultController')
        prop_dict = {p['name']: p['value'] for p in props.get('properties', [])}
        log(f"resultUI ref: {prop_dict.get('resultUI')}")
else:
    log('No RunResultController found')

# Find all Canvas objects to see where the duplicate resides
canvas_res = unity_skills.call_skill('gameobject_find', component='Canvas')
log(f"Found {canvas_res.get('count',0)} Canvas objects")
if canvas_res.get('count',0) > 0:
    for obj in canvas_res['objects']:
        # Check if it also has RunResultController
        comps = unity_skills.call_skill('gameobject_get_components', instanceId=obj['instanceId'])
        comp_names = [c['type'] for c in comps.get('components', [])]
        if 'RunResultController' in comp_names:
            log(f"Canvas {obj.get('name')} (ID {obj.get('instanceId')}) has RunResultController attached!")

print('[Inspect] Done')
