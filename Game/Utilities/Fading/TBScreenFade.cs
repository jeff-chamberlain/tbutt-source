using UnityEngine;
using System.Collections;

namespace TButt
{
    /// <summary>
    /// Gets attached to a camera at the beginning of a scene by TBFadeManager.
    /// Not used on platforms that support fading with a compositor (like SteamVR).
    /// 
    /// Controlled by TBFade (static script)
    /// </summary>
    /// MARKING AS EXECUTE IN EDIT MODE FOR OTHERWORLD SPECIFIC DEBUGGING
    [ExecuteInEditMode]
    public class TBScreenFade : MonoBehaviour
	{		
        [HideInInspector]
		public Material fadeMaterial = null;

        void OnPostRender()
        {
            // BLOCK ADDED BY OTHERWORLD TO AID EXECUTION IN EDIT MODE
            {
                if ( !enabled
                || fadeMaterial == null )
                {
                    return;
                }
            }
                fadeMaterial.SetPass(0);
                GL.PushMatrix();
                GL.LoadOrtho();
                GL.Color(fadeMaterial.color);
                GL.Begin(GL.QUADS);
                GL.Vertex3(0f, 0f, -12f);
                GL.Vertex3(0f, 1f, -12f);
                GL.Vertex3(1f, 1f, -12f);
                GL.Vertex3(1f, 0f, -12f);
                GL.End();
                GL.PopMatrix();
        }
	}
}
