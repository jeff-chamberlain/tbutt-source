using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;
using DG.Tweening;

namespace TButt.InteractionSystem
{
	public class TBPhysicsInteractiveObject : TBInteractiveObjectBase
	{
		public Rigidbody[] attachedRigidbodies;
		public PhysicsOptions physicsOptions;
		public ResetOptions resetOptions;

		public bool setKinematicIfCantBeSelected = false;

		[Tooltip ("If set to true, only check the list of selectable objects when determining if the object can be grabbed.")]
		public bool overrideSelectableParts = false;
		[Tooltip ("These are the parts of the object the player can actually grab. In most cases, we don't use this, but in the case of something with a handle where we only want the handle selectable, this is useful.")]
		public GameObject[] selectableParts;

		private const float _MaxVelocityChange = 10f;
		private const float _MaxAngularVelocityChange = 20f;
		private const float _VelocityMagic = 6000f;
		private const float _AngularVelocityMagic = 50f;
		private const int _VelocityHistorySteps = 5;
		private const int _MaxDepenetrationIterations = 10;	//the max number of times we run calculations to move this object outside geometry if it's penetrating

		//cached vars
		protected Rigidbody _Rigidbody;
		protected Bounds _ColliderBounds;

		//populated when object is grabbed
		protected TBInteractorBase _attachedInteractor;	//the hand this object is currently attached to
		protected Transform _pickupTransform;				//reference to the hand's pickup transform when this object is grabbed
		protected float _startingDrag = -1;				//store off these values when the object is grabbed so we can reset them when the object is released
		protected float _startingAngularDrag = -1;

		protected Vector3?[] _velocityHistory;			//keeps track of the object's velocity while being held
		protected Vector3?[] _angularVelocityHistory;	//keeps track of the object's angular velocity while being held
		protected int _currentVelocityHistoryStep = 0;
		protected Vector3 _externalVelocity;			//velocity from external factors
		protected Vector3 _externalAngularVelocity;     //angular velocity from external factors
		protected Vector3 _lastPosition;
		protected Quaternion _lastRotation;

		protected bool _canBeSelectedByReticle = false;

		private bool _subscribedToFixedUpdate = false;  //keeps track of whether or not this object has already subscribed to fixed update so we don't accidentally subscribe too many times
		protected bool _travellingToHand = false;         //is this object currently tweening to the player's hand?

		private IEnumerator _waitForSleepRoutine;
		private IEnumerator _resetTimerRoutine;

		//throwing
		protected bool _thrownEventCalled = true;
		protected const float THROW_SPEED = 7f;
		public InteractiveObjectEvent OnObjectThrown;

		protected override void Awake ()
		{
			_Rigidbody = GetComponentInChildren<Rigidbody> ();
			if (_Rigidbody == null)
			{
				_Rigidbody = GetComponentInParent<Rigidbody> ();
				//Debug.Log (gameObject.name + ": Searching parent objects for Rigidbody...", gameObject);
			}

			if (_Rigidbody == null)
				Debug.LogError ("There was no Rigidbody found on object " + gameObject.name, gameObject);

			base.Awake ();

			_Colliders = GetComponentsInChildren<Collider> ();

			//makes it so we don't get weird jittering
			_Rigidbody.maxAngularVelocity = 100f;

			resetOptions.Init (_Rigidbody.position, _Rigidbody.rotation);

			if (TBHighlightManager.instance != null)
				gameObject.AddComponent<TBHighlight> ();
		}

		protected override void OnEnable ()
		{
			base.OnEnable ();

			SubscribeToFixedUpdate ();
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();

			UnsubscribeFromFixedUpdate ();
		}

