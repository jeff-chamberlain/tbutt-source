using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;
using DG.Tweening;

namespace TButt.InteractionSystem
{
	public abstract class TBControllerAnimatorBase<T> : MonoBehaviour
	{
		protected Animator _Animator;
		protected TBInteractorHand _Interactor;

		protected T[] _buttons;
		protected string[] _animatorParams;
		protected int[] _animatorHashes;
		protected Dictionary<int, int> _buttonHashes = new Dictionary<int, int> ();

		//set this value to true in awake if you only need button events and no update loop
		protected bool _useButtonEventsOnly = false;
		protected bool _ignoreButtonInput = false;

		//extra somewhat-commonly used animator hashes
		protected int _joystickX = Animator.StringToHash ("joystickX");
		protected int _joystickY = Animator.StringToHash ("joystickY");
		protected int _hoveringOverObject = Animator.StringToHash ("hoveringOverObject");

		//layers
		protected int _BaseLayer;
		protected int _FingersLayer;
		protected int _IndexLayer;
		protected int _ThumbLayer;
		protected float[] _layerWeights;
		protected int _layerCount = 4;
		protected float _layerWeightLerpSpeed = 30f;

		protected virtual void Awake ()
		{
			_Animator = GetComponent<Animator> ();
			_Interactor = GetComponentInParent<TBInteractorHand> ();

			_layerWeights = new float[_layerCount];

			DefineButtons ();
			DeclareAnimatorParams ();

			InitHashes ();

			BuildButtonDictionary ();
		}

		protected virtual void OnEnable ()
		{
			_Interactor.Events.OnInteractiveObjectHoverEnter += OnHoverEnter;
			_Interactor.Events.OnInteractiveObjectHoverExit += OnHoverExit;

			if (!_useButtonEventsOnly)
				TBCore.OnUpdate += UpdateButtons;
		}

		protected virtual void OnDisable ()
		{
			_Interactor.Events.OnInteractiveObjectHoverEnter -= OnHoverEnter;
			_Interactor.Events.OnInteractiveObjectHoverExit -= OnHoverExit;

			if (!_useButtonEventsOnly)
				TBCore.OnUpdate -= UpdateButtons;
		}

		protected virtual bool OnHoverEnter (TBInteractiveObjectBase obj)
		{
			_Animator.SetBool (_hoveringOverObject, true);
			return true;
		}

		protected virtual bool OnHoverExit (TBInteractiveObjectBase obj)
		{
			_Animator.SetBool (_hoveringOverObject, false);
			return true;
		}

		protected abstract void DefineButtons ();
		protected virtual void UpdateButtons () { }
		
		protected virtual void DeclareAnimatorParams ()
		{
			_animatorParams = new string[]
			{
				"trigger",
				"grip",
				"button1",
				"button2",
				"button3",
				"button4",
				"joystick"
			};
		}

		protected virtual void InitHashes ()
		{
			_animatorHashes = new int[_animatorParams.Length];

			for (int i = 0; i < _animatorParams.Length; i++)
			{
				_animatorHashes[i] = Animator.StringToHash (_animatorParams[i]);
			}

			_BaseLayer = _Animator.GetLayerIndex ("Base Layer");
			_FingersLayer = _Animator.GetLayerIndex ("Fingers Layer");
			_IndexLayer = _Animator.GetLayerIndex ("Index Layer");
			_ThumbLayer = _Animator.GetLayerIndex ("Thumb Layer");
		}

		protected abstract void BuildButtonDictionary ();

		protected void SetAnimatorFloat (int buttonId, float value)
		{
			_Animator.SetFloat (_buttonHashes[buttonId], value);
		}

		protected void SetAnimatorFloat (int buttonId, float value, float dampTime)
		{
			_Animator.SetFloat (_buttonHashes[buttonId], value, dampTime, Time.deltaTime);
		}

		protected void SetAnimatorFloatWithHash (int animatorHash, float value)
		{
			_Animator.SetFloat (animatorHash, value);
		}

		protected void SetAnimatorFloatWithHash (int animatorHash, float value, float dampTime)
		{
			_Animator.SetFloat (animatorHash, value, dampTime, Time.deltaTime);
		}

		protected void SetLayerWeight (int layer, float destinationWeight)
		{
			_Animator.SetLayerWeight (layer, destinationWeight);
			_layerWeights[layer] = destinationWeight;
		}

		protected virtual void SetAllLayerWeights (float destinationWeight)
		{
			SetLayerWeight (_FingersLayer, destinationWeight);
			SetLayerWeight (_IndexLayer, destinationWeight);
			SetLayerWeight (_ThumbLayer, destinationWeight);
		}

		protected void SetIgnoreButtonInput (bool val)
		{
			_ignoreButtonInput = val;

			if (val)
			{
				SetAllLayerWeights (0f);
			}
		}
	}
}
