"""Read-only UE4 v4 pak inspection/extraction into this study archive."""
import argparse, hashlib, io, json, struct, zlib
from pathlib import Path
PAK=Path('E:/SteamLibrary/steamapps/common/OCTOPATH TRAVELER/Octopath_Traveler/Content/Paks/Octopath_Traveler-WindowsNoEditor.pak')
ROOT=Path(__file__).resolve().parent
def number(f,fmt):return struct.unpack('<'+fmt,f.read(struct.calcsize('<'+fmt)))[0]
def string(f):
 n=number(f,'i'); assert abs(n)<1048576
 return f.read(n).rstrip(b'\0').decode('utf8') if n>=0 else f.read(-n*2).decode('utf-16-le').rstrip('\0')
def index():
 with PAK.open('rb') as f:
  f.seek(-45,2); encrypted=number(f,'B'); magic=number(f,'I'); version=number(f,'I'); off=number(f,'q'); size=number(f,'q'); digest=f.read(20)
  assert magic==0x5a6f12e1 and version==4 and not encrypted,(magic,version,encrypted)
  f.seek(off); raw=f.read(size); assert hashlib.sha1(raw).digest()==digest,'Index checksum mismatch'
 r=io.BytesIO(raw); mount=string(r); count=number(r,'i'); entries=[]
 for _ in range(count):
  name=string(r); start=r.tell(); offset=number(r,'q'); compressed=number(r,'q'); size=number(r,'q'); method=number(r,'I'); sha=r.read(20).hex()
  blocks=[]
  if method:
   for j in range(number(r,'i')):blocks.append([number(r,'q'),number(r,'q')])
  flags=number(r,'B'); block_size=number(r,'I')
  entries.append(dict(path=name,offset=offset,compressed_size=compressed,size=size,method=method,sha1=sha,blocks=blocks,flags=flags,block_size=block_size,header_size=r.tell()-start))
 assert r.tell()==len(raw),(r.tell(),len(raw))
 return dict(source=str(PAK),pak_version=version,index_encrypted=bool(encrypted),mount=mount,entry_count=count,index_sha1=digest.hex(),entries=entries)
def extract(e):
 assert not e['flags']&1,'Encrypted entry unsupported'
 with PAK.open('rb') as f:
  if not e['method']:f.seek(e['offset']+e['header_size']); data=f.read(e['size'])
  else:
   assert e['method']==1,'Unsupported compression'
   data=b''; packed=b''
   for start,end in e['blocks']:
    f.seek(start); block=f.read(end-start); packed+=block; data+=zlib.decompress(block)
 assert len(data)==e['size']
 assert hashlib.sha1(packed if e['method'] else data).hexdigest()==e['sha1'],'File checksum mismatch'
 target=(ROOT/'extracted'/e['path']).resolve()
 assert target.is_relative_to((ROOT/'extracted').resolve())
 target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
 return str(target)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--filter',default='');p.add_argument('--extract',action='store_true');a=p.parse_args(); result=index()
 (ROOT/'pak_index.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
 selected=[e for e in result['entries'] if a.filter.lower() in e['path'].lower()]
 print(json.dumps({k:v for k,v in result.items() if k!='entries'},ensure_ascii=False))
 print('Selected:',len(selected))
 for e in selected[:120]:print(e['path'],e['size'], '-> '+extract(e) if a.extract else '')