		protected virtual void OnFixedUpdate ()
		{
			if (HasAttachedInteractor ())
			{
				bool dropped = false;
					
				if (!_travellingToHand)
					dropped = CheckForDrop ();

				if (!dropped)
				{
					if (physicsOptions.collideWithStaticGeoWhileHeld)
					{
						UpdateVelocities ();
					}
					else
					{
						//we only need to update the velocity history if the object gets hard attached to the hand
						UpdateVelocityHistory ();
					}
				}
			}

			AddExternalVelocities ();

			DetectThrow ();
		}

		protected virtual void DetectThrow ()
		{
			if (_Rigidbody.isKinematic || HasAttachedInteractor () || _thrownEventCalled)
				return;

			if (_Rigidbody.velocity.magnitude > THROW_SPEED)
			{
				if (OnObjectThrown != null)
					OnObjectThrown (this, null);

				_thrownEventCalled = true;
			}
		}

		public void SubscribeToThrowEvent (InteractiveObjectEvent callback)
		{
			OnObjectThrown += callback;
		}

		public void UnsubscribeFromThrowEvent (InteractiveObjectEvent callback)
		{
			OnObjectThrown -= callback;
		}

		protected override void OnHandSelect (TBInteractorBase interactor)
		{
			base.OnHandSelect (interactor);

			_thrownEventCalled = false;

			if (_waitForSleepRoutine != null)
				StopCoroutine (_waitForSleepRoutine);
			if (_resetTimerRoutine != null)
				StopCoroutine (_resetTimerRoutine);

			_startingDrag = _Rigidbody.drag;
			_startingAngularDrag = _Rigidbody.angularDrag;
			_Rigidbody.drag = 0;
			_Rigidbody.angularDrag = 0.05f;

			if (physicsOptions.disableKinematicOnGrab)
			{
				_Rigidbody.isKinematic = false;
			}

			_attachedInteractor = interactor;

			ObjectSelectionBehavior (interactor);

			ResetVelocityHistory ();
		}

		protected virtual void ObjectSelectionBehavior (TBInteractorBase interactor)
		{
			if (physicsOptions.collideWithStaticGeoWhileHeld)
			{
				_pickupTransform = _attachedInteractor.GetPickupTransform ();
				_attachedInteractor.SetPickupTransformPosAndRot (_Rigidbody.position, _Rigidbody.rotation);
			}
			else
			{
				_Rigidbody.isKinematic = true;
				_Rigidbody.transform.SetParent (_attachedInteractor.transform);
				_lastPosition = _Rigidbody.position;
				_lastRotation = _Rigidbody.rotation;
			}
		}

		public override bool OnHoverEnter (Collider c, TBInteractorBase interactor)
		{
			if (overrideSelectableParts)
			{
				if (!ContainedInSelectableParts (c.gameObject))
					return false;
			}
			else
			{
				if (c.attachedRigidbody == null)
					return false;

				if (gameObject.GetInstanceID () != c.gameObject.GetInstanceID () && _Rigidbody.gameObject.GetInstanceID () != c.attachedRigidbody.gameObject.GetInstanceID () && !ContainedInAttachedRigidbodies (c.gameObject))
					return false;
			}
			
			if (interactor.GetInteractorType () == TBInteractorType.Reticle && !_canBeSelectedByReticle)
				return false;

			interactor.SetHoveredInteractiveObject (this);
			if (interactor.GetInteractorType () == TBInteractorType.Hand)
				OnHandHoverEnter (interactor);
			else
				OnReticleHoverEnter (interactor);

			if (OnHoverStartByInteractor != null)
				OnHoverStartByInteractor (this, interactor);
			return true;
		}

		public override bool OnHoverExit (Collider c, TBInteractorBase interactor)
		{
			if (overrideSelectableParts)
			{
				if (!ContainedInSelectableParts (c.gameObject))
					return false;
			}
			else
			{
				if (c.attachedRigidbody == null)
					return false;

				if (gameObject.GetInstanceID () != c.gameObject.GetInstanceID () && _Rigidbody.gameObject.GetInstanceID () != c.attachedRigidbody.gameObject.GetInstanceID () && !ContainedInAttachedRigidbodies (c.gameObject))
					return false;
			}

			if (interactor.GetInteractorType () == TBInteractorType.Reticle && !_canBeSelectedByReticle)
				return false;

			interactor.UnsetHoveredInteractiveObject (this);
			if (interactor.GetInteractorType () == TBInteractorType.Hand)
				OnHandHoverExit (interactor);
			else
				OnReticleHoverExit (interactor);

			if (OnHoverExitByInteractor != null)
				OnHoverExitByInteractor (this, interactor);
			return true;
		}

