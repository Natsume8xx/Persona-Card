using System.Collections;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        IEnumerator CaptureShopFeedback(string folder)
        {
            yield return new WaitForSecondsRealtime(1);
            core.Action("start");core.EvaluateForTest("openShop(false);coins=10000;__flush();");state=core.Snapshot();Render();
            motionOn=true;Act("shop-select",S(state["shop"]["selectedItemId"]));
            var detail=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="商店详情区域");
            var origin=detail.anchoredPosition-Vector2.right*12;
            Require(detail.GetComponent<CanvasGroup>().alpha<1,"Shop detail did not start its reveal.");
            var saved=state["storage"].ToString();yield return new WaitForSecondsRealtime(.3f);
            Require(Vector2.Distance(detail.anchoredPosition,origin)<.1f&&detail.GetComponent<CanvasGroup>().alpha==1,"Shop detail did not restore its authored position and alpha.");
            Require(saved==state["storage"].ToString(),"Shop motion mutated storage.");
            var before=state;core.EvaluateForTest("completeShopPurchase(shopItemById.get(selectedShopItemId),()=> 'test purchase');__flush();");state=core.Snapshot();Render();ShopFeedback(before,"click","#shop-buy",0);
            var notice=fxRoot.GetComponentInChildren<CanvasGroup>();Require(notice!=null&&!notice.blocksRaycasts&&!notice.GetComponent<Text>().raycastTarget,"Success notice blocks input.");
            yield return new WaitForSecondsRealtime(.12f);yield return CapturePage(folder,"shop-purchase-feedback");
            yield return new WaitForSecondsRealtime(1.25f);Require(notice==null,"Success notice failed to clean up.");
            motionOn=false;Act("shop-select",S(state["shop"]["selectedItemId"]));
            detail=pageRoot.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="商店详情区域");
            Require(detail.GetComponent<CanvasGroup>()==null,"Disabled motion still starts a reveal.");
            Render();ShopFeedback(before,"click","#shop-buy",0);
            notice=fxRoot.GetComponentInChildren<CanvasGroup>();Require(notice!=null&&notice.alpha==1,"Motion-off mode lost static success feedback.");
            yield return new WaitForSecondsRealtime(1.3f);Require(notice==null,"Static notice failed to clean up.");
            Debug.Log("NATIVE_SHOP_FEEDBACK_PASSED");
        }
        JObject feedbackShopState;Coroutine shopDetailMotion;
        void RememberFeedbackShop(JObject before)
        {
            if(before?["shop"]!=null)feedbackShopState=before;
            else if((bool?)before?["menu"]==true)feedbackShopState=null;
        }
        void ShopFeedback(JObject before,string action,string id,int slot)
        {
            if(baking||S(state["dialog"])!="#shop-dialog")return;
            var comparison=before["shop"]!=null?before:feedbackShopState??before;
            var success=S(comparison["node"]?["id"])==S(state["node"]?["id"])?NativeShopFeedback.Success(comparison,state,action,id,slot):null;
            if(success!=null){
                // This overlay is presentation only and never intercepts clicks.
                var notice=new GameObject("商店成功反馈",typeof(RectTransform),typeof(CanvasGroup),typeof(Text));
                notice.transform.SetParent(fxRoot,false);var rect=(RectTransform)notice.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(680,52);rect.anchoredPosition=new Vector2(280,-190);
                var text=notice.GetComponent<Text>();text.font=font;text.fontSize=28;text.alignment=TextAnchor.MiddleCenter;text.color=Gold;text.text="◆  "+success;text.raycastTarget=false;
                var group=notice.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
                StartCoroutine(ShopNotice(group,rect));
            }
            if(motionOn&&(success!=null||action=="shop-select"||action=="shop-persona"||action=="shop-tab")){
                var detail=pageRoot.GetComponentsInChildren<RectTransform>().FirstOrDefault(r=>r.name=="商店详情区域");
                if(detail!=null){
                    // 同成长 dialog：页面会被下一次 Render 连根重建，旧协程持有的 detail 会进入 Destroy 排队的将死窗口，
                    // 该窗口内 GetComponent/AddComponent 可能抛异常或静默返回 null。先停掉旧协程，新协程内全部容忍。
                    if(shopDetailMotion!=null)StopCoroutine(shopDetailMotion);
                    shopDetailMotion=StartCoroutine(RevealShopDetail(detail));
                }
            }
        }
        IEnumerator RevealShopDetail(RectTransform detail)
        {
            // detail 可能来自已进入将死窗口的旧页：拿不到 CanvasGroup（null/异常）就静默放弃动画，绝不崩溃。
            CanvasGroup group=null;
            try{
                if(detail!=null&&detail.gameObject!=null&&detail.gameObject.activeInHierarchy){
                    group=detail.GetComponent<CanvasGroup>();
                    if(group==null)group=detail.gameObject.AddComponent<CanvasGroup>();
                }
            }catch(System.Exception){group=null;}
            if(group==null)yield break;
            var origin=detail.anchoredPosition;float alpha=group.alpha;
            for(float t=0;t<.22f&&detail!=null&&detail.gameObject!=null&&detail.gameObject.activeInHierarchy&&group!=null;t+=Time.unscaledDeltaTime){
                float p=1-Mathf.Pow(1-Mathf.Clamp01(t/.22f),3);
                try{group.alpha=alpha*Mathf.Lerp(.55f,1,p);detail.anchoredPosition=origin+Vector2.right*(12*(1-p));}
                catch(System.Exception){yield break;}
                yield return null;
            }
            try{if(detail!=null&&detail.gameObject!=null&&group!=null){detail.anchoredPosition=origin;group.alpha=alpha;}}catch(System.Exception){}
        }
        IEnumerator ShopNotice(CanvasGroup group,RectTransform rect)
        {
            var origin=rect.anchoredPosition;
            for(float t=0;t<1.2f&&group!=null&&group.gameObject.activeInHierarchy;t+=Time.unscaledDeltaTime){
                if(motionOn){group.alpha=Mathf.Clamp01((1.2f-t)/.3f);rect.anchoredPosition=origin+Vector2.up*(10*Mathf.Clamp01(t/.25f));}yield return null;
            }
            if(group!=null)Destroy(group.gameObject);
        }
    }
}
