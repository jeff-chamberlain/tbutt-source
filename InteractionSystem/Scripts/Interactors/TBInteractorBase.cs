using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TButt;
using UnityEngine.VR;

namespace TButt.InteractionSystem
{
    public class TBInteractorBase : MonoBehaviour
    {
        [HideInInspector]
        public new Transform transform;

		public AttachParams attachParams;
		public TBInput.Controller controller;
        public LayerMask layerMask = -1;
        public TBInput.Button selectButton = TBInput.Button.PrimaryTrigger;
		public bool hideInteractorVisualsOnSelect = true;
        public InteractorEvents Events = new InteractorEvents();

		[Header ("[OBJECT INTERACTION SETTINGS]")]
		[Tooltip ("If left null, the object's transform will be used by default.")]
		public Transform interactionCenter;
		public float interactionRadius = 0.1f;
		[Tooltip ("If the player's interactor gets farther away from the object than this value, then the object gets automatically dropped.")]
		public float dropDistance = 0.1f;

		protected float _sqrInteractionRadius;

		//cached vars
		protected Renderer[] _Renderers;
		protected Transform _PickupTransform;

		protected TBInteractorType _interactorType;

		protected bool _isDisabled;
        protected GameObject _hoveredUIObject;
        protected GameObject _selectedUIObject;
        protected Collider _hoveredCollider;
        protected GameObject _selectedObject;
		protected TBInteractiveObjectBase _potentialHoveredInteractiveObject;
		protected Collider _potentialHoveredCollider;
		protected float _distToPotentialHoveredObject = Mathf.Infinity;
        protected TBInteractiveObjectBase _hoveredInteractiveObject;
        protected TBInteractiveObjectBase _selectedInteractiveObject;

		private bool _subscribedToNodeAttachEvent = false;

		protected virtual void Awake()
        {
            transform = GetComponent<Transform>();
			InitRenderers ();

			if (interactionCenter == null)
			{
				interactionCenter = transform;
			}

			InitPickupTransform ();

			interactionRadius *= TBCameraRig.instance.transform.localScale.x;
			dropDistance *= TBCameraRig.instance.transform.localScale.x;
			_sqrInteractionRadius = interactionRadius * interactionRadius;
		}

		protected virtual void InitRenderers ()
		{
			Renderer[] r = GetComponentsInChildren<Renderer> ();
			List<Renderer> renderers = new List<Renderer> ();

			for (int i = 0; i < r.Length; i++)
			{
				Type t = r[i].GetType ();
				if (t != typeof (LineRenderer))
					renderers.Add (r[i]);
			}

			_Renderers = renderers.ToArray ();
		}

		protected virtual void OnEnable ()
		{
            if (TBCore.instance == null)
                return;

			TBInteractorManager.instance.RegisterInteractor (this, _interactorType, controller);

            Transform target = TBTracking.GetTransformForNode(attachParams.nodeToAttachWith);

			if (target != null)
				AttachToNode (attachParams.nodeToAttachWith, target);
			else
				SubscribeToNodeAttachEvent ();
		}

		protected virtual void OnDisable ()
		{
            if (TBCore.instance == null)
                return;

            UnsubscribeFromNodeAttachEvent();

            try
            {
                TBInteractorManager.instance.RemoveInteractor(_interactorType, controller);
            }
            catch
            {
                Debug.LogWarning("Interacter Group Array was null when " + gameObject.name + " tried to remove itself. Unsusbscribing anyways.");
            }

		}

		protected virtual void AttachToNode (UnityEngine.XR.XRNode node, Transform t)
		{
			if (node == attachParams.nodeToAttachWith)
			{
                if (_interactorType == TBInteractorType.Hand)
                {
					transform.SetParent (t);
					transform.localPosition = attachParams.localPositionOffset;
                    transform.localEulerAngles = attachParams.localRotationOffset;
                }
				else
				{
                    if(transform.parent != t)
					    transform.MakeZeroedChildOf (t);
				}

                if (interactionCenter != null)
                {
                    Vector3 offset = interactionCenter.localPosition;
                    interactionCenter.transform.SetParent(transform.parent);
                    //transform.localPosition = -interactionCenter.localPosition + attachParams.localPositionOffset;
                    interactionCenter.transform.SetParent(transform);
                    interactionCenter.transform.localPosition = offset;     
                }
			}
		}

		void SubscribeToNodeAttachEvent ()
		{
			if (!_subscribedToNodeAttachEvent)
			{
				TBTracking.OnNodeConnected += AttachToNode;
				_subscribedToNodeAttachEvent = true;
			}
		}

		void UnsubscribeFromNodeAttachEvent ()
		{
			if (_subscribedToNodeAttachEvent)
			{
				TBTracking.OnNodeConnected -= AttachToNode;
				_subscribedToNodeAttachEvent = false;
			}
		}