		bool ContainedInAttachedRigidbodies (GameObject go)
		{
			if (attachedRigidbodies.Length == 0)
				return false;

			for (int i = 0; i < attachedRigidbodies.Length; i++)
				if (attachedRigidbodies[i].gameObject.GetInstanceID () == go.GetInstanceID ())
					return true;

			return false;
		}

		bool ContainedInSelectableParts (GameObject go)
		{
			if (selectableParts.Length == 0)
				return false;

			for (int i = 0; i < selectableParts.Length; i++)
			{
				if (selectableParts[i] == null)
					continue;

				if (selectableParts[i].GetInstanceID () == go.GetInstanceID ())
					return true;
			}

			return false;
		}

		protected override void OnHandDeselect (TBInteractorBase interactor)
		{
			base.OnHandDeselect (interactor);

			if (!physicsOptions.collideWithStaticGeoWhileHeld)
			{
				//cancel any dotween stuff we're doing to move the object into the player's hand
				//_Rigidbody.transform.DOKill ();
				_Rigidbody.transform.SetParent (null);

				SetTravellingToHand (false);

				Vector3 depenetrationVector = Vector3.zero;
				Depenetrate (ref depenetrationVector);

				_Rigidbody.velocity = Vector3.zero;
				_Rigidbody.angularVelocity = Vector3.zero;
				_Rigidbody.position += depenetrationVector;
				_Rigidbody.isKinematic = false;
			}

			_attachedInteractor = null;
			_pickupTransform = null;

			//reset drag values
			_Rigidbody.drag = _startingDrag;
			_Rigidbody.angularDrag = _startingAngularDrag;

			if (physicsOptions.enableKinematicOnRelease)
			{
				_Rigidbody.isKinematic = true;
			}

			if (physicsOptions.enableGravityOnRelease)
			{
				_Rigidbody.useGravity = true;
			}

			ApplyVelocityHistory ();
			ResetVelocityHistory ();
		}

		protected virtual void UpdateVelocities ()
		{
			float deltaTime = Time.deltaTime;
			float fixedDeltaTime = Time.fixedDeltaTime;
			float velocityMagic = _VelocityMagic / (deltaTime / fixedDeltaTime);
			float angularVelocityMagic = _AngularVelocityMagic / (deltaTime / fixedDeltaTime);

			Quaternion rotationDelta;
			Vector3 positionDelta;

			float angle;
			Vector3 axis;

			CalculateVelocityDeltas (out rotationDelta, out positionDelta);

			rotationDelta.ToAngleAxis (out angle, out axis);

			if (angle > 180)
				angle -= 360;

			if (angle != 0)
			{
				Vector3 angularTarget = angle * axis;
				if (float.IsNaN (angularTarget.x) == false)
				{
					angularTarget = (angularTarget * angularVelocityMagic) * deltaTime;
					_Rigidbody.angularVelocity = Vector3.MoveTowards (_Rigidbody.angularVelocity, angularTarget, _MaxAngularVelocityChange);
				}
			}

			Vector3 velocityTarget = (positionDelta * velocityMagic) * deltaTime;
			if (float.IsNaN (velocityTarget.x) == false)
			{
				_Rigidbody.velocity = Vector3.MoveTowards (_Rigidbody.velocity, velocityTarget, _MaxVelocityChange);
			}

			UpdateVelocityHistory ();
		}

