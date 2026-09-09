using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    // Existing PlayerPrefs format retained. Preview is separate from the last committed settings.
    public sealed class NativeSettingsSession
    {
        public sealed class Values
        {
            public float Brightness=1,Volume=.8f,Speed=1;
            public bool Music=true,Sfx=true,Motion=true,Shake=true;
            public Values Copy()=>(Values)MemberwiseClone();
            public JObject Json()=>new JObject{["brightness"]=Brightness,["volume"]=Volume,["speed"]=Speed,["music"]=Music,["sfx"]=Sfx,["motion"]=Motion,["shake"]=Shake};
        }
        Values saved;
        public Values Draft {get;private set;}
        public NativeSettingsSession(string json=null)
        {
            saved=new Values();
            try
            {
                var s=JObject.Parse(json??"{}");
                saved.Brightness=Mathf.Clamp((float?)s["brightness"]??1,.7f,1.2f);saved.Volume=Mathf.Clamp01((float?)s["volume"]??.8f);
                float speed=(float?)s["speed"]??1;saved.Speed=speed==1.5f||speed==2?speed:1;
                saved.Music=(bool?)s["music"]??true;saved.Sfx=(bool?)s["sfx"]??true;saved.Motion=(bool?)s["motion"]??true;saved.Shake=(bool?)s["shake"]??true;
            }
            catch{saved=new Values();}
            Begin();
        }
        public void Begin()=>Draft=saved.Copy();
        public void Cancel()=>Begin();
        public void Defaults()=>Draft=new Values();
        public string Commit(){saved=Draft.Copy();return saved.Json().ToString();}
        public string SavedJson=>saved.Json().ToString();
    }
}
