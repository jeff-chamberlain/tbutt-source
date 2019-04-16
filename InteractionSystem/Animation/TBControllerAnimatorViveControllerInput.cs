using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt.Input;
#if TB_STEAM_VR
using Valve.VR;
#endif

namespace TButt.InteractionSystem
{
#if !TB_STEAM_VR
	public class TBControllerAnimatorViveControllerInput : TBControllerAnimatorBase<TBInput.Button>
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
	public class TBControllerAnimatorViveControllerInput : TBControllerAnimatorBase<EVRButtonId>
	{
		protected override void Awake ()
		{
			base.Awake ();

			//we set a bool in the animator at runtime to determine what set of animations we're going to use for the hand
			_Animator.SetBool ("ViveController", true);
		}

		protected override void UpdateButtons ()
		{
			//trigger
			SetAnimatorFloat ((int)_buttons[0], TBSteamVRInput.instance.ResolveAxis1D (_buttons[0], _Interactor.controller));

			//grip
			SetAnimatorFloat ((int)_buttons[1], TBSteamVRInput.instance.ResolveButton (_buttons[1], _Interactor.controller) ? 1f : 0f, 0.1f);

			//touchpad
			SetAnimatorFloat ((int)_buttons[2], TBSteamVRInput.instance.ResolveTouch (_buttons[2], _Interactor.controller) ? 1f : 0f);

			Vector2 touchpadInput = TBSteamVRInput.instance.ResolveAxis2D (_buttons[2], _Interactor.controller);
			_Animator.SetFloat (_joystickX, touchpadInput.x);
			_Animator.SetFloat (_joystickY, touchpadInput.y);
		}

		protected override void DefineButtons ()
		{
			_buttons = new EVRButtonId[]
			{
				EVRButtonId.k_EButton_SteamVR_Trigger,
				EVRButtonId.k_EButton_Grip,
				EVRButtonId.k_EButton_SteamVR_Touchpad
			};
		}

		protected override void DeclareAnimatorParams ()
		{
			_animatorParams = new string[]
			{
				"trigger",
				"grip",
				"joystick"
			};
		}

		protected override void BuildButtonDictionary ()
		{
			for (int i = 0; i < _buttons.Length; i++)
				_buttonHashes.Add ((int)_buttons[i], _animatorHashes[i]);
		}
#endif
	}
}