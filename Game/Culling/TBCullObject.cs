using UnityEngine;
using System.Collections;
using TButt;

namespace TButt
{
    /// <summary>
    /// Sets which platforms this object should be allowed on.
    /// Requires a TBCullingManager script in the scene somewhere.
    /// </summary>
	public class TBCullObject : MonoBehaviour
    {
		public TBSettings.TBQualityLevel lowestAllowed;
		public TBSettings.TBQualityLevel highestAllowed;

        /// <summary>
        /// Called from TBCullingManager when it evaluates all of the TBCullObjects in a scene.
        /// </summary>
        public void DestroyObject()
        {
            Destroy(gameObject);
        }
	}
}
