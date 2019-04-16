using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;

namespace TButt.InteractionSystem
{
    public class TBUIElement : TBInteractiveObjectBase
    {
        protected override void OnHandHoverEnter(TBInteractorBase interactor)
        {
            base.OnHandHoverEnter(interactor);

            if (TBInputModule.instance.allowHandInteractions)
                TBInputModule.instance.OnHoverEnterHand(gameObject);
        }

        protected override void OnHandHoverExit(TBInteractorBase interactor)
        {
            base.OnHandHoverExit(interactor);

            if (TBInputModule.instance.allowHandInteractions)
                TBInputModule.instance.OnHoverExitHand(gameObject);
        }

        protected override void OnHandSelect(TBInteractorBase interactor)
        {
            if (TBInputModule.instance.allowHandInteractions)
                TBInputModule.instance.OnSelectHand(gameObject);
        }
    }
}