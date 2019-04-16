using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;

namespace TButt.InteractionSystem
{
    public class TBInteractorReticleCaster : TBInteractorBase
    {
        public Transform reticleTransform;
        public bool useSphereCast;
		public float spherecastRadius = 0.05f;
		public float raycastDistance;
        public bool applyEasing;
        public bool orientToSurface;
		public bool flipSurfaceNormal = false;
        public bool scaleWithDistance;
		
		protected Transform _pointerSourceTransform;
        protected int _interactorIndex;
        protected float _defaultScale;
        protected float _savedRaycastDistance;
        protected float _savedLastDistance;
        protected bool _raycastingActive = true;
		protected float _defaultSpherecastRadius;

        // stuff used for calculating reticle interactions
        protected RaycastHit _hit;
        protected bool _raycastIsHittingObject;
        protected float _uiDistnace;
        protected float _lastDistance = 0f;
        protected float _maxScaleChangePerFrame = 0.05f;
        protected float _maxDistanceChangePerFrame = 100;
        protected Vector3 _hitNormal;

        protected override void Awake()
        {
			_defaultSpherecastRadius = spherecastRadius;

            base.Awake();
            if (_pointerSourceTransform == null)
                _pointerSourceTransform = GetComponent<Transform>();
            _defaultScale = reticleTransform.localScale.x;
            _savedRaycastDistance = raycastDistance;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Enable UGUI support
            if (TBInputModule.instance != null)
                TBInputModule.instance.AddReticleCaster(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Disable UGUI support
            if (Application.isPlaying)
            {
                if (TBInputModule.instance != null)
                    TBInputModule.instance.RemoveReticleCaster(this);
            }
        }

        protected override void OnSelect(TBInteractiveObjectBase obj)
        {
            base.OnSelect(obj);
        }

        public override bool UpdateInteractor()
        {
            DoRaycast();

            UpdateReticlePosition();

            if (scaleWithDistance)
                UpdateReticleScale();

            if (TBInput.GetButtonDown(selectButton, controller))
            {
                if (HasHoveredUIObject())
                {
                    TBInputModule.instance.OnSelectReticle(_interactorIndex);
                    SetSelectedUIObject(_hoveredUIObject);
                }
                else if (HasHoveredInteractiveObject())
                    OnSelect(_hoveredInteractiveObject);

                if (Events.OnSelectDown != null)
                    Events.OnSelectDown();
            }
            else if (TBInput.GetButtonUp(selectButton, controller))
            {
                if (HasSelectedUIObject())
                {
                    TBInputModule.instance.OnDeselectReticle(_interactorIndex);
                    SetSelectedUIObject(null);
                }
                else if (HasSelectedInteractiveObject())
                    OnDeselect(_selectedInteractiveObject);

                if (Events.OnSelectDown != null)
                    Events.OnSelectUp();
            }
            return true;
        }

        protected virtual void DoRaycast()
        {
            //shoot a ray or a spherecast out from your pointer device
            if (useSphereCast)
                _raycastIsHittingObject = Physics.SphereCast(_pointerSourceTransform.position, spherecastRadius, _pointerSourceTransform.forward, out _hit, raycastDistance, layerMask);
            else
                _raycastIsHittingObject = Physics.Raycast(_pointerSourceTransform.position, _pointerSourceTransform.forward, out _hit, raycastDistance, layerMask);

            if (!_raycastIsHittingObject)
            {
                OnHoverEnter(null);
            }
            else
            {
                OnHoverEnter(_hit.collider);
                //if (_hit.collider.attachedRigidbody != null)
                //    OnHoverEnter(_hit.collider.attachedRigidbody.gameObject);
                //else
                //    OnHoverEnter(_hit.collider.gameObject);
            }
        }

        /// <summary>
        //  Sets the position of the reticle, using the distance from CalculateReticleDistance.
        /// </summary>
        protected void UpdateReticlePosition()
        {
            if (HasHoveredCollider ())
            {
                CalculateReticleDistance(Vector3.Distance(_hit.point, _pointerSourceTransform.position), !applyEasing);
            }
            else
            {
                if(_raycastingActive)
                    CalculateReticleDistance(_lastDistance, !applyEasing, true);
                else
                    CalculateReticleDistance(0f, !applyEasing, false);
            }
        }

        public void CalculateReticleDistance(float distance, bool instantly = false, bool allowOverride = false)
        {
            float uiDistance = raycastDistance + 1;

            if (!HasSelectedInteractiveObject())
            {
                if (TBInputModule.instance != null)
                {
                    if (TBInputModule.instance.allowReticleInteractions)
                    {
                        if (TBInputModule.instance.IsPointingAtUIObject(_interactorIndex))
                        {
                            uiDistance = Vector3.Distance(TBInputModule.instance.GetCursorPosition(_interactorIndex), _pointerSourceTransform.position);
                            if ((uiDistance < distance) || allowOverride)
                            {
                                SetHoveredUIObject(TBInputModule.instance.GetHoveredObject(_interactorIndex));
                            }
                            else
                                SetHoveredUIObject(null);
                        }
                        else
                            SetHoveredUIObject(null);
                    }
                    else
                        SetHoveredUIObject(null);
                }
            }
            if (HasHoveredUIObject())
            { 
                    distance = uiDistance;
                    if (orientToSurface)
                        reticleTransform.rotation = TBInputModule.instance.GetCursorRotation(_interactorIndex);
                    else
                        reticleTransform.FaceCamera();
            }
            else
            {
                if (orientToSurface && _raycastIsHittingObject)
                    reticleTransform.rotation = Quaternion.LookRotation(_hit.normal * (flipSurfaceNormal ? -1f : 1f));
                else
                    reticleTransform.FaceCamera();
            }

            if (instantly)
            {
                reticleTransform.position = _pointerSourceTransform.position + _pointerSourceTransform.forward * distance;
                _lastDistance = distance;
            }
            else
            {
                float distanceChange = distance - _lastDistance;
                float absDistanceChange = Mathf.Min(Mathf.Abs(distanceChange), _maxDistanceChangePerFrame);
                float newDistanceValue = _lastDistance + absDistanceChange * Mathf.Sign(distanceChange);

                reticleTransform.position = _pointerSourceTransform.position + _pointerSourceTransform.forward * newDistanceValue;

                _lastDistance = newDistanceValue;
            }

            #if UNITY_EDITOR
            Debug.DrawLine(reticleTransform.position, _pointerSourceTransform.position, Color.yellow);
            #endif
        }

        public virtual void SetRaycastingActive (bool active, bool showReticleArt = true)
        {
            if (_raycastingActive == active)
                return;

            if(!active)
            {
                _savedRaycastDistance = raycastDistance;
                _savedLastDistance = _lastDistance;
                raycastDistance = 0;
            }
            else
            {
                _lastDistance = _savedLastDistance;
                raycastDistance = _savedRaycastDistance;
            }

			_raycastingActive = active;

            if (Events.OnInteractorPaused != null)
                Events.OnInteractorPaused(!active);
        }

        public virtual void SetRaycastDistance(float d)
        {
            if (!_raycastingActive)
                _savedRaycastDistance = d;
            else
                raycastDistance = d;
        }

        protected virtual void UpdateReticleScale()
        {
            float cursorScale = _lastDistance * _defaultScale;
            reticleTransform.localScale = Vector3.one * cursorScale;
        }

        public Transform GetReticleTransform()
        {
            return reticleTransform;
        }

        public float GetLastDistance()
        {
            return _lastDistance;
        }

        public Transform GetPointerSourceTransform()
        {
            return _pointerSourceTransform;
        }

        public bool RaycastingIsPaused()
        {
            return !_raycastingActive;
        }

		public bool IsRaycastingActive ()
		{
			return _raycastingActive;
		}

        public void SetIndexFromUIInputModule(int index)
        {
            _interactorIndex = index;
        }
    }
}