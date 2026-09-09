using System.Collections.Generic;
namespace PersonaCards.WebAligned
{
    // Presentation-only mirror of audio-effects.js recordings, gain and cooldown.
    public sealed class NativeCueSequence
    {
        readonly Dictionary<string,(string[] files,float gain,float cooldown)> cues=new(){
            ["select"]=(new[]{"card-slide-1","card-slide-2","card-slide-3"},.48f,.055f),
            ["tick"]=(new[]{"chip-lay-1","chip-lay-2"},.32f,.065f),
            ["play"]=(new[]{"card-place-1","card-place-2"},.72f,.18f),
            ["discard"]=(new[]{"card-shove-1"},.55f,.18f),
            ["score"]=(new[]{"chips-stack-1"},.64f,.18f),
            ["personaCharge"]=(new[]{"glass_002"},.28f,.16f),
            ["personaTravel"]=(new[]{"card-fan-1"},.3f,.14f),
            ["personaImpact"]=(new[]{"chips-collide-1"},.48f,.1f),
            ["buy"]=(new[]{"chips-handle-1"},.65f,.18f),
            ["forge"]=(new[]{"open_001"},.48f,.3f),
            ["button"]=(new[]{"click_001"},.32f,.09f)
        };
        readonly Dictionary<string,float> last=new();readonly Dictionary<string,int> next=new();
        public bool TryNext(string name,float now,bool enabled,out string file,out float gain)
        {
            file=null;gain=0;
            if(!enabled||!cues.TryGetValue(name,out var cue)||(last.TryGetValue(name,out var time)&&now-time<cue.cooldown))return false;
            int index=next.TryGetValue(name,out var value)?value:0;
            file=cue.files[index];gain=cue.gain;next[name]=(index+1)%cue.files.Length;last[name]=now;return true;
        }
    }
}
