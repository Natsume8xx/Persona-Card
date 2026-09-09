using Newtonsoft.Json.Linq;
namespace PersonaCards.WebAligned
{
    // Copy the source presentation conditions; never decides whether an unlock is legal.
    public static class NativeShopAffixPresentation
    {
        public const string Empty="该人格没有可铸造的副属性。";
        public static string Description(JToken affix)
        {
            if((bool?)affix["unlocked"]==true)return (string)affix["effectText"]??"";
            var availability=affix["availability"];
            string reason=(string)availability?["reason"];
            if(reason=="PROFILE_LOCKED"&&!string.IsNullOrEmpty((string)availability?["nextProfileId"]))return "未解锁 · 需 "+(string)availability["nextProfileId"]+" 商店";
            if(reason=="PREVIOUS_SLOT_LOCKED")return "未解锁 · 请先解锁第二属性";
            return "未解锁";
        }
    }
}
