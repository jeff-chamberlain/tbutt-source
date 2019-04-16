using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace TButt
{
    /// <summary>
    /// Manages all types of screenfades.
    /// </summary>
    /// MARKED PARTIAL FOR OW SPECIFIC EXTENSION
    public static partial class TBFade
    {
        public static Color32 defaultColor = new Color(0, 0, 0, 1);
        public static Color32 sceneChangeColor = new Color(0, 0, 0, 1);
        public static Color32 trackingBoundsColor = new Color(0, 0, 0, 1);

        private static Shader _screenFadeShader;

        static TBScreenFade screenFade;

        private static Color32 _lastFadeColor = new Color(0, 0, 0, 1);   // useful for tracking what the last color we faded out to was, so we can fade back in from it
        private static bool _isFading;                                  // keeps track of fade state

        /// <summary>
        /// Fades the screen out to the specified color over the specified time. Uses default color if no color is provided.
        /// </summary>
        /// <param name="time">in seconds</param>
        /// <param name="color">will use TBFadeManager.defaultColor if null</param>
        /// <param name="ignoreTimestep">ON by default</param>
        public static void FadeOut(float time = 1.5f, Color? color = null, bool ignoreTimestep = true, Ease easeType = Ease.Linear)
        {
            InterruptFade();
            Color fadeColor = color ?? defaultColor;

            if (_lastFadeColor.a == 0)
                _lastFadeColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);

            screenFade.enabled = true;
            screenFade.fadeMaterial.color = _lastFadeColor;
            DOTween.To(() => screenFade.fadeMaterial.color,
                x => screenFade.fadeMaterial.color = x,
                fadeColor,
                time)
                .SetEase(easeType)
                .SetUpdate(ignoreTimestep)
                .OnComplete(FinishFadeOut)
                .SetId("ScreenFade");

            _lastFadeColor = fadeColor;                 // store off the last fade color
            _isFading = true;
		}

        /// <summary>
        /// Fades back in from whatever color we last faded out to, or uses the default color if this is the first fade.
        /// Note that some platforms (like Gear VR and Morpheus) should always fade in from black in the first scene.
        /// 
        /// This can be called at any time, but it is automatically called by TBCameraRig every time a camera instance is spawned (like at the start of a new scene).
        /// Note that it is called in TBCameraRig's Awake function for HMDs without compositor fade support, and in Start for those with compositor fade support.
        /// </summary>
        /// <param name="time">in seconds</param>
        /// <param name="ignoreTimestep"></param>
        public static void FadeIn(float time = 1.5f, bool ignoreTimestep = true, Ease easeType = Ease.Linear)
        {
            // If there's not already a TBScreenFade, that means we're loading a new scene. We want to add one to the camera.
            if (screenFade == null)
            {
                Debug.Log("Fade in started!");
                AddScreenFade();
                screenFade.fadeMaterial.color = sceneChangeColor; // Default the color to fully opaque on new scene loads.
            }

            InterruptFade();

            screenFade.enabled = true;

            DOTween.To(() => screenFade.fadeMaterial.color,
                x => screenFade.fadeMaterial.color = x,
                new Color(_lastFadeColor.r / 255.0f, _lastFadeColor.g / 255.0f, _lastFadeColor.b / 255.0f, 0),
                time)
                .SetEase(easeType)
                .SetUpdate(ignoreTimestep)
                .OnComplete(FinishFadeIn)
                .SetId("ScreenFade");

			_isFading = true;
        }

        public static void AddScreenFade()
        {
            if (_screenFadeShader == null)
                _screenFadeShader = Shader.Find("TButt/UI/Unlit Transparent Color");

            if (TBCameraRig.instance.GetCameraMode() == TBCameraRig.CameraMode.Single)
            {
                screenFade = TBCameraRig.instance.GetCenterEyeCamera().gameObject.AddComponent<TBScreenFade>();
                screenFade.fadeMaterial = new Material(_screenFadeShader);
                screenFade.fadeMaterial.color = Color.black;
            }
            else
            {
                screenFade = TBCameraRig.instance.GetCenterEyeCamera().gameObject.AddComponent<TBScreenFade>();
                screenFade.fadeMaterial = new Material(_screenFadeShader);
                //TBCameraRig.instance.GetRightEyeCamera().gameObject.AddComponent<TBScreenFade>().fadeMaterial = screenFade.fadeMaterial;
            }
        }

        /// <summary>
        /// If there's a fade in progress, this will stop it so a new fade can start from where it left off.
        /// </summary>
        public static void InterruptFade()
        {
            // Early out if we're not in the middle of a fade.
            if (!_isFading)
                return;

            // Stop the fade and record its current state. We want to store off this color and the alpha so we can start the next fade from it smoothly.
            DOTween.Kill("ScreenFade");

			_lastFadeColor = screenFade.fadeMaterial.color;
        }

        /// <summary>
        /// True if we in the middle of a fade
        /// </summary>
        /// <returns></returns>
        public static bool IsFading()
        {
            return _isFading;
        }

        private static void FinishFadeIn()
        {
            screenFade.enabled = false;

            _lastFadeColor = screenFade.fadeMaterial.color;
            TBLogging.LogMessage("Fade in Finished!");
            _isFading = false;
        }

        private static void FinishFadeOut()
        {
            _lastFadeColor = screenFade.fadeMaterial.color;
            _isFading = false;
        }
    }
}