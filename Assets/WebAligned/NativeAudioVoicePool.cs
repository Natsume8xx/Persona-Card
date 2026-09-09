using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed class NativeAudioVoicePool:MonoBehaviour
    {
        readonly List<(AudioSource source,bool music,float gain)> voices=new();
        float master;bool musicEnabled,sfxEnabled;
        public int ActiveCount=>voices.Count(v=>v.source!=null&&v.source.isPlaying);
        public static bool Allows(int total,int sameRecording)=>total<10&&sameRecording<3;
        public void Configure(bool music,bool sfx,float volume)
        {
            musicEnabled=music;sfxEnabled=sfx;master=Mathf.Clamp01(volume);
            foreach(var v in voices){if(v.source==null)continue;if(master<=0||!(v.music?music:sfx))v.source.Stop();v.source.volume=master*v.gain;}
        }
        public bool TryPlay(AudioClip clip,float gain,bool music=false)
        {
            if(clip==null||master<=0||!(music?musicEnabled:sfxEnabled))return false;
            int total=0,same=0;foreach(var v in voices)if(v.source!=null&&v.source.isPlaying){total++;if(v.source.clip==clip)same++;}
            if(!Allows(total,same))return false;
            int index=voices.FindIndex(v=>v.source!=null&&!v.source.isPlaying);
            AudioSource source;
            if(index<0){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;index=voices.Count;voices.Add((source,music,gain));}
            else source=voices[index].source;
            voices[index]=(source,music,gain);source.clip=clip;source.volume=master*gain;source.Play();return true;
        }
        void OnDisable(){foreach(var v in voices)if(v.source!=null)v.source.Stop();}
    }
}
