from pathlib import Path
import urllib.request,json,hashlib,struct,zlib,datetime
stage=Path.cwd()/'reports/visual-overhaul-execution/asset-stage';stage.mkdir(exist_ok=True)
fonts=[('oxanium','Oxanium[wght].ttf','Oxanium-Variable.ttf'),('barlow','Barlow-Regular.ttf','Barlow-Regular.ttf'),('barlow','Barlow-SemiBold.ttf','Barlow-SemiBold.ttf'),('sharetechmono','ShareTechMono-Regular.ttf','ShareTechMono-Regular.ttf')]
def fetch(url):
 req=urllib.request.Request(url,headers={'User-Agent':'OpenAntares-M14-asset-provenance'})
 with urllib.request.urlopen(req,timeout=45) as response:return response.read()
def name_version(data):
 count=struct.unpack('>H',data[4:6])[0]
 for i in range(count):
  tag,checksum,offset,length=struct.unpack('>4sIII',data[12+i*16:28+i*16])
  if tag==b'name':
   table=data[offset:offset+length];_,n,base=struct.unpack('>HHH',table[:6]);values=[]
   for j in range(n):
    platform,encoding,language,nameid,size,start=struct.unpack('>HHHHHH',table[6+j*12:18+j*12])
    if nameid==5:
     text=table[base+start:base+start+size].decode('utf-16-be' if platform in [0,3] else 'latin1',errors='replace');values.append((language!=1033,text))
   return sorted(values)[0][1] if values else 'Version name not present'
 raise ValueError('Font name table missing')
manifest=[]
for folder,original,name in fonts:
 target=stage/'fonts'/folder;target.mkdir(parents=True,exist_ok=True);url='https://raw.githubusercontent.com/google/fonts/main/ofl/'+folder+'/'+urllib.parse.quote(original)
 data=fetch(url);assert len(data)>10000 and data[:4] in [b'\x00\x01\x00\x00',b'OTTO'];(target/name).write_bytes(data)
 licence=target/'OFL.txt'
 if not licence.exists():licence.write_bytes(fetch('https://raw.githubusercontent.com/google/fonts/main/ofl/'+folder+'/OFL.txt'))
 text=licence.read_text(encoding='utf-8-sig');assert 'SIL OPEN FONT LICENSE' in text and 'Version 1.1' in text
 metadata=fetch('https://raw.githubusercontent.com/google/fonts/main/ofl/'+folder+'/METADATA.pb');(target/'UPSTREAM-METADATA.pb').write_bytes(metadata)
 row={'family':folder,'path':'art/'+(target/name).relative_to(stage).as_posix(),'source':url,'version':name_version(data),'sha256':hashlib.sha256(data).hexdigest(),'licence':'SIL OFL 1.1','licencePath':'art/'+licence.relative_to(stage).as_posix(),'licenceSha256':hashlib.sha256(licence.read_bytes()).hexdigest()};manifest.append(row);print(folder,name,row['version'],flush=True)
frames=stage/'ui/frames';icons=stage/'ui/icons';frames.mkdir(parents=True,exist_ok=True);icons.mkdir(parents=True,exist_ok=True)
palette={'ink':(8,16,26),'plate':(22,37,52),'edge':(62,88,105),'rim':(119,150,161),'cyan':(114,222,229),'copper':(236,180,112),'disabled':(14,23,33)}
def png(path,w,h,fill,accent):
 pixels=bytearray(w*h*4);cut=16
 for y in range(h):
  for x in range(w):
   if x+y<cut or (w-1-x)+(h-1-y)<cut:continue
   edge=min(x,y,w-1-x,h-1-y,x+y-cut,(w-1-x)+(h-1-y)-cut)
   color=accent if edge<2 else palette['edge'] if edge<4 else fill
   if 14<=x<46 and 7<=y<10:color=accent
   if w-46<=x<w-14 and h-10<=y<h-7:color=palette['edge']
   at=(y*w+x)*4;pixels[at:at+4]=bytes((*color,255))
 raw=b''.join(b'\0'+pixels[y*w*4:(y+1)*w*4] for y in range(h))
 def chunk(tag,data):return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
styles={'outer_frame':(256,128,palette['plate'],palette['rim']),'screen_inset':(128,128,palette['ink'],palette['edge']),'command_normal':(128,96,palette['plate'],palette['edge']),'command_hover':(128,96,(26,51,65),palette['cyan']),'command_pressed':(128,96,(10,30,42),palette['copper']),'command_disabled':(128,96,palette['disabled'],(45,61,72)),'command_focus':(128,96,palette['plate'],palette['cyan']),'tab_unlatched':(128,64,(14,25,38),palette['edge']),'tab_latched':(128,64,(21,49,59),palette['cyan']),'workspace_frame':(192,128,palette['plate'],palette['rim'])}
for state in ['normal','hover','pressed','disabled','focus']:
 value=styles['command_'+state];styles['end_turn_'+state]=(192,96,value[2],palette['copper'] if state!='disabled' else value[3])
