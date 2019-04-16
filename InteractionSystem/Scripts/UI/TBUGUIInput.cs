using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/*
namespace TButt
{
    /// <summary>
    /// A class that enables interactions with Unity GUI objects in VR.
    /// Inspired by https://github.com/VREALITY/ViveUGUIModule/
    /// </summary>
    public class TBUGUIInput : BaseInputModule
    {
        public static TBUGUIInput instance;

        [Header(" [Cursor setup]")]
        public Sprite cursorSprite;
        public Material cursorMaterial;
        public float defaultCursorScale = 0.00025f;

        [Space(10)]

        [Header(" [Runtime variables]")]
        [Tooltip("Indicates whether or not the gui was hit by any controller this frame")]
        public bool guiHit;

        [Tooltip("Indicates whether or not a button was used this frame")]
        public bool buttonUsed;

        [Tooltip("Generated cursors")]
        public RectTransform[] cursors;

        private GameObject[] _currentPoints;
        private GameObject[] _currentPresses;
        private GameObject[] _currentDrags;

        private PointerEventData[] _pointEvents;

        private int _numcursors;
        private Transform[] controllerTransforms;

        private bool _initialized = false;

        private float _defaultUIDistance = 1f;
        private bool _cursorPulsing = false;

        [Tooltip("Generated non rendering camera (used for raycasting ui)")]
        public Camera controllerCamera;

        protected override void Start()
        {
            base.Start();

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "main")
                _defaultUIDistance = 0.68f;

            transform.MakeZeroedChildOf(TBCameraRig.instance.GetCenter());

            if (_initialized)
                return;

            instance = this;

            // Create camera for the controller(s) to use for raycasting.
            controllerCamera = new GameObject("TBUGUInput Camera").AddComponent<Camera>();
            controllerCamera.clearFlags = CameraClearFlags.Nothing;
            controllerCamera.cullingMask = 0;

            _initialized = true;

            _numcursors = 2;

            cursors = new RectTransform[_numcursors];
            controllerTransforms = new Transform[_numcursors];

            // TODO: Add support for multiple cursors when using Touch, Move, Vive, etc.

            for (int i = 0; i < _numcursors; i++)
            {
                GameObject cursor = new GameObject("Cursor " + i);
                Canvas canvas = cursor.AddComponent<Canvas>();      // Potential future optimization - don't use a unique canvas for these if we can batch them.
                cursor.AddComponent<CanvasRenderer>();
                cursor.AddComponent<CanvasScaler>();
                cursor.AddComponent<UIIgnoreRaycast>();
                cursor.AddComponent<GraphicRaycaster>();

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 1000;
                canvas.sortingLayerName = "Foreground";

                Image image = cursor.AddComponent<Image>();
                image.sprite = cursorSprite;
                image.material = cursorMaterial;

                if (cursorSprite == null)
                    Debug.LogError("Set cursorSprite on " + this.gameObject.name + " to the sprite you want to use as your cursor.", this.gameObject);

                cursors[i] = cursor.GetComponent<RectTransform>();

                controllerTransforms[i] = new GameObject().transform;
                controllerTransforms[i].gameObject.name = "TBGUIInput Transform " + i;
            }

            _currentPoints = new GameObject[cursors.Length];
            _currentPresses = new GameObject[cursors.Length];
            _currentDrags = new GameObject[cursors.Length];
            _pointEvents = new PointerEventData[cursors.Length];

            // Set all of our canvases to use the controller camera as their world camera.
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                canvas.worldCamera = controllerCamera;
            }

            _initialized = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (controllerCamera == null)
                return;

            for (int i = 0; i < cursors.Length; i++)
            {
                if (cursors[i] != null)
                    cursors[i].gameObject.SetActive(true);
            }

            if (controllerCamera.gameObject != null)
                controllerCamera.gameObject.SetActive(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (controllerCamera == null)
                return;

            for(int i = 0; i < cursors.Length; i++)
            {
                if (cursors[i] != null)
                    cursors[i].gameObject.SetActive(false);
            }

            if (controllerCamera.gameObject != null)
                controllerCamera.gameObject.SetActive(false);
        }

        void Update()
        {
            switch(TBInput.GetActiveController())
            {
                case TBInput.Controller.ClickRemote:
                case TBInput.Controller.Gamepad:
                    controllerTransforms[0].position = TBCameraRig.instance.GetCenter().position;
                    controllerTransforms[0].rotation = TBCameraRig.instance.GetCenter().rotation;
                    break;
                case TBInput.Controller.Mobile3DOFController:
                    controllerTransforms[0].position = TBCameraRig.instance.GetCenter().position;
                    //controllerTransforms[0].rotation = TB3DOFArmModel.instance.GetPointerSourceTransform().rotation;
                    break;
                default:
                    for(int i = 0; i < controllerTransforms.Length; i++)
                    {
                        controllerTransforms[i].position = TBInput.GetPosition(TBInput.Controller.LHandController);
                        controllerTransforms[i].rotation = TBInput.GetPosition(TBInput.Controller.LHandController);
                    }
                    break;
            }
            controllerTransforms[0].position = TBCameraRig.instance.GetCenter().position;
            #if DAYDREAM || FAKE_DAYDREAM || GEAR_VR || FAKE_GEAR_VR
            if(ControlManager.currentControlType == FPControlType.Daydream)
                controllerTransforms[0].rotation = TB3DOFArmModel.instance.GetPointerSourceTransform().rotation;
            else
                controllerTransforms[0].rotation = TBCameraRig.instance.GetTBCenter().transform.rotation;
#else
            controllerTransforms[0].rotation = TBCameraRig.instance.GetCenter().rotation;
#endif

        }

        #region INPUT MODULE CLASSES
        private bool GetLookPointerEventData(int index)
        {
            if (_pointEvents[index] == null)
                _pointEvents[index] = new PointerEventData(base.eventSystem);
            else
                _pointEvents[index].Reset();

            _pointEvents[index].delta = Vector2.zero;

            // The screen's midpoint is defined differently in native integrations.
            if(TBCore.IsNativeIntegration())
                _pointEvents[index].position = new Vector2(UnityEngine.VR.VRSettings.eyeTextureWidth / 2, UnityEngine.VR.VRSettings.eyeTextureHeight / 2);
            else
                _pointEvents[index].position = new Vector2(Screen.width / 2, Screen.height / 2);

            _pointEvents[index].scrollDelta = Vector2.zero;

            base.eventSystem.RaycastAll(_pointEvents[index], m_RaycastResultCache);
            _pointEvents[index].pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            if (_pointEvents[index].pointerCurrentRaycast.gameObject != null)
            {
                guiHit = true; //gets set to false at the beginning of the process event
            }

            m_RaycastResultCache.Clear();
            return true;
        }

        // update the cursor location and whether it is enabled
        // this code is based on Unity's DragMe.cs code provided in the UI drag and drop example
        private void UpdateCursor(int index, PointerEventData pointData)
        {
            if (pointData == null)
            {
                cursors[index].position = controllerCamera.transform.position + controllerCamera.transform.forward * _defaultUIDistance;
                cursors[index].FaceCamera();
            }
            else if (_pointEvents[index].pointerCurrentRaycast.gameObject != null)
            {
                //cursors[index].gameObject.SetActive(true);

                if (pointData.pointerEnter != null)
                {
                    RectTransform draggingPlane = pointData.pointerEnter.GetComponent<RectTransform>();
                    Vector3 globalLookPos;
                    if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, pointData.position, pointData.enterEventCamera, out globalLookPos))
                    {
                        cursors[index].position = globalLookPos;
                        cursors[index].rotation = draggingPlane.rotation;
                    }
                }
            }
            else
            {
                cursors[index].position = controllerCamera.transform.position + controllerCamera.transform.forward * _defaultUIDistance;
                cursors[index].FaceCamera();
            }

            // scale cursor based on distance to camera
            float lookPointDistance = (cursors[index].position - TBCameraRig.instance.GetCenter().position).magnitude;
            float cursorScale = lookPointDistance * defaultCursorScale;
            if (cursorScale < defaultCursorScale)
            {
                cursorScale = defaultCursorScale;
            }

            cursors[index].localScale = Vector3.one * cursorScale;
        }


        public void ClearSelection()
        {
            if (base.eventSystem.currentSelectedGameObject)
            {
                base.eventSystem.SetSelectedGameObject(null);
            }
        }

        private void Select(GameObject go)
        {
            ClearSelection();

            if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
            {
                base.eventSystem.SetSelectedGameObject(go);
            }
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (base.eventSystem.currentSelectedGameObject == null)
                return false;

            BaseEventData data = GetBaseEventData();

            ExecuteEvents.Execute(base.eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

            return data.used;
        }

        #endregion

        private void UpdateCameraPosition(int index)
        {
            controllerCamera.transform.position = controllerTransforms[index].position;
            controllerCamera.transform.rotation = controllerTransforms[index].rotation;
        }

        public override void Process()
        {
            guiHit = false;
            buttonUsed = false;

            // send update events if there is a selected object - this is important for InputField to receive keyboard events
            SendUpdateEventToSelectedObject();

            // see if there is a UI element that is currently being looked at
            for (int index = 0; index < cursors.Length; index++)
            {
                if (controllerTransforms[index].gameObject.activeInHierarchy == false)
                {
                    if (cursors[index].gameObject.activeInHierarchy == true)
                    {
                        cursors[index].gameObject.SetActive(false);
                    }
                    continue;
                }

                UpdateCameraPosition(index);

                bool hit = GetLookPointerEventData(index);
                if (hit == false)
                {
                    // update cursor
                    UpdateCursor(index, null);
                    continue;
                }

                _currentPoints[index] = _pointEvents[index].pointerCurrentRaycast.gameObject;

                // handle enter and exit events (highlight)
                base.HandlePointerExitAndEnter(_pointEvents[index], _currentPoints[index]);

                // update cursor
                UpdateCursor(index, _pointEvents[index]);

                if (controllerTransforms[index] != null)
                {
                    if (ButtonDown(index))
                    {
                        CursorPulse();
                        ClearSelection();

                        _pointEvents[index].pressPosition = _pointEvents[index].position;
                        _pointEvents[index].pointerPressRaycast = _pointEvents[index].pointerCurrentRaycast;
                        _pointEvents[index].pointerPress = null;

                        if (_currentPoints[index] != null)
                        {
                            _currentPresses[index] = _currentPoints[index];

                            GameObject newPressed = ExecuteEvents.ExecuteHierarchy(_currentPresses[index], _pointEvents[index], ExecuteEvents.pointerDownHandler);

                            if (newPressed == null)
                            {
                                // some UI elements might only have click handler and not pointer down handler
                                newPressed = ExecuteEvents.ExecuteHierarchy(_currentPresses[index], _pointEvents[index], ExecuteEvents.pointerClickHandler);
                                if (newPressed != null)
                                {
                                    _currentPresses[index] = newPressed;
                                }
                            }
                            else
                            {
                                _currentPresses[index] = newPressed;

                                // we want to do click on button down at same time, unlike regular mouse processing
                                // which does click when mouse goes up over same object it went down on
                                // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                                ExecuteEvents.Execute(newPressed, _pointEvents[index], ExecuteEvents.pointerClickHandler);
                            }

                            if (newPressed != null)
                            {
                                _pointEvents[index].pointerPress = newPressed;
                                _currentPresses[index] = newPressed;
                                Select(_currentPresses[index]);
                                buttonUsed = true;
                            }

                            ExecuteEvents.Execute(_currentPresses[index], _pointEvents[index], ExecuteEvents.beginDragHandler);
                            _pointEvents[index].pointerDrag = _currentPresses[index];
                            _currentDrags[index] = _currentPresses[index];
                        }
                    }

                    if (ButtonUp(index))
                    {
                        if (_currentDrags[index])
                        {
                            ExecuteEvents.Execute(_currentDrags[index], _pointEvents[index], ExecuteEvents.endDragHandler);
                            if (_currentPoints[index] != null)
                            {
                                ExecuteEvents.ExecuteHierarchy(_currentPoints[index], _pointEvents[index], ExecuteEvents.dropHandler);
                            }
                            _pointEvents[index].pointerDrag = null;
                            _currentDrags[index] = null;
                        }
                        if (_currentPresses[index])
                        {
                            ExecuteEvents.Execute(_currentPresses[index], _pointEvents[index], ExecuteEvents.pointerUpHandler);
                            _pointEvents[index].rawPointerPress = null;
                            _pointEvents[index].pointerPress = null;
                            _currentPresses[index] = null;
                        }
                    }

                    // drag handling
                    if (_currentDrags[index] != null)
                    {
                        ExecuteEvents.Execute(_currentDrags[index], _pointEvents[index], ExecuteEvents.dragHandler);
                    }
                }
            }
        }

        void CursorPulse()
        {
            if (_cursorPulsing)
                return;

            cursors[0].DOPunchScale(cursors[0].transform.localScale * 1.05f, 0.25f, 4)
                                    .OnStart(() => SetCursorPulsing(true))
                                    .OnComplete(() => SetCursorPulsing(false))
                                    .SetUpdate(true);
        }

        void SetCursorPulsing(bool val)
        {
            _cursorPulsing = val;
        }

        private bool ButtonDown(int index)
        {
#if DAYDREAM || FAKE_DAYDREAM
            return TBDaydreamController.GetButtonDown(TBAVRInputType.ClickButton);
#elif RIFT
            return OVRInput.GetUp(OVRInput.Button.One);
#elif GEAR_VR
            return TBTouchpad.instance.GetTouchDown() || TB3DOFController.GetButtonDown(TB3DOFController.InputType.PrimaryButton) || TB3DOFController.GetButtonDown(TB3DOFController.InputType.PrimaryTrigger);
            //return (ControllerDevices[index] != null && ControllerDevices[index].GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger) == true);
#else
            return TBGamepad.ButtonWasPressed(TBInputType.Action1);
#endif
        }

        private bool ButtonUp(int index)
        {
#if DAYDREAM || FAKE_DAYDREAM
            return TBDaydreamController.GetButtonUp(TBAVRInputType.ClickButton);
#elif RIFT
            return OVRInput.GetUp(OVRInput.Button.One);
#elif GEAR_VR
            return TBTouchpad.instance.GetTouchUp() || TB3DOFController.GetButtonUp(TB3DOFController.InputType.PrimaryButton) || TB3DOFController.GetButtonUp(TB3DOFController.InputType.PrimaryTrigger);

            //return (ControllerDevices[index] != null && ControllerDevices[index].GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger) == true);
#else
            return TBGamepad.ButtonWasReleased(TBInputType.Action1);
#endif
        }
    }
}
*/