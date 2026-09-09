// Native host surface. No browser, network or CLR access is exposed to game rules.
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
