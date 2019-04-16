using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace TButt
{
    /// <summary>
    /// Stores references to individual game managers (for specific games).
    /// Treats menu scenes (in a subfolder with "Menu" in the name) differently from other scenes.
    /// </summary>
    public class TBGameCore : MonoBehaviour
    {
        public static TBGameCore instance;
        public TBGameManagerBase[] presetManagers;

        private List<GameObject> _tempManagers;

        // set this script up to listen for a "next scene is loaded and we're ready to change" event from TBSceneManager.
        void OnEnable()
        {
            TBSceneManager.OnEnterNewScene += OnEnterNewScene;
        }

        void OnDisable()
        {
			TBSceneManager.OnEnterNewScene -= OnEnterNewScene;
        }

        void OnEnterNewScene()
        {
            PurgeTempGameManagers();
            AddGameManagers();
        }

        void Awake()
        {
            if (instance != null)
            {
				Destroy (this);
                return;
            }

            float loadTime = Time.realtimeSinceStartup;
            TBLogging.LogMessage("Loading GameManagers!");

            instance = this;
            AddSuperGameManagers();

            _tempManagers = new List<GameObject>();
            

            DontDestroyOnLoad(this);
            OnEnterNewScene();
            TBLogging.LogMessage("Finished loading GameManagers! " + (Time.realtimeSinceStartup - loadTime));
        }

        /// <summary>
        /// Adds managers that get destroyed between scenes.
        /// </summary>
        void AddGameManagers()
        {
			//Debug.Log ("This is a menu scene: " + TBSceneManager.instance.IsMenuScene ());
            for (int i = 0; i < presetManagers.Length; i++)
            {
                if (!presetManagers[i].isSuperManager)
                {
                    if ((presetManagers[i].scenesToUseIn == TBGameManagerBase.SceneTypes.Game) && !TBSceneManager.instance.IsMenuScene())
                    {
                        Transform newManager = Instantiate(presetManagers[i]).transform;
                        newManager.transform.MakeZeroedChildOf(transform);
						_tempManagers.Add (newManager.gameObject);
                    }
                    else if((presetManagers[i].scenesToUseIn == TBGameManagerBase.SceneTypes.Menu) && TBSceneManager.instance.IsMenuScene())
                    {
                        Transform newManager = Instantiate(presetManagers[i]).transform;
                        newManager.transform.MakeZeroedChildOf(transform);
						_tempManagers.Add (newManager.gameObject);
					}
                    else if ((presetManagers[i].scenesToUseIn == TBGameManagerBase.SceneTypes.All))
                    {
                        Transform newManager = Instantiate(presetManagers[i]).transform;
                        newManager.transform.MakeZeroedChildOf(transform);
						_tempManagers.Add (newManager.gameObject);
					}
                }
				

			}
        }

        /// <summary>
        /// Add managers that persist between scenes.
        /// Make sure these also get added to the below AddSuperGameManagersAsync function, which is used on-device for first load to avoid a lengthy black screen.
        /// </summary>
        void AddSuperGameManagers()
        {
            if (presetManagers != null)
            {
                for (int i = 0; i < presetManagers.Length; i++)
                {
                    if (presetManagers[i].isSuperManager)
                    {
                        Transform newManager = Instantiate(presetManagers[i]).transform;
                        newManager.transform.MakeZeroedChildOf(transform);
                        DontDestroyOnLoad(newManager);
                    }
                }
            }
        }

        public void PurgeTempGameManagers()
        {
            if (_tempManagers.Count == 0)
                return;

            for (int i = 0; i < _tempManagers.Count; i++)
            {
                Destroy (_tempManagers[i]);
            }
            _tempManagers.Clear();
        }
    }
}