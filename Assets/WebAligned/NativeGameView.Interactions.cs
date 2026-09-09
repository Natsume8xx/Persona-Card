using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        public void OpenSettings(){if(busy)return;settings.Begin();settingsOpen=true;Render();}
        public void CancelSettings(){settings.Cancel();settingsOpen=false;Render();}
        void SaveSettings(){string json=settings.Commit();if(settingsPersistence){PlayerPrefs.SetString("web-aligned-settings-v1",json);PlayerPrefs.Save();}settingsOpen=false;Render();}
        public void HandleEscape()
        {
            if(busy||state==null)return;
            bool cancelledDrag=false;
            if(pageRoot!=null)foreach(var drag in pageRoot.GetComponentsInChildren<NativePersonaDrag>())if(drag.Dragging){drag.Cancel();cancelledDrag=true;}
            if(cancelledDrag){foreach(var slot in pageRoot.GetComponentsInChildren<NativePersonaDropSlot>())slot.OnPointerExit(null);return;}
            if(settingsOpen){CancelSettings();return;}
            if(S(state["dialog"])!=""){Act("escape");return;}
            if(!(bool)state["menu"])OpenSettings();
        }
        static bool CanDismissDialog(string id)=>id=="#deck-dialog"||id=="#hand-rules-dialog"||id=="#start-loadout-dialog"||id=="#native-gallery"||id=="#shop-upgrade-dialog";
        void ApplyBrightness()
        {
            if(baking||dimmer==null)return;
            if(brightnessMaterial==null){var shader=Resources.Load<Shader>("WebBrightness");if(shader==null)throw new InvalidOperationException("Brightness shader is missing.");brightnessMaterial=new Material(shader);}
            brightnessMaterial.SetFloat("_Brightness",QualitySettings.activeColorSpace==ColorSpace.Linear?Mathf.Pow(brightness,2.2f):brightness);
            dimmer.material=brightnessMaterial;dimmer.color=Color.white;
        }
        void RenderSettings()
        {
            Picture(content,"backgrounds/image081",0,0,1920,1080);Panel(content,0,0,1920,1080,new Color(0,0,0,.58f),false);
            var p=Panel(content,400,65,1120,950,Ink);Label(p,"设置",65,24,990,70,44,Pale,TextAnchor.MiddleCenter);Rule(p,65,110,990);
            SettingSlider(p,"画面亮度",145,brightness,.7f,1.2f,v=>{brightness=v;ApplyBrightness();});
            SettingSlider(p,"主音量",245,volume,0,1,v=>{volume=v;ApplyAudio((bool)state["menu"]);});
            Btn(p,"背景音乐 · "+(musicOn?"开启":"关闭"),65,350,475,62,()=>{musicOn=!musicOn;Render();},musicOn);
            Btn(p,"操作音效 · "+(sfxOn?"开启":"关闭"),580,350,475,62,()=>{sfxOn=!sfxOn;Render();},sfxOn);
            Btn(p,"界面动效 · "+(motionOn?"开启":"关闭"),65,445,475,62,()=>{motionOn=!motionOn;Render();},motionOn);
            var shake=Btn(p,"屏幕震动 · "+(shakeOn?"开启":"关闭"),580,445,475,62,()=>{shakeOn=!shakeOn;Render();},shakeOn);
            Label(p,"结算播放速度",65,550,300,45,25,Pale);
            var speeds=new[]{1f,1.5f,2f};var names=new[]{"1.0× 标准","1.5× 流畅","2.0× 快速"};
            for(int i=0;i<3;i++){float choice=speeds[i];Btn(p,names[i],365+i*230,540,210,62,()=>{speed=choice;Render();},Mathf.Approximately(speed,choice));}
            if(!(bool)state["menu"])
            {
                Btn(p,"重播教学",65,645,475,58,()=>{settingsOpen=false;Act("click","#replay-tutorial");});
                Btn(p,"返回主菜单",580,645,475,58,()=>{settings.Cancel();settingsOpen=false;Act("save-menu");});
            }
            Btn(p,"恢复默认",65,770,280,68,()=>{settings.Defaults();Render();});
            Btn(p,"取消",410,770,280,68,CancelSettings);Btn(p,"保存设置",755,770,300,68,SaveSettings,true);
            Label(p,"音乐：Kevin MacLeod · CC BY 4.0   |   音效：Kenney · CC0",65,877,990,32,17,Muted,TextAnchor.MiddleCenter);
            Btn(p,"×",1020,20,60,60,CancelSettings);
        }
        void SettingSlider(RectTransform p,string title,float y,float value,float min,float max,Action<float> changed)
        {
            Label(p,title,65,y,280,45,26,Pale);var output=Label(p,Mathf.RoundToInt(value*100)+"%",955,y,100,45,25,Gold,TextAnchor.MiddleRight);
            var r=Rect(p,title,365,y+8,555,38);var slider=NativePageFactory.Ensure<Slider>(r.gameObject);slider.fillRect=null;slider.handleRect=null;
            var bar=Panel(r,0,15,555,8,new Color(.22f,.18f,.12f),false);var fill=Panel(bar,0,0,555,8,Gold,false);var handle=Panel(r,0,0,18,38,Pale,false);
            fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=new Vector2(18,0);handle.anchoredPosition=Vector2.zero;slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=min;slider.maxValue=max;slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveAllListeners();slider.onValueChanged.AddListener(v=>{output.text=Mathf.RoundToInt(v*100)+"%";changed(v);});
        }
        void RenderNewRunConfirm()
        {
            Panel(content,0,0,1920,1080,new Color(0,0,0,.75f),false);var p=Panel(content,485,310,950,430,new Color32(14,12,11,255));
            Label(p,"开始新游戏",60,35,830,70,40,Gold,TextAnchor.MiddleCenter);
            Label(p,"开始新游戏会覆盖当前进度，是否继续？",80,145,790,95,29,Pale,TextAnchor.MiddleCenter);
            Btn(p,"取消",95,300,330,70,()=>Act("new-run-cancel"));Btn(p,"继续",525,300,330,70,()=>Act("new-run-confirm"),true);
        }
        void BindPersonaDetails(RectTransform panel,string id)
        {
            if(string.IsNullOrEmpty(id))return;panel.name="查看人格 · "+id;var button=NativePageFactory.Ensure<Button>(panel.gameObject);button.targetGraphic=panel.GetComponent<Image>();button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(!busy)Act("persona-detail",id);});
        }
        void RenderPersonaDetails()
        {
            Panel(content,0,0,1920,1080,new Color(0,0,0,.78f),false);var p=Panel(content,290,115,1340,850,new Color32(14,12,11,255));var detail=state["personaDetail"];
            Label(p,S(detail?["name"]),440,45,800,75,42,Gold);Rule(p,440,135,800);
            Picture(p,Art(S(detail?["portrait"])),65,110,305,545);
            string copy="触发条件\n"+S(detail?["trigger"])+"\n\n生效效果\n"+S(detail?["effect"]);
            if(S(detail?["growth"])!="")copy+="\n\n成长方式\n"+S(detail["growth"]);
            if(S(detail?["affixText"])!="")copy+="\n\n副属性\n"+S(detail["affixText"]);
            TextScroll(p,copy,440,175,800,480,26,Pale);
            Btn(p,"返回",505,730,330,70,()=>Act("click","#persona-detail-done"),true);Btn(p,"×",1250,20,60,60,HandleEscape);
        }
    }
}