		/// <summary>
		/// Calculates the rotational and positional deltas of the attached interactor over the last frame. Used by the UpdateVelocities function. 
		/// </summary>
		/// <param name="rotDelta"></param>
		/// <param name="posDelta"></param>
		protected virtual void CalculateVelocityDeltas (out Quaternion rotDelta, out Vector3 posDelta)
		{
			rotDelta = _pickupTransform.rotation * Quaternion.Inverse (_Rigidbody.rotation);
			posDelta = (_pickupTransform.position - _Rigidbody.position);
		}

		protected void UpdateVelocityHistory ()
		{
			if (_velocityHistory != null)
			{
				_currentVelocityHistoryStep++;
				if (_currentVelocityHistoryStep >= _velocityHistory.Length)
				{
					_currentVelocityHistoryStep = 0;
				}

				if (physicsOptions.collideWithStaticGeoWhileHeld)
				{
					_velocityHistory[_currentVelocityHistoryStep] = _Rigidbody.velocity;
					_angularVelocityHistory[_currentVelocityHistoryStep] = _Rigidbody.angularVelocity;
				}
				else
				{
					//if the object isn't suposed to collide with static geometry, we have to manually calculate 
					//the velocity and angular velocity every frame since the object gets set to kinematic and is 
					//hard attached to the hand

					float deltaTime = Time.deltaTime;
					_velocityHistory[_currentVelocityHistoryStep] = (_Rigidbody.position - _lastPosition) / deltaTime;

					Quaternion rotation = _Rigidbody.rotation * Quaternion.Inverse (_lastRotation);
					float angle;
					Vector3 axis;

					rotation.ToAngleAxis (out angle, out axis);
					_angularVelocityHistory[_currentVelocityHistoryStep] = (angle * axis * Mathf.Deg2Rad) / deltaTime;

					_lastPosition = _Rigidbody.position;
					_lastRotation = _Rigidbody.rotation;
				}
			}
		}

		/// <summary>
		/// Applies the average velocity of the object over a set number of frames.
		/// </summary>
		protected virtual void ApplyVelocityHistory ()
		{
			if (_velocityHistory != null)
			{
				Vector3? meanVelocity = GetMeanVector (_velocityHistory);
				if (meanVelocity != null)
				{
					_Rigidbody.velocity = meanVelocity.Value;
				}

				Vector3? meanAngularVelocity = GetMeanVector (_angularVelocityHistory);
				if (meanAngularVelocity != null)
				{
					_Rigidbody.angularVelocity = meanAngularVelocity.Value;
				}
			}
		}

		/// <summary>
		/// Clears out the velocity history when an object is first picked up or dropped.
		/// </summary>
		protected virtual void ResetVelocityHistory ()
		{
			if (_VelocityHistorySteps > 0)
			{
				_currentVelocityHistoryStep = 0;

				_velocityHistory = new Vector3?[_VelocityHistorySteps];
				_angularVelocityHistory = new Vector3?[_VelocityHistorySteps];
			}
		}

		protected Vector3? GetMeanVector (Vector3?[] positions)
		{
			float x = 0f;
			float y = 0f;
			float z = 0f;

			int count = 0;
			for (int index = 0; index < positions.Length; index++)
			{
				if (positions[index] != null)
				{
					x += positions[index].Value.x;
					y += positions[index].Value.y;
					z += positions[index].Value.z;

					count++;
				}
			}

			if (count > 0)
			{
				Vector3 mean;
				mean.x = x / count;
				mean.y = y / count;
				mean.z = z / count;

				return mean;
			}

			return null;
		}

		protected virtual void AddExternalVelocities ()
		{
			if (_externalVelocity != Vector3.zero)
			{
				_Rigidbody.velocity = Vector3.Lerp (_Rigidbody.velocity, _externalVelocity, 0.5f);
				_externalVelocity = Vector3.zero;
			}

			if (_externalAngularVelocity != Vector3.zero)
			{
				_Rigidbody.angularVelocity = Vector3.Lerp (_Rigidbody.angularVelocity, _externalAngularVelocity, 0.5f);
				_externalAngularVelocity = Vector3.zero;
			}
		}

