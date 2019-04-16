using System.Collections;
using UnityEngine;
using TButt;

namespace TButt
{
	public abstract class TBObjectBase : MonoBehaviour
	{
		private Transform _transform;
		public new Transform transform
		{
			get
			{
				if (_transform == null)
					_transform = GetComponent<Transform> ();

				return _transform;
			}
		}

		protected bool _subscribedToUpdate = false;
		protected bool _subscribedToFixedUpdate = false;
		protected bool _subscribedToLateUpdate = false;

		protected virtual void Awake () { }
		protected virtual void OnEnable () { }
		protected virtual void OnDisable () { }

		protected void SubscribeToUpdate (bool subscribe)
		{
			if (_subscribedToUpdate == subscribe)
				return;

			if (subscribe)
			{
				TBCore.OnUpdate += OnUpdate;
			}
			else
			{
				TBCore.OnUpdate -= OnUpdate;
			}

			_subscribedToUpdate = subscribe;
		}

		protected void SubscribeToFixedUpdate (bool subscribe)
		{
			if (_subscribedToFixedUpdate == subscribe)
				return;

			if (subscribe)
			{
				TBCore.OnFixedUpdate += OnFixedUpdate;
			}
			else
			{
				TBCore.OnFixedUpdate -= OnFixedUpdate;
			}

			_subscribedToFixedUpdate = subscribe;
		}

		protected void SubscribeToLateUpdate (bool subscribe)
		{
			if (_subscribedToLateUpdate == subscribe)
				return;

			if (subscribe)
			{
				TBCore.OnLateUpdate += OnLateUpdate;
			}
			else
			{
				TBCore.OnLateUpdate -= OnLateUpdate;
			}

			_subscribedToLateUpdate = subscribe;
		}


		protected virtual void OnUpdate () { }
		protected virtual void OnFixedUpdate () { }
		protected virtual void OnLateUpdate () { }
	}
}