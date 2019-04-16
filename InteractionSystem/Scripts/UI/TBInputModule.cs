using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TButt;

namespace TButt.InteractionSystem
{
    /// <summary>
    /// UGUI Input Module extension that works with TButt hand and reticle interactions.
    /// </summary>
    public class TBInputModule : BaseInputModule
    {
        [Header("UI Interaction Settings")]
        public bool allowReticleInteractions = true;
        public bool allowHandInteractions = true;

        [Space(10)]

        [Header(" [Runtime variables]")]
        [Tooltip("Indicates whether or not the gui was hit by any controller this frame")]
        public bool guiHit;

        [Tooltip("Indicates whether or not a button was used this frame")]
        public bool buttonUsed;

        private GameObject _targetObject;
        private PointerEventData _pointerEventData;

        private UIInteractor[] _uiInteractors;
        private bool _initialized = false;

        [HideInInspector]
        private Camera _interactorCamera;   // Camera used for raycasting against UI.

        private GameObject _hoveredObject;


        private static TBInputModule _instance;
        public static TBInputModule instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<TBInputModule>();
                if (_instance == null)
                    Debug.LogError("No TBInputModule was found in the scene! Cannot execute UI interactions.");

                return _instance;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (_initialized)
                return;

            // Create camera for the controller(s) to use for raycasting.
            _interactorCamera = new GameObject("UI Camera (TButt)").AddComponent<Camera>();
            _interactorCamera.clearFlags = CameraClearFlags.Nothing;
            _interactorCamera.cullingMask = 0;
            _interactorCamera.stereoTargetEye = StereoTargetEyeMask.None;

            foreach (Canvas c in FindObjectsOfType<Canvas>())
            {
                c.worldCamera = _interactorCamera;
            }

            _initialized = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_interactorCamera == null)
                return;

            if (_interactorCamera.gameObject != null)
                _interactorCamera.gameObject.SetActive(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_interactorCamera == null)
                return;

