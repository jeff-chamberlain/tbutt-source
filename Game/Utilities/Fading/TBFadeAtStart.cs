using UnityEngine;
using System.Collections;

namespace TButt
{
    public class TBFadeAtStart : MonoBehaviour
    {
        public float fadeDuration = 0.5f;
        public float fadeDelay = 0.5f;
        public bool ignoreTimestep = false;

        IEnumerator Start()
        {
            #if TB_STEAM_VR
            if(TBCore.GetActivePlatform() == VRPlatform.SteamVR)
                SteamVR_Events.LoadingFadeIn.Send(fadeDuration);
            #endif

            TBFade.FadeIn(fadeDuration, ignoreTimestep);

            while (TBFade.IsFading())
                yield return null;

            Destroy(this);
        }
    }
}