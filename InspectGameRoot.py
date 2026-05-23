import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Inspect] {msg}")

# Find GameRoot objects
res = unity_skills.call_skill('gameobject_find', name='GameRoot')
log(f"Found {res.get('count',0)} GameRoot objects")
if res.get('count',0) > 0:
    for obj in res['objects']:
        log(f"GameRoot ID: {obj.get('instanceId')}, Path: {obj.get('path')}")
        # List components on this object
        comps = unity_skills.call_skill('gameobject_get_components', instanceId=obj['instanceId'])
        comp_names = [c['type'] for c in comps.get('components', [])]
        log(f"Components: {comp_names}")
        # If RunResultController present
        if 'RunResultController' in comp_names:
            log('RunResultController found on GameRoot')
            # Get its resultUI reference
            props = unity_skills.call_skill('component_get_properties', instanceId=obj['instanceId'], componentType='RunResultController')
            prop_dict = {p['name']: p['value'] for p in props.get('properties', [])}
            log(f"resultUI ref: {prop_dict.get('resultUI')}")
else:
    log('No GameRoot found')
print('[Inspect] Done')
