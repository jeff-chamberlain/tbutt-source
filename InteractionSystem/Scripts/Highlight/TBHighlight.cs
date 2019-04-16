using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt.InteractionSystem
{
	public class TBHighlight : MonoBehaviour
	{
		private TBInteractiveObjectBase _InteractiveObject;
		private TBHighlightSettingsOverride _HighlightOverride;
		private Renderer[] _Renderers;
		private Material[][] _DefaultMats;
		private Material[][] _HighlightMats;

		private bool _highlighted = false;
		private bool _subscribedToHoverEvents;
		private bool _canBeHighlighted = true;

		void Awake ()
		{
			_InteractiveObject = GetComponent<TBInteractiveObjectBase> ();

			InitHighlights ();
		}

		void OnEnable ()
		{
			if (_InteractiveObject == null)
				return;

			SubscribeToEvents ();
		}

		void OnDisable ()
		{
			if (_InteractiveObject == null)
				return;

			UnsubscribeFromEvents ();
		}

		void OnHoverStartByInteractor (TBInteractiveObjectBase obj, TBInteractorBase interactor)
		{
			if (_InteractiveObject.CanBeSelected ())
				Highlight (true);
		}

		void OnHoverExitByInteractor (TBInteractiveObjectBase obj, TBInteractorBase interactor)
		{
			Highlight (false);
		}

		void OnSelectByInteractor (TBInteractiveObjectBase obj, TBInteractorBase interactor)
		{
			Highlight (false);
		}

		void SubscribeToEvents ()
		{
			if (!_subscribedToHoverEvents)
			{
				_InteractiveObject.OnHoverStartByInteractor += OnHoverStartByInteractor;
				_InteractiveObject.OnHoverExitByInteractor += OnHoverExitByInteractor;
				_InteractiveObject.OnSelectByInteractor += OnSelectByInteractor;

				_subscribedToHoverEvents = true;
			}
		}

		void UnsubscribeFromEvents ()
		{
			if (_subscribedToHoverEvents)
			{
				_InteractiveObject.OnHoverStartByInteractor -= OnHoverStartByInteractor;
				_InteractiveObject.OnHoverExitByInteractor -= OnHoverExitByInteractor;
				_InteractiveObject.OnSelectByInteractor -= OnSelectByInteractor;

				_subscribedToHoverEvents = false;
			}
		}

		void InitHighlights ()
		{
			_HighlightOverride = GetComponent<TBHighlightSettingsOverride> ();
			_Renderers = GetComponentsInChildren<Renderer> ();

			List<Renderer> temp = new List<Renderer> ();

			for (int i = 0; i < _Renderers.Length; i++)
			{
				if (_Renderers[i].GetType () != typeof (ParticleSystemRenderer))
				{
					if (_Renderers[i].GetComponent<TBHighlightExclude> () == null)
						temp.Add (_Renderers[i]);
				}
			}

			_Renderers = temp.ToArray ();

			_DefaultMats = new Material[_Renderers.Length][];
			_HighlightMats = new Material[_Renderers.Length][];

			for (int i = 0; i < _Renderers.Length; i++)
			{
				_DefaultMats[i] = new Material[_Renderers[i].sharedMaterials.Length];
				_HighlightMats[i] = new Material[_Renderers[i].sharedMaterials.Length];
				for (int j = 0; j < _Renderers[i].sharedMaterials.Length; j++)
				{
					_DefaultMats[i][j] = _Renderers[i].sharedMaterials[j];
					_HighlightMats[i][j] = TBHighlightManager.instance.RegisterMaterial (_Renderers[i].sharedMaterials[j], _HighlightOverride);
				}
			}
		}

		void Highlight (bool on)
		{
			if (!_canBeHighlighted)
				return;

			if (on && !_highlighted)
			{
				_highlighted = true;
				for (int i = 0; i < _Renderers.Length; i++)
				{
					_Renderers[i].sharedMaterials = _HighlightMats[i];
				}
			}
			else if (!on && _highlighted)
			{
				_highlighted = false;
				for (int i = 0; i < _Renderers.Length; i++)
				{
					_Renderers[i].sharedMaterials = _DefaultMats[i];
				}
			}
		}

		public void SetCanBeHighlighted (bool val)
		{
			_canBeHighlighted = val;
		}
	}
}
