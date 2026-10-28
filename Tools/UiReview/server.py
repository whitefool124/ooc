"""Local-only OCC screenshot review; no Unity or external service is started."""
import argparse,json,mimetypes,os,shutil,tempfile,threading
from datetime import datetime,timezone
from http.server import SimpleHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote,urlsplit

APP=Path(__file__).resolve().parent
ROOT=APP.parents[1]
EVIDENCE=ROOT/'Worldbuilding/归档/2026-10-22_第一阶段界面体系'
DATA=json.loads((APP/'data.json').read_text(encoding='utf-8'))
IDS={p['id'] for p in DATA['pages']}
LOCK=threading.Lock()
def validate(value):
    if not isinstance(value,dict) or value.get('schema')!='occ-ui-user-review-v1' or value.get('version')!=DATA['version']:raise ValueError('Wrong review version')
    if not isinstance(value.get('overallNotes'),str) or len(value['overallNotes'])>10000:raise ValueError('Invalid overall notes')
    if not isinstance(value.get('pages'),dict):raise ValueError('Invalid pages')
    clean={'schema':value['schema'],'version':value['version'],'updatedAt':str(value.get('updatedAt',''))[:64],'overallNotes':value['overallNotes'],'pages':{}}
    for key,page in value['pages'].items():
        if key not in IDS or not isinstance(page,dict):raise ValueError('Invalid page')
        score=page.get('score')
        if score!='' and (type(score) is not int or not 0<=score<=10):raise ValueError('Invalid score')
        if page.get('verdict') not in ('pending','reject','revise','pass') or page.get('agreement') not in ('','agree','partial','disagree') or page.get('priority') not in ('','P0','P1','P2'):raise ValueError('Invalid verdict')
        if not isinstance(page.get('notes'),str) or len(page['notes'])>10000:raise ValueError('Invalid notes')
        clean['pages'][key]={field:page[field] for field in ('score','verdict','agreement','notes','priority')}
    return clean
def read_store():
    if not STORE.exists():return {'revision':0,'review':None}
    value=json.loads(STORE.read_text(encoding='utf-8'))
    if not isinstance(value.get('revision'),int):raise ValueError('Invalid stored revision')
    validate(value['review'])
    return value
class Handler(SimpleHTTPRequestHandler):
    def __init__(self,*args,**kwargs):super().__init__(*args,directory=str(APP),**kwargs)
    def log_message(self,fmt,*args):print(datetime.now().isoformat(timespec='seconds'),fmt%args,flush=True)
    def end_headers(self):
        self.send_header('X-Content-Type-Options','nosniff');self.send_header('Referrer-Policy','no-referrer')
        self.send_header('Content-Security-Policy',"default-src 'self'; img-src 'self' blob:; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'self'")
        self.send_header('Cache-Control','no-cache');super().end_headers()
    def output(self,status,value):
        body=json.dumps(value,ensure_ascii=False).encode('utf-8');self.send_response(status);self.send_header('Content-Type','application/json; charset=utf-8');self.send_header('Content-Length',str(len(body)));self.end_headers();self.wfile.write(body)
    def do_GET(self):
        path=unquote(urlsplit(self.path).path)
        if path=='/api/review':
            try:
                with LOCK:self.output(200,read_store())
            except Exception as exc:self.output(500,{'error':'Stored review cannot be read; original file preserved','detail':str(exc)})
            return
        if path.startswith('/evidence/'):
            parts=path.split('/')
            if len(parts)!=4 or parts[2] not in ('current','before') or parts[3] not in {f'{i}{suffix}.png' for i in IDS for suffix in ('','_960')}:
                self.send_error(404);return
            target=EVIDENCE/('ui-final' if parts[2]=='current' else 'before')/parts[3]
            if not target.is_file():self.send_error(404);return
            body=target.read_bytes();self.send_response(200);self.send_header('Content-Type','image/png');self.send_header('Content-Length',str(len(body)));self.end_headers();self.wfile.write(body);return
        if path not in ('/','/index.html','/app.js','/style.css','/data.json','/favicon.ico'):
            self.send_error(404);return
        if path=='/favicon.ico':self.send_response(204);self.end_headers();return
        super().do_GET()
    def do_POST(self):
        if self.path!='/api/review':self.send_error(404);return
        expected=f'http://127.0.0.1:{self.server.server_port}'
        origin=self.headers.get('Origin')
        if origin and origin not in (expected,f'http://localhost:{self.server.server_port}'):self.output(403,{'error':'Local origin required'});return
        if 'application/json' not in self.headers.get('Content-Type',''):self.output(415,{'error':'JSON required'});return
        try:
            size=int(self.headers.get('Content-Length','0'))
            if not 0<size<=1000000:raise ValueError('Invalid body length')
            value=validate(json.loads(self.rfile.read(size).decode('utf-8')))
            with LOCK:
                old=read_store()
                if self.headers.get('If-Match')!=str(old['revision']):self.output(409,{'error':'Review updated in another tab','revision':old['revision']});return
                STORE.parent.mkdir(parents=True,exist_ok=True)
                if STORE.exists():
                    history=STORE.parent/'review-history';history.mkdir(exist_ok=True)
                    shutil.copy2(STORE,history/f'{STORE.stem}_rev{old["revision"]}_{datetime.now().strftime("%Y%m%dT%H%M%S%f")}.json')
                result={'revision':old['revision']+1,'savedAt':datetime.now(timezone.utc).isoformat(),'review':value}
                fd,name=tempfile.mkstemp(prefix='.review-',suffix='.json',dir=str(STORE.parent))
                try:
                    with os.fdopen(fd,'w',encoding='utf-8') as stream:json.dump(result,stream,ensure_ascii=False,indent=2)
                    os.replace(name,STORE)
                finally:
                    if Path(name).exists():Path(name).unlink()
            self.output(200,{'revision':result['revision'],'savedAt':result['savedAt']})
        except (ValueError,UnicodeError) as exc:self.output(400,{'error':str(exc)})
        except Exception as exc:self.output(500,{'error':'Save failed; export a backup','detail':str(exc)})
if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--port',type=int,default=8876);parser.add_argument('--store',type=Path,default=ROOT/'Worldbuilding/归档/2026-10-22_界面人工审核/user-review.json');args=parser.parse_args()
    STORE=args.store.resolve()
    if not STORE.is_relative_to(ROOT/'Worldbuilding/归档'):parser.error('Review store must stay in the review archive')
    server=ThreadingHTTPServer(('127.0.0.1',args.port),Handler)
    print(f'OCC review: http://127.0.0.1:{args.port}\nStore: {STORE}',flush=True)
    try:server.serve_forever()
    except KeyboardInterrupt:server.server_close()
