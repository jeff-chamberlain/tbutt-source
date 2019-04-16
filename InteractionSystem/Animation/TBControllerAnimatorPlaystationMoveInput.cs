using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt.Input;
using System;

namespace TButt.InteractionSystem
{
#if !TB_PSVR
	public class TBControllerAnimatorPlaystationMoveInput : TBControllerAnimatorBase<TBInput.Button>
	{
		protected override void DefineButtons ()
		{
			throw new NotImplementedException ();
		}

		protected override void BuildButtonDictionary ()
		{
			throw new NotImplementedException ();
		}
#else

	public class TBControllerAnimatorPlaystationMoveInput : TBControllerAnimatorBase<TBPSVRInput.Button>
	{
		private int _triggerHash = Animator.StringToHash ("trigger");

		protected override void Awake ()
		{
			base.Awake ();

			_Animator.SetBool ("PlaystationMove", true);
		}

		protected override void UpdateButtons ()
		{
			if (_ignoreButtonInput)
				return;

			float lerpSpeed = Time.deltaTime * _layerWeightLerpSpeed;

			float fingersVal = TBPSVRInput.instance.ResolveAxis1D (_buttons[0], _Interactor.controller);
			SetAnimatorFloat ((int)_buttons[0], fingersVal);

			float thumbVal = TBPSVRInput.instance.ResolveButton (_buttons[1], _Interactor.controller) ? 1f : 0f;
			SetAnimatorFloat ((int)_buttons[1], thumbVal);

			//the index finger is animated with a button, but instead depends on what multiple buttons are doing
			//we have to hash a separate value for "trigger" because it's the animator value we want, but we don't want it to match up with any kind of button press
			float indexVal = 0f;
			if (thumbVal > 0f)
			{
				indexVal = fingersVal;
				SetAnimatorFloatWithHash (_triggerHash, fingersVal);
			}
			else
			{
				SetAnimatorFloatWithHash (_triggerHash, indexVal, 0.1f);
			}

			SetLayerWeight (_IndexLayer, Mathf.Lerp (_layerWeights[_IndexLayer], (indexVal + fingersVal + thumbVal) > 0f ? 1f : 0f, lerpSpeed));
			SetLayerWeight (_FingersLayer, _layerWeights[_IndexLayer]);
			SetLayerWeight (_ThumbLayer, _layerWeights[_IndexLayer]);
		}

		protected override void DefineButtons ()
		{
			_buttons = new TBPSVRInput.Button[]
			{
				TBPSVRInput.Button.MoveTrigger,
				TBPSVRInput.Button.MoveButton
			};
		}

		protected override void BuildButtonDictionary ()
		{
			for (int i = 0; i < _buttons.Length; i++)
				_buttonHashes.Add ((int)_buttons[i], _animatorHashes[i]);
		}

		protected override void DeclareAnimatorParams ()
		{
			_animatorParams = new string[]
			{
				"grip",
				"button1"
			};
		}
#endif
	}

}
