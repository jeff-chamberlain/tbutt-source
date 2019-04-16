using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TButt
{
	public static class TBMathHelper
	{
		public static float Remap (float value, float inLow, float inHigh, float outLow, float outHigh)
		{
			return outLow + (value - inLow) * (outHigh - outLow) / (inHigh - inLow);
		}

		public static float Remap01 (float value, float inLow, float inHigh)
		{
			return Remap (value, inLow, inHigh, 0f, 1f);
		}
	}
}
