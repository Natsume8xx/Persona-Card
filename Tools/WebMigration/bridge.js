// Replaces only browser presentation; play/discard/score/progression remain original.
animateScoreResolution=async function(result){__lastScore=JSON.parse(JSON.stringify(result));renderScoreBreakdown(result.breakdown);};
function __plain(s){return String(s??'').replace(/<br\s*\/?\s*>/gi,'\n').replace(/<\/(?:p|div|li|h\d|article|dd|small)>/gi,'\n').replace(/<[^>]*>/g,' ').replace(/&nbsp;/g,' ').replace(/&amp;/g,'&').replace(/&lt;/g,'<').replace(/&gt;/g,'>').replace(/[ \t]+/g,' ').trim()}
function __text(id){const e=__get(id);return __plain(e.textContent||e.innerHTML)}
function __persona(id){const state=personaRuntime.getState(),instance=state.personaInstancesById[id],template=instance&&personaRuntime.getTemplate(instance.templateId);if(!template)return null;const feedback=PersonaFeedback.cardView(template,instance.runtimeState);return{id,name:template.name,portrait:template.portrait||'',trigger:feedback.trigger,effect:feedback.reward,instance,template,affixes:personaAffixSlots(template,instance).map(slot=>({...slot,effectText:slot.affixId?personaRuntime.getSubAffix(slot.affixId)?.effectText||'':'',availability:!slot.unlocked?personaRuntime.getSubAffixUnlockAvailability(instance.instanceId,slot.slotIndex,{profileId:currentShopProfileId()}):null}))}}
function NativeSnapshot(){
 const state=runController.getState(),ps=personaRuntime.getState(),dialogs=[...__elements.values()].filter(e=>e.open).sort((a,b)=>b.openOrder-a.openOrder),dialog=dialogs[0]?.key||(window.battleTutorial?.active?'#native-tutorial':''),node=currentStageNode();
 let preview=null;if(selected.size&&!inputLocked)preview=resolveScore([...selected].map(i=>hand[i]),false);
 const text={};for(const [k,e]of __elements)if(k.startsWith('#')&&(e.textContent||e.innerHTML))text[k]=__plain(e.textContent||e.innerHTML);
 const library=personaPool.map(p=>{const d=personaDetailData(p);return{id:p.id,name:d.template.name,portrait:d.template.portrait||p.portrait||'',trigger:d.presentation.triggerText,effect:d.presentation.effectText}});
 const disabled={};for(const [key,e]of __elements)disabled[key]=e.disabled;
 const snapshot={scoreDetails:__plain(__get("#score-breakdown").innerHTML.replace(/<\/summary>/g,"<br>")),settingsRequested:__nativeSettingsRequested,personaDetail:__nativePersonaDetail,handSortMode,disabled,tutorial:window.battleTutorial?{index:battleTutorial.index,steps:battleTutorial.steps}:null,library,pendingLoadout:pendingLoadoutIds,menu:__nativeMenu,dialog,node,score,target:battleTarget(),hands,discards,coins,earnedThisBattle,deckCount:deck.length,hand,selected:[...selected],inputLocked,preview,lastScore:__lastScore,personas:ps.equippedPersonaInstanceIds.map(__persona),pool:ps.runPersonaPool.map(__persona),text,canContinue:!!runSave.read(),nodes:currentRunTemplate().coreNodeIds.map(id=>runNodeById.get(id)),rules:currentHandTypes(),runDeck,stageLimit:activeStageLimitView(),storage:__storage,errors:__errors};
 if(dialog==='#shop-dialog'){const session=ensureShopSession();snapshot.shop={tab:shopTab,refreshCost:ShopRuntime.refreshCost(session.refreshIndex),offers:session.offers.map(o=>{const item=shopItemById.get(o.itemId),card=shopPlayingCard(item);return {...o,item,view:shopItemView(item),art:card?cardArtPath(card):null}}),selectedItemId:selectedShopItemId,selectedPersonaId:selectedShopPersonaId};}
 if(dialog==='#persona-growth-dialog')snapshot.growth={persona:__persona(currentGrowthInstanceId),selectedSlot:growthSelectedSlot};
 if(dialog==='#target-carry-dialog')snapshot.carry={selected:targetCarrySelectionId,ids:runController.getRunMetrics().generatedPersonaInstanceIds};
 if(dialog==='#shop-upgrade-dialog'){const item=shopItemById.get(shopUpgradeItemId);snapshot.upgradeTargets=shopUpgradeTargets(item);snapshot.upgradeSelected=shopUpgradeTargetId;}
 snapshot.deckTarget=!!deckShopItemId;snapshot.deckSelected=deckSelectedUid;snapshot.deckTab=deckTab;snapshot.deckSortMode=deckSortMode;snapshot.deckCards=sortedDeckCards(deckCardsForTab());snapshot.cardDetails=Object.fromEntries(runDeck.map(card=>[card.uid,{upgrade:cardUpgradeSummary(card),suitBonus:deckSuitGrowth(card).bonus}]));
 return JSON.stringify(snapshot);
}
function NativeAction(json){
 const a=JSON.parse(json);__lastScore=null;__errors=[];__nativeSettingsRequested=false;
 switch(a.type){


 case 'tutorial':window.battleTutorial?.open(true);break;
 case 'loadout':if(runSave.summary())__get('#native-new-run-confirm').showModal();else openStartPersonaLoadout();break;
 case 'new-run-confirm':__get('#native-new-run-confirm').close();openStartPersonaLoadout();break;
 case 'new-run-cancel':__get('#native-new-run-confirm').close();break;
 case 'loadout-place':if(__get('#start-loadout-dialog').open)placePendingPersona(a.id,a.index);break;
 case 'loadout-remove':if(__get('#start-loadout-dialog').open&&Number.isInteger(a.index)&&a.index>=0&&a.index<pendingLoadoutIds.length)removePendingPersona(a.index);break;
 case 'start':for(const e of __elements.values())e.close();clearRunSave();__nativeMenu=false;reset();runController.startRun(PERSONA_BALANCE_MANIFEST.activeRunTemplateId);break;
 case 'continue':__nativeMenu=false;if(!runSave.restore())__nativeMenu=true;break;
 case 'menu':__nativeMenu=true;for(const e of __elements.values())e.close();window.battleTutorial?.close(false);break;
 case 'resume':__nativeMenu=false;break;
 case 'toggle':if(!inputLocked)toggle(a.index);break;
 case 'play':if(!inputLocked&&selected.size&&hands>0)play().catch(e=>__errors.push(e.stack||String(e)));break;
 case 'discard':if(!inputLocked&&selected.size&&discards>0)discard().catch(e=>__errors.push(e.stack||String(e)));break;
 case 'click':{const e=__get(a.id);if(!e.disabled&&typeof e.onclick==='function'){const result=e.onclick();if(result?.catch)result.catch(e=>__errors.push(e.stack||String(e)))}break;}
 case 'sort':setHandSort(a.id|| (handSortMode==='rank'?'suit':'rank'));break;
 case 'shop-select':selectShopItem(a.id);break;
 case 'shop-tab':setShopTab(a.id);break;
 case 'shop-persona':selectShopPersona(a.id,shopPersonaEntries());break;
 case 'unlock':unlockPersonaAffix(a.id,a.index);break;
 case 'growth-slot':growthSelectedSlot=a.index;renderPersonaGrowth();break;
 case 'deck-select':selectDeckTarget(a.id);break;
 case 'deck-tab':if(!deckShopItemId){deckTab=a.id;deckSelectedUid=null;renderDeckDialog()}break;
 case 'upgrade-select':shopUpgradeTargetId=a.id;renderShopUpgradeTargets();break;
 case 'carry-select':targetCarrySelectionId=a.id;runController.setNodeRuntime({selectedPersonaInstanceId:a.id});renderTargetCarry();break;
 case 'gallery':syncPermanentPersonaPool();__get('#native-gallery').showModal();break;
 case 'save-menu':window.commitRunSave?.('battle','return_to_menu');__nativeMenu=true;for(const e of __elements.values())e.close();break;
 case 'persona-detail':{
   const runtime=__persona(a.id);
   if(runtime){__nativePersonaDetail={...runtime,affixText:__plain(personaAffixMarkup(runtime.template,runtime.instance)),growth:runtime.template.mainEffect?.growthText||''};__get('#persona-detail-dialog').showModal();}
   else{const persona=personaPool.find(p=>p.id===a.id);if(persona&&openPersonaDetail(a.id)){const {template,presentation}=personaDetailData(persona);__nativePersonaDetail={id:a.id,name:template.name,portrait:template.portrait||persona.portrait||'',trigger:presentation.triggerText,effect:presentation.effectText,affixText:__text('#persona-detail-affixes'),growth:template.mainEffect?.growthText||''};}}
   break;
 }
 case 'escape':{
   const dialog=NativeSnapshot();const top=JSON.parse(dialog).dialog;
   if(top==='#native-tutorial')window.battleTutorial?.close(true);
   else if(top==='#native-gallery'||top==='#native-new-run-confirm')__get(top).close();
   else{
     const closeByDialog={'#persona-detail-dialog':'#persona-detail-close','#start-loadout-dialog':'#start-loadout-close','#deck-dialog':'#deck-close','#hand-rules-dialog':'#hand-rules-close','#shop-upgrade-dialog':'#shop-upgrade-cancel'};
     const close=closeByDialog[top];if(close){const e=__get(close);if(typeof e.onclick==='function')e.onclick();}
   }
   break;
 }
 case 'close-gallery':__get('#native-gallery').close();break;
 default:throw Error('Unknown native action: '+a.type);
 }
 __flush();return true;
}
function NativeRestoreStorage(json){__storage=JSON.parse(json);syncPermanentPersonaPool();try{const saved=JSON.parse(localStorage.getItem('persona-loadout')||'null');if(Array.isArray(saved))equippedPersonaIds=normalizedGalleryLoadout(saved)}catch{}return true}