            if (_interactorCamera.gameObject != null)
                _interactorCamera.gameObject.SetActive(false);
        }

        /// <summary>
        /// Adds a reticle caster to the list of UI interactors.
        /// </summary>
        /// <param name="interactor"></param>
        public void AddReticleCaster(TBInteractorReticleCaster interactor)
        {
            if (_uiInteractors == null)
                _uiInteractors = new UIInteractor[] { new UIInteractor() };
            else
                _uiInteractors = TBArrayExtensions.AddToArray<UIInteractor>(_uiInteractors, new UIInteractor());

            _uiInteractors[_uiInteractors.Length-1].interactor = interactor;

            interactor.SetIndexFromUIInputModule(_uiInteractors.Length - 1);
        }

        /// <summary>
        /// Removes a reticle caster from the list of UI interactors.
        /// </summary>
        /// <param name="interactor"></param>
        public void RemoveReticleCaster(TBInteractorBase interactor)
        {
            if (_uiInteractors == null)
                return;
            else
            {
                for(int i = 0; i < _uiInteractors.Length; i++)
                {
                    if (_uiInteractors[i].interactor != null)
                        if (_uiInteractors[i].interactor == interactor)
                        {
                            _uiInteractors = TBArrayExtensions.RemoveFromArray<UIInteractor>(_uiInteractors, i);
                            break;
                        }
                }
            }
        }

        private bool GetLookPointerEventData(int index)
        {
            if (_uiInteractors[index].pointEvent == null)
                _uiInteractors[index].pointEvent = new PointerEventData(base.eventSystem);
            else
                _uiInteractors[index].pointEvent.Reset();

            _uiInteractors[index].pointEvent.delta = Vector2.zero;

            // The screen's midpoint is defined differently in native integrations.
            //if (TBCore.IsNativeIntegration())
            //    _uiInteractors[index].pointEvent.position = new Vector2(UnityEngine.VR.VRSettings.eyeTextureWidth / 2, UnityEngine.VR.VRSettings.eyeTextureHeight / 2);
         //   else
                _uiInteractors[index].pointEvent.position = new Vector2(Screen.width / 2, Screen.height / 2);

            _uiInteractors[index].pointEvent.scrollDelta = Vector2.zero;

            base.eventSystem.RaycastAll(_uiInteractors[index].pointEvent, m_RaycastResultCache);
            _uiInteractors[index].pointEvent.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            if (_uiInteractors[index].pointEvent.pointerCurrentRaycast.gameObject != null)
            {
                guiHit = true; //gets set to false at the beginning of the process event
            }

            m_RaycastResultCache.Clear();
            return true;
        }

        private void UpdateReticlePosition(int index)
        {
            if (_uiInteractors[index].pointEvent == null)
            {
                // Don't move the reticle if it isn't hitting UI.
                return;
            }

            if (_uiInteractors[index].pointEvent.pointerCurrentRaycast.gameObject != null)
            {
                if ( _uiInteractors[index].pointEvent.pointerEnter != null)
                {
                    RectTransform draggingPlane = _uiInteractors[index].pointEvent.pointerEnter.GetComponent<RectTransform>();
                    Vector3 globalLookPos;

                    if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, _uiInteractors[index].pointEvent.position, _uiInteractors[index].pointEvent.enterEventCamera, out globalLookPos))
                    {
                        _uiInteractors[index].reticlePosition = globalLookPos;
                        _uiInteractors[index].reticleOrientation = draggingPlane.rotation;
                    }
                }
            }
        }

        public void OnSelectReticle(int index)
        {
            ClearSelection();

            _uiInteractors[index].pointEvent.pressPosition = _uiInteractors[index].pointEvent.position;
            _uiInteractors[index].pointEvent.pointerPressRaycast = _uiInteractors[index].pointEvent.pointerCurrentRaycast;
            _uiInteractors[index].pointEvent.pointerPress = null;

            if (_uiInteractors[index].currentPoint != null)
            {
                _uiInteractors[index].currentPress = _uiInteractors[index].currentPoint;

                GameObject newPressed = ExecuteEvents.ExecuteHierarchy(_uiInteractors[index].currentPress, _uiInteractors[index].pointEvent, ExecuteEvents.pointerDownHandler);

                if (newPressed == null)
                {
                    // some UI elements might only have click handler and not pointer down handler
                    newPressed = ExecuteEvents.ExecuteHierarchy(_uiInteractors[index].currentPress, _uiInteractors[index].pointEvent, ExecuteEvents.pointerClickHandler);
                    if (newPressed != null)
                    {
                        _uiInteractors[index].currentPress = newPressed;
                    }
                }
                else
                {
                    _uiInteractors[index].currentPress = newPressed;

                    // we want to do click on button down at same time, unlike regular mouse processing
                    // which does click when mouse goes up over same object it went down on
                    // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                    ExecuteEvents.Execute(newPressed, _uiInteractors[index].pointEvent, ExecuteEvents.pointerClickHandler);
                }

                if (newPressed != null)
                {
                    _uiInteractors[index].pointEvent.pointerPress = newPressed;
                    _uiInteractors[index].currentPress = newPressed;
                    Select(_uiInteractors[index].currentPress);
                    buttonUsed = true;
                }

                ExecuteEvents.Execute(_uiInteractors[index].currentPress, _uiInteractors[index].pointEvent, ExecuteEvents.beginDragHandler);
                _uiInteractors[index].pointEvent.pointerDrag = _uiInteractors[index].currentPress;
                _uiInteractors[index].currentDrag = _uiInteractors[index].currentPress;
            }
        }

        public void OnDeselectReticle(int index)
        {
            if (_uiInteractors[index].currentDrag)
            {
                ExecuteEvents.Execute(_uiInteractors[index].currentDrag, _uiInteractors[index].pointEvent, ExecuteEvents.endDragHandler);
                if (_uiInteractors[index].currentPoint != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_uiInteractors[index].currentPoint, _uiInteractors[index].pointEvent, ExecuteEvents.dropHandler);
                }
                _uiInteractors[index].pointEvent.pointerDrag = null;
                _uiInteractors[index].currentDrag = null;
            }

            if (_uiInteractors[index].currentPress)
            {
                ExecuteEvents.Execute(_uiInteractors[index].currentPress, _uiInteractors[index].pointEvent, ExecuteEvents.pointerUpHandler);
                _uiInteractors[index].pointEvent.rawPointerPress = null;
                _uiInteractors[index].pointEvent.pointerPress = null;
                _uiInteractors[index].currentPress = null;
            }
        }

        public void OnHoverEnterHand(GameObject target)
        {
            _pointerEventData = new PointerEventData(eventSystem);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerEnterHandler);
        }

        public void OnHoverExitHand(GameObject target)
        {
            _pointerEventData = new PointerEventData(eventSystem);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerExitHandler);
        }

        public void OnSelectHand(GameObject target)
        {
            _targetObject = target;
        }

        public override void Process()
        {
            // send update events if there is a selected object - this is important for InputField to receive keyboard events
            SendUpdateEventToSelectedObject();

            if (allowHandInteractions)
                ProcessHands();

            if (allowReticleInteractions)
                ProcessReticles();           
        }

        private void ProcessReticles()
        {
            // Reticle
            guiHit = false;
            buttonUsed = false;

            if (_uiInteractors == null)
                return;

            // see if there is a UI element that is currently being looked at
            for (int index = 0; index < _uiInteractors.Length; index++)
            {
                if (!_uiInteractors[index].interactor.gameObject.activeInHierarchy)
                    continue;

                _interactorCamera.transform.SetPositionAndRotation(_uiInteractors[index].interactor.GetPointerSourceTransform().position, _uiInteractors[index].interactor.GetPointerSourceTransform().rotation);

                bool hit = GetLookPointerEventData(index);

                if ((hit == false) || _uiInteractors[index].interactor.RaycastingIsPaused())   // only finish processing UI events if we got a hit on a UI element
                    continue;

                _uiInteractors[index].currentPoint = _uiInteractors[index].pointEvent.pointerCurrentRaycast.gameObject;

                // handle enter and exit events (highlight)
                base.HandlePointerExitAndEnter(_uiInteractors[index].pointEvent, _uiInteractors[index].currentPoint);

                UpdateReticlePosition(index);

                if (_uiInteractors[index].currentDrag != null)
                    ExecuteEvents.Execute(_uiInteractors[index].currentDrag, _uiInteractors[index].pointEvent, ExecuteEvents.dragHandler);
            }
        }

        private void ProcessHands()
        {
            if (_targetObject)
            {
                BaseEventData data = GetBaseEventData();
                data.selectedObject = _targetObject;
                ExecuteEvents.Execute(_targetObject, data, ExecuteEvents.submitHandler);

                _targetObject = null;
            }
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

        private int GetInteractorIndex(TBInteractorReticleCaster interactor)
        {
            for (int i = 0; i < _uiInteractors.Length; i++)
            {
                if (_uiInteractors[i].interactor == interactor)
                    return i;
            }
            return -1;
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (base.eventSystem.currentSelectedGameObject == null)
                return false;

            BaseEventData data = GetBaseEventData();

            ExecuteEvents.Execute(base.eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);

            return data.used;
        }

        public bool IsPointingAtUIObject(int index)
        {
            if (index < _uiInteractors.Length)
            {
                if (_uiInteractors[index].currentPoint != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check IsPointingAtUIObject before calling!!
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector3 GetCursorPosition(int index)
        {
            return _uiInteractors[index].reticlePosition;
        }

        /// <summary>
        /// Check IsPointingAtUIObject before calling!!
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Quaternion GetCursorRotation(int index)
        {
            return _uiInteractors[index].reticleOrientation;
        }

        public GameObject GetHoveredObject(int index)
        {
            return _uiInteractors[index].currentPoint;
        }

        public struct UIInteractor
        {
            public GameObject currentPoint;
            public GameObject currentPress;
            public GameObject currentDrag;
            public PointerEventData pointEvent;
            public TBInteractorReticleCaster interactor;
            public Vector3 reticlePosition;
            public Quaternion reticleOrientation;
        }
    }
}
