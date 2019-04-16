using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt.InteractionSystem
{
	public class TBHighlightSettingsOverride : MonoBehaviour
	{
		[Tooltip ("If this value is greater than -1, it will use a premade hightlight override as defined in the highlight manager.")]
		public int premadeHighlightOverride = -1;
		[Tooltip ("Define specific highlight override settings for this object.")]
		public TBHighlightManager.HighlightDef highlightOverride;
	}
}
