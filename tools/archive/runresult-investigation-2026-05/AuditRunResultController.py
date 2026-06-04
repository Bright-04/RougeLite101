import sys, json, os
sys.path.append(r'e:/Repositories/RougeLite101/.agents/skills/unity-skills/scripts')
import unity_skills

def log(msg):
    print(f"[Audit] {msg}")

# Helper to pretty print json snippets
def dump(obj):
    return json.dumps(obj, indent=2)[:500]

# 1. Find all GameObjects with RunResultController component
rr_objs = unity_skills.call_skill('gameobject_find', component='RunResultController')
log(f"RunResultController objects found: {rr_objs.get('count',0)}")
for obj in rr_objs.get('objects', []):
    iid = obj.get('instanceId')
    name = obj.get('name')
    path = obj.get('path')
    log(f"Object: name={name}, instanceId={iid}, path={path}")
    info = unity_skills.call_skill('gameobject_get_info', instanceId=iid)
    log(f"Info: {dump(info)}")
    # Check if it is a prefab instance (look for 'prefabParent' field if exists)
    if 'prefabParent' in info:
        log(f"Prefab parent asset: {info.get('prefabParent')}")

# 2. Locate GameRoot and GameManager objects
for target in ['GameRoot', 'GameManager']:
    res = unity_skills.call_skill('gameobject_find', name=target)
    log(f"Search for {target}: count={res.get('count',0)}")
    for obj in res.get('objects', []):
        iid = obj.get('instanceId')
        log(f"{target} instanceId={iid}, path={obj.get('path')}")
        info = unity_skills.call_skill('gameobject_get_info', instanceId=iid)
        log(f"Info: {dump(info)}")

# 3. Locate Canvas_UI
canvas = unity_skills.call_skill('gameobject_find', name='Canvas_UI')
log(f"Canvas_UI objects: {canvas.get('count',0)}")
for obj in canvas.get('objects', []):
    iid = obj.get('instanceId')
    log(f"Canvas_UI instanceId={iid}, path={obj.get('path')}")
    comps = unity_skills.call_skill('gameobject_get_components', instanceId=iid)
    comp_names = [c['type'] for c in comps.get('components', [])]
    log(f"Components: {comp_names}")
    info = unity_skills.call_skill('gameobject_get_info', instanceId=iid)
    log(f"Info: {dump(info)}")

# 4. Find EndGameResultUI component (should be on RunResultOverlay)
end_ui = unity_skills.call_skill('gameobject_find', component='EndGameResultUI')
log(f"EndGameResultUI objects: {end_ui.get('count',0)}")
for obj in end_ui.get('objects', []):
    iid = obj.get('instanceId')
    log(f"EndGameResultUI instanceId={iid}, path={obj.get('path')}")
    # Get its parent hierarchy to confirm under Canvas_UI/RunResultOverlay
    hierarchy = unity_skills.call_skill('gameobject_get_hierarchy', instanceId=iid)
    log(f"Hierarchy: {hierarchy.get('hierarchy')}" )

# 5. List all prefab assets (type prefab) to see if any contain RunResultController
prefabs = unity_skills.call_skill('asset_find', type='Prefab')
log(f"Prefabs found: {prefabs.get('count',0)}")
# For each prefab, we could load its hierarchy, but that's expensive; just list names
for asset in prefabs.get('objects', [])[:5]:
    log(f"Prefab asset: {asset.get('path')} (name={asset.get('name')})")

print('[Audit] Done')
