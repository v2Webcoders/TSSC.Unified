(function(){
var d=document,$=function(s,r){return(r||d).querySelector(s)},$$=function(s,r){return[].slice.call((r||d).querySelectorAll(s))},app=$('#app'),tt,PAGES={"v-dash": "index.html"},DEF={'v-att':'all','v-leave':'all','v-ap':'open'},S={fy:'2026-27',d:{},q:''};
try{var _f=sessionStorage.getItem('tssc_fy');if(_f)S.fy=_f;var _d=sessionStorage.getItem('tssc_d');if(_d)S.d=JSON.parse(_d)}catch(e){}
function save(){try{sessionStorage.setItem('tssc_fy',S.fy);sessionStorage.setItem('tssc_d',JSON.stringify(S.d))}catch(e){}}
function parse(h){var m=(h||'').match(/^#v-(att|leave|ap)-(\w+)$/);return m?{id:'v-'+m[1],sf:m[2],h:h}:{id:(h||'').replace('#',''),sf:'',h:h}}
function go(h){var p=parse(h);if(d.getElementById(p.id)){if(h===location.hash)route();else location.hash=h}else if(PAGES[p.id])location.href=PAGES[p.id]+(p.sf?h:'')}
function toast(m){var t=$('#toast');t.textContent=m;t.hidden=false;clearTimeout(tt);tt=setTimeout(function(){t.hidden=true},4200)}
function filt(p){
  var t=$('table',p);if(!t)return;
  var v=p.closest('.view'),sf=(v&&v.dataset.sf)||'',qs=[],ss=[],tot=0,lim=+p.dataset.limit||0,okRows=[];
  if(S.q)qs.push(S.q);
  $$('[data-filter]',p).forEach(function(e){var x=e.value.trim().toLowerCase();if(!x)return;e.type==='search'?qs.push(x):ss.push(x)});
  $$('tbody tr',t).forEach(function(r){
    var tk=(r.dataset.s||'').split(' '),okS=!sf||sf==='all'||!r.dataset.s||tk.indexOf(sf)>-1;
    if(okS)tot++;
    var tx=r.textContent.toLowerCase(),cells=$$('td',r).map(function(c){return c.textContent.trim().toLowerCase()});
    var ok=okS&&qs.every(function(x){return tx.indexOf(x)>-1})&&ss.every(function(x){return cells.indexOf(x)>-1});
    r.hidden=!ok;if(ok)okRows.push(r)});
  var mt=okRows.length,n=mt;
  if(lim&&mt>lim){
    /* show only the newest rows (original order), wherever they sit after sorting */
    var keep=okRows.slice().sort(function(a,b){return a.dataset.rk-b.dataset.rk}).slice(0,lim);
    okRows.forEach(function(r){r.hidden=keep.indexOf(r)<0});n=lim;
  }
  var c=$('.cnt',p);if(c)c.textContent=c.textContent.replace(/Showing \d+ of \d+/,'Showing '+n+' of '+(lim?mt:tot));
  var e=$('.empty',p);if(e)e.hidden=n>0;
}
/* ---- column sorting ---- */
var MON={jan:0,feb:1,mar:2,apr:3,may:4,jun:5,jul:6,aug:7,sep:8,oct:9,nov:10,dec:11},
    RANK={high:3,medium:2,low:1,normal:0,overdue:4,pending:3,returned:2,approved:1,rejected:0};
function keyOf(tx){
  var l=tx.trim().toLowerCase(),m=l.match(/^(\d{1,2}) ([a-z]{3})[a-z]*(?: (\d{4}))?$/);
  if(m&&MON[m[2]]!==undefined)return{n:1,v:new Date(+(m[3]||2026),MON[m[2]],+m[1]).getTime()};
  if(RANK[l]!==undefined)return{n:1,v:RANK[l]};
  if(/^[\d.,%₹ ]+$/.test(l)){var x=parseFloat(l.replace(/[,%₹ ]/g,''));if(!isNaN(x))return{n:1,v:x}}
  return{n:0,v:l};
}
function sortBy(th){
  var tb=th.closest('table'),i=th.cellIndex,cur=th.getAttribute('data-dir'),
      first=/priority|status/i.test(th.textContent)?'desc':'asc',
      next=!cur?first:(cur===first?(first==='asc'?'desc':'asc'):'');
  $$('th',tb).forEach(function(h){h.removeAttribute('data-dir');h.removeAttribute('aria-sort')});
  var body=$('tbody',tb),rows=$$('tr',body);
  if(next){th.setAttribute('data-dir',next);th.setAttribute('aria-sort',next==='asc'?'ascending':'descending')}
  rows.sort(function(a,b){
    if(!next)return a.dataset.rk-b.dataset.rk;
    var x=keyOf(a.cells[i].textContent),y=keyOf(b.cells[i].textContent),c;
    c=(x.n&&y.n)?x.v-y.v:String(x.v).localeCompare(String(y.v),undefined,{numeric:true});
    if(c===0)c=a.dataset.rk-b.dataset.rk;
    return next==='desc'?-c:c});
  rows.forEach(function(r){body.appendChild(r)});
  var pn=tb.closest('.panel');if(pn)filt(pn);
}
$$('.panel table').forEach(function(t){
  if($('th[colspan],th[rowspan]',t))return;
  $$('tbody tr',t).forEach(function(r,k){r.dataset.rk=k});
  $$('th',t).forEach(function(h){if(/^action$/i.test(h.textContent.trim()))return;h.classList.add('sortable');h.title='Click to sort';h.tabIndex=0;h.setAttribute('role','button')});
});
function nfilt(p,val){
  var n=0,tot=0;$$('.nitem',p).forEach(function(i){tot++;var ok=(!val||i.dataset.p===val)&&(!S.q||i.textContent.toLowerCase().indexOf(S.q)>-1);i.hidden=!ok;if(ok)n++});
  var c=$('.cnt',p);if(c)c.textContent=c.textContent.replace(/Showing \d+ of \d+/,'Showing '+n+' of '+tot);
}
function refresh(){
  $$('[data-fy]').forEach(function(x){x.hidden=x.dataset.fy!==S.fy});
  $$('[data-fysel]').forEach(function(s){s.value=S.fy});
  var blk='[data-fy="'+S.fy+'"] ';
  $$(blk+'[data-datesel]').forEach(function(s){if(!S.d[S.fy])S.d[S.fy]=s.options[0].value;s.value=S.d[S.fy]});
  var fd0=$(blk+'[data-date]');if(fd0&&!S.d[S.fy])S.d[S.fy]=fd0.dataset.date;
  $$(blk+'[data-date]').forEach(function(x){x.hidden=x.dataset.date!==S.d[S.fy]});
  $$('[data-fyn]').forEach(function(b){var m=b.dataset.fyn.split('|').filter(function(p){return p.indexOf(S.fy+':')===0})[0];var n=m?m.split(':')[1]:'0';b.textContent=n;b.hidden=n==='0'});
  $$('.panel').forEach(function(p){if($('table',p))filt(p);else if($('.nitem',p)){var o=$('[data-nf].on',p);nfilt(p,o?o.dataset.nf:'')}});
  $$('.dept').forEach(function(r){r.hidden=!!S.q&&r.textContent.toLowerCase().indexOf(S.q)<0});
}
function route(){
  var v=$('.view'),p=parse(location.hash),sf=p.sf;
  if(d.getElementById(p.id)&&d.getElementById(p.id).classList.contains('view')){v=d.getElementById(p.id)}else sf='';
  if(!sf&&DEF[v.id])sf=DEF[v.id];
  var full=DEF[v.id]?'#'+v.id+'-'+sf:'#'+v.id;
  $$('.view').forEach(function(x){x.hidden=x!==v});v.dataset.sf=sf;
  $('#crumb').textContent=v.dataset.crumb.replace(/&amp;/g,'&');
  $$('.ecard[data-href],.tab[data-href]',v).forEach(function(b){b.classList.toggle('on',b.dataset.href===full)});
  $$('#nav .nv').forEach(function(n){var w=n.parentNode,on=n.dataset.href===full||$$('.sub button',w).some(function(b){return b.dataset.href===full});n.classList.toggle('on',on);if(w.classList.contains('has'))w.classList.toggle('open',on)});
  $$('#nav .sub button').forEach(function(b){b.classList.toggle('on',b.dataset.href===full)});
  app.classList.remove('nav-open');refresh();
}
d.addEventListener('click',function(e){
  var t=e.target,x;
  if(x=t.closest('th.sortable')){sortBy(x);return}
  if(t.closest('[data-close]')){x=t.closest('dialog');x.close();if(t.closest('[data-toast]'))toast(t.closest('[data-toast]').dataset.toast);return}
  if(x=t.closest('[data-toast]'))return toast(x.dataset.toast);
  if(x=t.closest('[data-m]'))return $('#'+x.dataset.m).showModal();
  if(x=t.closest('[data-tab]')){var sec=x.closest('section');$$('[data-tab]',sec).forEach(function(b){b.classList.toggle('on',b===x)});$$('[data-pane]',sec).forEach(function(p){p.hidden=p.dataset.pane!==x.dataset.tab});return}
  if(x=t.closest('[data-nf]')){var p=x.closest('.panel');$$('[data-nf]',p).forEach(function(b){b.classList.toggle('on',b===x)});nfilt(p,x.dataset.nf);return}
  if(x=t.closest('[data-href]')){go(x.dataset.href);return}
  if(x=t.closest('[data-scroll]')){var a=$('[data-anchor="'+x.dataset.scroll+'"]',t.closest('.view'));if(a)a.scrollIntoView({behavior:'smooth'});return}
  if(x=t.closest('[data-copy]')){var ta=$('textarea',x.closest('.view'));if(navigator.clipboard)navigator.clipboard.writeText(ta.value).then(function(){toast('Notes copied.')},function(){toast('Select and copy the notes yourself.')});return}
  if(x=t.closest('[data-toggle]')){x.parentNode.classList.toggle('open');return}
  if(t.closest('#hb')){app.clientWidth<900?app.classList.toggle('nav-open'):app.classList.toggle('collapsed');return}
  if(t.closest('#tg'))return app.classList.toggle('nav-open');
  if(t.closest('#scrim'))return app.classList.remove('nav-open');
  if(t.closest('#pbtn')){var mm=$('#pmenu');mm.hidden=!mm.hidden;return}
  $('#pmenu').hidden=true;if(t.tagName==='DIALOG')t.close();
});
d.addEventListener('keydown',function(e){if((e.key==='Enter'||e.key===' ')&&e.target.matches('[role=button]')){e.preventDefault();e.target.click()}});
d.addEventListener('input',function(e){var t=e.target;if(t.id==='q'){S.q=t.value.trim().toLowerCase();refresh()}else if(t.hasAttribute('data-filter')){var p=t.closest('.panel');if(p)filt(p)}});
d.addEventListener('change',function(e){var t=e.target;
  if(t.hasAttribute('data-fysel')){S.fy=t.value;save();refresh()}
  else if(t.hasAttribute('data-datesel')){S.d[S.fy]=t.value;save();refresh()}
  else if(t.hasAttribute('data-filter')){var p=t.closest('.panel');if(p)filt(p)}});
window.addEventListener('hashchange',function(){route();requestAnimationFrame(function(){window.scrollTo(0,0)})});route();
})();
