#!/usr/bin/env python3
from pathlib import Path
import struct, sys, json, uuid, hashlib, os

if len(sys.argv) < 2:
    raise SystemExit("usage: patch_touch_dll.py <Assembly-CSharp.dll>")
src=Path(sys.argv[1])
raw=bytearray(src.read_bytes())

TARGETS={
 'ArcanoidControls','ArtefactologyControls','DoodleJumpControlScheme',
 'PackmanControls','PlatformerShooterControls','RoadCrossControls',
 'RunnerControls','SnakeControls','WolfControls'
}

def rva2off(b,rva):
    pe=struct.unpack_from('<I',b,0x3c)[0]
    num=struct.unpack_from('<H',b,pe+6)[0]
    optsz=struct.unpack_from('<H',b,pe+20)[0]
    opt=pe+24
    sec=opt+optsz
    for i in range(num):
        o=sec+i*40
        vs,va,rs,rp=struct.unpack_from('<IIII',b,o+8)
        if va <= rva < va+max(vs,rs):
            return rp+rva-va
    raise ValueError(f"RVA not mapped: {rva:x}")

def us_stream(b):
    pe=struct.unpack_from('<I',b,0x3c)[0]
    opt=pe+24
    magic=struct.unpack_from('<H',b,opt)[0]
    dd=opt+(96 if magic==0x10b else 112)
    cli_rva,_=struct.unpack_from('<II',b,dd+14*8)
    cli=rva2off(b,cli_rva)
    md_rva,_=struct.unpack_from('<II',b,cli+8)
    md=rva2off(b,md_rva)
    if b[md:md+4] != b'BSJB':
        raise ValueError("not .NET metadata")
    verlen=struct.unpack_from('<I',b,md+12)[0]
    p=(md+16+verlen+3)&~3
    _,streams=struct.unpack_from('<HH',b,p); p+=4
    for _ in range(streams):
        off,size=struct.unpack_from('<II',b,p); p+=8
        end=b.index(0,p)
        name=bytes(b[p:end]).decode('ascii')
        p=(end+1+3)&~3
        if name=="#US":
            return md+off,size
    raise ValueError("#US stream missing")

def clen(b,p):
    x=b[p]
    if x & 0x80 == 0: return x,1
    if x & 0xC0 == 0x80: return ((x&0x3f)<<8)|b[p+1],2
    if x & 0xE0 == 0xC0: return ((x&0x1f)<<24)|(b[p+1]<<16)|(b[p+2]<<8)|b[p+3],4
    raise ValueError("bad compressed integer")

def bid(asset,action,path,processors,interactions):
    return str(uuid.uuid5(uuid.NAMESPACE_URL,f"ffh-touch:{asset}:{action}:{path}:{processors}:{interactions}"))

def add(o,a,path,processors="",interactions=""):
    bs=o["maps"][0]["bindings"]
    if any(b.get("action")==a and b.get("path")==path and b.get("processors","")==processors for b in bs):
        return
    bs.append({"name":"","id":bid(o["name"],a,path,processors,interactions),"path":path,
      "interactions":interactions,"processors":processors,"groups":"","action":a,
      "isComposite":False,"isPartOfComposite":False})

def replace(o,a,old,new):
    for m in o.get("maps",[]):
        for b in m.get("bindings",[]):
            if b.get("action")==a and b.get("path")==old: b["path"]=new

def swipe4(o):
    add(o,"Left","<Pointer>/delta/x","invert,scale(factor=0.05),clamp(min=0,max=1)")
    add(o,"Right","<Pointer>/delta/x","scale(factor=0.05),clamp(min=0,max=1)")
    add(o,"Up","<Pointer>/delta/y","scale(factor=0.05),clamp(min=0,max=1)")
    add(o,"Down","<Pointer>/delta/y","invert,scale(factor=0.05),clamp(min=0,max=1)")

def patch(o):
    n=o.get("name")
    if n in ("ArcanoidControls","DoodleJumpControlScheme"):
        add(o,"Left","<Pointer>/delta/x","invert,scale(factor=0.05),clamp(min=0,max=1)")
        add(o,"Right","<Pointer>/delta/x","scale(factor=0.05),clamp(min=0,max=1)")
    elif n in ("PackmanControls","SnakeControls","WolfControls"):
        swipe4(o)
    elif n in ("RunnerControls","RoadCrossControls"):
        add(o,"Up","<Pointer>/delta/y","scale(factor=0.05),clamp(min=0,max=1)")
        add(o,"Down","<Pointer>/delta/y","invert,scale(factor=0.05),clamp(min=0,max=1)")
    elif n=="PlatformerShooterControls":
        add(o,"Shoot","<Pointer>/press")
    elif n=="ArtefactologyControls":
        replace(o,"Drag","<Mouse>/leftButton","<Pointer>/press")
        replace(o,"MousePosition","<Mouse>/position","<Pointer>/position")
        add(o,"Return","<Touchscreen>/touch1/press")
        add(o,"Return","<Pointer>/press","", "multiTap(tapCount=2)")
    else:
        return False
    return True

us,size=us_stream(raw)
rel=1
seen=set()
while rel<size:
    n,ls=clen(raw,us+rel)
    if n==0:
        rel+=ls; continue
    start=us+rel+ls
    data=bytes(raw[start:start+n])
    flag=data[-1:] if n%2 else b""
    body=data[:-1] if n%2 else data
    try: s=body.decode("utf-16le")
    except UnicodeDecodeError:
        rel+=ls+n; continue
    if s.lstrip().startswith("{") and '"maps"' in s and '"bindings"' in s:
        try: o=json.loads(s)
        except Exception: o=None
        if o and o.get("name") in TARGETS and patch(o):
            compact=json.dumps(o,ensure_ascii=False,separators=(",",":"))
            if len(compact)>len(s):
                raise RuntimeError(f'{o["name"]}: JSON slot overflow {len(compact)} > {len(s)}')
            compact += " "*(len(s)-len(compact))
            enc=compact.encode("utf-16le")+flag
            if len(enc)!=n: raise RuntimeError("encoded size changed")
            raw[start:start+n]=enc
            seen.add(o["name"])
    rel+=ls+n

missing=TARGETS-seen
if missing: raise RuntimeError("missing input assets: "+", ".join(sorted(missing)))

before=hashlib.sha256(src.read_bytes()).hexdigest()
tmp=src.with_suffix(src.suffix+".touch.tmp")
tmp.write_bytes(raw)
after=hashlib.sha256(tmp.read_bytes()).hexdigest()
os.replace(tmp,src)
print(f"touch patched {len(seen)} input assets")
print("sha256 before",before)
print("sha256 after ",after)
