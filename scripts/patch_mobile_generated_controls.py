#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(sys.argv[1] if len(sys.argv) > 1 else "work/exported/ExportedProject")
scripts = root / "Assets" / "Scripts" / "Assembly-CSharp"

def inject(file_name, lines):
    p = scripts / file_name
    if not p.exists():
        print(f"WARN missing {p}")
        return False
    s = p.read_text(encoding="utf-8-sig", errors="strict")
    marker = 'asset = InputActionAsset.FromJson('
    pos = s.find(marker)
    if pos < 0:
        print(f"WARN no FromJson in {p}")
        return False
    end = s.find(');', pos)
    if end < 0:
        print(f"WARN no end of FromJson in {p}")
        return False
    end += 2
    tag = "// FFH_ANDROID_TOUCH_PATCH"
    if tag in s:
        print(f"SKIP already patched {file_name}")
        return True
    payload = "\n\t\t" + tag + "\n"
    for line in lines:
        payload += "\t\t" + line + "\n"
    s = s[:end] + payload + s[end:]
    p.write_text(s, encoding="utf-8")
    print(f"PATCHED {file_name}")
    return True

ok = True

# This minigame reads absolute Mouse position, so a virtual button overlay is not enough.
ok &= inject("ArtefactologyControls.cs", [
    'asset.FindAction("Drag", throwIfNotFound: true).AddBinding("<Touchscreen>/primaryTouch/press");',
    'asset.FindAction("MousePosition", throwIfNotFound: true).AddBinding("<Touchscreen>/primaryTouch/position");',
    'asset.FindAction("Return", throwIfNotFound: true).AddBinding("<Touchscreen>/touch1/press");',
])

# Shooting can use a normal screen tap as well as the on-screen L button.
ok &= inject("PlatformerShooterControls.cs", [
    'asset.FindAction("Shoot", throwIfNotFound: true).AddBinding("<Touchscreen>/primaryTouch/press");',
])

# RoadCross already has Pointer press for Up. A two-finger/secondary touch gives Down.
ok &= inject("RoadCrossControls.cs", [
    'asset.FindAction("Down", throwIfNotFound: true).AddBinding("<Touchscreen>/touch1/press");',
])

if not ok:
    raise SystemExit(2)