		public void AddExternalVelocity (Vector3 velocity)
		{
			if (_externalVelocity == Vector3.zero)
			{
				_externalVelocity = velocity;
			}
			else
			{
				_externalVelocity = Vector3.Lerp (_externalVelocity, velocity, 0.5f);
			}
		}

		public void AddExternalAngularVelocity (Vector3 angularVelocity)
		{
			if (_externalAngularVelocity == Vector3.zero)
			{
				_externalAngularVelocity = angularVelocity;
			}
			else
			{
				_externalAngularVelocity = Vector3.Lerp (_externalAngularVelocity, angularVelocity, 0.5f);
			}
		}

		/// <summary>
		/// Checks to see if the object should be dropped because it's getting too far away from the player's hand
		/// </summary>
		/// <returns></returns>
		protected virtual bool CheckForDrop ()
		{
			if (_attachedInteractor == null)
				return false;

			if (_attachedInteractor.dropDistance > 0f)
			{
				float shortestDistance = float.MaxValue;

				//otherwise, if the object doesn't have an interaction point, we check the distance to the closest point on the collider
				for (int i = 0; i < _Colliders.Length; i++)
				{
					Vector3 closest = _Colliders[i].ClosestPoint (_attachedInteractor.interactionCenter.position);
					float distance = Vector3.Distance (_attachedInteractor.interactionCenter.position, closest);

					if (distance < shortestDistance)
					{
						shortestDistance = distance;
					}
				}

				if (shortestDistance > _attachedInteractor.dropDistance)
				{
					DroppedBecauseOfDistance ();
					return true;
				}
			}

			return false;
		}

		protected void DroppedBecauseOfDistance ()
		{
			//Debug.Log ("Forced to drop");
			_attachedInteractor.ForceDeselect ();
		}

		public Bounds GetColliderBounds (bool recalculateFirst = true)
		{
			if (recalculateFirst)
				RecalculateColliderBounds ();

			return _ColliderBounds;
		}

		protected void RecalculateColliderBounds ()
		{
			CalculateBounds (ref _ColliderBounds);
		}

		/// <summary>
		/// Calculates the world space bounds of the colliders on this object
		/// </summary>
		/// <param name="bounds"></param>
		void CalculateBounds (ref Bounds bounds, Vector3? boundsCenterOverride = null)
		{
			//we always want to encapsulate the bounds around the object's position
			bounds.center = transform.position;
			bounds.size = Vector3.zero;

			for (int i = 0; i < _Colliders.Length; i++)
			{
				bounds.Encapsulate (_Colliders[i].bounds);
			}

			//after encapsulation, we offset the bounds' center if there is an override value
			if (boundsCenterOverride != null)
				bounds.center = (Vector3)boundsCenterOverride;
		}

		void Depenetrate (ref Vector3 depenetrationOffset, Vector3? startPos = null)
		{
			//move the object out of colliders if it's inside another object
			for (int i = 0; i < _MaxDepenetrationIterations; i++)
			{
				if (!CalculateDepenetrationOffset (ref depenetrationOffset, startPos))
				{
					//Debug.Log ("It took <color=green><b>" + i + " </b></color> iterations to depenetrate <color=yellow>" + gameObject.name + "</color> from surrounding colliders. Total distance moved: " + depenetrationVector.magnitude + " meters.", gameObject);
					break;
				}
#if UNITY_EDITOR
				if (i == _MaxDepenetrationIterations - 1)
					Debug.Log ("Reached max number of depenetration iterations which is <color=red>" + _MaxDepenetrationIterations + "</color> for object <color=yellow>" + gameObject.name + "</color>", gameObject);
#endif
			}
		}

