using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TButt
{
    public class TBSceneManager : MonoBehaviour
    {
        private static TBSceneManager _instance;
        public static TBSceneManager instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = TBCore.instance.gameObject.AddComponent<TBSceneManager>();
                    _instance.OnNewLevelStart();
                }
                return _instance;
            }
        }

        private bool _isSplashScreen;
        private bool _loading;
        private bool _isMenuScene;
        private AsyncOperation _asyncLoad;
        private bool _isChangingScene = false;
        private string _nextScene;
        private string _previousScene;

        public delegate void SceneEvent();

        private void OnEnable()
        {
            TBCore.OnNewScene += OnNewLevelStart;
        }

        private void OnDisable()
        {
            TBCore.OnNewScene -= OnNewLevelStart;
        }


        /// <summary>
        /// Fires when new scenes are started, not including loading scene or splash screens.
        /// </summary>
        public static event SceneEvent OnEnterNewScene;
        /// <summary>
        /// Fires when we've faded out for a scene load
        /// </summary>
        public static event SceneEvent OnLoadingFadedOut;
        /// <summary>
        /// Fires when a new scene starts to load
        /// </summary>
        public static event SceneEvent OnLoadStart;

        #region PUBLICLY ACCESSIBLE FUNCTIONS
        /// <summary>
        /// Changes to a new scene. Use this instead of Application.LoadLevel EVERYWHERE.
        /// </summary>
        /// <param name="sceneName">Name of the scene you want to load.</param>
        /// <param name="fadeDuration">In seconds.</param>
        /// <param name="useLoadingScene">If true, we'll proceed to a loading screen to load the level in the background first.</param>
        /// <param name="asyncLoading">Load the level in the background before fading out. You usually want this to be false. Loading screens will take over and handle it differently.</param>
        public void ChangeScene(string sceneName, float fadeDuration = 1.5f, bool useLoadingScene = false, bool asyncLoading = false)
        {
            if (_isChangingScene)
            {
                Debug.LogWarning("Attempted to change scene during a scene change. This is noooooo good, so the request was ignored.");
                return;
            }

            if (useLoadingScene)
                _nextScene = sceneName;
            else
                _nextScene = null;

            _previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (!useLoadingScene)
                StartCoroutine(LoadSceneRoutine(sceneName, fadeDuration, asyncLoading));
            else
                StartCoroutine(LoadSceneRoutine("Loading", fadeDuration, false));
        }

        /// <summary>
        /// Returns level loading progress if an async level load is in progress.
        /// Can be used for animating loading bars, etc.
        /// </summary>
        /// <returns>float between 0f and 1f</returns>
        public float GetLoadingProgress()
        {
            if (_asyncLoad != null)
                return _asyncLoad.progress;
            else
            {
                Debug.LogWarning("Attempting to check the loading progress of a scene load, but no scene load currently exists.");
                return 1;
            }
        }

        /// <summary>
        /// Returns the name of the scene that the player was in before this one, not including loading screens.
        /// </summary>
        /// <returns>old scene's name as reported by Application.loadedLevelName during that scene</returns>
        public string GetPreviousSceneName()
        {
            return _previousScene;
        }

        /// <summary>
        /// Reports whether or not the current scene is a splash screen.
        /// This is used to disable certain functionality when we're on splash screens.
        /// </summary>
        /// <returns></returns>
        public bool IsSplashScreen()
        {
            return _isSplashScreen;
        }

        public bool IsLoadingScreen()
        {
            return _loading;
        }

        public bool IsMenuScene()
        {
            return _isMenuScene;
        }

        public string GetNextScene()
        {
            return _nextScene;
        }
        #endregion

        #region INTERNAL FUNCTIONS
        /// <summary>
        /// Handles the loading of the level. Should only be called from within TBSceneManager.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="fadeDuration"></param>
        /// <param name="asyncLoading"></param>
        /// <returns></returns>
        private IEnumerator LoadSceneRoutine(string sceneName, float fadeDuration, bool asyncLoading)
        {
            Debug.Log("Starting scene load...");
            float loadStartTime = Time.realtimeSinceStartup;
            _isChangingScene = true;

            if (OnLoadStart != null)
                OnLoadStart();

            #if TB_STEAM_VR
            if (TBCore.GetActivePlatform() == VRPlatform.SteamVR)
                SteamVR_Events.Loading.Send(true);
            #endif

            // If we're using async loading, start the async operation and wait until it reaches 90%.
            // The progress won't extend past 90% if its allowSceneActivation value is set to false.
            if (asyncLoading)
            {
                _asyncLoad = SceneManager.LoadSceneAsync(sceneName);
                _asyncLoad.allowSceneActivation = false;
                while (_asyncLoad.progress < 0.9f)
                    yield return new WaitForEndOfFrame();
            }

            #if TB_STEAM_VR
            if (TBCore.GetActivePlatform() == VRPlatform.SteamVR)
                SteamVR_Events.LoadingFadeOut.Send(fadeDuration);
            #endif

            TBFade.FadeOut(fadeDuration);

            while (TBFade.IsFading())
                yield return new WaitForEndOfFrame();

            if (OnLoadingFadedOut != null)
                OnLoadingFadedOut();

            // After fade is complete, allow scene activation on an async operation, or just simply loading the level.
            if (asyncLoading)
                _asyncLoad.allowSceneActivation = true;
            else
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            Debug.Log("Finished scene load in " + (Time.realtimeSinceStartup - loadStartTime));
        }

        /// <summary>
        /// Only called from TBCoreEvent.
        /// Runs maintenance scripts that need to be called whenever a new scene is loaded.
        /// </summary>
        /// <param name="level"></param>
        public void OnNewLevelStart()
        {
			_isChangingScene = false;

			string lowercasePath = SceneManager.GetActiveScene ().path.ToLower ();
			//Debug.Log ("<color=yellow>Current scene is : " + SceneManager.GetActiveScene ().path + "</color>");

            _isMenuScene = lowercasePath.Contains("menu");
            _isSplashScreen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Splash-");
            _loading = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Loading");

			if (OnEnterNewScene != null)
				OnEnterNewScene ();

			if (IsSplashScreen())
                return;
            if (IsLoadingScreen())
                return;
        }
        #endregion
    }
}