		/// <summary>
		/// Called by the InteractorManager's update function.
		/// </summary>
		public virtual bool UpdateInteractor()
        {
			if (_isDisabled)
                return false;

			if (TBInput.GetButtonDown (selectButton, controller))
			{
				Select ();
			}
            else if(TBInput.GetButton(selectButton, controller))
            {
                // Don't do anything.
            }
			else if (TBInput.GetButtonUp (selectButton, controller))
			{
				Deselect ();
			}

            return true;
        }

        protected void Select (TBInteractiveObjectBase obj = null)
        {
			if (Events.OnSelectDown != null)
                Events.OnSelectDown();

			if (obj == null)
			{
				if (HasHoveredInteractiveObject ())
					OnSelect (_hoveredInteractiveObject);
			}
			else
			{
				OnSelect (obj);
			}
        }

        protected void Deselect()
        {
            if (Events.OnSelectUp != null)
                Events.OnSelectUp();

            if (HasSelectedInteractiveObject())
                OnDeselect(_selectedInteractiveObject);
        }

		public virtual void ForceSelect (TBInteractiveObjectBase obj = null)
		{
			//Debug.Log ("Force selected: " + obj.name);
			Select (obj);
		}

		public void ForceDeselect ()
		{
			Deselect ();
		}

        protected virtual void OnSelect(TBInteractiveObjectBase obj)
        {
			if (obj != null)
			{
				if (TBInteractorManager.Events.OnSelect != null)
					TBInteractorManager.Events.OnSelect (obj.gameObject, this);
			}

            obj.OnSelect(this);

			_selectedObject = obj.gameObject;

			if ((hideInteractorVisualsOnSelect && !obj.overrideInteractorVisualsOnSelect)
				|| (obj.overrideInteractorVisualsOnSelect && obj.hideInteractorVisualsOnSelect))
			{
				ToggleInteractorVisuals (false);
			}
        }

        protected virtual void OnDeselect(TBInteractiveObjectBase obj)
        {
			//Debug.Log ("Item deselected " + obj.name + " by interactor " + this.GetType ());
            obj.OnDeselect(this);

			_selectedObject = null;
            _selectedInteractiveObject = null;

			if ((hideInteractorVisualsOnSelect && !obj.overrideInteractorVisualsOnSelect)
				|| (obj.overrideInteractorVisualsOnSelect && obj.hideInteractorVisualsOnSelect))
			{
				ToggleInteractorVisuals (true);
			}
        }

        protected virtual void OnHoverEnter (Collider c)
        {
			if (_hoveredCollider != null)
			{
				if (c != _hoveredCollider)
				{
					OnHoverExit (_hoveredCollider);
				}
			}

			if (c != null)
            {
				if (c != _hoveredCollider)
					if (TBInteractorManager.Events.OnHoverEnter != null)
						TBInteractorManager.Events.OnHoverEnter(c, this);
            }

			SetHoveredCollider (c);
        }

        protected virtual void OnHoverExit(Collider c)
        {
            if(TBInteractorManager.Events.OnHoverExit != null)
                TBInteractorManager.Events.OnHoverExit(c, this);
        }

        /// <summary>
        /// Fires when the interactor is removed from the interactor manager.
        /// </summary>
        public virtual void Remove()
        {
            return;
        }

		protected virtual void SetHoveredCollider (Collider c)
		{
			_hoveredCollider = c;
		}

		public virtual Collider GetHoveredCollider ()
		{
			return _hoveredCollider;
		}

        public virtual bool HasHoveredCollider ()
        {
            return _hoveredCollider != null;
        }

        public virtual TBInteractiveObjectBase GetSelectedInteractiveObject()
        {
            return _selectedInteractiveObject;
        }

        public virtual TBInteractiveObjectBase GetHoveredInteractiveObject()
        {
            return _hoveredInteractiveObject;
        }

        public virtual bool HasSelectedInteractiveObject()
        {
            return _selectedInteractiveObject != null;
        }

        public virtual bool HasHoveredInteractiveObject()
        {
            return _hoveredInteractiveObject != null;
        }

        public virtual bool HasHoveredUIObject()
        {
            return _hoveredUIObject != null;
        }

        public virtual bool HasSelectedUIObject()
        {
            return _selectedUIObject != null;
        }

        public virtual void SetSelectedInteractiveObject(TBInteractiveObjectBase obj)
        {
            _selectedInteractiveObject = obj;
            if (Events.OnInteractiveObjectSelect != null)
                Events.OnInteractiveObjectSelect(obj);
        }

        public virtual void SetHoveredInteractiveObject (TBInteractiveObjectBase obj)
        {
            _hoveredInteractiveObject = obj;
            if (Events.OnInteractiveObjectHoverEnter != null)
                Events.OnInteractiveObjectHoverEnter(obj);

			ResetPotentialHoveredObject ();
        }