		/// <summary>
		/// Calculates a posiiton where this object won't be penetrating geometry.
		/// </summary>
		/// <param name="depenetrationPosition"></param>
		/// <param name="startPos">Optional starting position for the object. If this is null, the starting position is assumed to be the object's current position.</param>
		/// <returns>True if the object is intersecting geometry and a position has been calculated.</returns>
		bool CalculateDepenetrationOffset (ref Vector3 depenetrationOffset, Vector3? startPos)
		{
			//get the bounds of this object's colliders in a theoretical position based on its original position and where its attempting to be offset.

			if (startPos != null)
				CalculateBounds (ref _ColliderBounds, startPos + depenetrationOffset);
			else
				CalculateBounds (ref _ColliderBounds, _Rigidbody.position + depenetrationOffset);

			//TBDebug.DrawBounds (_ColliderBounds, new Color (Random.value, Random.value, Random.value), 7f);
			//find all nearby colliders based on the bounds of the colliders on this object
			Collider[] overlappingColliders = Physics.OverlapBox (_ColliderBounds.center, _ColliderBounds.extents, Quaternion.identity, -1, QueryTriggerInteraction.Ignore);

			//the direction and distance of this iterations's displacement
			Vector3 direction;
			float distance;

			//whichever collider has the farthest to move in order to depenetrate
			Collider deepestCollider = null;
			Vector3 moveDirection = Vector3.zero;
			float greatestDistance = 0f;

			bool penetrating = false;
			//number of penetrating colliders
			int penetratingCount = 0;
			//iterate through surrounding colliders
			for (int i = 0; i < overlappingColliders.Length; i++)
			{
				if (overlappingColliders[i].attachedRigidbody != null)
				{
					//if the overlapping collider is part of this object, skip it
					if (overlappingColliders[i].attachedRigidbody == _Rigidbody)
						continue;
				}

				//iterate through the colliders on this object
				for (int j = 0; j < _Colliders.Length; j++)
				{
					//check penetration with each of the surrounding colliders
					penetrating = Physics.ComputePenetration (_Colliders[j], _Colliders[j].transform.position + depenetrationOffset, _Colliders[j].transform.rotation, overlappingColliders[i], overlappingColliders[i].transform.position, overlappingColliders[i].transform.rotation, out direction, out distance);

					if (penetrating)
					{
						moveDirection += direction;

						if (distance > greatestDistance)
						{
							greatestDistance = distance;
							deepestCollider = _Colliders[j];
							//Debug.Log ("Deepest Collider is " + _Colliders[j] + " which should move " + distance + " to depenetrate from surrounding geometry.");
						}
						penetratingCount++;
					}
				}
			}

			if (deepestCollider != null)
			{
				//average out the move direction
				moveDirection /= penetratingCount;
				moveDirection.Normalize ();

#if UNITY_EDITOR
				Debug.DrawRay (transform.position + depenetrationOffset, moveDirection * greatestDistance, new Color (Random.value, Random.value, Random.value), 7f);
#endif

				//we averaged out the movement direction and then apply the greatest movement distance to minimize the number of iterations we need to make this object properly depenetrate
				depenetrationOffset += moveDirection * greatestDistance;

				return true;
			}
			return false;
		}

		public void ToggleColliders (bool on)
		{
			for (int i = 0; i < _Colliders.Length; i++)
				_Colliders[i].enabled = on;
		}

		protected void SetTravellingToHand (bool val)
		{
			//Debug.Log ("Travelling to hand " + val);
			_travellingToHand = val;
			ToggleColliders (!val);
		}

		protected bool HasAttachedInteractor ()
		{
			return _attachedInteractor != null;	
		}

		public override void SetCanBeSelected (bool val)
		{
			base.SetCanBeSelected (val);

			if (val)
			{
				if (setKinematicIfCantBeSelected)
				{
					SetKinematic (false);
				}
			}
			else
			{
				if (setKinematicIfCantBeSelected)
				{
					SetKinematic (true);
				}
			}
			
		}

