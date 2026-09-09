using System.Collections.Generic;
using UnityEngine;
namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        readonly NativeCueSequence cueSequence=new();
        NativeAudioVoicePool voicePool;
        readonly Dictionary<string,AudioClip> operationClips=new();
        void PlayCue(string name)
        {
            if(!cueSequence.TryNext(name,Time.unscaledTime,!baking&&sfxOn&&volume>0&&voicePool!=null&&voicePool.ActiveCount<10,out var file,out var gain))return;
            if(!operationClips.TryGetValue(file,out var clip)){clip=Resources.Load<AudioClip>("WebAudio/sfx/"+file);operationClips[file]=clip;}
            if(clip==null)return;
            if(!voicePool.TryPlay(clip,gain))return;
            if(name=="forge"||name=="score")musicMixer?.Duck();
            if(!settingsPersistence)Debug.Log("NATIVE_AUDIO_CUE="+name+":"+file);
        }
        void PlayOperationAudio(string action,bool fallback=true)
        {
            var cues=core.DrainAudioCues();if(baking||volume<=0)return;
            bool handled=false;
            foreach(var cue in cues){
                string name=S(cue);
                if(name.StartsWith("stinger:")){musicMixer?.Result(name.Substring(8));handled=true;continue;}
                if(name=="buy"||name=="forge"||name=="select"||name=="button"||
                    (name=="discard"&&(action=="loadout-remove"||action=="unlock"))){handled=true;PlayCue(name);}
            }
            if(!handled&&fallback)PlayCue(action=="toggle"?"select":"button");
        }
    }
}