        public virtual void UnsetSelectedInteractiveObject(TBInteractiveObjectBase obj)
        {
            if (obj == _hoveredInteractiveObject)
            {
                _selectedObject = null;
                _selectedInteractiveObject = null;
            }
            if (Events.OnInteractiveObjectDeselect != null)
                Events.OnInteractiveObjectDeselect(obj);
        }

        public virtual void UnsetHoveredInteractiveObject(TBInteractiveObjectBase obj)
        {
            if (obj == _hoveredInteractiveObject)
            {
                _hoveredInteractiveObject = null;
				_hoveredCollider = null;
            }
            if (Events.OnInteractiveObjectHoverExit != null)
                Events.OnInteractiveObjectHoverExit(obj);

			ResetPotentialHoveredObject ();
		}

		/// <summary>
		/// Checks checks to see if the passed in interactive object is the closest interactive object to the interaction center
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="hoveredCollider"></param>
		public virtual void EvaluateForHover (TBInteractiveObjectBase obj, Collider hoveredCollider)
		{
			float dist = (hoveredCollider.ClosestPoint (interactionCenter.position) - interactionCenter.position).magnitude;

			if (dist < _distToPotentialHoveredObject)
			{
				_potentialHoveredInteractiveObject = obj;
				_distToPotentialHoveredObject = dist;
				_potentialHoveredCollider = hoveredCollider;
			}

			Debug.DrawLine (hoveredCollider.ClosestPoint (interactionCenter.position), interactionCenter.position);
		}

		protected void ResetPotentialHoveredObject ()
		{
			_potentialHoveredInteractiveObject = null;
			_potentialHoveredCollider = null;
			_distToPotentialHoveredObject = Mathf.Infinity;
		}

        public virtual void SetHoveredUIObject(GameObject go)
        {
            if (go == _hoveredUIObject)
                return;

            if (_hoveredUIObject != null)
            {
                if (Events.OnUIHoverExit != null)
                    Events.OnUIHoverExit(go);
            }

            _hoveredUIObject = go;

            if (go != null)
            {
                if (Events.OnUIHoverEnter != null)
                    Events.OnUIHoverEnter(go);
            }
        }

        public virtual void SetSelectedUIObject(GameObject go)
        {
            if (go == _selectedUIObject)
                return;

            if (_selectedUIObject != null)
            {
                if (Events.OnUIDeselect != null)
                    Events.OnUIDeselect(go);
            }

            _selectedUIObject = go;

            if (go != null)
            {
                if (Events.OnUISelect != null)
                    Events.OnUISelect(go);
            }
        }

        public TBInteractorType GetInteractorType ()
		{
			return _interactorType;
		}

		public float GetSqrInteractionRadius ()
		{
			return _sqrInteractionRadius;
		}

		//creates a transform used to track the delta position of 
		void InitPickupTransform ()
		{
			_PickupTransform = new GameObject ("PickupTransform").transform;
			_PickupTransform.parent = transform;
			_PickupTransform.localPosition = interactionCenter.localPosition;
			_PickupTransform.localRotation = interactionCenter.localRotation;
		}

		public void SetPickupTransformPosAndRot (Vector3 pos, Quaternion rot)
		{
			_PickupTransform.position = pos;
			_PickupTransform.rotation = rot;
		}

		public Transform GetPickupTransform ()
		{
			return _PickupTransform;
		}

		/// <summary>
		/// Toggles the renderers for this interactor. Override this function if you wanna do anything fancy.
		/// </summary>
		/// <param name="on"></param>
		protected virtual void ToggleInteractorVisuals (bool on)
		{
			for (int i = 0; i < _Renderers.Length; i++)
				_Renderers[i].enabled = on;
		}

		[System.Serializable]
		public struct AttachParams
		{
			public UnityEngine.XR.XRNode nodeToAttachWith;
			public Vector3 localPositionOffset;
			public Vector3 localRotationOffset;
		}

        public class InteractorEvents
        {
            public delegate bool InteractorEvent();
            public delegate bool InteractorBoolEvent(bool on);
            public delegate bool InteractiveObjectEvent(TBInteractiveObjectBase obj);
            public delegate bool UIEvent(GameObject obj);

            public InteractorBoolEvent OnInteractorPaused;
            public InteractorEvent OnSelectDown;
            public InteractorEvent OnSelectUp;
            public UIEvent OnUIHoverEnter;
            public UIEvent OnUIHoverExit;
            public UIEvent OnUISelect;
            public UIEvent OnUIDeselect;
            public InteractiveObjectEvent OnInteractiveObjectHoverEnter;
            public InteractiveObjectEvent OnInteractiveObjectHoverExit;
            public InteractiveObjectEvent OnInteractiveObjectSelect;
            public InteractiveObjectEvent OnInteractiveObjectDeselect;
        }
    }
}