"""Generated resource import; requires Pillow. Does not delete old/unknown assets."""
from pathlib import Path
import sys, shutil, json, hashlib
from PIL import Image

source=Path(sys.argv[1] if len(sys.argv)>1 else 'E:/GameDemo/MVP')
destination=Path(sys.argv[2] if len(sys.argv)>2 else 'Assets/WebAligned/Resources')
folders=['backgrounds','personas-v2','opponents','cards','battle-actions','battle-hud','battle-tools','main-menu','shop-buttons','shop-resources','settings']
manifest=[]
back=source/'assets/art/card-back-occult-v1.png'
(destination/'WebArt').mkdir(parents=True,exist_ok=True)
shutil.copy2(back,destination/'WebArt'/back.name)
for folder in folders:
    for asset in (source/'assets/art'/folder).iterdir():
        if asset.suffix.lower() not in ('.png','.webp'): continue
        output=destination/'WebArt'/folder/(asset.stem+'.png')
        output.parent.mkdir(parents=True,exist_ok=True)
        if asset.suffix.lower()=='.png': shutil.copy2(asset,output)
        else: Image.open(asset).save(output)
        manifest.append({'source':str(asset.relative_to(source)),'output':str(output.relative_to(destination)), 'sha256':hashlib.sha256(asset.read_bytes()).hexdigest()})
for asset in (source/'assets/audio').rglob('*'):
    if not asset.is_file(): continue
    output=destination/'WebAudio'/asset.relative_to(source/'assets/audio')
    output.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(asset,output)
    manifest.append({'source':str(asset.relative_to(source)),'output':str(output.relative_to(destination)), 'sha256':hashlib.sha256(asset.read_bytes()).hexdigest()})
(destination/'asset-provenance.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf8')
print(f'Imported {len(manifest)} web assets and recorded source hashes.')
