using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt.InteractionSystem
{
	public class TBControllerAnimatorTBInput : TBControllerAnimatorBase<TBInput.Button>
	{
		

		protected override void UpdateButtons ()
		{
			if (TBInput.SupportsTouch (_buttons[0]))
			{
				bool triggerTouch = TBInput.GetTouch (_buttons[0], _Interactor.controller);

				//we average the touch and the trigger pull value to get the appropriate animation
				SetAnimatorFloat ((int)_buttons[0], (TBInput.GetAxis1D (_buttons[0], _Interactor.controller) + (triggerTouch ? 1f : 0f)) * 0.5f, 0.1f);
			}
			else
			{
				SetAnimatorFloat ((int)_buttons[0], TBInput.GetAxis1D (_buttons[0], _Interactor.controller), 0.1f);
			}

			SetAnimatorFloat ((int)_buttons[1], TBInput.GetAxis1D (_buttons[1], _Interactor.controller));
			
			for (int i = 2; i < _buttons.Length; i++)
			{
				if (TBInput.SupportsTouch (_buttons[i]))
				{
					SetAnimatorFloat ((int)_buttons[i], TBInput.GetTouch (_buttons[i], _Interactor.controller) ? 1f : 0f);
				}
				else
				{
					SetAnimatorFloat ((int)_buttons[i], TBInput.GetButton (_buttons[i], _Interactor.controller) ? 1f : 0f);
				}
			}
		}

		protected override void DefineButtons ()
		{
			_buttons = new TBInput.Button[]
			{
				TBInput.Button.PrimaryTrigger,
				TBInput.Button.SecondaryTrigger,
				TBInput.Button.Action1,
				TBInput.Button.Action2,
				TBInput.Button.Action3,
				TBInput.Button.Action4,
				TBInput.Button.Joystick
			};
		}

		protected override void BuildButtonDictionary ()
		{
			for (int i = 0; i < _buttons.Length; i++)
				_buttonHashes.Add ((int)_buttons[i], _animatorHashes[i]);
		}
	}
}
