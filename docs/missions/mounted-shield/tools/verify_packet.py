#!/usr/bin/env python3
"""Read-only mission integrity + runtime PNG verification; standard library only."""
import hashlib
import json
from pathlib import Path
import struct
import sys
import zlib


def require(condition, message):
    if not condition:
        raise ValueError(message)


def png_rgba(path):
    data = path.read_bytes()
    require(data[:8] == b'\x89PNG\r\n\x1a\n', 'Not a PNG: ' + str(path))
    pos, header, compressed, ended = 8, None, bytearray(), False
    while pos < len(data):
        require(pos + 12 <= len(data), 'Truncated PNG chunk')
        size = struct.unpack('>I', data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + size]
        end = pos + 12 + size
        require(end <= len(data), 'Truncated PNG payload')
        crc = struct.unpack('>I', data[end - 4:end])[0]
        require(zlib.crc32(kind + body) & 0xffffffff == crc, 'PNG CRC mismatch')
        if kind == b'IHDR':
            header = struct.unpack('>IIBBBBB', body)
        elif kind == b'IDAT':
            compressed.extend(body)
        elif kind == b'IEND':
            ended = True
            break
        pos = end
    require(ended and header is not None, 'Incomplete PNG')
    width, height, depth, color, compression, filtering, interlace = header
    require((depth, color, compression, filtering, interlace) == (8, 6, 0, 0, 0),
            'Expected non-interlaced 8-bit RGBA PNG')
    stride = width * 4
    raw = zlib.decompress(compressed)
    require(len(raw) == height * (stride + 1), 'Unexpected PNG raster size')
    previous, pixels = bytearray(stride), bytearray()
    for y in range(height):
        offset = y * (stride + 1)
        method = raw[offset]
        row = bytearray(raw[offset + 1:offset + 1 + stride])
        require(method <= 4, 'Unsupported PNG filter')
        for x in range(stride):
            left = row[x - 4] if x >= 4 else 0
            above = previous[x]
            upper_left = previous[x - 4] if x >= 4 else 0
            if method == 1:
                predictor = left
            elif method == 2:
                predictor = above
            elif method == 3:
                predictor = (left + above) // 2
            elif method == 4:
                p = left + above - upper_left
                pa, pb, pc = abs(p - left), abs(p - above), abs(p - upper_left)
                predictor = left if pa <= pb and pa <= pc else above if pb <= pc else upper_left
            else:
                predictor = 0
            row[x] = (row[x] + predictor) & 255
        pixels.extend(row)
        previous = row
    return width, height, pixels


def main():
    root = Path(__file__).resolve().parents[1]
    records = (root / 'CHECKSUMS.sha256').read_text(encoding='utf-8').splitlines()
    checked = set()
    for line in records:
        expected, name = line.split('  ', 1)
        rel = Path(name)
        require(not rel.is_absolute() and '..' not in rel.parts, 'Unsafe checksum path')
        require(name not in checked, 'Duplicate checksum path: ' + name)
        checked.add(name)
        target = root / rel
        require(target.is_file(), 'Missing file: ' + name)
        observed = hashlib.sha256(target.read_bytes()).hexdigest()
        require(observed == expected, 'Hash mismatch: ' + name)
    manifest = json.loads((root / 'assets/icon-manifest.json').read_text(encoding='utf-8'))
    runtime = manifest['runtime_export']
    require(runtime in checked, 'Runtime file not covered by checksums')
    width, height, pixels = png_rgba(root / runtime)
    require((width, height) == (64, 64), 'Runtime PNG must be 64x64')
    alpha = pixels[3::4]
    require(min(alpha) == 0 and max(alpha) == 255, 'Expected real transparency and opaque ink')
    require(all(alpha[i] == 0 for i in (0, width - 1, width * (height - 1), width * height - 1)),
            'Runtime corners must be transparent')
    ink = tuple(manifest['final_foreground_rgb'])
    colors = {tuple(pixels[i:i + 3]) for i in range(0, len(pixels), 4) if pixels[i + 3]}
    require(colors == {ink}, 'Runtime foreground is not the exact uniform reference ink')
    require(ink == (155, 77, 58), 'Unexpected final color; owner requested the supplied reference')
    for entry in manifest['files']:
        path = root / entry['path']
        require(path.is_file(), 'Missing art/provenance file: ' + entry['path'])
        require(path.stat().st_size == entry['size_bytes'], 'Asset size mismatch: ' + entry['path'])
        require(hashlib.sha256(path.read_bytes()).hexdigest() == entry['sha256'],
                'Asset manifest hash mismatch: ' + entry['path'])
    print('PASS: ' + str(len(checked)) + ' checksummed files verified.')
    print('PASS: runtime PNG is 64x64, RGBA8, with transparent corners and uniform #9B4D3A ink.')
    print('Scope: packet and icon bytes only. No implementation, build or native game test is implied.')


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, KeyError, struct.error, zlib.error) as exc:
        print('FAIL: ' + str(exc), file=sys.stderr)
        sys.exit(1)
