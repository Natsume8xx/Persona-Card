using System.Collections;
using UnityEngine;
namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        IEnumerator CheckPersonaTiming(string folder)
        {
            var saved=core.Snapshot().ToString();var savedSpeed=speed;var savedBusy=busy;busy=true;speed=1;resolutionFast=false;
            var frame=personaRects[0].GetComponentInChildren<NativeFrameGraphic>();var color=frame.color;
            float started=Time.unscaledTime;var playback=StartCoroutine(ShowPersonaTrigger(0));
            yield return new WaitForSecondsRealtime(.5f);
            var trail=fxRoot.GetComponent<NativeEnergyTrail>();Require(trail!=null&&trail.Progress>0&&trail.Progress<1,"Persona trail does not follow the travel stage.");
            yield return CapturePage(folder,"persona-travel");yield return playback;float normal=Time.unscaledTime-started;
            yield return null;Require(fxRoot.GetComponent<NativeEnergyTrail>()==null&&frame.color==color,"Persona effect did not clean up.");
            speed=4;started=Time.unscaledTime;yield return ShowPersonaTrigger(0);float accelerated=Time.unscaledTime-started;
            yield return null;Require(accelerated<normal*.8f,"Persona travel ignores playback speed.");
            Require(fxRoot.GetComponent<NativeEnergyTrail>()==null&&frame.color==color,"Accelerated effect left a trail or highlight.");
            Require(saved==core.Snapshot().ToString(),"Persona presentation changed rule state.");
            speed=savedSpeed;busy=savedBusy;Debug.Log("NATIVE_PERSONA_TIMING_PASSED");
        }
    }
}
