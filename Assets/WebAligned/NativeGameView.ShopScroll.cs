using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        readonly Dictionary<string,float> shopScrollPositions=new();
        readonly List<(string key,ScrollRect scroll)> activeShopScrolls=new();
        string scrollShopNode;
        void RememberShopScrolls()
        {
            if(baking)return;
            foreach(var entry in activeShopScrolls)if(entry.scroll!=null&&entry.scroll.gameObject.activeInHierarchy)
                shopScrollPositions[entry.key]=entry.scroll.verticalNormalizedPosition;
            activeShopScrolls.Clear();
            if((bool?)state?["menu"]==true){shopScrollPositions.Clear();scrollShopNode=null;}
        }
        void BindShopScroll(RectTransform body,string key)
        {
            if(baking)return;
            string node=S(state["node"]?["id"]);
            if(scrollShopNode!=node){shopScrollPositions.Clear();scrollShopNode=node;}
            activeShopScrolls.Add((key,body.GetComponentInParent<ScrollRect>()));
        }
        void RestoreShopScrolls()
        {
            if(baking||activeShopScrolls.Count==0)return;
            Canvas.ForceUpdateCanvases();
            foreach(var entry in activeShopScrolls)if(entry.scroll!=null){entry.scroll.StopMovement();entry.scroll.verticalNormalizedPosition=shopScrollPositions.TryGetValue(entry.key,out var position)?Mathf.Clamp01(position):1;}
        }
    }
}
