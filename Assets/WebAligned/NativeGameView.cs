using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using PersonaCards.UI;
using PersonaCards.Cards;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView : MonoBehaviour
    {
        static readonly Color Gold = new Color32(211,175,99,255), Ink = new Color32(14,12,11,238), Pale = new Color32(243,225,185,255), Muted = new Color32(163,152,134,255);
        public NativePageTemplate[] EditablePages = Array.Empty<NativePageTemplate>();
        public Font InterfaceFont;
        NativePageFactory pageFactory; RectTransform pageRoot; bool baking; string activePageId;
        readonly Dictionary<string,Sprite> sprites=new();
        readonly List<RectTransform> playedSlots=new();
        NativeRules core; JObject state; Font font; RectTransform root, content, fxRoot, handArea;
        readonly Dictionary<int,NativeCardMotion> cards = new();
        readonly Dictionary<int,Vector2> cardTargets = new();
        readonly List<RectTransform> personaRects = new();
        Text previewType, chipsLabel, multLabel, previewScore, message, totalLabel;
        Button playButton, discardButton; bool busy, settingsOpen; int selectedLoadoutSlot; float speed{get=>settings.Draft.Speed;set=>settings.Draft.Speed=value;}
        string error = ""; AudioSource audioSource,musicSource; AudioClip clickSound, dealSound, scoreSound; NativeMusicMixer musicMixer;
        NativeSettingsSession settings=new NativeSettingsSession();
        float brightness{get=>settings.Draft.Brightness;set=>settings.Draft.Brightness=value;}
        float volume{get=>settings.Draft.Volume;set=>settings.Draft.Volume=value;}
        bool musicOn{get=>settings.Draft.Music;set=>settings.Draft.Music=value;}
        bool sfxOn{get=>settings.Draft.Sfx;set=>settings.Draft.Sfx=value;}
        bool motionOn{get=>settings.Draft.Motion;set=>settings.Draft.Motion=value;}
        bool shakeOn{get=>settings.Draft.Shake;set=>settings.Draft.Shake=value;}
        Image dimmer;Material brightnessMaterial;bool settingsPersistence=true;
        bool resolutionFast;float PlaybackSpeed=>speed*(resolutionFast?4:1);
        public JObject CurrentState => state;
        public NativeRules Rules => core;
        void Awake()
        {
            Application.targetFrameRate=60;
            var cameraObject=new GameObject("Native Camera",typeof(Camera),typeof(AudioListener));cameraObject.transform.SetParent(transform,false);cameraObject.transform.position=new Vector3(0,0,-10);var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
            font=InterfaceFont!=null?InterfaceFont:Font.CreateDynamicFontFromOSFont(new[]{"Noto Serif SC","SimSun","Microsoft YaHei","Arial"},32);
            foreach(var page in EditablePages)if(page!=null)page.gameObject.SetActive(false);
            var authoredCanvas=transform.Find("Editable UI");if(authoredCanvas!=null)authoredCanvas.gameObject.SetActive(false);
            var canvas=new GameObject("Native UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().vertexColorAlwaysGammaSpace=true;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root=canvas.GetComponent<RectTransform>();
            var events=new GameObject("Native Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform,false);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.48f;musicSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicMixer=gameObject.AddComponent<NativeMusicMixer>();musicMixer.Initialize(musicSource);voicePool=GetComponent<NativeAudioVoicePool>();
            clickSound=Resources.Load<AudioClip>("WebAudio/sfx/click_001");dealSound=Resources.Load<AudioClip>("WebAudio/sfx/card-slide-1");scoreSound=Resources.Load<AudioClip>("WebAudio/sfx/chip-lay-1");
            settingsPersistence=!Environment.GetCommandLineArgs().Contains("--capture");
            settings=new NativeSettingsSession(settingsPersistence?PlayerPrefs.GetString("web-aligned-settings-v1","{}"):null);
            try { core=new NativeRules(!Environment.GetCommandLineArgs().Contains("--capture"));state=core.Snapshot();Render(); }catch(Exception e){error=e.ToString();Debug.LogException(e);RenderError();}
        }
        void OnDestroy(){core?.Dispose();if(brightnessMaterial!=null){if(Application.isPlaying)Destroy(brightnessMaterial);else DestroyImmediate(brightnessMaterial);}foreach(var sprite in sprites.Values)if(sprite!=null&&Application.isPlaying)Destroy(sprite);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var capture=Array.IndexOf(args,"--capture");
            if(capture<0||capture+1>=args.Length)yield break;
            var folder=args[capture+1];System.IO.Directory.CreateDirectory(folder);
            if(args.Contains("--shop-feedback-checks")){yield return CaptureShopFeedback(folder);yield break;}
            if(args.Contains("--six-checks")){yield return CaptureSixFixes(folder);yield break;}
            yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-menu.png"));
            yield return new WaitForSecondsRealtime(.5f);Act("loadout");yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-loadout.png"));
            Act("click","#start-loadout-confirm");yield return new WaitForSecondsRealtime(.5f);Act("click","#stage-start");yield return new WaitForSecondsRealtime(2);yield return new WaitUntil(()=>!busy);Act("click","#tutorial-skip");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-battle.png"));
            Act("toggle",index:0);Act("toggle",index:1);yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-selected.png"));
            Act("play");yield return new WaitForSecondsRealtime(.65f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-resolution.png"));
            yield return new WaitForSecondsRealtime(5);yield return new WaitUntil(()=>!busy);System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"native-state.json"),state.ToString());
            Act("click","#table-pile");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-deck.png"));Act("click","#deck-done");
            settingsOpen=true;Render();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-settings.png"));settingsOpen=false;
            var captured=new HashSet<string>();
            for(int guard=0;guard<85;guard++)
            {
                state=core.Snapshot();Render();yield return null;var dialog=S(state["dialog"]);if((bool)state["menu"])break;
                if(dialog!=""&&captured.Add(dialog)){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"native-"+dialog.TrimStart('#')+".png"));System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"state-"+dialog.TrimStart('#')+".json"),state.ToString());}
                switch(dialog)
                {
                    case "#stage-intro-dialog":state=core.Action("click","#stage-start");break;
                    case "#native-tutorial":state=core.Action("click","#tutorial-skip");break;
                    case "":core.Action("toggle",index:0);core.Action("play");core.EvaluateForTest("score=battleTarget();finish(true);__flush();");break;
                    case "#settlement-dialog":state=core.Action("click","#result-primary");break;
                    case "#persona-growth-dialog":
                        var generated=state["growth"]["persona"].ToString();var growthPage=pageRoot;var growthSaved=state["storage"].ToString();
                        var growthScrolls=pageRoot.GetComponentsInChildren<ScrollRect>();foreach(var scroll in growthScrolls)scroll.verticalNormalizedPosition=.35f;
                        var positions=growthScrolls.Select(s=>s.verticalNormalizedPosition).ToArray();
                        ClickVisible("成长槽位 1");yield return null;
                        Require(pageRoot==growthPage,"Growth selection rebuilt the page.");
                        Require(growthSaved==state["storage"].ToString(),"Growth preview changed storage.");
                        for(int k=0;k<growthScrolls.Length;k++)Require(Mathf.Abs(growthScrolls[k].verticalNormalizedPosition-positions[k])<.03f,"Growth selection lost reading position.");
                        Act("growth-slot",index:2);Act("growth-slot",index:1);yield return new WaitForSecondsRealtime(.25f);
                        Require(growthNoteGroup==null||growthNoteGroup.alpha==growthNoteAlpha,"Rapid growth selection left faded text.");
                        var enabledMotion=motionOn;motionOn=false;Act("growth-slot",index:0);Require(growthSelectionMotion==null,"Disabled motion still animates growth selection.");Act("growth-slot",index:1);motionOn=enabledMotion;
                        Require((int)state["growth"]["selectedSlot"]==1&&state["growth"]["persona"].ToString()==generated,"Growth selection mutated generated persona.");
                        Require(pageRoot.GetComponentsInChildren<Text>().Single(t=>t.name=="成长替换说明").text==T("#persona-growth-replace-note"),"Growth replacement note differs from web.");
                        yield return CapturePage(folder,"growth-compare-"+S(state["node"]["id"]));ClickVisible(T("#persona-growth-confirm"));break;
                    case "#shop-dialog":
                        core.Action("shop-tab","forge");state=core.Action("shop-persona",S(state["pool"].Last()["id"]));Render();yield return null;
                        var affixList=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="铸造副属性列表");
                        var affixScroll=affixList.GetComponentInParent<ScrollRect>();Require(affixScroll!=null&&affixScroll.viewport.GetComponent<RectMask2D>()!=null,"Forge affixes are not clipped to a scroll viewport.");
                        affixScroll.verticalNormalizedPosition=0;
                        var browse=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="商店浏览列表").GetComponentInParent<ScrollRect>();browse.verticalNormalizedPosition=.25f;
                        var selectedPersona=S(state["shop"]["selectedPersonaId"]);var savedShop=state["storage"].ToString();Act("shop-persona",selectedPersona);yield return null;
                        browse=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="商店浏览列表").GetComponentInParent<ScrollRect>();
                        Require(Mathf.Abs(browse.verticalNormalizedPosition-.25f)<.03f,"Forge selection reset list scroll.");Require(savedShop==state["storage"].ToString(),"Reselecting persona changed storage.");
                        Act("shop-tab","goods");yield return null;Act("shop-tab","forge");yield return null;
                        browse=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="商店浏览列表").GetComponentInParent<ScrollRect>();Require(Mathf.Abs(browse.verticalNormalizedPosition-.25f)<.03f,"Forge tab lost scroll position.");
                        yield return CapturePage(folder,"native-forge-"+S(state["node"]["id"]));state=core.Action("click","#shop-leave");break;
                    case "#target-report-dialog":state=core.Action("click","#target-report-continue");break;
                    case "#target-carry-dialog":var ids=(JArray)state["carry"]["ids"];if(ids.Count>0)core.Action("carry-select",S(ids[0]));state=core.Action("click","#target-carry-confirm");break;
                    default:throw new InvalidOperationException("Unverified native dialog: "+dialog);
                }
            }
            Render();Debug.Log("NATIVE_VISUAL_JOURNEY_PASSED");
        }
        void Update()
        {
            if(Keyboard.current==null||state==null)return;
            if(busy&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)resolutionFast=true;
            if(Keyboard.current.escapeKey.wasPressedThisFrame){HandleEscape();return;}
            if(!busy&&!settingsOpen&&S(state["dialog"])=="#native-tutorial"){
                if(Keyboard.current.enterKey.wasPressedThisFrame||Keyboard.current.rightArrowKey.wasPressedThisFrame)Act("click","#tutorial-next");
                else if(Keyboard.current.leftArrowKey.wasPressedThisFrame)Act("click","#tutorial-prev");return;
            }
            if(busy||settingsOpen||(bool)state["menu"]||S(state["dialog"])!="")return;
            if(Keyboard.current.spaceKey.wasPressedThisFrame&&playButton!=null&&playButton.interactable)Act("play");
            if(Keyboard.current.dKey.wasPressedThisFrame&&discardButton!=null&&discardButton.interactable)Act("discard");
        }
        public void Act(string type,string id=null,int index=0)
        {
            if(busy)return;
            if(type=="play"||type=="discard"){if(!settingsOpen&&NativeBattleInput.CanResolve(state,type))StartCoroutine(Resolve(type));return;}
            try
            {
                if(type=="loadout"||type=="new-run-confirm")loadoutScroll=1;
                if(type=="gallery")galleryScroll=1;
                var before=state;RememberFeedbackShop(before);
                state=core.Action(type,id,index);PlayOperationAudio(type);
                if((bool?)state["settingsRequested"]==true){settings.Begin();settingsOpen=true;}
                if(type=="growth-slot"&&UpdateGrowthSelection(before))return;
                if(type=="toggle"){UpdateSelection();return;}
                Render();ShopFeedback(before,type,id,index);
                if(type=="click"&&id=="#stage-start")StartCoroutine(OpeningDeal());
            }catch(Exception e){error=e.Message;Debug.LogException(e);RenderError();}
        }
        void Render()
        {
            StopGrowthSelectionMotion();
            RememberShopScrolls();
            if((activePageId=="Loadout"||activePageId=="Gallery")&&pageRoot!=null){var old=pageRoot.GetComponentsInChildren<ScrollRect>().FirstOrDefault(r=>r.name=="Persona viewport");if(old!=null){if(activePageId=="Gallery")galleryScroll=old.verticalNormalizedPosition;else loadoutScroll=old.verticalNormalizedPosition;}}
            foreach(Transform child in root){child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}
            activePageId=PageId(state,settingsOpen);
            var template=baking?null:EditablePages.FirstOrDefault(p=>p!=null&&p.PageId==activePageId);
            pageRoot=template!=null?(RectTransform)Instantiate(template.gameObject,root,false).transform:new GameObject(activePageId,typeof(RectTransform)).GetComponent<RectTransform>();
            pageRoot.SetParent(root,false);pageRoot.gameObject.SetActive(true);pageRoot.anchorMin=pageRoot.anchorMax=pageRoot.pivot=new Vector2(.5f,.5f);pageRoot.anchoredPosition=Vector2.zero;pageRoot.sizeDelta=new Vector2(1920,1080);pageRoot.localScale=Vector3.one;
            pageFactory=new NativePageFactory(pageRoot,pageRoot.GetComponent<NativePageTemplate>());
            cards.Clear();cardTargets.Clear();personaRects.Clear();playedSlots.Clear();
            content=Rect(pageRoot,"Content",0,0,1920,1080);fxRoot=Rect(pageRoot,"Native Effects",0,0,1920,1080);
            var menu=(bool)state["menu"];
            Picture(content,menu?"backgrounds/image080":"backgrounds/image082",0,0,1920,1080);
            Panel(content,0,0,1920,1080,new Color(0,0,0,.16f),false);
            if(menu)RenderMenu();else RenderBattle();
            var dialog=S(state["dialog"]);if(dialog!="")RenderDialog(dialog);
            if(settingsOpen)RenderSettings();
            fxRoot.SetAsLastSibling();
            dimmer=Panel(pageRoot,0,0,1920,1080,Color.clear,false).GetComponent<Image>();dimmer.raycastTarget=false;
            pageFactory.Finish();
            if(activePageId=="Battle"||activePageId=="Upgrade"||WarmCommercePage)NativeSurfaceFinish.ApplyPage(pageRoot);
            CaptureGrowthAppearance();
            RestoreShopScrolls();
            if((activePageId=="Loadout"||activePageId=="Gallery")&&!baking){Canvas.ForceUpdateCanvases();var list=pageRoot.GetComponentsInChildren<ScrollRect>().FirstOrDefault(r=>r.name=="Persona viewport");if(list!=null)list.verticalNormalizedPosition=activePageId=="Gallery"?galleryScroll:loadoutScroll;}
            foreach(var pair in cards){var r=(RectTransform)pair.Value.transform;pair.Value.Setup(r.anchoredPosition,((JArray)state["selected"]).Any(v=>(int)v==pair.Key),-(pair.Key-(((JArray)state["hand"]).Count-1)*.5f)*1.5f);}
            NativeCardMotion.AnimationEnabled=motionOn;NativeButtonMotion.AnimationEnabled=motionOn;
            if(!baking){ApplyAudio(menu);ApplyBrightness();}
        }
        void RenderMenu()
        {
            Label(content,"人格牌",625,175,670,130,96,Pale,TextAnchor.MiddleCenter);
            Label(content,"◆",630,330,660,42,23,Gold,TextAnchor.MiddleCenter);
            Rule(content,770,370,380);Label(content,"用牌局，认识人格",630,400,660,55,27,Muted,TextAnchor.MiddleCenter);
            var start=Btn(content,"开始游戏",660,510,600,80,()=>Act("loadout"),true);DecorateButton(start,"main-menu/start-game-v2");
            var cont=Btn(content,"继续游戏",660,600,600,80,()=>Act("continue"));cont.interactable=(bool)state["canContinue"];DecorateButton(cont,"main-menu/start-game-v2");
            var gallery=Btn(content,"人格图鉴",660,690,600,80,()=>Act("gallery"));DecorateButton(gallery,"main-menu/persona-gallery-v2");var settings=Btn(content,"设置",660,780,600,80,OpenSettings);DecorateButton(settings,"main-menu/settings-v2");
            Label(content,"镜子会记住每一次选择",560,935,800,45,20,Muted,TextAnchor.MiddleCenter);
        }
        void RenderBattle()
        {
            var node=state["node"];
            Label(content,System.Text.RegularExpressions.Regex.IsMatch(S(node?["id"]),"^N[0-9]+$")?$"节点 {S(node?["id"]).Replace("N","")} / 17":"",1470,24,380,45,22,Muted,TextAnchor.MiddleRight);
            var toolRules=Btn(content,"",38,38,60,60,()=>Act("click","#open-hand-rules"));DecorateButton(toolRules,"battle-tools/hand-rules-icon-v3");var toolDeck=Btn(content,"",112,38,60,60,()=>Act("click","#table-pile"));DecorateButton(toolDeck,"battle-tools/deck-icon-v3");var toolSettings=Btn(content,"",186,38,60,60,OpenSettings);DecorateButton(toolSettings,"battle-tools/settings-icon-v3");
            var personas=(JArray)state["personas"];
            for(var i=0;i<4;i++)
            {
                var y=165+i*192;var panel=Panel(content,34,y,400,180,Ink);personaRects.Add(panel);
                if(personas[i]?.Type==JTokenType.Null){Label(panel,"空槽位",20,30,300,60,24,Muted);continue;}
                RenderPersonaSummary(panel,personas[i]);
            }
            var centerX=468f;for(var i=0;i<5;i++){
                var slot=Panel(content,centerX+31+i*169,76,153,222,new Color(.035f,.035f,.05f,.75f));
                playedSlots.Add(slot);Picture(slot,"card-back-occult-v1",0,0,153,222);NativePageFactory.Ensure<CanvasGroup>(slot.gameObject).alpha=.58f;
            }
            Rule(content,485,337,850);Label(content,"◆",780,317,270,40,18,Gold,TextAnchor.MiddleCenter);
            var preview=Panel(content,468,368,890,138,new Color(.045f,.032f,.018f,.94f));var previewFrame=preview.GetComponentInChildren<NativeFrameGraphic>();previewFrame.color=Gold;previewFrame.Thickness=2;
            for(int divider=0;divider<3;divider++)Panel(preview,260+divider*195,23,1,92,new Color(.65f,.49f,.26f,.42f),false);previewType=Label(preview,"等待选择",15,55,245, h:60,size:34,color:Pale,align:TextAnchor.MiddleCenter);
            Label(preview,"当前牌型",15,18,245,28,17,Muted,TextAnchor.MiddleCenter);
            Label(preview,"基础分",275,18,175,28,17,Muted,TextAnchor.MiddleCenter);chipsLabel=Label(preview,"—",275,55,175,60,36,Gold,TextAnchor.MiddleCenter);
            Label(preview,"倍率",475,18,165,28,17,Muted,TextAnchor.MiddleCenter);multLabel=Label(preview,"×—",475,55,165,60,36,Gold,TextAnchor.MiddleCenter);
            Label(preview,"最终得分",660,18,215,28,17,Muted,TextAnchor.MiddleCenter);previewScore=Label(preview,"—",660,55,215,60,42,Pale,TextAnchor.MiddleCenter);
            message=Label(content,T("#message"),490,525,850,46,22,Pale,TextAnchor.MiddleCenter);
            var messageShadow=NativePageFactory.Ensure<Shadow>(message.gameObject);messageShadow.effectColor=new Color(0,0,0,.95f);messageShadow.effectDistance=new Vector2(1,-2);
            // Keep the retired layout key so subsequent authored panel overrides stay attached.
            Rect(content,"Panel",915,581,420,48).gameObject.SetActive(false);
            handArea=Rect(content,"Hand",475,600,880,315);var hand=(JArray)state["hand"];
            for(var i=0;i<hand.Count;i++){
                var index=i;float spacing=Mathf.Min(114,820f/Math.Max(1,hand.Count-1));var x=440+(i-(hand.Count-1)*.5f)*spacing;
                var selected=((JArray)state["selected"]).Any(v=>(int)v==i);var target=new Vector2(x,selected?-42:0);cardTargets[i]=target;
                var offset=i-(hand.Count-1)*.5f;var rect=Card(handArea,hand[i],x-62,97+offset*offset*1.4f,124,190,()=>Act("toggle",index:index));
                var motion=NativePageFactory.Ensure<NativeCardMotion>(rect.gameObject);motion.Setup(rect.anchoredPosition,selected,-offset*1.5f);cards[i]=motion;
            }
            discardButton=Btn(content,"弃牌",555,935,270, h:86,()=>Act("discard"));
            var sorting=Panel(content,845,956,165,48,new Color(0,0,0,.65f));sorting.name="手牌排序";
            Label(sorting,"手牌排序",0,-27,165,23,16,Muted,TextAnchor.MiddleCenter);
            var byRank=Btn(sorting,"大小",4,3,76,42,()=>Act("sort","rank"),S(state["handSortMode"])=="rank");
            var bySuit=Btn(sorting,"花色",85,3,76,42,()=>Act("sort","suit"),S(state["handSortMode"])=="suit");
            byRank.interactable=bySuit.interactable=!(bool)state["inputLocked"];
            playButton=Btn(content,"出牌",1043,935,270,86,()=>Act("play"),true);
            DecorateButton(discardButton,"battle-actions/discard-button-v1");DecorateButton(playButton,"battle-actions/play-button-v1");
            var right=Panel(content,1390,92,490,82,Ink);right.name="对手名称";Label(right,S(node?["opponent"]?["name"]),20,10,450, h:60,size:29,color:Pale);
            var portrait=Panel(content,1390,190,490,230,Ink);Picture(portrait,Art(S(node?["opponent"]?["portrait"])),14,14,168,202);
            Label(portrait,"“镜中的倒影，\n还认识现在的你吗？”",210,44,250,135,24,Gold,TextAnchor.MiddleCenter);
            var rules=Panel(content,1390,440,490,140,Ink);Label(rules,"◈  本场规则",22,13,450,42,25,Gold);Label(rules,S((state["stageLimit"] as JObject)?["description"]) is string desc&&desc!=""?desc:"无",22,62,440, h:60,size:19,color:Muted);
            RenderBattleStatus();
            UpdateSelection();
        }
        void UpdateSelection()
        {
            if(message!=null&&!busy)message.text=T("#message");
            var selected=(JArray)state["selected"];foreach(var p in cards)p.Value.Select(selected.Any(v=>(int)v==p.Key));
            var preview=state["preview"];bool has=preview?.Type==JTokenType.Object;
            if(previewType!=null){previewType.text=has?S(preview["type"]):"等待选择";chipsLabel.text=has?S(preview["chips"]):"—";multLabel.text=has?"×"+S(preview["mult"])+(S(preview["xmult"])!="1"?" ×"+S(preview["xmult"]):""):"×—";previewScore.text=has?S(preview["total"]):"—";}
            if(playButton!=null)playButton.interactable=!busy&&!(bool)state["inputLocked"]&&selected.Count>0&&(int)state["hands"]>0;
            if(discardButton!=null)discardButton.interactable=!busy&&!(bool)state["inputLocked"]&&selected.Count>0&&(int)state["discards"]>0;
        }
        IEnumerator OpeningDeal(HashSet<string> retained=null)
        {
            if(!motionOn)yield break;
            busy=true;UpdateSelection();var list=cards.Where(pair=>retained==null||!retained.Contains(S(state["hand"][pair.Key]["uid"]))).Select(pair=>pair.Value).ToArray();foreach(var card in list)card.gameObject.SetActive(false);
            foreach(var card in list){card.gameObject.SetActive(true);card.Deal(new Vector2(850,-440));PlayCue("select");yield return new WaitForSecondsRealtime(.055f/speed);}
            yield return new WaitForSecondsRealtime(.45f/speed);busy=false;UpdateSelection();
        }
        IEnumerator Resolve(string type)
        {
            busy=true;resolutionFast=false;UpdateSelection();var chosen=((JArray)state["selected"]).Select(x=>(int)x).ToArray();
            var retained=((JArray)state["hand"]).Where((c,i)=>!chosen.Contains(i)).Select(c=>S(c["uid"])).ToHashSet();
            var views=chosen.Where(cards.ContainsKey).Select(i=>cards[i]).ToArray();
            foreach(var view in views)view.ExternalMotion=true;
            var starts=views.Select(v=>v.transform.position).ToArray();
            if(type=="play")for(int i=0;i<views.Length&&i<playedSlots.Count;i++){playedSlots[i].gameObject.SetActive(false);views[i].transform.SetParent(fxRoot,true);}
            for(float t=motionOn?0:1;t<1;t+=Time.unscaledDeltaTime*2.8f*PlaybackSpeed){
                for(var i=0;i<views.Length;i++){
                    var slot=playedSlots[Mathf.Min(i,playedSlots.Count-1)];var target=slot.TransformPoint(Vector3.zero);
                    if(type=="discard")target=starts[i]+Vector3.down*350+Vector3.right*90;
                    var u=Mathf.Clamp01(t-i*.035f);var e=1-Mathf.Pow(1-u,3);views[i].transform.position=Vector3.Lerp(starts[i],target,e)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*70);views[i].transform.localRotation=Quaternion.Euler(0,0,type=="discard"?u*-20:0);if(type=="play")views[i].transform.localScale=Vector3.Lerp(Vector3.one,new Vector3(153f/124f,222f/190f,1),e);
                    if(type=="discard")views[i].GetComponent<CanvasGroup>().alpha=1-u;
                }yield return null;
            }
            // Finish every selected card at its destination; staggered interpolation can end short.
            if(motionOn)for(int i=0;i<views.Length;i++){
                if(type=="play"){views[i].transform.position=playedSlots[Mathf.Min(i,playedSlots.Count-1)].TransformPoint(Vector3.zero);views[i].transform.localRotation=Quaternion.identity;views[i].transform.localScale=new Vector3(153f/124f,222f/190f,1);}
                else views[i].GetComponent<CanvasGroup>().alpha=0;
            }
            JObject next=null;try{next=core.Action(type);}catch(Exception e){error=e.Message;Debug.LogException(e);}
            if(next==null){busy=false;RenderError();yield break;}
            if(type=="play"&&!(next["lastScore"] is JObject)){state=next;busy=false;Render();yield break;}
            if(chosen.Length>0)PlayCue(type=="play"?"play":"discard");
            if(type=="play"&&motionOn){
                var result=next["lastScore"] as JObject;var evs=result?["events"] as JArray??new JArray();float chips=0,mult=0,xmult=1;
                foreach(var e in evs.Where(e=>S(e["phase"])!="汇总"&&S(e["phase"])!="人格调试")){
                    chips+=(float?)e["chipsDelta"]??0;mult+=(float?)e["multDelta"]??0;xmult*=(float?)e["xmultFactor"]??1;
                    message.text=S(e["source"])+" · "+S(e["detail"]);chipsLabel.text=chips.ToString("0.##");multLabel.text="×"+(mult*xmult).ToString("0.##");previewScore.text=Mathf.RoundToInt(chips*mult*xmult).ToString();PlayCue("tick");
                    if(S(e["phase"])=="人格牌"){
                        var source=S(e["source"]).Split('·')[0].Trim();
                        var matches=((JArray)state["personas"]).Select((p,i)=>new{p,i}).Where(x=>x.p?.Type==JTokenType.Object&&S(x.p["name"])==source).ToArray();
                        yield return ShowPersonaTrigger(matches.Length==1?matches[0].i:-1);
                    }else yield return WaitForPlayback(.11f);
                }
                previewScore.text=S(result?["total"]);previewType.text=S(result?["type"]);message.text="本手获得 "+S(result?["total"])+" 分";
                var floatText=Label(fxRoot,"+"+S(result?["total"]),660,535,560,100, size:68,color:Gold,align:TextAnchor.MiddleCenter);
                for(float t=0;t<1;t+=Time.unscaledDeltaTime*2*PlaybackSpeed){floatText.rectTransform.anchoredPosition=new Vector2(660,-535+t*35);floatText.transform.localScale=Vector3.one*(1+Mathf.Sin(t*Mathf.PI)*.08f);if(shakeOn)content.anchoredPosition=new Vector2(Mathf.Sin(t*71)*3*(1-t),Mathf.Cos(t*59)*2*(1-t));yield return null;}content.anchoredPosition=Vector2.zero;
            }
            if(type=="play"&&motionOn){for(float t=0;t<1;t+=Time.unscaledDeltaTime*2.5f*PlaybackSpeed){AnimateScoreTotal((int)next["score"],t);foreach(var view in views){var group=view.GetComponent<CanvasGroup>();group.alpha=1-t;view.transform.position+=Vector3.right*(Time.unscaledDeltaTime*130);}yield return null;}}
            if(type=="play"&&next["lastScore"]!=null&&S(next["dialog"])=="")PlayCue("score");
            state=next;busy=false;Render();PlayOperationAudio("resolution",false);if(S(state["dialog"])=="")yield return OpeningDeal(retained);
        }
        void RenderDialog(string id)
        {
            if(id=="#native-tutorial"){RenderTutorial();return;}
            if(id=="#native-new-run-confirm"){RenderNewRunConfirm();return;}
            if(id=="#persona-detail-dialog"){RenderPersonaDetails();return;}
            if(id=="#shop-dialog"){Picture(content,"backgrounds/image078",0,0,1920,1080);Panel(content,0,0,1920,1080,new Color(0,0,0,.28f),false);var shopPanel=Panel(content,70,60,1780,960,new Color(.025f,.02f,.015f,.55f));Label(shopPanel,"镜厅商店",50,20,1680,75,48,Gold);Rule(shopPanel,50,115,1680);RenderShopPage(shopPanel);return;}
            Panel(content,0,0,1920,1080,new Color(0,0,0,.7f),false);
            var panel=Panel(content,290,90,1340,900,new Color32(14,12,11,255));string title=id switch{"#stage-intro-dialog"=>S(state["node"]?["opponent"]?["name"]),"#settlement-dialog"=>T("#result-title"),"#shop-dialog"=>"镜厅商店","#persona-growth-dialog"=>"获得人格","#target-report-dialog"=>"本局报告","#target-carry-dialog"=>"选择永久带出的人格","#deck-dialog"=>"本局牌库","#hand-rules-dialog"=>"牌型规则","#start-loadout-dialog"=>"选择本局人格","#native-gallery"=>"人格图鉴",_=>T("#shop-upgrade-title")};
            Label(panel,title,45,20,1250,75,42,Pale,TextAnchor.MiddleCenter);Rule(panel,45,111,1250);
            if(id=="#stage-intro-dialog"){
                Picture(panel,Art(S(state["node"]?["opponent"]?["portrait"])),90,175,285,420);Label(panel,"目 标 分 数",500,220,640,50,25,Muted,TextAnchor.MiddleCenter);Label(panel,S(state["target"]),500,290,640,140,90,Gold,TextAnchor.MiddleCenter);Label(panel,T("#stage-rule"),500,470,640,120,25,Muted,TextAnchor.MiddleCenter);Click(panel,"开始战斗",450,735,440,"#stage-start");
            }else if(id=="#native-tutorial"){
                var step=state["tutorial"]["steps"][(int)state["tutorial"]["index"]];Label(panel,S(step["title"]),130,210,1080,100,44,Gold,TextAnchor.MiddleCenter);Label(panel,S(step["copy"]),200,360,940,200,32,Pale,TextAnchor.MiddleCenter);Label(panel,$"{(int)state["tutorial"]["index"]+1} / 4",500,600,340,50,24,Muted,TextAnchor.MiddleCenter);Click(panel,"上一步",80,770,340,"#tutorial-prev",false);Click(panel,"跳过教学",500,770,340,"#tutorial-skip",false);Click(panel,T("#tutorial-next"),920,770,340,"#tutorial-next");
            }else if(id=="#settlement-dialog"){
                Label(panel,T("#result-subtitle"),120,135,1100,65,24,Muted,TextAnchor.MiddleCenter);var scoreText=Label(panel,T("#result-score")+"  /  "+T("#result-target"),220,235,900,140, size:80,color:Gold,align:TextAnchor.MiddleCenter);scoreText.resizeTextForBestFit=true;scoreText.resizeTextMinSize=40;scoreText.resizeTextMaxSize=80;
                if((int)state["score"]>=(int)state["target"])RenderSettlementRewards(panel);else Label(panel,"距离目标还差 "+T("#result-gap")+" 分",260,430,820,160,30,Muted,TextAnchor.MiddleCenter);
                Click(panel,(int)state["score"]<(int)state["target"]?"查看本局报告":T("#result-primary"),440,730,460,"#result-primary");Click(panel,"返回主菜单",490,815,360,"#result-menu",false);
            }
            else if(id=="#persona-growth-dialog"){
                RenderGrowthPage(panel);
            }else if(id=="#deck-dialog"){
                if(!(bool)state["deckTarget"]){var tabs=new[]{"deck","used","discarded"};var names=new[]{"当前牌库","已用牌","已弃牌"};for(int i=0;i<3;i++){var tab=tabs[i];Btn(panel,names[i],170+i*330,130,300,50,()=>Act("deck-tab",tab),S(state["deckTab"])==tab);}}
                RenderDeckSorting(panel);
                CardGrid(panel,(JArray)state["deckCards"],(bool)state["deckTarget"]);Click(panel,(bool)state["deckTarget"]?"确认目标并购买":"关闭",740,800,420,(bool)state["deckTarget"]?"#deck-confirm":"#deck-done");if((bool)state["deckTarget"])Click(panel,"取消",170,800,420,"#deck-close",false);
            }else if(id=="#hand-rules-dialog"){
                var rules=(JArray)state["rules"];for(int i=0;i<rules.Count;i++){var r=rules[i];var x=i<6?70:710;var y=145+(i%6)*99;Label(panel,S(r["name"]),x,y,265,40,26,Pale);Label(panel,$"{r["chips"]} 筹码  × {r["mult"]}",x+270,y,270,40,25,Gold);Label(panel,S(r["description"]),x,y+42,565,47,16,Muted);}Click(panel,"返回牌桌",460,805,420,"#hand-rules-done");
            }else if(id=="#shop-upgrade-dialog"){
                RenderUpgradePage(panel);
            }else if(id=="#start-loadout-dialog"||id=="#native-gallery")RenderGallery(panel,id=="#start-loadout-dialog");
            else if(id=="#target-report-dialog"){
                Label(panel,T("#target-report-summary"),70,140,530,330,26,Gold);TextScroll(panel,T("#target-report-battles"),700,150,550,600,23,Pale);TextScroll(panel,T("#target-report-personas"),70,480,570,270,20,Muted);Click(panel,"进入人格带出",460,805,420,"#target-report-continue");
            }else if(id=="#target-carry-dialog"){
                var ids=(JArray)state["carry"]["ids"];var pool=(JArray)state["pool"];for(int i=0;i<ids.Count;i++){var key=S(ids[i]);var p=pool.FirstOrDefault(p=>S(p["id"])==key);PersonaCard(panel,p,65+i*400,150,375,430);Btn(panel,S(state["carry"]["selected"])==key?"已选择":"选择此人格",65+i*400,610,375,65,()=>Act("carry-select",key));}if(ids.Count==0)Label(panel,"本局在失败前没有生成人格，可以直接结束。",170,345,1000,140,34,Muted,TextAnchor.MiddleCenter);Label(panel,T("#target-carry-note"),80,710,1180,65,26,Muted,TextAnchor.MiddleCenter);Click(panel,T("#target-carry-confirm"),460,805,420,"#target-carry-confirm");
            }
            if(CanDismissDialog(id))Btn(panel,"×",1250,20,60,60,HandleEscape);
        }
        void RenderTutorial()
        {
            int index=(int)state["tutorial"]["index"];var step=state["tutorial"]["steps"][index];var focus=index switch{0=>new UnityEngine.Rect(1380,590,510,215),1=>new UnityEngine.Rect(470,640,900,310),2=>new UnityEngine.Rect(460,360,910,160),_=>new UnityEngine.Rect(25,155,420,775)};
            var shade=new Color(0,0,0,.74f);Panel(content,0,0,1920,focus.y,shade,false);Panel(content,0,focus.y,focus.x,focus.height,shade,false);Panel(content,focus.xMax,focus.y,1920-focus.xMax,focus.height,shade,false);Panel(content,0,focus.yMax,1920,1080-focus.yMax,shade,false);Panel(content,focus.x,focus.y,focus.width,focus.height,Color.clear,false);
            Rule(content,focus.x,focus.y,focus.width);Rule(content,focus.x,focus.yMax,focus.width);Panel(content,focus.x,focus.y,1,focus.height,Gold,false);Panel(content,focus.xMax,focus.y,1,focus.height,Gold,false);
            float x=index==0?1320:index==3?475:730,y=index==0?815:index==1?315:index==2?540:350;var p=Panel(content,x,y,570,250,Ink);Label(p,$"{index+1:00} / 04 · {S(step["label"])}",24,15,520,28,15,Muted);Label(p,S(step["title"]),24,48,520,45,30,Gold);Label(p,S(step["copy"]),24,105,520,80,20,Pale);
            var prev=Btn(p,"上一步",280,195,120,40,()=>Act("click","#tutorial-prev"));prev.interactable=index>0;Btn(p,"跳过教学",24,195,135,40,()=>Act("click","#tutorial-skip"));Btn(p,T("#tutorial-next"),415,195,130,40,()=>Act("click","#tutorial-next"),true);
        }
        bool WarmCommercePage=>activePageId=="Shop"||activePageId=="Forge"||activePageId=="PersonaGrowth";
        void RenderShopPage(RectTransform panel)
        {
            var resource=Panel(panel,50,155,230,585,new Color(0,0,0,.65f));var labels=new[]{"当前金币","牌库数量","已有人格"};var values=new[]{S(state["coins"]),((JArray)state["runDeck"]).Count.ToString(),((JArray)state["pool"]).Count.ToString()};var icons=new[]{"coin-v1","deck-v1","persona-v1"};for(int i=0;i<3;i++){Label(resource,labels[i],25,28+i*175,180,35,21,Muted);Picture(resource,"shop-resources/"+icons[i],25,80+i*175,62,62);Label(resource,values[i],110,85+i*175,100,60,40,Gold);}
            var browserPanel=Panel(panel,310,155,535,585,new Color(0,0,0,.63f));bool forge=S(state["shop"]["tab"])=="forge";Btn(browserPanel,"商品",15,15,245,58,()=>Act("shop-tab","goods"),!forge);Btn(browserPanel,"人格铸造",275,15,245,58,()=>Act("shop-tab","forge"),forge);
            var pool=(JArray)state["pool"];var offers=(JArray)state["shop"]["offers"];int count=forge?pool.Count:offers.Count;var list=ScrollBody(browserPanel,12,95,511,470,count*119);list.name="商店浏览列表";BindShopScroll(list,forge?"personas":"offers:"+string.Join(",",offers.Select(o=>S(o["item"]["id"]))));
            for(int i=0;i<count;i++)
            {
                if(forge){var p=pool[i];var b=Btn(list,"",0,i*119,497,108,()=>Act("shop-persona",S(p["id"])),S(state["shop"]["selectedPersonaId"])==S(p["id"]));Picture(b.transform,Art(S(p["portrait"])),15,10,58,88);Label(b.transform,S(p["name"]),90,16,390,35,26,Pale);Label(b.transform,S(p["effect"]),90,60,390,35,19,Gold);}
                else{var o=offers[i];var item=o["item"];var b=Btn(list,"",0,i*119,497,108,()=>Act("shop-select",S(item["id"])),S(state["shop"]["selectedItemId"])==S(item["id"]));if(S(o["art"])!="")Picture(b.transform,Art(S(o["art"])),15,10,58,88);else Label(b.transform,S(o["view"]["icon"]),15,18,58,70,35,Gold,TextAnchor.MiddleCenter);Label(b.transform,S(item["name"]),90,16,390,35,26,Pale);Label(b.transform,$"◉ {item["price"]}"+((int)o["purchaseCount"]>0?"  已购买":""),90,60,390,35,21,Gold);}
            }
            var detail=Panel(panel,875,155,850,585,new Color(0,0,0,.65f));detail.name="商店详情区域";
            if(forge){var p=pool.FirstOrDefault(p=>S(p["id"])==S(state["shop"]["selectedPersonaId"]));if(p!=null){PersonaCard(detail,p,25,25,800,260);var aff=(JArray)p["affixes"];var affixBody=ScrollBody(detail,30,310,790,250,aff.Count*110);affixBody.name="铸造副属性列表";BindShopScroll(affixBody,"affixes:"+S(p["id"]));if(aff.Count==0){var empty=Label(affixBody,NativeShopAffixPresentation.Empty,16,30,750,80,24,Muted);empty.name="铸造空状态";}for(int i=0;i<aff.Count;i++){int slot=i;var a=aff[i];var row=Panel(affixBody,0,i*110,790,100,new Color(0,0,0,.5f));Label(row,"副属性 "+(i+1),16,10,130,30,20,Muted);TextScroll(row,NativeShopAffixPresentation.Description(a),16,42,440,50,20,Pale);var unlock=Btn(row,(bool)a["unlocked"]?"已解锁":$"解锁 · {a["unlockCost"]} 金币",490,15,280,60,()=>Act("unlock",S(p["id"]),slot));unlock.interactable=!(bool)a["unlocked"]&&(bool?)a["availability"]?["allowed"]==true;}}}
            else{var o=offers.FirstOrDefault(o=>S(o["item"]["id"])==S(state["shop"]["selectedItemId"]));if(o!=null){if(S(o["art"])!="")Picture(detail,Art(S(o["art"])),40,115,235,355);else Label(detail,S(o["view"]["icon"]),40,160,235,180,90,Gold,TextAnchor.MiddleCenter);Label(detail,T("#shop-detail-name"),330,65,470,105,38,Pale);Rule(detail,330,185,470);TextScroll(detail,T("#shop-detail-effect"),330,220,470,290,25,Pale);}}
            TextScroll(panel,T("#shop-note"),50,770,1660,48,18,Muted);
            if(!forge){var refresh=Btn(panel,T("#shop-refresh"),710,835,320,80,()=>Act("click","#shop-refresh"));refresh.interactable=(bool?)state["disabled"]?["#shop-refresh"]!=true;DecorateButton(refresh,"shop-buttons/purchase-v1");var buy=Btn(panel,T("#shop-buy"),1055,835,320,80,()=>Act("click","#shop-buy"));buy.interactable=(bool?)state["disabled"]?["#shop-buy"]!=true;DecorateButton(buy,"shop-buttons/purchase-v1");}var leave=Btn(panel,"离开商店",1400,835,320,80,()=>Act("click","#shop-leave"));DecorateButton(leave,"shop-buttons/leave-v1");
        }
        RectTransform ScrollBody(RectTransform parent,float x,float y,float w,float h,float bodyHeight){var viewport=Rect(parent,"Scroll viewport",x,y,w,h);NativePageFactory.Ensure<Image>(viewport.gameObject).color=Color.clear;NativePageFactory.Ensure<RectMask2D>(viewport.gameObject);var scroll=NativePageFactory.Ensure<ScrollRect>(viewport.gameObject);var body=Rect(viewport,"Scroll content",0,0,w,Mathf.Max(h,bodyHeight));scroll.content=body;scroll.viewport=viewport;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;return body;}
        void TextScroll(RectTransform parent,string text,float x,float y,float w,float h,int size,Color color){var body=ScrollBody(parent,x,y,w,h,h);var label=Label(body,text,0,0,w,h,size,color);if(WarmCommercePage)label.lineSpacing=1.15f;float height=Mathf.Max(h,label.preferredHeight+12);body.sizeDelta=new Vector2(w,height);label.rectTransform.sizeDelta=new Vector2(w,height);}
        float loadoutScroll=1,galleryScroll=1;
        void RenderGallery(RectTransform panel,bool loadout)
        {
            var library=(JArray)state["library"];
            var equipped=(JArray)state["pendingLoadout"];
            if(loadout){
                for(int i=0;i<equipped.Count;i++){
                    int slot=i;var id=S(equipped[i]);var persona=library.FirstOrDefault(p=>S(p["id"])==id);
                    var button=Btn(panel,$"{i+1}  ·  "+(persona==null?"空槽位":S(persona["name"])),45+i*315,130,300,90,()=>{selectedLoadoutSlot=slot;Render();},selectedLoadoutSlot==i);
                    button.name="阵容槽位 "+i;
                    var label=button.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(persona==null?12:65,-5);label.rectTransform.sizeDelta=new Vector2(persona==null?240:185,70);label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=24;
                    NativePageFactory.Ensure<NativePersonaDropSlot>(button.gameObject).Bind(this,key=>Act("loadout-place",key,slot));
                    NativePageFactory.Ensure<NativePersonaDrag>(button.gameObject).Bind(this,id,persona==null?"":S(persona["name"]),root,font);
                    if(persona!=null){
                        if(S(persona["portrait"])!="")Picture((RectTransform)button.transform,Art(S(persona["portrait"])),10,10,45,70);
                        var remove=Btn(button.transform,"×",255,5,40,40,()=>Act("loadout-remove",index:slot));remove.name="移除槽位 "+i;
                    }
                }
                Label(panel,T("#start-loadout-count")+"  ·  "+T("#start-loadout-note"),45,226,1250,36,22,Muted);
            }
            if(!loadout)Label(panel,"永久收藏 · 点击原画可阅读详细能力",45,120,1250,30,19,Muted);
            var viewport=Rect(panel,"Persona viewport",35,loadout?275:165,1270,loadout?495:605);
            NativePageFactory.Ensure<Image>(viewport.gameObject).color=Color.clear;NativePageFactory.Ensure<RectMask2D>(viewport.gameObject);
            var scroll=NativePageFactory.Ensure<ScrollRect>(viewport.gameObject);var rowHeight=loadout?300:490;
            var body=Rect(viewport,"Personas",0,0,1270,Mathf.Max(viewport.rect.height,Mathf.Ceil(library.Count/4f)*rowHeight));scroll.content=body;scroll.viewport=viewport;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
            for(int i=0;i<library.Count;i++){
                var p=library[i];var id=S(p["id"]);var x=10+(i%4)*315;var y=10+(i/4)*rowHeight;
                RectTransform card;
                if(!loadout&&S(p["portrait"])!=""){card=Panel(body,x,y,300,470,Ink);BindPersonaDetails(card,id);Picture(card,Art(S(p["portrait"])),10,10,280,450);}else card=PersonaCard(body,p,x,y,300,loadout?235:470);
                if(loadout){
                    NativePageFactory.Ensure<NativePersonaDrag>(card.gameObject).Bind(this,id,S(p["name"]),root,font);
                    var equippedSlot=equipped.ToList().FindIndex(v=>S(v)==id);
                    var equip=Btn(body,equippedSlot==selectedLoadoutSlot?"已在当前槽位":(equippedSlot>=0?"移至槽位 ":"装备至槽位 ")+(selectedLoadoutSlot+1),x,y+240,300,45,()=>Act("loadout-place",id,selectedLoadoutSlot),equippedSlot>=0);
                    equip.name="装备人格 · "+id;
                }
            }
            if(loadout){
                Click(panel,"返回",90,805,260,"#start-loadout-close",false);
                var confirm=Btn(panel,"确认阵容并开始",460,805,420,65,()=>Act("click","#start-loadout-confirm"),true);
                confirm.interactable=(bool?)state["disabled"]?["#start-loadout-confirm"]!=true;
                NativePageFactory.Ensure<CanvasGroup>(confirm.gameObject).alpha=confirm.interactable?1:.4f;
                Label(panel,"拖动人格卡或槽位可调整阵容 · 点击人格查看详情",45,775,1250,28,18,Muted);
            }
            else Btn(panel,"关闭",460,805,420,65,()=>Act("close-gallery"));
        }

        void CardGrid(RectTransform panel,JArray deck,bool selectable)
        {
            // Scrollable native grid retains access to every modified or duplicate card.
            var viewport=Rect(panel,"Deck viewport",40,260,1260,510);NativePageFactory.Ensure<Image>(viewport.gameObject).color=Color.clear;NativePageFactory.Ensure<RectMask2D>(viewport.gameObject);var scroll=NativePageFactory.Ensure<ScrollRect>(viewport.gameObject);var body=Rect(viewport,"Cards",0,0,1260,Mathf.Ceil(deck.Count/12f)*165);scroll.content=body;scroll.viewport=viewport;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
            if(deck.Count==0)Label(body,"这里暂时没有牌。",50,95,1160,90,28,Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<deck.Count;i++){var card=deck[i];var rect=Card(body,card,10+(i%12)*103,10+(i/12)*165,92,140,selectable?()=>Act("deck-select",S(card["uid"])):(Action)null);if(S(state["deckSelected"])==S(card["uid"])){var outline=rect.GetComponentInChildren<NativeFrameGraphic>();outline.color=Gold;outline.Thickness=4;outline.SetVerticesDirty();}}
        }
        RectTransform PersonaCard(RectTransform parent,JToken p,float x,float y,float w,float h)
        {
            if(p==null||p.Type==JTokenType.Null)return null;var card=Panel(parent,x,y,w,h,Ink);BindPersonaDetails(card,S(p["id"]));bool art=S(p["portrait"])!="";if(art)Picture(card,Art(S(p["portrait"])),12,12,w*.31f,h-24);float left=art?w*.36f:18,copyWidth=w-left-16;Label(card,S(p["name"]),left,15,copyWidth,45,25,Pale);var growth=S((p["template"] as JObject)?["mainEffect"]?["growthText"]);var copy="触发条件\n"+S(p["trigger"])+"\n\n生效效果\n"+S(p["effect"])+(growth!=""?"\n\n成长方式\n"+growth:"");TextScroll(card,copy,left,68,copyWidth,h-80,18,Gold);return card;
        }
        void ApplyAudio(bool menu){if(baking||audioSource==null)return;audioSource.mute=!sfxOn;audioSource.volume=volume*.48f;musicMixer?.Configure(menu||S(state?["dialog"])=="#shop-dialog",musicOn,sfxOn,volume);}

        void RenderError(){if(content==null)content=Rect(root,"Error",0,0,1920,1080);var p=Panel(content,200,200,1520,680,Ink);Label(p,"运行检查发现问题",40,30,1400,80,40,Gold);Label(p,error,40,130,1400,480,22,Pale);}
        RectTransform Card(RectTransform parent,JToken card,float x,float y,float w,float h,Action action)
        {
            var rect=Panel(parent,x,y,w,h,Color.clear);rect.GetComponentInChildren<NativeFrameGraphic>().color=Color.clear;rect.name="Card "+S(card["uid"]);NativePageFactory.Ensure<CanvasGroup>(rect.gameObject);
            var suit=S(card["s"]) switch{"♠"=>Suit.Spades,"♥"=>Suit.Hearts,"♦"=>Suit.Diamonds,_=>Suit.Clubs};var rank=(Rank)((int?)card["ri"]??2);var suitIndex=S(card["s"]) switch{"♠"=>0,"♥"=>1,"♣"=>2,_=>3};var rankIndex=(int)rank==14?0:(int)rank-1;var tex=Resources.Load<Texture2D>("WebArt/cards/image"+(suitIndex*13+rankIndex+1).ToString("000"))??CardFaceCatalog.FaceFor(suit,rank);
            if(tex!=null){var image=NativePageFactory.Ensure<RawImage>(Rect(rect,"Face",2,2,w-4,h-4).gameObject);image.texture=tex;image.raycastTarget=false;}else Label(rect,S(card["r"])+"\n"+S(card["s"]),5,5,w-10,h-10,40,suit==Suit.Hearts||suit==Suit.Diamonds?new Color(.65f,.12f,.15f):Color.black,TextAnchor.MiddleCenter);
            if(action!=null){var b=NativePageFactory.Ensure<Button>(rect.gameObject);b.targetGraphic=rect.GetComponent<Image>();b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>action());}
            var detail=state?["cardDetails"]?[S(card["uid"])];if(detail!=null){var upgrade=S(detail["upgrade"]);var suitBonus=(float?)detail["suitBonus"]??0;if(upgrade!=""||suitBonus>0){var badge=Panel(rect,2,h-30,w-4,28,Ink,false);Label(badge,upgrade+(suitBonus>0?$" +{suitBonus:0.#}":""),3,2,w-10,24,12,Gold,TextAnchor.MiddleCenter);}}
            return rect;
        }
        RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)=>pageFactory!=null?pageFactory.Rect(parent,name,x,y,w,h):CreateFallbackRect(parent,name,x,y,w,h);
        static RectTransform CreateFallbackRect(Transform parent,string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        RectTransform Panel(Transform parent,float x,float y,float w,float h,Color color,bool border=true){var r=Rect(parent,"Panel",x,y,w,h);var image=NativePageFactory.Ensure<Image>(r.gameObject);image.color=color;if(border){var frame=NativePageFactory.Ensure<NativeFrameGraphic>(Rect(r,"Frame",0,0,w,h).gameObject);frame.color=new Color(Gold.r,Gold.g,Gold.b,.35f);frame.raycastTarget=false;}return r;}
        void Rule(Transform parent,float x,float y,float w){Panel(parent,x,y,w,1,new Color(Gold.r,Gold.g,Gold.b,.5f),false);}
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size=24,Color? color=null,TextAnchor align=TextAnchor.UpperLeft,int unused=0){var r=Rect(parent,"Text",x,y,w,h);var t=NativePageFactory.Ensure<Text>(r.gameObject);t.font=font;t.fontSize=size;t.fontStyle=size>=26?FontStyle.Bold:FontStyle.Normal;t.color=color??Pale;t.text=text;r.name=text.Length>26?text.Substring(0,26):text;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.lineSpacing=.85f;t.supportRichText=false;return t;}
        Button Btn(Transform parent,string text,float x,float y,float w,float h,Action action,bool primary=false){var r=Panel(parent,x,y,w,h,primary?new Color32(89,62,28,255):new Color32(30,29,34,250));if(WarmCommercePage)r.GetComponent<Image>().color=GrowthRowColor(primary);r.name=text;var button=NativePageFactory.Ensure<Button>(r.gameObject);button.targetGraphic=r.GetComponent<Image>();var colors=button.colors;colors.highlightedColor=new Color(1.22f,1.17f,1.06f);colors.pressedColor=new Color(.72f,.67f,.58f);colors.disabledColor=new Color(.42f,.42f,.42f);button.colors=colors;Label(r,text,12,5,w-24,h-10,24,primary?Pale:WarmCommercePage?new Color32(216,199,168,255):Gold,TextAnchor.MiddleCenter);button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(!busy)action();});NativePageFactory.Ensure<NativeButtonMotion>(r.gameObject);return button;}
        void Click(Transform p,string title,float x,float y,float w,string id,bool primary=true){var b=Btn(p,title,x,y,w,65,()=>Act("click",id),primary);b.interactable=(bool?)state["disabled"]?[id]!=true;}
        RawImage InsertArt(RectTransform parent,string path,float x,float y,float w,float h){var r=Picture(parent,path,x,y,w,h);if(r==null)return null;r.SetAsFirstSibling();return r.GetComponent<RawImage>();}
        void DecorateButton(Button b,string path){if(string.IsNullOrEmpty(b.name))b.name=path;b.targetGraphic.color=Color.white;var outline=b.GetComponentInChildren<NativeFrameGraphic>();if(outline!=null)outline.enabled=false;var texture=Resources.Load<Texture2D>("WebArt/"+path);if(texture==null)return;var image=(Image)b.targetGraphic;if(!sprites.TryGetValue(path,out var sprite)){sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));sprites[path]=sprite;}image.sprite=sprite;image.type=Image.Type.Simple;if(path=="shop-buttons/purchase-v1"){var caption=b.GetComponentInChildren<Text>();caption.rectTransform.anchoredPosition=new Vector2(110,-5);caption.rectTransform.sizeDelta=new Vector2(((RectTransform)b.transform).sizeDelta.x-125,70);caption.fontSize=21;caption.resizeTextForBestFit=true;caption.resizeTextMinSize=16;caption.resizeTextMaxSize=21;}}
        RectTransform Picture(Transform parent,string path,float x,float y,float w,float h){var texture=Resources.Load<Texture2D>("WebArt/"+path);if(texture==null)return null;if(path.StartsWith("personas-v2/")||path.StartsWith("opponents/")){float fit=Mathf.Min(w/texture.width,h/texture.height);float nw=texture.width*fit,nh=texture.height*fit;x+=(w-nw)*.5f;y+=(h-nh)*.5f;w=nw;h=nh;}var r=Rect(parent,"Art",x,y,w,h);var image=NativePageFactory.Ensure<RawImage>(r.gameObject);image.texture=texture;image.raycastTarget=false;return r;}
        static string Art(string path)=>path.Replace("assets/art/","").Replace(".png","").Replace(".webp","");
        string T(string id)=>S(state?["text"]?[id]);static string S(JToken token)=>token?.Type==JTokenType.Null?"":token?.ToString()??"";
        void Sound(AudioClip clip){if(audioSource!=null&&clip!=null)audioSource.PlayOneShot(clip);}
    }
}
























