using System.Collections;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        // Read playback speed every frame so click-to-accelerate also affects event pauses.
        IEnumerator WaitForPlayback(float duration)
        {
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime*PlaybackSpeed)
                yield return null;
        }
    }
}
