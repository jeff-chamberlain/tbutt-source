using UnityEngine;
using System.Collections;

namespace TButt
{
    /// <summary>
    /// Base class for managers that must be instantiated from prefabs.
    /// </summary>
    public abstract class TBGameManagerBase : MonoBehaviour
    {
        [Tooltip("SuperManagers always spawn on first load, and are not destroyed between scenes.")]
        public bool isSuperManager = false;

        [Tooltip("If checked, this manager will spawn in menu scenes even if it is not a SuperManager.")]
        public SceneTypes scenesToUseIn = SceneTypes.All;

        public enum SceneTypes
        {
            All,
            Game,
            Menu
        }

		protected virtual void Awake ()
		{
			Initialize ();
		}

        public virtual void Initialize()
        {

        }
    }
}

