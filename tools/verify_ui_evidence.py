"""Check captured SAI2 GUI regression evidence (requires Pillow)."""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops


def verify(directory):
    directory = Path(directory)
    before = Image.open(directory / '07-dark-unreadable-dialog.png').convert('RGB')
    after = Image.open(directory / '14-dark-dialog-fixed.png').convert('RGB')
    # The same New Canvas dialog labels, excluding editable fields and borders.
    labels = (822, 461, 891, 643)
    counts = []
    for screenshot in (before, after):
        pixels = list(zip(*(channel.tobytes() for channel in screenshot.crop(labels).split())))
        counts.append({'dark': sum(max(p) < 40 for p in pixels),
                       'light': sum(min(p) > 150 for p in pixels)})
    assert counts[0]['dark'] > 500 and counts[0]['light'] == 0, 'Unreadable-label reproduction differs'
    assert counts[1]['dark'] == 0 and counts[1]['light'] > 500, 'Dark dialog labels regressed'
    dark = Image.open(directory / 'dark-artwork.png').convert('RGBA')
    restored = Image.open(directory / 'restored-artwork.png').convert('RGBA')
    assert dark.size == restored.size == (512, 512), 'Unexpected export dimensions'
    difference = ImageChops.difference(dark, restored)
    assert all(channel.getextrema() == (0, 0) for channel in difference.split()), 'Artwork pixels differ after restore'
    pixels = list(zip(*(channel.tobytes() for channel in dark.convert('RGB').split())))
    black = sum(max(p) < 32 for p in pixels)
    red = sum(p[0] > 180 and p[1] < 60 and p[2] < 60 for p in pixels)
    assert black > 1000 and red > 1000, 'Expected black/red artwork is absent'
    final = json.loads((directory / 'after-classic-restore.json').read_text())
    expected = 'c1f2b05cf5fb875b7ceb60d3b3809363803bae0a089e567fea8704f892cfdefa'
    assert final['restoredSha256'].lower() == expected, 'Classic restore hash differs'
    assert final['backupRemoved'] and final['stateRemoved'], 'Restore left stale state'
    return {'result': 'PASS', 'dialogLabelsBefore': counts[0], 'dialogLabelsAfter': counts[1],
            'exportSize': list(dark.size), 'exportDifferentPixels': 0,
            'blackPixels': black, 'redPixels': red,
            'darkExportSha256': hashlib.sha256((directory / 'dark-artwork.png').read_bytes()).hexdigest(),
            'restoredExportSha256': hashlib.sha256((directory / 'restored-artwork.png').read_bytes()).hexdigest(),
            'restoredExecutableSha256': final['restoredSha256'],
            'backupRemoved': True, 'stateRemoved': True}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('evidence_directory', type=Path)
    args = parser.parse_args()
    print(json.dumps(verify(args.evidence_directory), indent=2))
