using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;

namespace TButt.InteractionSystem
{
    public class TBInteractorHand : TBInteractorBase
    {
		public bool useDefaultHandAnimator = false;

		[Header ("[DEBUG]")]
		public bool drawGrabRadius = false;
		public TBInput.Button recenterButton = TBInput.Button.Action2;

		//cached references
		protected Rigidbody _Rigidbody;

        protected Vector3 _localPalmDirection = Vector3.down;
		protected bool _holdingObject = false;
		protected bool _canGrabObjects = true;

		protected Collider[] _nearbyColliders;
		protected int _maxNearbyColliders = 10;

        protected override void Awake()
        {
            _interactorType = TBInteractorType.Hand;

            base.Awake();

			// Physics Attach Point
			_Rigidbody = GetComponent<Rigidbody>();
            if (_Rigidbody == null)
				_Rigidbody = gameObject.AddComponent<Rigidbody>();
			_Rigidbody.isKinematic = true;

			if (controller == TBInput.Controller.LHandController)
				_localPalmDirection = Vector3.up;
			else if (controller == TBInput.Controller.RHandController)
				_localPalmDirection = Vector3.down;

			_nearbyColliders = new Collider[_maxNearbyColliders];

			if (useDefaultHandAnimator)
				AddHandAnimationScriptByPlatform (TBCore.GetActivePlatform ());
		}

		public override bool UpdateInteractor ()
		{
#if (UNITY_EDITOR && (TB_OCULUS || TB_STEAM_VR))
			if (TBInput.GetButtonDown (recenterButton, controller))
				TBCore.Internal.ResetTracking ();
#elif (!UNITY_EDITOR && (TB_OCULUS || TB_STEAM_VR))
			if (UnityEngine.Input.GetKeyDown (KeyCode.Space))
				TBCore.Internal.ResetTracking ();
#endif

            if (!base.UpdateInteractor ())
				return false;

			if (!_holdingObject && _canGrabObjects)
				FindNearestInteractiveObject ();

			return true;
		}

		/// <summary>
		/// Finds the closest interactive object to the hand.
		/// </summary>
		protected virtual void FindNearestInteractiveObject ()
		{
			int collidersCount = Physics.OverlapSphereNonAlloc (interactionCenter.position, interactionRadius, _nearbyColliders, layerMask);

			//if we're not overlapping anything, we might as well bail out
			if (collidersCount == 0)
			{
				OnHoverEnter (null);
				return;
			}

			//vector from the center of the player's controller to the nearest point on the bounds of a collider
			Vector3 vectorToNearestPoint;
			ResetPotentialHoveredObject ();

			for (int i = 0; i < _nearbyColliders.Length; i++)
			{
				if (_nearbyColliders[i] == null)
					continue;

				//ignore static geometry
				if (_nearbyColliders[i].attachedRigidbody == null)
					continue;

				vectorToNearestPoint = _nearbyColliders[i].ClosestPoint (interactionCenter.position) - interactionCenter.position;

				//if the object is behind the player's hand, ignore it
				if (Vector3.Dot (GetWorldPalmDirection (), vectorToNearestPoint.normalized) < -0.75f)
					continue;

				// we send out an event to all colliders so they can be evaluated as candidates for hand hovering
				TBInteractorManager.Events.OnHoverEvaluate (_nearbyColliders[i], this);
			}

			OnHoverEnter (_potentialHoveredCollider);

			//clear out the array each frame so we don't leave stuff in there
			Array.Clear (_nearbyColliders, 0, _nearbyColliders.Length);
		}

		protected override void OnSelect (TBInteractiveObjectBase obj)
		{
			if (!obj.CanBeSelected ())
				return;

			//check to see if this object is being held by the other hand. if so, force the other hand to drop it.
			if (obj.HasInteractor ())
				obj.GetInteractor ().ForceDeselect ();

			base.OnSelect (obj);

			if (!obj.CheckHoverEventsWhileInteracted ())
				_holdingObject = true;
		}

		protected override void OnDeselect (TBInteractiveObjectBase obj)
		{
			base.OnDeselect (obj);

			if (!obj.CheckHoverEventsWhileInteracted ())
				_holdingObject = false;
		}

		protected Vector3 GetWorldPalmDirection ()
		{
			return interactionCenter.TransformDirection (_localPalmDirection);
		}

		/// <summary>
		/// Adds the appropriate hand animation script to the object based on the current platform. Override this function if you want to add custom animation scripts to your hands.
		/// </summary>
		/// <param name="platform"></param>
		protected virtual void AddHandAnimationScriptByPlatform (VRPlatform platform)
		{
			switch (platform)
			{
				case VRPlatform.OculusPC:
					gameObject.AddComponent<TBControllerAnimatorOculusTouchInput> ();
					break;
				case VRPlatform.SteamVR:
					gameObject.AddComponent<TBControllerAnimatorViveControllerInput> ();
					break;
				default:
					gameObject.AddComponent<TBControllerAnimatorTBInput> ();
					break;
			}
		}

		public void SetCanGrabObjects (bool val)
		{
			_canGrabObjects = val;
		}

		public void SetHoldingObject (bool val)
		{
			_holdingObject = val;
		}

		#region GIZMOS
		protected virtual void OnDrawGizmosSelected ()
		{
			if (!drawGrabRadius)
				return;

			Transform t = GetComponent<Transform> ();

			Gizmos.color = new Color (0f, 1f, 0.5f, 0.25f);

			if (interactionCenter == null)
			{
				Gizmos.DrawSphere (t.position, interactionRadius);
			}
			else
			{
				Gizmos.DrawSphere (interactionCenter.position, interactionRadius);
			}

			Gizmos.color = new Color (0f, 1f, 0.5f);

			if (interactionCenter == null)
			{
				Gizmos.DrawWireSphere (t.position, interactionRadius);

				if (Application.isPlaying)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawRay (t.position, GetWorldPalmDirection () * interactionRadius * 2f);
				}
			}
			else
			{
				Gizmos.DrawWireSphere (interactionCenter.position, interactionRadius);

				if (Application.isPlaying)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawRay (interactionCenter.position, GetWorldPalmDirection () * interactionRadius * 2f);
				}
			}
		} 
		#endregion
	}
}