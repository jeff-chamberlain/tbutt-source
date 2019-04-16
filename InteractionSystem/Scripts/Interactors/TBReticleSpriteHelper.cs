using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TButt.InteractionSystem
{
    public class TBReticleSpriteHelper : MonoBehaviour
    {
        public TBInteractorReticleCaster reticleCaster;

        public Sprite uiSprite;
        public Sprite normalSprite;

        public Color selectingColor;
        public Color normalColor;
        public Color interactiveHoverColor;

        protected Image _image;

        private void Awake()
        {
            if (reticleCaster == null)
                reticleCaster = GetComponentInParent<TBInteractorReticleCaster>();

            _image = GetComponent<Image>();
        }

        protected void OnEnable()
        {
            reticleCaster.Events.OnSelectDown += OnSelectDown;
            reticleCaster.Events.OnSelectUp += OnSelectUp;
            reticleCaster.Events.OnUIHoverEnter += OnUIHoverEnter;
            reticleCaster.Events.OnUIHoverExit += OnUIHoverExit;
            reticleCaster.Events.OnUISelect += OnUISelect;
            reticleCaster.Events.OnUIDeselect += OnUIDeselect;
            reticleCaster.Events.OnInteractiveObjectSelect += OnInteractiveObjectSelect;
            reticleCaster.Events.OnInteractiveObjectDeselect += OnInteractiveObjectHoverDeselect;
            reticleCaster.Events.OnInteractiveObjectHoverEnter += OnInteractiveObjectHoverEnter;
            reticleCaster.Events.OnInteractiveObjectHoverExit += OnInteractiveObjectHoverExit;
        }

        protected void OnDisable()
        {
            reticleCaster.Events.OnSelectDown -= OnSelectDown;
            reticleCaster.Events.OnSelectUp -= OnSelectUp;
            reticleCaster.Events.OnUIHoverEnter -= OnUIHoverEnter;
            reticleCaster.Events.OnUIHoverExit -= OnUIHoverExit;
            reticleCaster.Events.OnUISelect -= OnUISelect;
            reticleCaster.Events.OnUIDeselect -= OnUIDeselect;
            reticleCaster.Events.OnInteractiveObjectSelect -= OnInteractiveObjectSelect;
            reticleCaster.Events.OnInteractiveObjectDeselect -= OnInteractiveObjectHoverDeselect;
            reticleCaster.Events.OnInteractiveObjectHoverEnter -= OnInteractiveObjectHoverEnter;
            reticleCaster.Events.OnInteractiveObjectHoverExit -= OnInteractiveObjectHoverExit;
        }

        protected bool OnSelectDown()
        {
            if (reticleCaster.HasHoveredUIObject())
                return false;
            else
                transform.DOPunchScale(transform.localScale * 1.05f, 0.15f);
            return false;
        }

        protected bool OnSelectUp()
        {
            return false;
        }

        protected bool OnUIHoverEnter(GameObject go)
        {
            if (!reticleCaster.HasSelectedInteractiveObject())
                _image.sprite = uiSprite;
            return false;
        }

        protected bool OnUIHoverExit(GameObject go)
        {
            _image.sprite = normalSprite;
            return false;
        }

        protected bool OnUISelect(GameObject go)
        {
            _image.color = selectingColor;
            transform.DOPunchScale(transform.localScale * 0.95f, 0.1f);
            return false;
        }

        protected bool OnUIDeselect(GameObject go)
        {
            _image.color = normalColor;
            return false;
        }

        protected bool OnInteractiveObjectHoverEnter(TBInteractiveObjectBase obj)
        {
            _image.color = interactiveHoverColor;
            return false;
        }

        protected bool OnInteractiveObjectHoverExit(TBInteractiveObjectBase obj)
        {
            _image.color = normalColor;
            return false;
        }

        protected bool OnInteractiveObjectSelect(TBInteractiveObjectBase obj)
        {
            _image.color = selectingColor;
            return false;
        }

        protected bool OnInteractiveObjectHoverDeselect(TBInteractiveObjectBase obj)
        {
            if (reticleCaster.HasHoveredInteractiveObject())
                _image.color = interactiveHoverColor;
            else
                _image.color = normalColor;
            return false;
        }
    }
}