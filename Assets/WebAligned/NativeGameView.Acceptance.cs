using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        // Only invoked by --capture --six-checks. Uses real UGUI raycasts and pointer click handlers.
        void ClickVisible(string name)
        {
            Canvas.ForceUpdateCanvases();var buttons=pageRoot.GetComponentsInChildren<Button>().Where(b=>b.name==name&&b.interactable).ToArray();
            if(buttons.Length!=1)throw new InvalidOperationException("Expected one visible button: "+name+"; got "+buttons.Length);
            var button=buttons[0];var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Button is covered or cannot receive input: "+name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        IEnumerator CapturePage(string folder,string name)
        {
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.2f);
        }
        void DragPersona(string sourceName,int targetSlot,bool cancel=false)
        {
            Canvas.ForceUpdateCanvases();
            var source=pageRoot.GetComponentsInChildren<NativePersonaDrag>().Single(d=>d.name==sourceName);
            var rect=(RectTransform)source.transform;
            var position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+25,rect.rect.yMax-25)));
            var pointer=new PointerEventData(EventSystem.current){position=position,button=PointerEventData.InputButton.Left,pointerDrag=source.gameObject};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IBeginDragHandler>(hits[0].gameObject)==source.gameObject,"Persona drag source is covered: "+sourceName);
            ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.beginDragHandler);Require(source.Dragging,"Drag did not create a preview.");
            if(cancel){HandleEscape();Require(!source.Dragging,"Escape did not cancel drag.");return;}
            var target=pageRoot.GetComponentsInChildren<NativePersonaDropSlot>().Single(d=>d.name=="阵容槽位 "+targetSlot);
            var destination=(RectTransform)target.transform;pointer.position=RectTransformUtility.WorldToScreenPoint(null,destination.TransformPoint(destination.rect.center));
            ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.dragHandler);hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
            Require(hits.Count>0&&ExecuteEvents.GetEventHandler<IDropHandler>(hits[0].gameObject)==target.gameObject,"Drop target is covered.");
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.dropHandler);
            if(source!=null)ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.endDragHandler);
        }
        IEnumerator CheckLoadout(string folder)
        {
            var original=state["pendingLoadout"].Select(S).ToArray();string saved=state["storage"].ToString();
            DragPersona("阵容槽位 0",1);yield return null;
            Require(S(state["pendingLoadout"][1])==original[0]&&S(state["pendingLoadout"][0])==original[1],"Slot drag did not swap personas.");
            var before=state["pendingLoadout"].ToString();DragPersona("阵容槽位 1",0,true);yield return null;
            Require(state["pendingLoadout"].ToString()==before&&S(state["dialog"])=="#start-loadout-dialog","Cancelling drag changed loadout or closed dialog.");
            ClickVisible("移除槽位 0");yield return null;
            Require((bool)state["disabled"]["#start-loadout-confirm"],"Incomplete loadout allows start.");
            Require(!pageRoot.GetComponentsInChildren<Button>().Single(b=>b.name=="确认阵容并开始").interactable,"Start button ignores incomplete loadout.");
            Require(pageRoot.GetComponentsInChildren<Button>().Single(b=>b.name=="确认阵容并开始").GetComponent<CanvasGroup>().alpha<.5f,"Disabled start button is not visibly dimmed.");
            yield return CapturePage(folder,"loadout-empty-slot");
            var scroll=pageRoot.GetComponentsInChildren<ScrollRect>().Single(r=>r.name=="Persona viewport");scroll.verticalNormalizedPosition=.25f;
            ClickVisible("阵容槽位 2");yield return null;
            scroll=pageRoot.GetComponentsInChildren<ScrollRect>().Single(r=>r.name=="Persona viewport");Require(Mathf.Abs(scroll.verticalNormalizedPosition-.25f)<.03f,"Selecting a slot reset library scroll.");
            scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();
            string first=S(state["library"][0]["id"]);DragPersona("查看人格 · "+first,0);yield return null;
            Require(S(state["pendingLoadout"][0])==first,"Library drag did not equip persona.");
            Require(state["storage"].ToString()==saved,"Pending loadout edits modified saves.");
            for(int i=0;i<original.Length;i++){Act("loadout-place",original[i],i);yield return null;}
            yield return CapturePage(folder,"loadout-ready");
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void SetSlider(string name,float value){var slider=pageRoot.GetComponentsInChildren<Slider>().Single(s=>s.name==name);slider.value=value;}
        IEnumerator CaptureSixFixes(string folder)
        {
            yield return null;
            voicePool.Configure(false,true,1);
            var poolClip=Resources.Load<AudioClip>("WebAudio/sfx/card-slide-1");
            for(int i=0;i<3;i++)Require(voicePool.TryPlay(poolClip,.01f),"Audio pool rejected an available voice.");
            Require(!voicePool.TryPlay(poolClip,.01f),"Audio pool exceeded three copies of a recording.");
            foreach(var file in new[]{"card-slide-2","card-slide-3","chip-lay-1","chip-lay-2","card-place-1","card-place-2","chips-stack-1"})Require(voicePool.TryPlay(Resources.Load<AudioClip>("WebAudio/sfx/"+file),.01f),"Audio pool rejected a free global slot.");
            Require(voicePool.ActiveCount==10&&!voicePool.TryPlay(Resources.Load<AudioClip>("WebAudio/sfx/card-shove-1"),.01f),"Audio pool exceeded the global cap.");
            voicePool.Configure(false,false,1);Require(voicePool.ActiveCount==0&&!voicePool.TryPlay(poolClip,.01f),"Disabled audio did not stop and reject effects.");
            voicePool.Configure(false,true,1);Require(voicePool.TryPlay(poolClip,.01f),"Stopped voice was not reusable.");
            voicePool.Configure(false,false,0);ApplyAudio((bool)state["menu"]);Debug.Log("NATIVE_AUDIO_POOL_PASSED");
            yield return new WaitForSecondsRealtime(1);
            ClickVisible("人格图鉴");yield return null;
            var galleryId=S(state["library"][0]["id"]);var galleryCard=pageRoot.GetComponentsInChildren<Button>().Single(b=>b.name=="查看人格 · "+galleryId);
            Require(galleryCard.GetComponentInChildren<RawImage>().rectTransform.rect.height>350,"Gallery did not use full portrait cards.");
            yield return CapturePage(folder,"gallery-portrait-grid");var galleryList=pageRoot.GetComponentsInChildren<ScrollRect>().Single(r=>r.name=="Persona viewport");galleryList.verticalNormalizedPosition=.25f;galleryId=S(state["library"][4]["id"]);yield return null;ClickVisible("查看人格 · "+galleryId);yield return null;Require(S(state["dialog"])=="#persona-detail-dialog","Gallery art did not open details.");HandleEscape();yield return null;Require(S(state["dialog"])=="#native-gallery","Gallery detail did not return to gallery.");galleryList=pageRoot.GetComponentsInChildren<ScrollRect>().Single(r=>r.name=="Persona viewport");Require(Mathf.Abs(galleryList.verticalNormalizedPosition-.25f)<.03f,"Gallery return lost scroll position.");ClickVisible("关闭");yield return null;ClickVisible("人格图鉴");yield return null;Require(pageRoot.GetComponentsInChildren<ScrollRect>().Single(r=>r.name=="Persona viewport").verticalNormalizedPosition>.99f,"Fresh gallery retained stale position.");ClickVisible("关闭");yield return null;
            ClickVisible("设置");yield return null;yield return null;SetSlider("画面亮度",.7f);yield return CapturePage(folder,"settings-70");
            foreach(var control in pageRoot.GetComponentsInChildren<Slider>()){Require(control.handleRect.rect.width<=20,"Slider handle is stretched: "+control.name);Require(control.fillRect.rect.width<=((RectTransform)control.transform).rect.width+1,"Slider fill overflows: "+control.name);}SetSlider("画面亮度",1.2f);yield return CapturePage(folder,"settings-120");ClickVisible("取消");yield return null;Require(Mathf.Approximately(brightness,1),"Cancel changed saved brightness.");
            ClickVisible("设置");yield return null;yield return null;SetSlider("画面亮度",.75f);ClickVisible("保存设置");yield return null;ClickVisible("设置");yield return null;ClickVisible("恢复默认");yield return null;Require(Mathf.Approximately(brightness,1),"Default brightness mismatch.");ClickVisible("取消");yield return null;Require(Mathf.Approximately(brightness,.75f),"Defaults were persisted by Cancel.");
            ClickVisible("设置");yield return null;ClickVisible("恢复默认");yield return null;ClickVisible("保存设置");yield return null;
            ClickVisible("开始游戏");yield return null;string id=S(state["library"][0]["id"]);ClickVisible("查看人格 · "+id);yield return null;Require(S(state["dialog"])=="#persona-detail-dialog","Details did not open.");yield return CapturePage(folder,"persona-detail");HandleEscape();yield return null;Require(S(state["dialog"])=="#start-loadout-dialog","Details did not return to loadout.");
            yield return CheckLoadout(folder);
            ClickVisible("确认阵容并开始");yield return null;yield return null;ClickVisible("开始战斗");yield return null;yield return new WaitForSecondsRealtime(2);if(S(state["dialog"])=="#native-tutorial")HandleEscape();yield return null;
            var authoredPosition=personaRects[0].anchoredPosition;var originalPoint=EffectPoint(personaRects[0],new Vector2(1,.5f));
            personaRects[0].anchoredPosition+=new Vector2(23,-11);
            Require(Vector2.Distance(EffectPoint(personaRects[0],new Vector2(1,.5f))-originalPoint,new Vector2(23,11))<.1f,"Persona effect does not follow edited layout.");
            personaRects[0].anchoredPosition=authoredPosition;
            yield return CheckPersonaTiming(folder);
            string uid=S(state["hand"][0]["uid"]);ClickVisible("Card "+uid);yield return null;Require(message.text==T("#message"),"Selection prompt is stale.");Require(cards[0].GetComponentInChildren<NativeFrameGraphic>().color.a>0,"Selected card highlight missing.");ClickVisible("花色");yield return null;Require(S(state["handSortMode"])=="suit"&&S(state["hand"][(int)state["selected"][0]]["uid"])==uid,"Suit sort lost selection.");yield return CapturePage(folder,"battle-suit-sort");ClickVisible("大小");yield return null;Require(S(state["handSortMode"])=="rank","Rank sort failed.");
            ClickVisible("查看人格 · "+S(state["personas"][0]["id"]));yield return null;yield return CapturePage(folder,"equipped-persona-detail");HandleEscape();yield return null;
            ClickVisible("battle-tools/settings-icon-v3");yield return null;ClickVisible("重播教学");yield return null;Require(S(state["dialog"])=="#native-tutorial","Replay failed.");yield return CapturePage(folder,"replay-tutorial");HandleEscape();yield return null;Require(settingsOpen,"Tutorial did not return to settings.");ClickVisible("取消");yield return null;
            ClickVisible("battle-tools/settings-icon-v3");yield return null;ClickVisible("返回主菜单");yield return null;string saved=state["storage"].ToString();ClickVisible("开始游戏");yield return null;Require(S(state["dialog"])=="#native-new-run-confirm","Overwrite confirmation missing.");yield return CapturePage(folder,"new-run-confirm");ClickVisible("取消");yield return null;Require(saved==state["storage"].ToString(),"Cancel overwrote the run.");
            ClickVisible("开始游戏");yield return null;ClickVisible("继续");yield return null;ClickVisible("返回");yield return null;Require(saved==state["storage"].ToString(),"Cancelling loadout overwrote the run.");ClickVisible("继续游戏");yield return null;Require(!(bool)state["menu"],"Existing run did not resume.");yield return CapturePage(folder,"resumed-battle");
            var previousHands=(int)state["hands"];var maximumHands=T("#hands-max");
            Require(pageRoot.GetComponentsInChildren<Text>().Single(t=>t.name=="出牌次数上限").text=="/ "+maximumHands,"Action maximum differs from web.");
            if(state["selected"].Count()==0){ClickVisible("Card "+S(state["hand"][0]["uid"]));yield return null;}
            ClickVisible("出牌");yield return null;yield return new WaitUntil(()=>!busy);
            Require((int)state["hands"]==previousHands-1,"Playing did not consume exactly one action.");
            Require(T("#hands-max")==maximumHands,"Action maximum changed after play.");
            Require(Mathf.Abs(scoreProgressFill.sizeDelta.x-(ScoreBarWidth-2)*ScoreRatio((float)state["score"]))<.1f,"Score progress differs from rules.");
            var handFill=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="出牌次数消耗条");
            Require(Mathf.Abs(handFill.sizeDelta.x-462*(int)state["hands"]/float.Parse(maximumHands))<.1f,"Action consumption bar is stale.");
            yield return CapturePage(folder,"web-status-after-play");
            Require(!pageRoot.GetComponentsInChildren<Button>(true).Any(b=>b.name=="查看本手得分详情"),"Removed score details button still exists.");
            string unchangedHand=state["hand"].ToString();ClickVisible("battle-tools/deck-icon-v3");yield return null;
            ClickVisible("牌库花色");yield return null;Require(S(state["deckSortMode"])=="suit","Deck suit sorting did not change mode.");
            yield return CapturePage(folder,"deck-suit-sort");ClickVisible("牌库大小");yield return null;Require(S(state["deckSortMode"])=="rank","Deck rank sorting failed.");
            ClickVisible("已用牌");yield return null;Require(state["deckCards"].Count()>0,"Played pile is empty after a play.");ClickVisible("牌库花色");yield return null;
            ClickVisible("已弃牌");yield return null;Require(state["deckCards"].Count()==0,"Unexpected discarded cards.");yield return CapturePage(folder,"deck-empty-pile");
            ClickVisible("当前牌库");yield return null;Require(S(state["deckSortMode"])=="suit","Changing pile lost sorting mode.");ClickVisible("关闭");yield return null;
            Require(state["hand"].ToString()==unchangedHand,"Deck sorting reordered battle hand.");
            yield return new WaitForSecondsRealtime(.75f);
            var resultCount=musicMixer.StingerPlayCount;musicMixer.Configure(false,false,true,.8f);musicMixer.Result("victory");
            Require(musicMixer.StingerPlayCount==resultCount+1,"Sfx fallback did not play a result cue.");
            musicMixer.Result("victory");Require(musicMixer.StingerPlayCount==resultCount+1,"Duplicate result cue played.");
            musicMixer.Configure(false,false,false,.8f);yield return new WaitForSecondsRealtime(.75f);musicMixer.Result("failure");
            Require(musicMixer.StingerPlayCount==resultCount+1,"Muted result cue played.");ApplyAudio((bool)state["menu"]);
            core.EvaluateForTest("{const adjustment=10-cardGoldThisBattle-personaGoldThisBattle;coins+=adjustment;earnedThisBattle+=adjustment;cardGoldThisBattle=3;personaGoldThisBattle=7;score=battleTarget();finish(true);__flush();}");state=core.Snapshot();Render();yield return null;
            Require(pageRoot.GetComponentsInChildren<Text>().Single(t=>t.name=="结算奖励 · 卡牌额外金币").text=="+ 3","Missing card coin reward.");
            Require(pageRoot.GetComponentsInChildren<Text>().Single(t=>t.name=="结算奖励 · 人格额外金币").text=="+ 7","Missing persona coin reward.");
            yield return CapturePage(folder,"settlement-all-rewards");ClickVisible(T("#result-primary"));yield return null;
            Require(S(state["dialog"])=="#stage-intro-dialog","Reward continuation failed.");
            yield return CheckUpgradePage(folder);
            Debug.Log("NATIVE_SIX_FIXES_PASSED");
        }
    }
}









