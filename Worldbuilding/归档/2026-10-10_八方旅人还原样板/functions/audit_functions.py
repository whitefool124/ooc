"""Inventory surviving cooked material expressions; never infer missing wiring as recovered."""
import collections
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
packages = ROOT / 'packages'
inventory = json.loads((packages / 'inventory.json').read_text(encoding='utf-8-sig'))
types = collections.Counter()
functions = []
parameters = []
references = []
inputs = []

def walk(value):
    if isinstance(value, dict):
        yield value
        for child in value.values():
            yield from walk(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk(child)

for path in sorted(packages.rglob('*.json')):
    if path.name == 'inventory.json':
        continue
    objects = json.loads(path.read_text(encoding='utf-8-sig'))
    inputs.append({'file': str(path.relative_to(ROOT)), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    names = {obj['Name'] for obj in objects}
    for obj in objects:
        types[obj['Type']] += 1
        p = obj.get('Properties', {})
        if obj['Type'] == 'MaterialFunction':
            expressions = p.get('FunctionExpressions', [])
            functions.append({'name': obj['Name'], 'slots': len(expressions), 'null_slots': sum(x is None for x in expressions)})
        if 'ParameterName' in p:
            parameters.append({'package': str(path.relative_to(packages)), 'name': p['ParameterName'], 'default': p.get('DefaultValue'), 'type': obj['Type']})
        for node in walk(p):
            expression = node.get('ExpressionName')
            if expression and expression != 'None' and expression not in names:
                references.append({'package': str(path.relative_to(packages)), 'owner': obj['Name'], 'unresolved_expression': expression})

scene = json.loads((ROOT.parent / 'T_Plain_L_A.json').read_text(encoding='utf-8-sig'))
sun = next(o for o in scene if o['Type'] == 'DirectionalLightComponent' and o.get('Outer') == 'LightSource')['Properties']
post = next(o for o in scene if o['Type'] == 'PostProcessVolume' and o['Name'] == 'PostProcessVolume_1')['Properties']['Settings']
report = {
    'status': 'QA_PENDING', 'packages': len(inventory['visited']), 'failed_packages': inventory['failures'],
    'export_types': dict(types), 'functions': functions, 'parameters': parameters,
    'unresolved_named_connections': references, 'inputs': inputs,
    'source_sun': sun, 'source_postprocess': post,
    'verification': {
        'unity': '6000.5.2f1', 'urp': '17.6.0', 'shader_messages': 0,
        'rendered_mesh_components': 948, 'used_materials': 72, 'object_collection_candidates': 68,
        'actual_resolutions': [[1920, 1080], [960, 540]],
        'material_key_1': 'True -> False -> True observed',
        'keys_2_3_4': 'AO=False, cloud=False, sourceGrade=True observed after real keyboard taps; defaults reset',
        'cleanup': 'Play=False; scene dirty=False; original UniversalRP and Flat ambient restored; research roots=0',
        'ao': 'Depth reconstruction, 0.45m radius, intensity 1, direct contribution 0; visually rendered',
        'cloud': 'Original linear mask; power 6 hypothesis; original projection size; animated offset observed',
        'grade_candidate': 'Gamma .6 maps URP W=-.4; Gain1.2 maps W=.2; too dark in actual view, default unchanged',
        'editor_performance': 'NOT_MEASURED', 'player_performance': 'NOT_MEASURED',
    },
    'limitations': [
        'Null expression slots and unresolved named connections prevent a complete material graph recovery.',
        'MPC defaults are recovered; lost arithmetic and runtime collection overrides are not verified.',
        'SSAO is contact ambient occlusion, not a recovered UE contact shadow ray march.',
        'Cloud output exponent and panner XY rates are not fully connected in the exported cooked graph.',
        'Cloud projection coordinate convention, 100m light-function fade, and shadowmap filtering remain approximate.',
        'URP SSAO does not reproduce UE AO Power8/Bias0.5.',
        'UE Basic auto exposure, shadow/midtone grading, filmic transform, DOF and bloom kernel remain unconverted.',
        'Cubemap face orientation and diffuse convolution are still approximate.',
    ],
}
(ROOT / 'audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'packages': report['packages'], 'failures': len(report['failed_packages']),
                  'functions': len(functions), 'null_slots': sum(f['null_slots'] for f in functions),
                  'unresolved_named_connections': len(references)}, ensure_ascii=False))
