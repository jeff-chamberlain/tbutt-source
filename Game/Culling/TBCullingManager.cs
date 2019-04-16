using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt
{
    public class TBCullingManager : TBGameManagerBase
    {
        public override void Initialize()
        {
            TBCullObject[] cullObjects = FindObjectsOfType<TBCullObject>();
            if (cullObjects != null)
            {
                for (int i = 0; i < cullObjects.Length; i++)
                {
                    if ((TBSettings.GetCurrentQualityLevel() < cullObjects[i].lowestAllowed) || (TBSettings.GetCurrentQualityLevel() > cullObjects[i].highestAllowed))
                        cullObjects[i].DestroyObject();
                }
            }
        }
    }
}