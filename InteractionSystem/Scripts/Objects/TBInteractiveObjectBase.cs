using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;

namespace TButt.InteractionSystem
{
	public class TBInteractiveObjectBase : MonoBehaviour
	{
		[HideInInspector]
		public new Transform transform
		{
			get
			{
				if (_transform == null)
				{
					_transform = GetComponent<Transform>();
				}

				return _transform;
			}
			set
			{
				_transform = value;
			}
		}

		[HideInInspector]
		public bool overrideInteractorVisualsOnSelect = false;
		[HideInInspector]
		public bool hideInteractorVisualsOnSelect = true;

		public bool startCanBeSelected = true;

		protected Collider[] _Colliders;

		//Usually, hands stop checking for nearby objects while one is being held. This allows you to override that functionality on a per-object basis.
		//override in subclasses
		protected bool _checkHoverEventsWhileInteracted = false;

		protected Transform _transform;
		protected bool _canBeSelected = true;
		protected TBInteractorBase _Interactor;

		public delegate void InteractiveObjectEvent(TBInteractiveObjectBase obj, TBInteractorBase interactor);

		public InteractiveObjectEvent OnSelectByInteractor;
		public InteractiveObjectEvent OnDeselectByInteractor;
		public InteractiveObjectEvent OnHoverStartByInteractor;
		public InteractiveObjectEvent OnHoverExitByInteractor;

		private bool _subscribedToHoverEvents = false;

		protected virtual void Awake()
		{
			transform = GetComponent<Transform>();
			_Colliders = GetComponentsInChildren<Collider>();
			SetCanBeSelected(startCanBeSelected);
		}

		protected virtual void OnEnable()
		{
			SubscribeToHoverEvents();
		}

		protected virtual void OnDisable()
		{
			UnsubscribeFromHoverEvents();
		}

		protected void SubscribeToHoverEvents()
		{
			if (!_subscribedToHoverEvents)
			{
				_subscribedToHoverEvents = true;
				TBInteractorManager.Events.OnHoverEnter += OnHoverEnter;
				TBInteractorManager.Events.OnHoverExit += OnHoverExit;
				TBInteractorManager.Events.OnHoverEvaluate += OnHoverEvaluate;
			}
		}

		protected void UnsubscribeFromHoverEvents()
		{
			if (_subscribedToHoverEvents)
			{
				_subscribedToHoverEvents = false;
				TBInteractorManager.Events.OnHoverEnter -= OnHoverEnter;
				TBInteractorManager.Events.OnHoverExit -= OnHoverExit;
				TBInteractorManager.Events.OnHoverEvaluate -= OnHoverEvaluate;
				if (_Interactor != null)
				{
					_Interactor.UnsetHoveredInteractiveObject(this);
					_Interactor.UnsetSelectedInteractiveObject(this);
				}
			}
		}

		public virtual bool OnSelect(TBInteractorBase interactor)
		{
			if (!CanBeSelected())
			{
				return false;
			}
			SetInteractor(interactor);
			interactor.SetSelectedInteractiveObject(this);
			if (interactor.GetInteractorType() == TBInteractorType.Hand)
			{
				OnHandSelect(interactor);
			}
			else
			{
				OnReticleSelect(interactor);
			}
			if (OnSelectByInteractor != null)
			{
				OnSelectByInteractor(this, interactor);
			}
			return true;
		}

		public virtual bool OnDeselect(TBInteractorBase interactor)
		{
			interactor.UnsetSelectedInteractiveObject(this);
			if (interactor.GetInteractorType() == TBInteractorType.Hand)
				OnHandDeselect(interactor);
			else
				OnReticleDeselect(interactor);

			if (OnDeselectByInteractor != null)
				OnDeselectByInteractor(this, interactor);

			SetInteractor(null);
			return true;
		}

		public virtual bool OnHoverEnter(Collider c, TBInteractorBase interactor)
		{
			if (!IsColliderOnThisObject(c))
				return false;
			//if (gameObject.GetInstanceID () != go.GetInstanceID ())
			//return false;

			interactor.SetHoveredInteractiveObject(this);
			if (interactor.GetInteractorType() == TBInteractorType.Hand)
				OnHandHoverEnter(interactor);
			else
				OnReticleHoverEnter(interactor);

			if (OnHoverStartByInteractor != null)
				OnHoverStartByInteractor(this, interactor);
			return true;
		}

		public virtual bool OnHoverExit(Collider c, TBInteractorBase interactor)
		{
			if (!IsColliderOnThisObject(c))
				return false;
			//if (gameObject.GetInstanceID () != go.GetInstanceID ())
			//    return false;

			interactor.UnsetHoveredInteractiveObject(this);
			if (interactor.GetInteractorType() == TBInteractorType.Hand)
				OnHandHoverExit(interactor);
			else
				OnReticleHoverExit(interactor);

			if (OnHoverExitByInteractor != null)
				OnHoverExitByInteractor(this, interactor);
			return true;
		}

		public virtual bool OnHoverEvaluate(Collider c, TBInteractorBase interactor)
		{
			if (IsColliderOnThisObject(c))
				interactor.EvaluateForHover(this, c);

			return true;
		}

		bool IsColliderOnThisObject(Collider c)
		{
			for (int i = 0; i < _Colliders.Length; i++)
			{
				if (_Colliders[i].GetInstanceID() == c.GetInstanceID())
					return true;
			}

			return false;
		}

		protected virtual void OnHandSelect(TBInteractorBase interactor)
		{

		}

		protected virtual void OnHandDeselect(TBInteractorBase interactor)
		{

		}

		protected virtual void OnReticleSelect(TBInteractorBase interactor)
		{

		}

		protected virtual void OnReticleDeselect(TBInteractorBase interactor)
		{

		}

		protected virtual void OnHandHoverEnter(TBInteractorBase interactor)
		{

		}

		protected virtual void OnHandHoverExit(TBInteractorBase interactor)
		{

		}

		protected virtual void OnReticleHoverEnter(TBInteractorBase interactor)
		{

		}

		protected virtual void OnReticleHoverExit(TBInteractorBase interactor)
		{

		}

		public virtual void SetCanBeSelected(bool val)
		{
			_canBeSelected = val;
		}

		public virtual bool CanBeSelected()
		{
			return _canBeSelected;
		}

		protected virtual void SetInteractor(TBInteractorBase interactor)
		{
			_Interactor = interactor;
		}

		public TBInteractorBase GetInteractor()
		{
			return _Interactor;
		}

		public bool HasInteractor()
		{
			return _Interactor != null;
		}

		public bool CheckHoverEventsWhileInteracted()
		{
			return _checkHoverEventsWhileInteracted;
		}
	}
}