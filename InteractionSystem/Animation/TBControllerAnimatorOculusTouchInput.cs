using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt.Input;

namespace TButt.InteractionSystem
{
#if !TB_OCULUS
	public class TBControllerAnimatorOculusTouchInput : TBControllerAnimatorBase<TBInput.Button>
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
	public class TBControllerAnimatorOculusTouchInput : TBControllerAnimatorBase<OVRInput.RawButton>
	{
		private bool _leftHand;

		protected override void Awake ()
		{
			base.Awake ();

			//we set a bool in the animator at runtime to determine what set of animations we're going to use for the hand
			_Animator.SetBool ("OculusTouch", true);
		}

		protected override void UpdateButtons ()
		{
			if (_ignoreButtonInput)
				return;

			float lerpSpeed = Time.deltaTime * _layerWeightLerpSpeed;
			//trigger
			bool triggerTouch = TBInputOculus.instance.ResolveTouch (_buttons[0], _Interactor.controller);

			float indexVal = (TBInputOculus.instance.ResolveAxis1D (_buttons[0], _Interactor.controller) + (triggerTouch ? 1f : 0f)) * 0.5f;
			//we average the touch and the trigger pull value to get the appropriate animation
			SetAnimatorFloat ((int)_buttons[0], indexVal, 0.1f);

			//grip
			float fingersVal = TBInputOculus.instance.ResolveAxis1D (_buttons[1], _Interactor.controller);
			SetAnimatorFloat ((int)_buttons[1], fingersVal);

			//face buttons
			float thumbVal = 0f;
			for (int i = 2; i < _buttons.Length; i++)
			{
				float thisVal = TBInputOculus.instance.ResolveTouch (_buttons[i], _Interactor.controller) ? 1f : 0f;
				SetAnimatorFloat ((int)_buttons[i], thisVal);

				thumbVal += thisVal;
			}

			//joystick input
			Vector2 joystickInput = TBInputOculus.instance.ResolveAxis2D (_buttons[4], _Interactor.controller);
			_Animator.SetFloat (_joystickX, joystickInput.x);
			_Animator.SetFloat (_joystickY, joystickInput.y);

			//we want to update the layer weights in unison so we don't get weird partial animations
			SetLayerWeight (_IndexLayer, Mathf.Lerp (_layerWeights[_IndexLayer], (indexVal + fingersVal + thumbVal) > 0f ? 1f : 0f, lerpSpeed));
			SetLayerWeight (_FingersLayer, _layerWeights[_IndexLayer]);
			SetLayerWeight (_ThumbLayer, _layerWeights[_IndexLayer]);
		}

		protected override void DefineButtons ()
		{
			_leftHand = _Interactor.controller == TBInput.Controller.LHandController;

			_buttons = new OVRInput.RawButton[]
			{
				_leftHand ? OVRInput.RawButton.LIndexTrigger : OVRInput.RawButton.RIndexTrigger,
				_leftHand ? OVRInput.RawButton.LHandTrigger : OVRInput.RawButton.RHandTrigger,
				_leftHand ? OVRInput.RawButton.X : OVRInput.RawButton.A,
				_leftHand ? OVRInput.RawButton.Y : OVRInput.RawButton.B,
				_leftHand ? OVRInput.RawButton.LThumbstick : OVRInput.RawButton.RThumbstick
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
				"trigger",
				"grip",
				"button1",
				"button2",
				"joystick"
			};
		}
#endif
	}
}
