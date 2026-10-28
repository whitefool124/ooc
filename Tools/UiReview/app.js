'use strict';
const $=id=>document.getElementById(id);
const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const labels={pending:'待审核',reject:'退回重做',revise:'修改后再看',pass:'通过'};
const agreements={agree:'基本认同',partial:'部分认同',disagree:'不认同'};
let data,review,selected,filter='all',issueIndex=0,revision=0,timer,localKey,busy=false,queued=false,serverReady=false,conflicted=false;
let source='current',resolution='960',pixelMode=false,markers=true;
const emptyPage=()=>({score:'',verdict:'pending',agreement:'',notes:'',priority:''});
const pageReview=id=>review.pages[id]||emptyPage();
function notify(message){$('notice').textContent=message;$('notice').hidden=false;clearTimeout(notify.timer);notify.timer=setTimeout(()=>$('notice').hidden=true,5500);}
function setSave(message,error=false){$('save-state').textContent=message;$('save-state').dataset.error=String(error);}
function validate(value){
  if(!value||value.schema!=='occ-ui-user-review-v1'||value.version!==data.version||!value.pages||typeof value.pages!=='object'||Array.isArray(value.pages))throw Error('记录版本不符，请导入本版界面的审核 JSON。');
  if(typeof value.overallNotes!=='string'||value.overallNotes.length>10000)throw Error('整体意见格式不正确。');
  const clean={schema:value.schema,version:data.version,updatedAt:typeof value.updatedAt==='string'?value.updatedAt:'',overallNotes:value.overallNotes,pages:{}};
  for(const [id,p] of Object.entries(value.pages)){
    if(!data.pages.some(x=>x.id===id)||!p||typeof p!=='object')throw Error('审核记录包含未知页面。');
    if(!['',0,1,2,3,4,5,6,7,8,9,10].includes(p.score)||!Object.hasOwn(labels,p.verdict)||!['','agree','partial','disagree'].includes(p.agreement)||!['','P0','P1','P2'].includes(p.priority)||typeof p.notes!=='string'||p.notes.length>10000)throw Error('审核字段格式不正确。');
    clean.pages[id]={score:p.score,verdict:p.verdict,agreement:p.agreement,notes:p.notes,priority:p.priority};
  }
  return clean;
}
function remember(){try{localStorage.setItem(localKey,JSON.stringify(review));return true;}catch{setSave('浏览器存储不可用，请及时导出备份',true);return false;}}
function changed(){review.updatedAt=new Date().toISOString();const local=remember();setSave(local?'已保存到浏览器 · 正在同步本机':'正在同步本机');clearTimeout(timer);timer=setTimeout(save,450);renderTable();renderProgress();}
async function save(){
  clearTimeout(timer);timer=null;
  if(conflicted){setSave('另一标签已更新，请先导出本页备份再刷新',true);return;}
  if(busy){queued=true;return;}
  busy=true;const snapshot=JSON.stringify(review);
  try{
    const response=await fetch('/api/review',{method:'POST',headers:{'Content-Type':'application/json','If-Match':String(revision)},body:snapshot});
    if(response.status===409){conflicted=true;throw Error('另一标签已更新，请先导出本页备份再刷新。');}
    if(!response.ok)throw Error('本机写入失败');
    const result=await response.json();revision=result.revision;serverReady=true;
    if(snapshot===JSON.stringify(review))setSave('已保存到本机');else queued=true;
  }catch(error){setSave(conflicted?error.message:'仅保存在浏览器，请导出备份；服务恢复后可重试',true);$('storage-note').textContent='本机服务未同步成功。当前意见仍在浏览器中，请导出 JSON 备份。';}
  finally{busy=false;if(queued&&!conflicted){queued=false;clearTimeout(timer);timer=setTimeout(save,100);}}
}
function visiblePages(){
  const needle=$('search').value.trim().toLocaleLowerCase();
  const list=data.pages.filter(p=>{
    const r=pageReview(p.id),reviewed=r.verdict!=='pending';
    if(filter==='reject'&&p.verdict!=='退回'||filter==='pending'&&reviewed||filter==='reviewed'&&!reviewed)return false;
    return !needle||[p.name,p.category,p.critique,...p.issues.map(i=>i.title),r.notes].join(' ').toLocaleLowerCase().includes(needle);
  });
  const flow=['landing','gift','map','loadout','spells','room','event','shop','workshop','medical','reward','battle','turn_hero','turn_enemy','turn_static','settings'];
  if($('sort').value==='flow')list.sort((a,b)=>flow.indexOf(a.id)-flow.indexOf(b.id));
  if($('sort').value==='score')list.sort((a,b)=>a.score-b.score);
  return list;
}
function renderProgress(){
  const count=data.pages.filter(p=>pageReview(p.id).verdict!=='pending').length;
  $('progress').value=count;$('progress-label').textContent=`${count} / ${data.pages.length} 已审核`;
  const remaining=data.pages.some(p=>pageReview(p.id).verdict==='pending');$('next-pending').disabled=!remaining;
  $('next-pending').textContent=remaining?'下一张待审核':'全部已审核';
}
function renderTable(){
  const list=visiblePages();$('visible-count').textContent=`${list.length} / ${data.pages.length} 个状态`;$('empty-state').hidden=list.length>0;
  $('table-body').innerHTML=list.map(p=>{const r=pageReview(p.id);return `<tr class="${p.id===selected?'selected':''}"><td><div class="page-cell"><img src="/evidence/current/${p.id}_960.png" alt="${esc(p.name)}缩略图"><div><button data-page="${p.id}" ${p.id===selected?'aria-current="true"':''}>${esc(p.name)}</button><small>${esc(p.category)}</small></div></div></td><td class="table-critique">${esc(p.critique)}<small>${p.issues.length} 项问题 · ${p.issues.map(i=>esc(i.title)).join(' / ')}</small></td><td><span class="table-score">${p.score}<small> / 10</small></span><span class="badge ${p.priority.toLowerCase()}">${p.priority} · ${esc(p.verdict)}</span></td><td class="user-cell"><span class="badge ${r.verdict}">${labels[r.verdict]}</span><small>${r.score===''?'未评分':`${r.score} / 10`}${r.agreement?` · ${agreements[r.agreement]}`:''}</small>${r.priority?`<small>你指定 ${r.priority}</small>`:''}</td><td class="user-comment">${esc(r.notes)}</td></tr>`;}).join('');
}
function selectPage(id,scroll=false){
  if(!data.pages.some(p=>p.id===id))return;selected=id;issueIndex=0;history.replaceState(null,'',`#${id}`);
  const p=data.pages.find(p=>p.id===id),r=pageReview(id);
  const beforeOption=$('source').querySelector('[value="before"]');beforeOption.disabled=(data.beforeUnavailable||[]).includes(id);beforeOption.textContent=beforeOption.disabled?'此状态无修订前参照':'修订前参照';
  if(beforeOption.disabled&&source==='before'){source='current';$('source').value=source;}
  $('selected-name').textContent=p.name;$('selected-meta').textContent=`${p.category} · ${data.pages.findIndex(x=>x.id===id)+1} / ${data.pages.length}`;
  $('self-score').innerHTML=`${p.score}<small> / 10</small>`;$('self-verdict').textContent=`${p.priority} · ${p.verdict}`;$('self-verdict').className=`badge ${p.priority.toLowerCase()}`;
  $('self-critique').textContent=p.critique;$('keep-note').textContent=`可以保留：${p.keep}`;
  $('user-score').value=String(r.score);$('user-verdict').value=r.verdict;$('user-agreement').value=r.agreement;$('user-notes').value=r.notes;$('user-priority').value=r.priority;
  renderImage();renderIssues();renderTable();renderProgress();
  if(scroll)$('selected-name').scrollIntoView({block:'start'});
}
function renderImage(){
  const p=data.pages.find(p=>p.id===selected),url=`/evidence/${source}/${p.id}${resolution==='960'?'_960':''}.png`;
  $('image-error').hidden=true;$('screenshot').src=url;$('screenshot').alt=`${p.name}，${source==='current'?'交付版本':'修订前参照'}，${resolution==='960'?'960×540':'1920×1080'}`;
  $('original-link').href=url;$('image-viewport').classList.toggle('pixel-mode',pixelMode);$('zoom').setAttribute('aria-pressed',String(pixelMode));$('zoom').textContent=pixelMode?'缩放至容器':'查看原始像素';
  $('image-note').textContent=source==='current'?`交付版本 · ${data.version} · ${pixelMode?'原图像素显示，可横向滚动':'按容器缩放；点击“查看原始像素”检查字与框'}。`:'历史截图仅作参照，角色状态或奖励内容未保证完全一致；问题标记已隐藏。';
  renderMarkers();
}
function renderMarkers(){const p=data.pages.find(p=>p.id===selected);$('annotations').innerHTML=markers&&source==='current'?p.issues.map((i,n)=>`<button class="marker ${n===issueIndex?'selected':''}" style="left:${i.area[0]}%;top:${i.area[1]}%;width:${i.area[2]}%;height:${i.area[3]}%" data-issue="${n}" aria-label="问题 ${n+1}：${esc(i.title)}" title="${esc(i.title)}"><span>${n+1}</span></button>`).join(''):'';}
function renderIssues(){const p=data.pages.find(p=>p.id===selected);$('issues').innerHTML=p.issues.map((i,n)=>`<details class="issue" id="issue-${n}" ${n===issueIndex?'open':''}><summary data-number="${n+1}" data-issue-summary="${n}">${esc(i.title)}<span class="badge ${i.priority.toLowerCase()}">${i.priority}</span></summary><dl><dt>看到</dt><dd>${esc(i.evidence)}</dd><dt>影响</dt><dd>${esc(i.impact)}</dd><dt>改法</dt><dd>${esc(i.suggestion)}</dd></dl></details>`).join('');}
function nextPage(direction){const list=visiblePages();if(!list.length){notify('当前筛选没有页面，请切回全部。');return;}let index=list.findIndex(p=>p.id===selected);if(index<0)index=direction>0?-1:0;selectPage(list[(index+direction+list.length)%list.length].id);}
function download(name,body,type){const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([body],{type}));a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(a.href),2000);}
function exportTable(type){
  const head=['页面','证据版本','我的评分','我的建议','严重程度','我的锐评','你的评分','你的结论','你对锐评的态度','你指定的优先级','你的意见'];
  const rows=data.pages.map(p=>{const r=pageReview(p.id);return[p.name,data.version,`${p.score}/10`,p.verdict,p.priority,p.critique,r.score===''?'':`${r.score}/10`,labels[r.verdict],agreements[r.agreement]||'',r.priority,r.notes];});
  if(type==='csv'){const cell=x=>{let text=String(x);if(/^\s*[=+\-@]/.test(text))text="'"+text;return `"${text.replace(/"/g,'""')}"`;};download('OCC_第一阶段界面审核.csv','\ufeff'+[head,...rows,['整体意见','','','','','','','','','',review.overallNotes]].map(r=>r.map(cell).join(',')).join('\r\n'),'text/csv;charset=utf-8');}
  else{const cell=x=>String(x).replace(/\|/g,'\\|').replace(/\r?\n/g,'<br>');download('OCC_第一阶段界面审核.md',`# 第一阶段界面审核\n\n证据版本：${data.version}\n\n${data.summary}\n\n| ${head.join(' | ')} |\n| ${head.map(()=>'---').join(' | ')} |\n${rows.map(r=>'| '+r.map(cell).join(' | ')+' |').join('\n')}\n\n## 你的整体意见\n\n${review.overallNotes||'尚未填写'}\n`,'text/markdown;charset=utf-8');}
  notify('已导出，包含全部页面和你的当前意见。');
}
async function start(){
  const response=await fetch('data.json');if(!response.ok)throw Error('审核数据无法读取，请使用启动脚本打开。');data=await response.json();localKey=`occ-ui-review:${data.version}`;
  review={schema:'occ-ui-user-review-v1',version:data.version,updatedAt:'',pages:{},overallNotes:''};
  let local=null;try{const text=localStorage.getItem(localKey);if(text)local=validate(JSON.parse(text));}catch{notify('浏览器中的旧记录无法读取，未覆盖它；请使用导入恢复备份。');}
  try{const result=await fetch('/api/review');if(!result.ok)throw Error();const saved=await result.json();revision=saved.revision;serverReady=true;
    if(saved.review)review=validate(saved.review);
    if(local&&local.updatedAt>review.updatedAt){review=local;queued=true;}
    setSave(saved.review?'已读取本机记录':'尚未填写 · 输入后自动保存');
  }catch{if(local)review=local;setSave('离线模式 · 请导出备份',true);}
  $('overall-critique').textContent=data.summary;$('basis').textContent=data.basis;$('rubric').textContent=data.rubric;$('overall-notes').value=review.overallNotes;
  for(let i=0;i<=10;i++)$('user-score').add(new Option(`${i} 分`,String(i)));
  $('table-body').addEventListener('click',e=>{const b=e.target.closest('[data-page]');if(b)selectPage(b.dataset.page,true);});
  $('previous').onclick=()=>nextPage(-1);$('next').onclick=()=>nextPage(1);
  $('source').onchange=e=>{source=e.target.value;renderImage();};$('resolution').onchange=e=>{resolution=e.target.value;renderImage();};$('zoom').onclick=()=>{pixelMode=!pixelMode;renderImage();};$('markers').onchange=e=>{markers=e.target.checked;renderMarkers();};
  $('screenshot').onerror=()=>$('image-error').hidden=false;
  $('annotations').onclick=e=>{const b=e.target.closest('[data-issue]');if(b){issueIndex=Number(b.dataset.issue);renderMarkers();renderIssues();$(`issue-${issueIndex}`).scrollIntoView({block:'nearest'});}};
  $('issues').addEventListener('click',e=>{const summary=e.target.closest('[data-issue-summary]');if(summary){issueIndex=Number(summary.dataset.issueSummary);renderMarkers();}});
  $('review-form').onsubmit=e=>e.preventDefault();
  for(const [id,key] of [['user-score','score'],['user-verdict','verdict'],['user-agreement','agreement'],['user-notes','notes'],['user-priority','priority']])$(id).addEventListener(id==='user-notes'?'input':'change',e=>{const r={...pageReview(selected)};r[key]=key==='score'?(e.target.value===''?'':Number(e.target.value)):e.target.value;review.pages[selected]=r;changed();});
  $('overall-notes').oninput=e=>{review.overallNotes=e.target.value;changed();};
  $('next-pending').onclick=()=>{const list=data.pages;let i=list.findIndex(p=>p.id===selected);for(let step=1;step<=list.length;step++){const p=list[(i+step)%list.length];if(pageReview(p.id).verdict==='pending'){selectPage(p.id);$('selected-name').scrollIntoView({block:'start'});return;}}notify('全部页面已审核。');};
  $('filter-buttons').onclick=e=>{const b=e.target.closest('[data-filter]');if(!b)return;filter=b.dataset.filter;for(const x of $('filter-buttons').children)x.setAttribute('aria-pressed',String(x===b));renderTable();};
  $('search').oninput=renderTable;$('sort').onchange=renderTable;
  $('export-csv').onclick=()=>exportTable('csv');$('export-md').onclick=()=>exportTable('md');$('export-json').onclick=()=>download('OCC_第一阶段审核备份.json',JSON.stringify(review,null,2),'application/json');
  $('import-button').onclick=()=>$('import-file').click();$('import-file').onchange=async e=>{const file=e.target.files[0];if(!file)return;try{if(file.size>1000000)throw Error('文件过大，请选择本审核页导出的 JSON。');const imported=validate(JSON.parse(await file.text()));const hasCurrent=Object.keys(review.pages).length||review.overallNotes;
      if(hasCurrent){download('OCC_导入前自动备份.json',JSON.stringify(review,null,2),'application/json');}
      review=imported;$('overall-notes').value=review.overallNotes;selectPage(selected);changed();notify(hasCurrent?'已导入，并下载了覆盖前的记录备份。':'已导入审核记录。');
    }catch(error){notify(error.message||'导入失败，当前意见未改变。');}e.target.value='';};
  window.addEventListener('beforeunload',()=>{if(timer&&!conflicted){clearTimeout(timer);if(!busy)fetch('/api/review',{method:'POST',headers:{'Content-Type':'application/json','If-Match':String(revision)},body:JSON.stringify(review),keepalive:true}).catch(()=>{});}});
  window.addEventListener('hashchange',()=>{const id=location.hash.slice(1);if(data.pages.some(p=>p.id===id))selectPage(id);});
  if(serverReady)$('storage-note').textContent='自动保存到本机审核记录；你写完后，我可以直接读取。也可导出 CSV、JSON 或 Markdown。';
  selectPage(data.pages.some(p=>p.id===location.hash.slice(1))?location.hash.slice(1):'map');
  if(queued){queued=false;save();}
}
start().catch(error=>{setSave('无法载入审核页',true);$('overall-critique').textContent=error.message;$('image-error').hidden=false;});