		public void SetKinematic (bool val)
		{
			_Rigidbody.isKinematic = val;

			if (attachedRigidbodies.Length == 0)
				return;

			for (int i = 0; i < attachedRigidbodies.Length; i++)
				attachedRigidbodies[i].isKinematic = val;
		}

		#region RESET STUFF
		protected virtual void OnCollisionEnter (Collision other)
		{
			if (!resetOptions.resetObjectWhenDropped)
				return;

			//don't worry about this stuff while the object is being held
			if (HasInteractor ())
				return;

			if (_waitForSleepRoutine != null)
				StopCoroutine (_waitForSleepRoutine);

			_waitForSleepRoutine = WaitForSleepRoutine (other);
			StartCoroutine (_waitForSleepRoutine);
		}

		IEnumerator WaitForSleepRoutine (Collision other)
		{
			while (!_Rigidbody.IsSleeping ())
				yield return null;


			if ((resetOptions.onlyResetIfOnFloor && Mathf.Abs (TBCameraRig.instance.GetCenter ().position.y - _Rigidbody.position.y) > 0.6f) || 
				!resetOptions.onlyResetIfOnFloor || 
				other.gameObject.GetComponent<ResetTrigger>() != null && resetOptions.onlyResetIfOnFloor == false ||
				other.gameObject.GetComponent<ResetTrigger>() != null && resetOptions.onlyResetIfOnFloor == true && other.gameObject.GetComponent<ResetTrigger>().IsFloor == true)
			{
				_resetTimerRoutine = ResetTimerRoutine ();
				StartCoroutine (_resetTimerRoutine);
			}
		}

		IEnumerator ResetTimerRoutine ()
		{
			yield return new WaitForSeconds (resetOptions.resetDelay);
			Reset();
		}

		public virtual void Reset()
		{
			ResetTransform();
		}

		protected void ResetTransform ()
		{
			Vector3 offsetVector = Vector3.zero;
			Depenetrate (ref offsetVector, resetOptions.GetStartingPosition ());

			_Rigidbody.velocity = Vector3.zero;
			_Rigidbody.angularVelocity = Vector3.zero;
			_Rigidbody.rotation = resetOptions.GetStartingRotation ();
			_Rigidbody.position = resetOptions.GetStartingPosition () + offsetVector;
		}
		#endregion

		#region EVENT LISTENERS
		protected void SubscribeToFixedUpdate ()
		{
			if (!_subscribedToFixedUpdate)
			{
				_subscribedToFixedUpdate = true;
				TBCore.OnFixedUpdate += OnFixedUpdate;
			}
		}

		protected void UnsubscribeFromFixedUpdate ()
		{
			if (_subscribedToFixedUpdate)
			{
				_subscribedToFixedUpdate = false;
				TBCore.OnFixedUpdate -= OnFixedUpdate;
			}
		}
		#endregion

		#region INLINE CLASSES
		[System.Serializable]
		public class PhysicsOptions
		{
			public bool disableKinematicOnGrab = true;
			public bool enableKinematicOnRelease = false;
			public bool enableGravityOnRelease = true;
			[Tooltip ("If true, the object will collide with everything. If set to false, it will only collide with dynamic objects.")]
			public bool collideWithStaticGeoWhileHeld = true;
		}

		[System.Serializable]
		public class ResetOptions
		{
			public bool resetObjectWhenDropped = false;
			public bool onlyResetIfOnFloor = true;
			public float resetDelay = 5f;

			private Vector3 _startingPos = Vector3.zero;
			private Quaternion _startingRot = Quaternion.identity;

			public void Init (Vector3 pos, Quaternion rot)
			{
				_startingPos = pos;
				_startingRot = rot;
			}

			public Vector3 GetStartingPosition ()
			{
				return _startingPos;
			}

			public Quaternion GetStartingRotation ()
			{
				return _startingRot;
			}
		}
		#endregion
	}
}
