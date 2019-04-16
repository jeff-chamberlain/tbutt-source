using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

namespace TButt.InteractionSystem
{
	[CustomEditor (typeof (TBInteractiveObjectBase), true)]
	public class TBInteractiveObjectBaseEditor : EasyEditor.EasyEditorBase
	{
		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();

			TBInteractiveObjectBase t = (TBInteractiveObjectBase)target;

			t.overrideInteractorVisualsOnSelect = EditorGUILayout.Toggle ("Override Interactor Visuals On Select", t.overrideInteractorVisualsOnSelect);

			if (GUI.changed)
            {
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage ();
                if ( prefabStage != null )
                {
                    EditorSceneManager.MarkSceneDirty ( prefabStage.scene );
                }
                else
                {
                    EditorUtility.SetDirty ( t );
                }
            }

			if (t.overrideInteractorVisualsOnSelect)
			{
				t.hideInteractorVisualsOnSelect = EditorGUILayout.Toggle ("Hide Interactor Visuals On Select", t.hideInteractorVisualsOnSelect);

                if ( GUI.changed )
                {
                    PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage ();
                    if ( prefabStage != null )
                    {
                        EditorSceneManager.MarkSceneDirty ( prefabStage.scene );
                    }
                    else
                    {
                        EditorUtility.SetDirty ( t );
                    }
                }
            }
		}
	}
}
