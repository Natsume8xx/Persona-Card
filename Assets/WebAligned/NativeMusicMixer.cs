using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed class NativeMusicMixer : MonoBehaviour
    {
        AudioSource menu,battle;NativeAudioVoicePool voices;
        bool inMenu,music,sfx,stingerUsesMusic;
        float master,menuLevel,battleLevel,duckUntil,lastStinger=-100;
        public int StingerPlayCount { get; private set; }
        public static bool ResultEnabled(bool musicOn,bool sfxOn,float volume)=>volume>0&&(musicOn||sfxOn);
        public void Initialize(AudioSource existing)
        {
            menu=existing;battle=gameObject.AddComponent<AudioSource>();voices=GetComponent<NativeAudioVoicePool>()??gameObject.AddComponent<NativeAudioVoicePool>();
            menu.loop=battle.loop=true;menu.playOnAwake=battle.playOnAwake=false;
            menu.clip=Resources.Load<AudioClip>("WebAudio/music/darkest-child");battle.clip=Resources.Load<AudioClip>("WebAudio/music/darkest-child-var-a");
        }
        public void Configure(bool useMenu,bool musicOn,bool sfxOn,float volume)
        {
            inMenu=useMenu;music=musicOn;sfx=sfxOn;master=Mathf.Clamp01(volume);
            voices?.Configure(music,sfx,master);
        }
        public void Duck(){duckUntil=Time.unscaledTime+1.1f;}
        public void Result(string result)
        {
            if(!ResultEnabled(music,sfx,master)||voices==null||Time.unscaledTime-lastStinger<.7f)return;
            var clip=Resources.Load<AudioClip>("WebAudio/sfx/"+(result=="victory"?"confirmation_002":"error_004"));if(clip==null)return;
            if(!voices.TryPlay(clip,result=="victory"?.52f:.4f,music))return;
            lastStinger=Time.unscaledTime;StingerPlayCount++;Duck();
        }
        void Update()
        {
            if(menu==null)return;
            Mix(menu,ref menuLevel,inMenu);Mix(battle,ref battleLevel,!inMenu);
        }
        void Mix(AudioSource source,ref float level,bool active)
        {
            if(!music||master<=0){source.Pause();source.volume=0;return;}
            level=Mathf.MoveTowards(level,active?1:0,Time.unscaledDeltaTime*.9f);
            if(active&&!source.isPlaying&&source.clip!=null){source.UnPause();if(!source.isPlaying)source.Play();}
            var edge=source.clip==null?0:Mathf.Min(1,source.time/.8f,Mathf.Max(0,(source.clip.length-source.time)/1.2f));
            source.volume=master*.38f*level*edge*(Time.unscaledTime<duckUntil?.42f:1);
            if(!active&&level==0)source.Pause();
        }
    }
}
