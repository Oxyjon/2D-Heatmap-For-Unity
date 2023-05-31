using UnityEngine;

namespace Utils
{
	public static class Helper
	{
		//Properties
		private static Gradient mGradient = new();
		private static readonly GradientColorKey[] gradientColorKeys = new GradientColorKey[5];
		private static readonly GradientAlphaKey[] gradientAlphaKeys = new GradientAlphaKey[2];
		
		//Functions

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_col"></param>
		/// <param name="_arraySize"></param>
		/// <returns></returns>
		public static Color[] CreateColorArray(Color _col, int _arraySize)
		{
			Color[] colors = new Color[_arraySize];
			for(int i = 0; i < _arraySize; i++)
			{
				colors[i] = _col;
			}
			return colors;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pixels"></param>
		/// <returns></returns>
		public static Color[] GradientColorizePixels(Color[] pixels)
		{
			gradientColorKeys[0].color = Color.blue;
			gradientColorKeys[0].time = 0.0F;

			gradientColorKeys[1].color = Color.cyan;
			gradientColorKeys[1].time = 0.25F;

			gradientColorKeys[2].color = Color.green;
			gradientColorKeys[2].time = 0.5F;

			gradientColorKeys[3].color = Color.yellow;
			gradientColorKeys[3].time = 0.75F;

			gradientColorKeys[4].color = Color.red;
			gradientColorKeys[4].time = 1.0F;

			gradientAlphaKeys[0].alpha = 0.0F;
			gradientAlphaKeys[0].time = 0.0F;
			gradientAlphaKeys[1].alpha = 0.9f;
			gradientAlphaKeys[1].time = 1.0F;

			mGradient.SetKeys(gradientColorKeys, gradientAlphaKeys);

			for(int i = 0; i < pixels.Length; i++)
			{
				pixels[i] *= 255f;
				float alpha = pixels[i].a;

				if(alpha == 0)
				{
					continue;
				}

				Color color = mGradient.Evaluate(alpha / 255f);

				pixels[i] = color;
			}

			return pixels;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static Texture2D LoadTextureFromPath(string path)
		{
			var data = System.IO.File.ReadAllBytes(path);
			Texture2D tex = new(2, 2);
			tex.LoadImage(data);

			return tex;
		}
	}
}