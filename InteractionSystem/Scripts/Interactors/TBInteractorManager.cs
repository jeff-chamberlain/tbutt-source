using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt;

namespace TButt.InteractionSystem
{
    public class TBInteractorManager : MonoBehaviour
    {
        private static TBInteractorManager _instance;
        public static TBInteractorManager instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject().AddComponent<TBInteractorManager>();
                }
                return _instance;
            }
        }

        protected TBInteractorGroup[] _interactorGroups;

        private void Update()
        {
            if (_interactorGroups == null)
                return;

            for (int i = 0; i < _interactorGroups.Length; i++)
            {
                for(int j = 0; j < _interactorGroups[i].interactors.Length; j++)
                {
                    if(_interactorGroups[i].interactors[j] != null)
                        _interactorGroups[i].interactors[j].UpdateInteractor();
                }
            }
        }

        /// <summary>
        /// Registers a TBInteractorBase object with TBInteractorManager. Note that only one interactor for each interactor type can be registered per controller.
        /// </summary>
        /// <param name="interactor"></param>
        /// <param name="type"></param>
        /// <param name="controller"></param>
        public void RegisterInteractor(TBInteractorBase interactor, TBInteractorType type, TBInput.Controller controller)
        {
            if (_interactorGroups == null)  // If interactor groups aren't initialized yet, create the first group.
            {
                _interactorGroups = new TBInteractorGroup[] { new TBInteractorGroup() };
                _interactorGroups[0].controller = controller;
                _interactorGroups[0].interactors = new TBInteractorBase[] { interactor };
            }
            else
            {
                for(int i = 0; i < _interactorGroups.Length; i ++)
                {
                    if (_interactorGroups[i].controller != controller)  // If this isn't the group for the controller type we need, skip this iteration of the loop.
                    {
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < _interactorGroups[i].interactors.Length; j++)   // if this is the right controller, interate through its interactors to see if we already have a group registered.
                        {
                            if (_interactorGroups[i].interactors[j] != null)
                            {
                                if (_interactorGroups[i].interactors[j].GetInteractorType() == type)
                                {
                                    Debug.LogError("Interactor type " + type + " is already registered for controller " + controller + "!");
                                    return;
                                }
                            }
                            if (j == _interactorGroups[i].interactors.Length - 1)
                            {
                                _interactorGroups[i].interactors = TBArrayExtensions.AddToArray<TBInteractorBase>(_interactorGroups[i].interactors, interactor);
                                return;
                            }
                        }
                    }
                }
                //Debug.Log(_interactorGroups.Length);
                _interactorGroups = TBArrayExtensions.AddToArray<TBInteractorGroup>(_interactorGroups, new TBInteractorGroup());

                //_interactorGroups.AddToArray<TBInteractorGroup>(new TBInteractorGroup());   // If we get this far, it means there was no existing group for the given controller type. So we make one.
                _interactorGroups[_interactorGroups.Length - 1].controller = controller;
                _interactorGroups[_interactorGroups.Length - 1].interactors = new TBInteractorBase[] { interactor };
            }
        }

        /// <summary>
        /// Removes an interactor from the group.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="controller"></param>
        public void RemoveInteractor(TBInteractorType type, TBInput.Controller controller)
        {
            if (_interactorGroups == null)  // If interactor groups aren't initialized yet, create the first group.
            {
                Debug.LogError("Unable to remove interactor because the interactor group array is null.");
                return;
            }
            else
            {
                for (int i = 0; i < _interactorGroups.Length; i++)
                {
                    if (_interactorGroups[i].controller != controller)  // If this isn't the group for the controller type we need, skip this iteration of the loop.
                        continue;

                    for (int j = 0; j < _interactorGroups[i].interactors.Length; j++)   // if this is the right controller, interate through its interactors to see if we already have a group registered.
                    {
                        if (_interactorGroups[i].interactors[j] != null)
                        {
                            if (_interactorGroups[i].interactors[j].GetInteractorType () == type)
                            {
                                _interactorGroups[i].interactors[j].Remove();
                                _interactorGroups[i].interactors[j] = null;
                                return;
                            }
                        }
                    }
                }
            }
        }

		public TBInteractorBase GetInteractor (TBInteractorType type, TBInput.Controller controller)
		{
			if (_interactorGroups == null)
			{
				return null;
			}
			else
			{
				for (int i = 0; i < _interactorGroups.Length; i++)
				{
					if (_interactorGroups[i].controller != controller)
						continue;

					for (int j = 0; j < _interactorGroups[i].interactors.Length; j++)
					{
						if (_interactorGroups[i].interactors[j] != null)
						{
							if (_interactorGroups[i].interactors[j].GetInteractorType () == type)
							{
								return _interactorGroups[i].interactors[j];
							}
						}
					}
				}
			}

			Debug.LogWarning ("Could not find interactor of type " + type + " for controller " + controller + ".");
			return null;
		}

		public int GetInteractorGroupCount ()
		{
			return _interactorGroups.Length;
		}

		public TBInteractorGroup[] GetInteractorGroups ()
		{
			return _interactorGroups;
		}

        #region INTERNAL FUNCTIONS
        /// <summary>
        /// Gets the array index for the InteractorGroup array for the specified controller type. Returns -1 if there is no group for that type.
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        protected int GetControllerGroupID(TBInput.Controller controller)
        {
            if(_interactorGroups == null)
            {
                Debug.Log("Attempted to access a controller's interactor group before the interactor groups were initialized.");
                return -1;
            }
            else
            {
                for (int i = 0; i < _interactorGroups.Length; i++)
                {
                    if (_interactorGroups[i].controller != controller)  // If this isn't the group for the controller type we need, skip this iteration of the loop.
                        continue;
                    else
                        return i;
                }
                Debug.Log("No group exists for requested controller type.");
                return -1;
            }
        }
        #endregion

        #region GET OBJECT / HAS OBJECT FUNCTIONS
        /// <summary>
        /// Returns the hovered GameObject of the specified interactor (can be null)
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Collider GetHoveredCollider (TBInput.Controller controller, TBInteractorType type = TBInteractorType.Hand)
        {
            int id = GetControllerGroupID(controller);
            if (id == -1)
                return null;

            for (int i = 0; i < _interactorGroups[id].interactors.Length; i++)
            {
                if (_interactorGroups[id].interactors[i].GetInteractorType () == type)
                    return _interactorGroups[id].interactors[i].GetHoveredCollider ();
            }

            return null;
        }

        /// <summary>
        /// Returns true if the specified interactor is hovering any GameObject.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasHoveredCollider (TBInput.Controller controller, TBInteractorType type = TBInteractorType.Hand)
        {
            int id = GetControllerGroupID(controller);
            if (id == -1)
                return false;

            for (int i = 0; i < _interactorGroups[id].interactors.Length; i++)
            {
                if (_interactorGroups[id].interactors[i].GetInteractorType () == type)
                    return _interactorGroups[id].interactors[i].HasHoveredCollider ();
            }

            return false;
        }

        /// <summary>
        /// Returns any selected interactive object for the given interactor (can be null)
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public TBInteractiveObjectBase GetSelectedInteractiveObject(TBInput.Controller controller, TBInteractorType type = TBInteractorType.Hand)
        {
            int id = GetControllerGroupID(controller);
            if (id == -1)
                return null;

            for (int i = 0; i < _interactorGroups[id].interactors.Length; i++)
            {
                if (_interactorGroups[id].interactors[i].GetInteractorType () == type)
                    return _interactorGroups[id].interactors[i].GetSelectedInteractiveObject();
            }

            return null;
        }

        /// <summary>
        /// Returns any hovered interactive object for the given interactor (can be null)
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public TBInteractiveObjectBase GetHoveredInteractiveObject(TBInput.Controller controller, TBInteractorType type = TBInteractorType.Hand)
        {
            int id = GetControllerGroupID(controller);
            if (id == -1)
                return null;

            for (int i = 0; i < _interactorGroups[id].interactors.Length; i++)
            {
                if (_interactorGroups[id].interactors[i].GetInteractorType () == type)
                    return _interactorGroups[id].interactors[i].GetHoveredInteractiveObject();
            }

            return null;
        }

        /// <summary>
        /// Returns true if the given interactor for a given controller is has a selected interactive object.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasSelectedInteractiveObject(TBInput.Controller controller, TBInteractorType type = TBInteractorType.Hand)
        {
            int id = GetControllerGroupID(controller);
            if (id == -1)
                return false;

            for (int i = 0; i < _interactorGroups[id].interactors.Length; i++)
            {
                if (_interactorGroups[id].interactors[i].GetInteractorType () == type)
                    return _interactorGroups[id].interactors[i].HasSelectedInteractiveObject();
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given interactor for a given controller is hovering an interactive object.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasHoveredInteractiveObject(TBInput.Controller controller, TBInteractorType type = TBInteractorType.Hand)
        {
            int id = GetControllerGroupID(controller);
            if (id == -1)
                return false;

            for (int i = 0; i < _interactorGroups[id].interactors.Length; i++)
            {
                if (_interactorGroups[id].interactors[i].GetInteractorType () == type)
                    return _interactorGroups[id].interactors[i].HasHoveredInteractiveObject();
            }

            return false;
        }
        #endregion

        public static class Events
        {
            public delegate bool ColliderInteractorEvent (Collider c, TBInteractorBase interactor);
			public delegate bool GameObjectInteractorEvent (GameObject go, TBInteractorBase interactor);

            public static ColliderInteractorEvent OnHoverEnter;
            public static ColliderInteractorEvent OnHoverExit;
			public static ColliderInteractorEvent OnHoverEvaluate;
			public static GameObjectInteractorEvent OnSelect;
        }
    }

    [System.Serializable]
    public struct TBInteractorGroup
    {
        public TBInput.Controller controller;
        public TBInteractorBase[] interactors;
    }

    public enum TBInteractorType
    {
        Reticle = 0,
        Hand    = 1
    }
}