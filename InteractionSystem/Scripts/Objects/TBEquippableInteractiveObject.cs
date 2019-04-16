using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace TButt.InteractionSystem
{
	/// <summary>
	/// Physics objects that you want to pick up in particular positions and orientations.
	/// Usually some kind of tool that you'll always want to use in a particular way i.e. a gun or a screwdriver.
	/// </summary>
	public class TBEquippableInteractiveObject : TBPhysicsInteractiveObject
	{
		[Tooltip ("A list of points where the object can be grabbed. The object will orient to the orientation of the selected interaction point when held by the player.")]
		public Transform[] leftEquipPoints;
		public Transform[] rightEquipPoints;

		protected Transform _closestInteractionPoint;     //the closest interaction point on this object to where it was grabbed
		protected int _defaultLayer;
		protected int _heldObjectsLayer;

		protected override void Awake ()
		{
			base.Awake ();
			_heldObjectsLayer = LayerMask.NameToLayer ("HeldObjects");
			_defaultLayer = gameObject.layer;
		}

		protected override void ObjectSelectionBehavior (TBInteractorBase interactor)
		{
			if (HasEquipPointsForInteractor (interactor))
			{
				_closestInteractionPoint = GetClosestInteractionPoint (interactor);

				if (physicsOptions.collideWithStaticGeoWhileHeld)
				{
					_pickupTransform = _attachedInteractor.GetPickupTransform ();
					_attachedInteractor.SetPickupTransformPosAndRot (transform.position, transform.rotation);
				}
				else
				{
					//if we don't want the object to collide with static geo, we just make it a child of the hand and turn its rigidbody kinematic
					_Rigidbody.isKinematic = true;
					transform.SetParent (_attachedInteractor.transform, true);

					//using local rotate quaternion was causing erratic behavior
					//using local rotate while trying to subtract euler angle rotations wasn't producing the right rotation
					//so now we calculate the rotation using quaternions, then pass in the euler angle value to the local rotate function
					Quaternion finalRotation = Quaternion.Inverse (_attachedInteractor.interactionCenter.localRotation) * Quaternion.Inverse (_closestInteractionPoint.localRotation);
					transform.DOLocalRotate (finalRotation.eulerAngles, 0.1f)
											.OnStart (() => SetTravellingToHand (true));

					//move the object into position over time so it doesn't just snap
					//turn off the colliders while moving to hand so it doesn't make objects go flying
					transform.DOLocalMove (_attachedInteractor.interactionCenter.localPosition - (finalRotation * _closestInteractionPoint.localPosition), 0.1f)
											.OnComplete (() => SetTravellingToHand (false));

					_lastPosition = transform.position;
					_lastRotation = transform.rotation;
				}
			}
			else
			{
				base.ObjectSelectionBehavior (interactor);
			}
		}

		/// <summary>
		/// Calculates the rotational and positional deltas of the attached interactor over the last frame. Used by the UpdateVelocities function. 
		/// </summary>
		/// <param name="rotDelta"></param>
		/// <param name="posDelta"></param>
		protected override void CalculateVelocityDeltas (out Quaternion rotDelta, out Vector3 posDelta)
		{
			if (HasEquipPointsForInteractor (_attachedInteractor))
			{
				rotDelta = _attachedInteractor.interactionCenter.rotation * Quaternion.Inverse (_closestInteractionPoint.rotation);
				posDelta = (_attachedInteractor.interactionCenter.position - _closestInteractionPoint.position);
			}
			else
			{
				base.CalculateVelocityDeltas (out rotDelta, out posDelta);
			}
		}

		protected override bool CheckForDrop ()
		{
			if (!HasAttachedInteractor ())
				return false;

			if (HasEquipPointsForInteractor (_attachedInteractor))
			{
				if (_attachedInteractor.dropDistance > 0f)
				{
					float shortestDistance = Vector3.Distance (_attachedInteractor.interactionCenter.position, _closestInteractionPoint.position);

					if (shortestDistance > _attachedInteractor.dropDistance)
					{
						Debug.Log ("Dropped because of distance: " + shortestDistance, gameObject);
						DroppedBecauseOfDistance ();
						return true;
					}
				}

				return false;
			}
			else
			{
				return base.CheckForDrop ();
			}
		}

		/// <summary>
		/// Finds the closest interaction point on this object to the player's hand when the object is grabbed
		/// </summary>
		/// <param name="interactor"></param>
		/// <returns></returns>
		protected Transform GetClosestInteractionPoint (TBInteractorBase interactor)
		{
			Transform[] equipPoints = interactor.controller == TBInput.Controller.LHandController ? leftEquipPoints : rightEquipPoints;
			Transform closestPoint = equipPoints[0];
			float closestDistance = Vector3.Distance (equipPoints[0].position, interactor.transform.position);

			for (int i = 1; i < equipPoints.Length; i++)
			{
				if (equipPoints[i] != null)
				{
					float dist = Vector3.Distance (equipPoints[i].position, interactor.transform.position);

					if (dist < closestDistance)
					{
						closestPoint = equipPoints[i];
						closestDistance = dist;
					}
				}
			}

			return closestPoint;
		}

		protected override void OnHandSelect (TBInteractorBase interactor)
		{
			base.OnHandSelect (interactor);

			//SetLayer (_heldObjectsLayer);
		}

		protected override void OnHandDeselect (TBInteractorBase interactor)
		{
			base.OnHandDeselect (interactor);

			_closestInteractionPoint = null;
			//SetLayer (_defaultLayer);
		}

		protected void SetLayer (int layer)
		{
			gameObject.layer = layer;

			for (int i = 0; i < transform.childCount; i++)
			{
				transform.GetChild (i).gameObject.layer = layer;

				for (int j = 0; j < transform.GetChild (i).childCount; j++)
				{
					transform.GetChild (i).GetChild (j).gameObject.layer = layer;
				}
			}
		}

		protected bool HasEquipPointsForInteractor (TBInteractorBase interactor)
		{
			if (interactor.controller == TBInput.Controller.LHandController && leftEquipPoints.Length > 0)
				return true;
			else if (interactor.controller == TBInput.Controller.RHandController && rightEquipPoints.Length > 0)
				return true;

			return false;
		}
	}
}
