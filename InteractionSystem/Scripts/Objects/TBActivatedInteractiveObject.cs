using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TButt.InteractionSystem
{
	public class TBActivatedInteractiveObject : TBInteractiveObjectBase
	{
		[System.Serializable]
		public class ActivationEvent : UnityEvent { }
		public ActivationEvent OnActivate;
		public ActivationEvent OnDeactivate;

		protected bool _activated = false;

		protected override void Awake ()
		{
			base.Awake ();

			_checkHoverEventsWhileInteracted = true;

			if (TBHighlightManager.instance != null)
				gameObject.AddComponent<TBHighlight> ();
		}

		protected override void OnHandSelect (TBInteractorBase interactor)
		{
			base.OnHandSelect (interactor);

			Activate ();
		}

		protected override void OnHandDeselect (TBInteractorBase interactor)
		{
			base.OnHandDeselect (interactor);

			Deactivate ();
		}

		protected override void OnHandHoverEnter (TBInteractorBase interactor)
		{
			base.OnHandHoverEnter (interactor);

			if (TBInput.GetButton (interactor.selectButton, interactor.controller))
				OnSelect (interactor);
		}

		protected override void OnHandHoverExit (TBInteractorBase interactor)
		{
			base.OnHandHoverExit (interactor);

			if (TBInput.GetButton (interactor.selectButton, interactor.controller))
				OnDeselect (interactor);
		}

		protected virtual void Activate ()
		{
			if (OnActivate != null && !_activated)
			{
				OnActivate.Invoke ();
				_activated = true;
			}
		}

		protected virtual void Deactivate ()
		{
			if (OnDeactivate != null && _activated)
			{
				OnDeactivate.Invoke ();
				_activated = false;
			}
		}
	}
}
