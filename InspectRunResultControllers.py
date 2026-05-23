import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Inspect] {msg}")

# Find all RunResultController components
rr_res = unity_skills.call_skill('gameobject_find', component='RunResultController')
log(f"Found {rr_res.get('count',0)} RunResultController objects")
if rr_res.get('count',0) > 0:
    for obj in rr_res['objects']:
        log(f"Object ID: {obj.get('instanceId')}, Name: {obj.get('name')}, Path: {obj.get('path')}")
        # Get properties of RunResultController
        props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='RunResultController')
        prop_dict = {p['name']: p['value'] for p in props.get('properties', [])}
        log(f"resultUI ref: {prop_dict.get('resultUI')}")
        # Check hierarchy
        hier = unity_skills.call_skill('gameobject_get_hierarchy', instanceId=obj['instanceId'])
        log(f"Hierarchy: {hier.get('hierarchy')}" )

# Find GameRoot object
gr_res = unity_skills.call_skill('gameobject_find', name='GameRoot')
log(f"Found {gr_res.get('count',0)} GameRoot objects")
if gr_res.get('count',0) > 0:
    for obj in gr_res['objects']:
        log(f"GameRoot ID: {obj.get('instanceId')}, Path: {obj.get('path')}")
        comps = unity_skills.call_skill('gameobject_get_components', instanceId=obj['instanceId'])
        comp_names = [c['type'] for c in comps.get('components', [])]
        log(f"Components on GameRoot: {comp_names}")
        if 'RunResultController' in comp_names:
            log('RunResultController present on GameRoot')
            # Get its resultUI ref
            props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='RunResultController')
            prop_dict = {p['name']: p['value'] for p in props.get('properties', [])}
            log(f"GameRoot RunResultController resultUI ref: {prop_dict.get('resultUI')}")
else:
    log('GameRoot not found')

print('[Inspect] Done')
