"""Generate a reviewed address table from two local x64 SAI binaries.

Development dependency: capstone. Binaries are read only and never distributed
by this script. Code locations require a unique instruction-context match;
skin locations require identical color bytes at the same section offset.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct

from capstone import Cs, CS_ARCH_X86, CS_MODE_64, CS_GRP_CALL, CS_GRP_JUMP
from capstone.x86 import X86_OP_MEM, X86_REG_RIP


def sections(binary):
    pe = struct.unpack_from('<I', binary, 60)[0]
    count = struct.unpack_from('<H', binary, pe + 6)[0]
    optional = struct.unpack_from('<H', binary, pe + 20)[0]
    result = {}
    for index in range(count):
        pos = pe + 24 + optional + index * 40
        name = binary[pos:pos + 8].rstrip(b'\0').decode('ascii')
        virtual_size, rva, size, raw = struct.unpack_from('<IIII', binary, pos + 8)
        result[name] = (raw, size, rva)
    return result


def relocate(old, new, addresses):
    before, after = sections(old), sections(new)
    if hashlib.sha256(old).hexdigest() != '962922289bb5b005212be8e24426aae50b8903b70965079dec8268454db2423d':
        raise ValueError('The original binary is not the verified 2024.02.22 baseline')
    old_pe = struct.unpack_from('<I', old, 60)[0]
    new_pe = struct.unpack_from('<I', new, 60)[0]
    old_base = struct.unpack_from('<Q', old, old_pe + 48)[0]
    new_base = struct.unpack_from('<Q', new, new_pe + 48)[0]
    # The complete skin payload must match, apart from rebased internal
    # pointers. Matching only a three-byte grey color would be ambiguous.
    for name in ('.appskin', '.srclibs'):
        old_raw, old_size, _ = before[name]
        new_raw, new_size, _ = after[name]
        if old_size != new_size:
            raise ValueError(f'{name}: section size changed')
        for relative in range(0, old_size, 8):
            a = old[old_raw + relative:old_raw + relative + 8]
            b = new[new_raw + relative:new_raw + relative + 8]
            if a == b:
                continue
            pointer = int.from_bytes(a, 'little') - old_base
            pointee = next((key for key, (_, size, rva) in before.items() if rva <= pointer < rva + size), None)
            if pointee is None:
                raise ValueError(f'{name}+{relative:X}: non-pointer payload changed')
            expected = new_base + pointer - before[pointee][2] + after[pointee][2]
            if int.from_bytes(b, 'little') != expected:
                raise ValueError(f'{name}+{relative:X}: payload or pointer target changed')
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    raw, size, rva = before['.pdata']
    text_raw, text_size, text_rva = before['.text']
    functions = []
    for pos in range(raw, raw + size - 11, 12):
        start, end, _ = struct.unpack_from('<III', old, pos)
        if text_rva <= start < end <= text_rva + text_size:
            functions.append((start - text_rva + text_raw, end - text_rva + text_raw))

    def pattern(instructions):
        result = b''
        for instruction in instructions:
            wildcard = set()
            if instruction.group(CS_GRP_JUMP) or instruction.group(CS_GRP_CALL):
                wildcard.update(range(instruction.imm_offset, instruction.imm_offset + instruction.imm_size))
            if any(op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP for op in instruction.operands):
                wildcard.update(range(instruction.disp_offset, instruction.disp_offset + instruction.disp_size))
            result += b''.join(b'.' if i in wildcard else re.escape(bytes([value]))
                               for i, value in enumerate(instruction.bytes))
        return result

    output = {}
    evidence = {}
    for key, value in addresses.items():
        offset = int(value, 16)
        section = next(name for name, (raw, size, _) in before.items() if raw <= offset < raw + size)
        old_raw, old_size, _ = before[section]
        new_raw, new_size, _ = after[section]
        if key.startswith('GlobalSection'):
            target = new_raw if 'From' in key else new_raw + new_size - 1
            method = 'section boundary'
        elif section == '.text':
            function_index = next(i for i, (start, end) in enumerate(functions) if start <= offset < end)
            start, end = functions[function_index]
            instructions = list(md.disasm(old[start:end], start))
            index = next(i for i, instruction in enumerate(instructions)
                         if instruction.address <= offset < instruction.address + instruction.size)
            hits = []
            for radius in (4, 8, 16, 32):
                context = instructions[max(0, index - radius):index + radius + 1]
                hits = [match.start() + offset - context[0].address
                        for match in re.finditer(pattern(context), new, re.DOTALL)]
                if len(hits) == 1:
                    break
            if len(hits) != 1:
                # Repeated drawing functions need their neighboring functions
                # as context; proximity alone is not sufficient evidence.
                start = functions[max(0, function_index - 2)][0]
                end = functions[min(len(functions) - 1, function_index + 2)][1]
                context = list(md.disasm(old[start:end], start))
                hits = [match.start() + offset - start
                        for match in re.finditer(pattern(context), new, re.DOTALL)]
            if len(hits) != 1:
                raise ValueError(f'{key}: ambiguous instruction context: {hits}')
            target = hits[0]
            method = 'unique instruction context'
        elif section == '.data':
            # These five slider colors form one contiguous color table. Its
            # first two entries have a unique 48-byte context in both builds.
            anchor = int(addresses['SlidersActiveBackground'], 16)
            context = old[anchor - 24:anchor + 24]
            hits = [m.start() + 24 for m in re.finditer(re.escape(context), new)]
            if len(hits) != 1 or not anchor <= offset <= anchor + 44:
                raise ValueError(f'{key}: unverified data table')
            target = hits[0] + offset - anchor
            method = 'unique slider table context'
        else:
            if section not in ('.appskin', '.srclibs') or old_size != new_size:
                raise ValueError(f'{key}: unverified section {section}')
            target = new_raw + offset - old_raw
            method = 'same skin section offset'
        is_boundary = 'From' in key or re.search(r'To\d*$', key) is not None
        if not is_boundary and old[offset:offset + 3] != new[target:target + 3]:
            raise ValueError(f'{key}: color bytes differ')
        output[key] = f'0x{target:08X}'
        evidence[key] = method
    return output, evidence


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('original', type=Path)
    parser.add_argument('target', type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    addresses = json.loads((root / 'ref/init/data/Address.json').read_text())
    old, new = args.original.read_bytes(), args.target.read_bytes()
    mapped, evidence = relocate(old, new, addresses)
    args.output.write_text(json.dumps(mapped, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'sourceSha256': hashlib.sha256(old).hexdigest(),
                      'targetSha256': hashlib.sha256(new).hexdigest(),
                      'addressCount': len(mapped),
                      'methods': {method: list(evidence.values()).count(method)
                                  for method in set(evidence.values())}}, indent=2))
