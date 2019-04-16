using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt
{

    public class TBResetDemo : MonoBehaviour {

        public TBInput.Button button;
        public TBInput.Controller controller;
        public string targetScene;
        public float buttonHoldTime;

        private float _startTime = 0f;
        private static bool _isResetting = false;


        void Update()
        {
            if (_isResetting)
            {
                _startTime = Time.realtimeSinceStartup;
                return;
            }

            if (TBInput.GetButtonDown(button, controller))
            {
                _startTime = Time.realtimeSinceStartup;
            }

            if(TBInput.GetButton(button,controller))
            {
                if (Time.realtimeSinceStartup - _startTime > buttonHoldTime)
                    StartCoroutine(Reset());
            }
        }

        private IEnumerator Reset()
        {
            _isResetting = true;
            yield return null;
            TBFade.FadeOut(1f);
            while (TBFade.IsFading())
                yield return null;
            Destroy(DarkTonic.MasterAudio.MasterAudio.Instance.gameObject);
            Destroy(PoolBoss.Instance.gameObject);
            Destroy(TBGameCore.instance.gameObject);
            Destroy(FindObjectOfType<DarkTonic.MasterAudio.PlaylistController>().gameObject);
            _isResetting = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }

}