for name,(w,h,fill,accent) in styles.items():png(frames/(name+'.png'),w,h,fill,accent)
shapes={
'production':'<path d="M3 19V9l5 3V7l5 3V5h7v14Z"/><path d="M6 16h2m3 0h2m3 0h2M15 3h4"/>',
'research':'<ellipse cx="12" cy="12" rx="10" ry="4"/><ellipse cx="12" cy="12" rx="10" ry="4" transform="rotate(60 12 12)"/><ellipse cx="12" cy="12" rx="10" ry="4" transform="rotate(120 12 12)"/><circle cx="12" cy="12" r="1" fill="white"/>',
'population':'<circle cx="12" cy="7" r="3"/><path d="M5 21v-5a7 7 0 0 1 14 0v5Z"/>',
'alert':'<path d="m12 2 10 18H2Z"/><path d="M12 8v6m0 3v1"/>',
'gear':'<path d="m9 2 6 0 1 4 4 1 2 5-3 3 0 4-5 3-3-3-4 0-3-5 3-3 0-4Z"/><circle cx="12" cy="12" r="3"/>',
'back':'<path d="m10 4-8 8 8 8M3 12h19"/>',
'map':'<circle cx="12" cy="12" r="3"/><path d="m12 1 1 6m-1 10 0 6M1 12h6m10 0h6M4 4l4 4m8 8 4 4M4 20l4-4m8-8 4-4"/>',
'scout':'<path d="m12 2 4 7 5 3-6 2-3 8-3-8-6-2 5-3Z"/>',
'settler':'<path d="m12 2 7 6v10l-7 4-7-4V8Z"/><path d="M8 10h8v6H8Zm4-7v5"/>',
'escort':'<path d="m12 2 4 8 5 3v6l-8-3-1 6-1-6-8 3v-6l5-3Z"/>',
'idle':'<circle cx="12" cy="12" r="8"/><path d="M9 8v8m6-8v8"/>',
'moving':'<path d="M2 12h19m-6-6 6 6-6 6M3 6h5M3 18h5"/>',
'combat':'<path d="m4 3 17 17-4 1L3 7ZM20 3 3 20l4 1L21 7Z"/>',
'damaged':'<path d="m3 6 6-3 4 4 8-2-3 15-7 1-8-8Z"/><path d="m14 7-4 5 4 2-3 7"/>',
'worker_support':'<circle cx="12" cy="5" r="3"/><path d="M7 12 4 18m13-6 3 6M9 10v10m6-10v10M7 12h10M8 22h3m2 0h3"/><path d="M2 6q5-4 5 3Q2 10 2 6Z"/>',
'worker_production':'<circle cx="12" cy="5" r="3"/><path d="M7 12 4 18m13-6 3 6M9 10v10m6-10v10M7 12h10M8 22h3m2 0h3M19 3v8m-3-8h6v3h-6Z"/>',
'worker_research':'<circle cx="12" cy="5" r="3"/><path d="M7 12 4 18m13-6 3 6M9 10v10m6-10v10M7 12h10M8 22h3m2 0h3M2 3h4M3 3v3l-2 4h6L5 6V3"/>'}
roles=json.loads((Path.cwd()/'content/core/expansion.json').read_text(encoding='utf-8-sig'))['vessels']
for role in roles:
 key=role['id'];shapes['role_'+key]=shapes['settler' if role.get('can_found_colony') else 'escort' if 'escort' in key else 'scout']
for name,shape in shapes.items():(icons/(name+'.svg')).write_text('<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24"><g fill="none" stroke="#ffffff" stroke-width="1.6" stroke-linejoin="bevel" stroke-linecap="square">'+shape+'</g></svg>\n',encoding='utf-8')
manifest={'fonts':manifest,'frames':[{'path':'art/'+p.relative_to(stage).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'scale':2} for p in frames.glob('*.png')],'icons':[{'path':'art/'+p.relative_to(stage).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in icons.glob('*.svg')],'originalArt':'New geometric command-console art authored for OpenAntares; CC0 dedication, no commercial-game source assets','retrievedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
(stage/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8');print('Prepared original frames/icons and licensed fonts in scratch; production sources unchanged',flush=True)
