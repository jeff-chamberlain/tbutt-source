using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt.InteractionSystem
{
    [RequireComponent(typeof(LineRenderer))]
    public class TBLaserPointer : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private TBInteractorReticleCaster _reticleCaster;

        [Tooltip("Between 0 and 1. (for 0 to 100% of the reticle distance)")]
		[Range (0f, 1f)]
        public float targetDistance;
        
        [Tooltip("In meters.")]
        public float maxDistance;

        [Tooltip("In meters.")]
        public float minDistance;

        [Tooltip("Distance from the casting point that the laser starts.")]
        public float startDistance;

        public Color startColor;
        public Color endColor;

        public bool applyEasing;

        private float _easedDistance;
        private Vector3[] _linePositions;

		private bool _subscribedToPauseEvent = false;

        private void Awake()
        {
            _reticleCaster = GetComponent<TBInteractorReticleCaster>();
            _lineRenderer = GetComponent<LineRenderer>();

			_lineRenderer.useWorldSpace = true;
            _lineRenderer.startColor = startColor;
            _lineRenderer.endColor = endColor;

            if(TBInteractorManager.instance.GetInteractor(TBInteractorType.Hand, _reticleCaster.controller) != null)
			    _lineRenderer.enabled = false;

            _linePositions = new Vector3[2];
        }

        private void OnEnable()
        {
            TBCore.OnUpdate += OnUpdate;
			SubscribeToInteractorPause ();
        }

        private void OnDisable()
        {
            TBCore.OnUpdate -= OnUpdate;
        }

        void OnUpdate()
        {
            float distance = _reticleCaster.GetLastDistance();
            distance *= targetDistance;

            if (distance > maxDistance)
                distance = maxDistance;
            else if (distance < minDistance)
                distance = minDistance;

            if (applyEasing)
                _easedDistance = Mathf.Lerp(_easedDistance, distance, Time.deltaTime * 10f);
            else
                _easedDistance = distance;

            _linePositions[0] = _reticleCaster.GetPointerSourceTransform().position + _reticleCaster.GetPointerSourceTransform().forward * startDistance;
            _linePositions[1] = _reticleCaster.GetPointerSourceTransform().position + _reticleCaster.GetPointerSourceTransform().forward * _easedDistance;

            _lineRenderer.SetPosition(0, _reticleCaster.GetPointerSourceTransform().position + _reticleCaster.GetPointerSourceTransform().forward * startDistance);
            _lineRenderer.SetPosition(1, _reticleCaster.GetPointerSourceTransform().position + _reticleCaster.GetPointerSourceTransform().forward * _easedDistance);

           // _lineRenderer.SetPositions(_linePositions); // this generates garbage because WTF Unity??
        }

		bool InteractorPaused (bool paused)
		{
			_lineRenderer.enabled = !paused;
			return true;
		}

		void SubscribeToInteractorPause ()
		{
			if (_subscribedToPauseEvent)
				return;

			_subscribedToPauseEvent = true;
			_reticleCaster.Events.OnInteractorPaused += InteractorPaused;
		}
    }
}