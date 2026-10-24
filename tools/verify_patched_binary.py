"""Verify color immediates and the exact UI-text patch (requires capstone)."""
import argparse
import json
from pathlib import Path
import struct
import hashlib

from capstone import Cs, CS_ARCH_X86, CS_MODE_64
from generate_version_addresses import sections


def verify(original, patched):
    if len(original) != len(patched):
        raise ValueError('File length changed')
    if original[:0x400] != patched[:0x400]:
        raise ValueError('PE header changed')
    layout = sections(original)
    for name, (raw, size, _) in layout.items():
        if name not in ('.text', '.data', '.appskin', '.srclibs'):
            if original[raw:raw + size] != patched[raw:raw + size]:
                raise ValueError(f'Protected section {name} changed')
    # Verify the deliberately added UI text fallback separately, then remove
    # those exact edits from the instruction-immediate comparison below.
    normalized = bytearray(patched)
    profiles = json.loads((Path(__file__).resolve().parents[1] / 'ref/init/data/BinaryProfiles.json').read_text())
    profile = next((p for p in profiles if p['Sha256'].lower() == hashlib.sha256(original).hexdigest()), {})
    fix = profile.get('ControlTextFix')
    ui_calls = 0
    if fix:
        cave, converter = int(fix['Cave'], 16), int(fix['Converter'], 16)
        helper = patched[cave:cave + 17]
        conversion = bytes.fromhex('89c8c1c0080fc8c3')
        if original[converter:converter + 8] != conversion or original[cave:cave + 17] != b'\xcc' * 17:
            raise ValueError('UI text baseline layout differs')
        if helper[:5] != bytes.fromhex('85c97505b9') or helper[8:] != b'\0' + conversion:
            raise ValueError('UI text helper instructions differ')
        for value in fix['Calls']:
            site = int(value, 16)
            if original[site] != 0xe8 or site + 5 + struct.unpack_from('<i', original, site + 1)[0] != converter:
                raise ValueError('UI text source call differs')
            if patched[site] != 0xe8 or site + 5 + struct.unpack_from('<i', patched, site + 1)[0] != cave:
                raise ValueError('UI text patched call has the wrong target')
            normalized[site:site + 5] = original[site:site + 5]
            ui_calls += 1
        normalized[cave:cave + 17] = original[cave:cave + 17]
        selection = int(fix['SelectionBackground'], 16)
        if original[selection:selection + 4] != bytes.fromhex('d0d0d000'):
            raise ValueError('Text selection source color differs')
        def linear(value):
            value /= 255
            return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4
        luminance = sum(weight * linear(value) for weight, value in zip((0.0722, 0.7152, 0.2126), helper[5:8]))
        channel = 0 if luminance > 0.179 else 255
        if patched[selection:selection + 4] != bytes([channel, channel, channel, 0]):
            raise ValueError('Text selection background has insufficient contrast')
        normalized[selection:selection + 4] = original[selection:selection + 4]
    patched = normalized
    text_raw, text_size, text_rva = layout['.text']
    pdata_raw, pdata_size, _ = layout['.pdata']
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    changed_functions = changed_instructions = 0
    covered = set()
    for pos in range(pdata_raw, pdata_raw + pdata_size - 11, 12):
        start, end, _ = struct.unpack_from('<III', original, pos)
        start, end = start - text_rva + text_raw, end - text_rva + text_raw
        if not text_raw <= start < end <= text_raw + text_size or original[start:end] == patched[start:end]:
            continue
        before = list(md.disasm(original[start:end], start))
        after = list(md.disasm(patched[start:end], start))
        if [(i.address, i.size, i.id) for i in before] != [(i.address, i.size, i.id) for i in after]:
            raise ValueError(f'Instruction layout changed at {start:X}')
        changed_functions += 1
        for a, b in zip(before, after):
            for index in range(a.size):
                if a.bytes[index] == b.bytes[index]:
                    continue
                if not a.imm_offset <= index < a.imm_offset + a.imm_size:
                    raise ValueError(f'Non-immediate instruction byte changed at {a.address + index:X}')
                covered.add(a.address + index)
            changed_instructions += a.bytes != b.bytes
    for offset in range(text_raw, text_raw + text_size):
        if original[offset] != patched[offset] and offset not in covered:
            raise ValueError(f'Unverified code change outside decoded immediates at {offset:X}')
    return {'changedFunctions': changed_functions, 'changedInstructions': changed_instructions,
            'changedCodeBytes': len(covered), 'uiTextCallSites': ui_calls, 'result': 'PASS'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('original', type=Path)
    parser.add_argument('patched', type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(args.original.read_bytes(), args.patched.read_bytes()), indent=2))
