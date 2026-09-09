using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed class NativePageFactory
    {
        readonly Dictionary<string,NativeAuthoredElement> elements=new();
        readonly Dictionary<string,int> counters=new();
        readonly HashSet<string> used=new();
        readonly List<Action> overrides=new();
        readonly RectTransform root;
        public NativePageFactory(RectTransform root,NativePageTemplate template)
        {
            this.root=root;
            if(template==null)return;
            foreach(var e in template.GetComponentsInChildren<NativeAuthoredElement>(true))
            {
                if(string.IsNullOrEmpty(e.Key))continue;
                elements[e.Key]=e;overrides.Add(e.RestoreAuthoredChanges());
            }
        }
        public RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            string parentKey=parent==root?"":parent.GetComponent<NativeAuthoredElement>()?.Key??parent.name;
            string prefix=parentKey+"/"+name;int index=counters.TryGetValue(prefix,out int count)?count:0;counters[prefix]=index+1;
            string key=prefix+"["+index+"]";used.Add(key);
            RectTransform r;
            if(elements.TryGetValue(key,out var element)&&element!=null)r=(RectTransform)element.transform;
            else
            {
                r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
                r.gameObject.AddComponent<NativeAuthoredElement>().Key=key;
            }
            r.gameObject.SetActive(true);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            return r;
        }
        public void Finish()
        {
            foreach(var p in elements)if(!used.Contains(p.Key)&&p.Value!=null)p.Value.gameObject.SetActive(false);
            foreach(var restore in overrides)restore();
        }
        public static T Ensure<T>(GameObject go) where T:Component
        {
            var component=go.GetComponent<T>();
            return component!=null?component:go.AddComponent<T>();
        }
    }
}
