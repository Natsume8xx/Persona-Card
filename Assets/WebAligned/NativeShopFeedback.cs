using System.Linq;
using Newtonsoft.Json.Linq;

namespace PersonaCards.WebAligned
{
    // Read-only presentation decisions; the source rules have already committed the action.
    public static class NativeShopFeedback
    {
        public static string Success(JObject before,JObject after,string action,string id,int slot)
        {
            if((string)after["dialog"]!="#shop-dialog")return null;
            if(action=="unlock"){
                var old=Find(before,id)?["affixes"] as JArray;var next=Find(after,id)?["affixes"] as JArray;
                if(slot>=0&&old!=null&&next!=null&&slot<old.Count&&slot<next.Count&&
                   (bool?)old[slot]["unlocked"]==false&&(bool?)next[slot]["unlocked"]==true)return "副属性已解锁";
            }
            if(action=="click"&&(id=="#shop-buy"||id=="#deck-confirm"||id=="#shop-upgrade-confirm")){
                var old=before["shop"]?["offers"] as JArray;var next=after["shop"]?["offers"] as JArray;
                if(old!=null&&next!=null&&next.Any(n=>old.Any(o=>(string)o["item"]?["id"]==(string)n["item"]?["id"]&&
                    (int?)n["purchaseCount"]>(int?)o["purchaseCount"])))return "购买成功";
            }
            return null;
        }
        static JToken Find(JObject state,string id)=>(state["pool"] as JArray)?.FirstOrDefault(p=>(string)p["id"]==id);
    }
}
