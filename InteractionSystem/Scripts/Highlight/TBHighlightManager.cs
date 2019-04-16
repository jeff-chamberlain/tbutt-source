using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt.InteractionSystem
{
	public class TBHighlightManager : TBGameManagerBase
	{
		public static TBHighlightManager instance;

		public HighlightDef defaultHighlight;
		public HighlightDef[] otherHighlights;

		private Dictionary<Material, Material> _hightlightMats = new Dictionary<Material, Material> ();

        public override void Initialize()
        {
			instance = this;
		}

		/// <summary>
		/// Registers the material with the highlight manager. If the material has not already been registered, a new highlight material will be created and added to the dictionary.
		/// </summary>
		/// <param name="baseMat"></param>
		/// <param name="highlightOverride"></param>
		/// <returns></returns>
		public Material RegisterMaterial (Material baseMat, TBHighlightSettingsOverride highlightOverride = null)
		{
			if (!_hightlightMats.ContainsKey (baseMat))
			{
				Material highlight;

				if (highlightOverride != null)
				{
					if (highlightOverride.premadeHighlightOverride > -1 && highlightOverride.premadeHighlightOverride < otherHighlights.Length)
					{
						//use premade highlight
						highlight = CreateHighlightMaterial (baseMat, otherHighlights[highlightOverride.premadeHighlightOverride]);
					}
					else
					{
						//use highlight settings passed in
						highlight = CreateHighlightMaterial (baseMat, highlightOverride.highlightOverride);
					}

					_hightlightMats.Add (baseMat, highlight);
				}
				else
				{
					//use default highlight
					highlight = CreateHighlightMaterial (baseMat);
					_hightlightMats.Add (baseMat, highlight);
				}

				return highlight;
			}
			else
			{
				return _hightlightMats[baseMat];
			}
		}

		/// <summary>
		/// Create a highlight material just by changing a color property on the base material
		/// </summary>
		/// <param name="baseMat"></param>
		/// <returns></returns>
		Material CreateHighlightMaterial (Material baseMat)
		{
			Material mat = new Material (baseMat);

			if (defaultHighlight.materialOverride != null)
			{
				//swap the shader first so it has array values for the properties of that shader
				mat.shader = defaultHighlight.materialOverride.shader;
				//copy the shader properties to our new material
				CopyShaderProperties (ref mat, defaultHighlight);
			}
			else if (defaultHighlight.shaderOverride != null)
			{
				mat.shader = defaultHighlight.shaderOverride;
				CopyShaderProperties (ref mat, defaultHighlight);
			}

			mat.EnableKeyword ("_EMISSION");    //this makes sure the standard shader emission works
			mat.name += "_Highlight";

			if (!string.IsNullOrEmpty (defaultHighlight.highlightColorShaderProperty))
				mat.SetColor (defaultHighlight.highlightColorShaderProperty, defaultHighlight.highlightColor);

			return mat;
		}

		/// <summary>
		/// Create a more custom highlight material.
		/// </summary>
		/// <param name="baseMat"></param>
		/// <param name="highlightDef"></param>
		/// <returns></returns>
		Material CreateHighlightMaterial (Material baseMat, HighlightDef highlightDef)
		{
			Material mat = new Material (baseMat); ;

			if (highlightDef.materialOverride != null)
			{
				//swap the shader first so it has array values for the properties of that shader
				mat.shader = highlightDef.materialOverride.shader;
				//copy the shader properties to our new material
				CopyShaderProperties (ref mat, highlightDef);
			}
			else if (highlightDef.shaderOverride != null)
			{
				mat.shader = highlightDef.shaderOverride;
				CopyShaderProperties (ref mat, highlightDef);
			}

			mat.name += "_Highlight";
			mat.EnableKeyword ("_EMISSION");    //this makes sure the standard shader emission works

			if (!string.IsNullOrEmpty (highlightDef.highlightColorShaderProperty))
				mat.SetColor (highlightDef.highlightColorShaderProperty, highlightDef.highlightColor);

			return mat;
		}

		/// <summary>
		/// Copies the given properties from one material to another
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="highlightDef"></param>
		void CopyShaderProperties (ref Material mat, HighlightDef highlightDef)
		{
			for (int i = 0; i < highlightDef.shaderPropertiesToCopy.Length; i++)
			{
				//make sure the property name isn't blank
				if (!string.IsNullOrEmpty (highlightDef.shaderPropertiesToCopy[i].propertyName))
				{
					switch (highlightDef.shaderPropertiesToCopy[i].propertyType)
					{
						case HighlightDef.PropertyType.Color:
							mat.SetColor (highlightDef.shaderPropertiesToCopy[i].propertyName, highlightDef.materialOverride.GetColor (highlightDef.shaderPropertiesToCopy[i].propertyName));
							break;
						case HighlightDef.PropertyType.Float:
							mat.SetFloat (highlightDef.shaderPropertiesToCopy[i].propertyName, highlightDef.materialOverride.GetFloat (highlightDef.shaderPropertiesToCopy[i].propertyName));
							break;
						case HighlightDef.PropertyType.Int:
							mat.SetInt (highlightDef.shaderPropertiesToCopy[i].propertyName, highlightDef.materialOverride.GetInt (highlightDef.shaderPropertiesToCopy[i].propertyName));
							break;
						case HighlightDef.PropertyType.Texture:
							mat.SetTexture (highlightDef.shaderPropertiesToCopy[i].propertyName, highlightDef.materialOverride.GetTexture (highlightDef.shaderPropertiesToCopy[i].propertyName));
							break;
						case HighlightDef.PropertyType.Vector:
							mat.SetVector (highlightDef.shaderPropertiesToCopy[i].propertyName, highlightDef.materialOverride.GetVector (highlightDef.shaderPropertiesToCopy[i].propertyName));
							break;
						case HighlightDef.PropertyType.Keyword:
							for (int j = 0; j < highlightDef.materialOverride.shaderKeywords.Length; j++)
							{
								mat.EnableKeyword (highlightDef.materialOverride.shaderKeywords[j]);
							}
							break;
					}
				}
			}
		}

		public Material GetHightlightMaterial (Material baseMat)
		{
			return _hightlightMats[baseMat];
		}

		[System.Serializable]
		public class HighlightDef
		{
			public Color highlightColor = new Color (1f, 0.756f, 0.016f);
			public string highlightColorShaderProperty = "_EmissionColor";
			[Tooltip ("A material that's set up the way you want it for the highlight. The highlight color defined here will still be used.")]
			public Material materialOverride;
			[Tooltip ("If this is null, the object's original Shader will be used.")]
			public Shader shaderOverride;
			public ShaderProperty[] shaderPropertiesToCopy;

			public enum PropertyType
			{
				Color,
				Float,
				Int,
				Texture,
				Vector,
				Keyword
			}

			[System.Serializable]
			public struct ShaderProperty
			{
				public string propertyName;
				public PropertyType propertyType;
			}
		}
	}
}
