#!/usr/bin/env python3
"""Check the delivered mission's bytes and screenshot provenance, offline.

Standard library only. This is an archive-integrity check, not a game test,
factory gate, digital signature, or authorization to execute the mission.
"""

import hashlib
import json
from pathlib import Path, PurePosixPath
import struct
import sys


def sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def member(root, name):
    rel = PurePosixPath(name)
    if rel.is_absolute() or '..' in rel.parts or '\\' in name or ':' in name:
        raise ValueError('Unsafe manifest path: ' + name)
    path = root.joinpath(*rel.parts)
    if path.is_symlink() or not path.is_file():
        raise ValueError('Missing or symlinked member: ' + name)
    if root not in path.resolve().parents:
        raise ValueError('Member resolves outside packet: ' + name)
    return path


def main():
    root = Path(__file__).resolve().parent.parent
    lines = (root / 'SHA256SUMS.txt').read_text(encoding='utf-8').splitlines()
    indexed = set()
    for line in lines:
        if not line:
            continue
        expected, name = line.split('  ', 1)
        if len(expected) != 64 or any(c not in '0123456789abcdef' for c in expected):
            raise ValueError('Malformed SHA-256 for: ' + name)
        if name in indexed:
            raise ValueError('Duplicate manifest member: ' + name)
        indexed.add(name)
        if sha256(member(root, name)) != expected:
            raise ValueError('Byte mismatch: ' + name)

    actual = {p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}
    expected_members = indexed | {'SHA256SUMS.txt'}
    if actual != expected_members:
        raise ValueError('Unexpected/missing files: ' + repr(sorted(actual ^ expected_members)))

    manifest = json.loads((root / 'evidence/screenshots/manifest.json').read_text(encoding='utf-8'))
    if len(manifest['files']) != 6:
        raise ValueError('Expected six owner screenshots')
    for item in manifest['files']:
        path = member(root, item['path'])
        if path.stat().st_size != item['bytes'] or sha256(path) != item['sha256']:
            raise ValueError('Screenshot original-byte mismatch: ' + item['path'])
        with path.open('rb') as image:
            header = image.read(24)
        if header[:8] != b'\x89PNG\r\n\x1a\n' or header[12:16] != b'IHDR':
            raise ValueError('Not a PNG with IHDR: ' + item['path'])
        if struct.unpack('>II', header[16:24]) != (item['width'], item['height']):
            raise ValueError('Screenshot dimensions differ: ' + item['path'])

    for path in root.rglob('*.json'):
        json.loads(path.read_text(encoding='utf-8'))
    print('PASS: {} hashed files and six original screenshots verified.'.format(len(indexed)))
    print('This verifies the mission packet only. No mod/game tests were run.')


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, KeyError, TypeError) as error:
        print('FAIL: ' + str(error), file=sys.stderr)
        sys.exit(1)
