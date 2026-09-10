// Native host surface. No browser or CLR access is exposed to game rules.
// 唯一网络例外：末尾的 fetch 包装 → C# nativeNet.Fetch（白名单地址，重写为腾讯云 SCF 代理）。
var window=globalThis;
var innerWidth=1920,innerHeight=1080;
var __storage={},__elements=new Map(),__timers=[],__nativeMenu=true,__lastScore=null,__errors=[];
var localStorage={getItem:k=>Object.prototype.hasOwnProperty.call(__storage,k)?__storage[k]:null,setItem:(k,v)=>{__storage[k]=String(v)},removeItem:k=>{delete __storage[k]}};
var console={log(){},warn(){},error(...x){__errors.push(x.join(' '))}};
function setTimeout(fn){__timers.push(fn);return __timers.length}function clearTimeout(){}
function __flush(){let guard=0;while(__timers.length&&guard++<500){const tasks=__timers.splice(0);for(const fn of tasks)fn()}if(guard>=500)throw Error('Native timer overflow')}
var performance={now:()=>Date.now()},CustomEvent=function(type){this.type=type};
function __element(key=''){
 const classes=new Set(),listeners={};
 const e={key,innerHTML:'',textContent:'',disabled:false,open:false,hidden:false,value:'all',children:[],style:{setProperty(k,v){this[k]=v},removeProperty(k){delete this[k]}},dataset:{},classList:{add(...v){v.forEach(x=>classes.add(x))},remove(...v){v.forEach(x=>classes.delete(x))},toggle(x,b){b=b===undefined?!classes.has(x):b;b?classes.add(x):classes.delete(x);return b},contains:x=>classes.has(x)},setAttribute(k,v){this[k]=v},getAttribute(k){return this[k]||null},removeAttribute(k){delete this[k]},append(...x){this.children.push(...x)},appendChild(x){this.children.push(x);return x},prepend(){},remove(){},querySelector(s){return __get(key+' '+s)},querySelectorAll(){return[]},addEventListener(t,fn){listeners[t]=fn},removeEventListener(){},getBoundingClientRect(){return{left:0,top:0,right:100,bottom:100,width:100,height:100}},showModal(){this.open=true;this.openOrder=++__dialogOrder},close(){this.open=false},focus(){},animate(){return{finished:Promise.resolve(),cancel(){}}},getContext(){return null},scrollTop:0,scrollHeight:0,clientHeight:0,offsetWidth:100};return e;
}
var __dialogOrder=0;
function __get(s){if(!__elements.has(s))__elements.set(s,__element(s));return __elements.get(s)}
function __queryAll(selector){if(selector==='#played-cards .played-card')return currentPlayed.map(card=>{const e=__get('played-'+card.uid);e.dataset.uid=String(card.uid);return e});return[]}
var document={documentElement:__element('html'),body:__element('body'),querySelector:__get,querySelectorAll:__queryAll,createElement:()=>__element(),createElementNS:()=>__element(),addEventListener(){}};
document.documentElement.classList.add('reduce-motion');
function getComputedStyle(){return{getPropertyValue(){return''}}}
function dispatchEvent(){}function addEventListener(){}
var __audioCues=[];
function gameSfx(name){if(typeof name==='string'&&__audioCues.length<64)__audioCues.push(name)}
function NativeDrainAudioCues(){return JSON.stringify(__audioCues.splice(0))}
function gameMusicStinger(result){if(result==='victory'||result==='failure')gameSfx('stinger:'+result)}
function showBattleFromRoute(){__nativeMenu=false}function showRouteMap(){}
function goMainMenuFromRun(){__nativeMenu=true;for(const e of __elements.values())e.close()}
function confirmStartRunWithLoadout(){clearRunSave();window.markPersonaCollectionRunStart?.();__nativeMenu=false;reset();runController.startRun(PERSONA_BALANCE_MANIFEST.activeRunTemplateId)}

var __nativeSettingsRequested=false,__nativePersonaDetail=null;
function closeSettingsForTutorial(){}
function openSettingsFromTutorial(){__nativeSettingsRequested=true}

// 远程 AI 的唯一网络出口。ai-persona-selection-client 在模块加载时捕获 root.fetch，
// 因此必须在 host 加载阶段定义。C# 侧只放行白名单 Worker 地址；网络异常/不可用 →
// reject → 客户端 catch → NETWORK_ERROR → 本地安全兜底，玩家无感。
var fetch=function(url,options){
 options=options||{};
 if(typeof nativeNet==='undefined'||!nativeNet||typeof nativeNet.Fetch!=='function')return Promise.reject(new Error('native fetch unavailable'));
 var body=options.body===undefined||options.body===null?'':String(options.body);
 var pending;
 try{pending=nativeNet.Fetch(String(url),body)}catch(e){return Promise.reject(e)}
 return Promise.resolve(pending).then(function(json){
  var envelope;try{envelope=JSON.parse(json)}catch(e){return {ok:false,status:0,json:function(){return Promise.resolve(null)}}}
  return {ok:!!envelope.ok,status:envelope.status||0,json:function(){return Promise.resolve(envelope.body)}};
 });
};
