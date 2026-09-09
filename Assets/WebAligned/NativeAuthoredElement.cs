using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    // The key binds a visual element to runtime data; renaming it in the Hierarchy is safe.
    // Baselines let authored presentation changes survive data refreshes without freezing game values.
    [DisallowMultipleComponent]
    public sealed class NativeAuthoredElement : MonoBehaviour
    {
        [HideInInspector] public string Key;
        [HideInInspector] public Vector2 Position, Size, AnchorMin, AnchorMax, Pivot;
        [HideInInspector] public Vector3 Scale;
        [HideInInspector] public Quaternion Rotation;
        [HideInInspector] public Color Color;
        [HideInInspector] public int FontSize;
        [HideInInspector] public FontStyle FontStyle;
        [HideInInspector] public float LineSpacing;
        [HideInInspector] public Font Font;
        [HideInInspector] public TextAnchor Alignment;
        [HideInInspector] public string Copy;
        [HideInInspector] public Texture Texture;
        [HideInInspector] public Sprite Sprite;
        [HideInInspector] public float BorderThickness;
        public void CaptureBaseline()
        {
            var r=(RectTransform)transform;
            Position=r.anchoredPosition;Size=r.sizeDelta;AnchorMin=r.anchorMin;AnchorMax=r.anchorMax;Pivot=r.pivot;Scale=r.localScale;Rotation=r.localRotation;
            var g=GetComponent<Graphic>();if(g!=null)Color=g.color;
            var t=GetComponent<Text>();if(t!=null){FontSize=t.fontSize;FontStyle=t.fontStyle;LineSpacing=t.lineSpacing;Font=t.font;Alignment=t.alignment;Copy=t.text;}
            var raw=GetComponent<RawImage>();if(raw!=null)Texture=raw.texture;
            var image=GetComponent<Image>();if(image!=null)Sprite=image.sprite;
            var frame=GetComponent<NativeFrameGraphic>();if(frame!=null)BorderThickness=frame.Thickness;
        }
        public System.Action RestoreAuthoredChanges()
        {
            var r=(RectTransform)transform;var p=r.anchoredPosition;var size=r.sizeDelta;var amin=r.anchorMin;var amax=r.anchorMax;var pivot=r.pivot;var scale=r.localScale;var rotation=r.localRotation;
            var g=GetComponent<Graphic>();var color=g!=null?g.color:Color;
            var t=GetComponent<Text>();int fs=t!=null?t.fontSize:FontSize;var font=t!=null?t.font:Font;var align=t!=null?t.alignment:Alignment;var copy=t!=null?t.text:Copy;
            var style=t!=null?t.fontStyle:FontStyle;float line=t!=null?t.lineSpacing:LineSpacing;
            var raw=GetComponent<RawImage>();var texture=raw!=null?raw.texture:Texture;
            var image=GetComponent<Image>();var sprite=image!=null?image.sprite:Sprite;
            var frame=GetComponent<NativeFrameGraphic>();float thickness=frame!=null?frame.Thickness:BorderThickness;
            return ()=>{
                if(this==null)return;
                if(amin!=AnchorMin)r.anchorMin=amin;if(amax!=AnchorMax)r.anchorMax=amax;if(pivot!=Pivot)r.pivot=pivot;
                if(p!=Position)r.anchoredPosition=p;if(size!=Size)r.sizeDelta=size;if(scale!=Scale)r.localScale=scale;if(rotation!=Rotation)r.localRotation=rotation;
                if(g!=null&&color!=Color)g.color=color;
                if(t!=null){if(fs!=FontSize){t.fontSize=fs;if(t.resizeTextForBestFit){t.resizeTextMaxSize=fs;t.resizeTextMinSize=Mathf.Min(t.resizeTextMinSize,fs);}}if(font!=Font)t.font=font;if(style!=FontStyle)t.fontStyle=style;if(line!=LineSpacing)t.lineSpacing=line;if(align!=Alignment)t.alignment=align;if(copy!=Copy&&t.text==Copy)t.text=copy;}
                if(raw!=null&&texture!=Texture)raw.texture=texture;if(image!=null&&sprite!=Sprite)image.sprite=sprite;
                if(frame!=null&&thickness!=BorderThickness){frame.Thickness=thickness;frame.SetVerticesDirty();}
            };
        }
    }
}

