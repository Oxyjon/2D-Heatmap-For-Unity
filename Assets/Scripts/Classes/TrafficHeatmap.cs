using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Utils;

namespace Classes
{
	public static class TrafficHeatmap
	{
		private const float ALPHA_STEP = 0.1f; //The starting alpha of the points

		private const int LINE_WIDTH = 1; //The line widths of the circles for the points
		private static Dictionary<Vector2, Color> pixelAlpha = new();
		private static Texture2D finalTexture;
		private static int totalPointsCount;
		private static int currentPointCount;
		
		public static Texture2D GetFinalTexture() => finalTexture;
		public static int GetTotalPointsCount => totalPointsCount;
		public static int GetCurrentPointCount => currentPointCount;

		/// <summary>
		/// Creates a Traffic heatmap from a list of world positions.
		/// This function assigns the conversion camera based on the mode whether it is 2D or 3D.
		/// It then takes the world positions and converts them to screen positions.
		/// It then takes the points and will fill in missing points based on the radius to form a line.
		/// From there it will take the total points and generate the texture
		/// </summary>
		/// <param name="_worldPoints"></param>
		/// <param name="_radius"></param>
		/// <param name="_is2D"></param>
		/// <returns></returns>
		public static async Task<Task> CreateHeatmap(Vector3[] _worldPoints, int _radius, bool _is2D)
		{
			//Initialise the Camera
			Camera camera = Camera.main;

			if(!_is2D)
			{
				if(GameObject.FindWithTag("HeatmapCamera"))
				{
					camera = GameObject.FindWithTag("HeatmapCamera").GetComponent<Camera>();
				}
				else
				{
					Debug.LogWarning("Camera Not Found, Please Add The Manager and Tag the Camera With 'HeatmapCamera'");
					return Task.CompletedTask;
				}
			}
			//Creates a Texture based on the Camera's Resolution
			finalTexture = new(camera!.pixelWidth, camera!.pixelHeight, TextureFormat.ARGB32, false);

			
			//Sets the Texture to be Transparent
			finalTexture.SetPixels(Helper.CreateColorArray(new Color(1f, 1f, 1f, 0f), finalTexture.width * finalTexture.height), 0);

			//Converts the World Points to Screen Points
			Vector2[] points = new Vector2[_worldPoints.Length];
			for(int i = 0; i < _worldPoints.Length; i++)
			{
				points[i] = camera.WorldToScreenPoint(_worldPoints[i]);
			}

			//Generates the Points in between the Points by the Radius
			List<Vector2> pointsList = new();
			for(int i = 0; i < points.Length - 1; i++)
			{
				Vector2 point1 = points[i];
				Vector2 point2 = points[i + 1];
				float distance = Vector2.Distance(point1, point2);
				int numberOfPoints = (int) (distance / _radius);
				for(int j = 0; j < numberOfPoints; j++)
				{
					float x = Mathf.Lerp(point1.x, point2.x, (float) j / numberOfPoints);
					float y = Mathf.Lerp(point1.y, point2.y, (float) j / numberOfPoints);
					pointsList.Add(new Vector2(x, y));
				}
			}
			
			Vector2[] totalPoints = pointsList.ToArray();
			Color color = new(1f, 1f, 1f, ALPHA_STEP);
			totalPointsCount = totalPoints.Length - 1;
			for(int i = 0; i < totalPoints.Length; i++) 
			{
				await Task.Delay(1);
				await GenerateTrafficTexture(totalPoints[i],_radius, finalTexture, color);
				currentPointCount = i;
			}

			finalTexture.Apply();
			finalTexture.SetPixels(Helper.GradientColorizePixels(finalTexture.GetPixels(0)), 0);
			finalTexture.Apply();
			
			return Task.CompletedTask;

		}

		/// <summary>
		/// The function assigns all the pixels on the heatmap based on the total points
		/// It uses a radius to create a circle around the point and then assigns the pixels
		/// It will assign the colors and scale based on the alpha. The more points in a cluster the more alpha it will
		/// have, meaning it will be darker(more red).
		/// It sets the colors and then overwrites the surrounding pixels back to defaults for the next pixel.
		/// </summary>
		/// <param name="_point"></param>
		/// <param name="_radius"></param>
		/// <param name="_mapTexture"></param>
		/// <param name="_color"></param>
		/// <returns></returns>
		private static Task GenerateTrafficTexture(Vector2 _point, int _radius, Texture2D _mapTexture, Color _color)
		{
			//Clear the Dictionary
			pixelAlpha.Clear();

			//Draw the Circles and fill them in
			for(int r = 0; r < _radius; r += LINE_WIDTH)
			{
				//phi formula to generate the circumference
				for(float phi = 0; phi < 360; phi += 0.1f)
				{
					//Calculates the X and Y coordinates of the point
					int x = (int) (_point.x + r * Mathf.Cos(phi * Mathf.Deg2Rad));
					int y = (int) (_point.y + r * Mathf.Sin(phi * Mathf.Deg2Rad));
					// Loops through the width of the line and set the alpha
					for(int y2 = y; y2 > y - LINE_WIDTH; y2--)
					{
						for(int x2 = x; x2 < x + LINE_WIDTH; x2++)
						{
							Vector2 coord = new(x2, y2);
							if(pixelAlpha.ContainsKey(coord))
								pixelAlpha[coord] = _color;
							else
								pixelAlpha.Add(new Vector2(x2, y2), _color);
						}
					}
				}


				//Decreases the Alpha for the next circle
				_color = new Color(_color.r, _color.g, _color.b, _color.a - ALPHA_STEP / ((float) _radius / LINE_WIDTH));
			}

			//We want to make sure to only add the finalized results to the old results
			foreach((Vector2 coord, Color newColor) in pixelAlpha)
			{
				Color previousColor = _mapTexture.GetPixel((int) coord.x, (int) coord.y);
				_mapTexture.SetPixel((int) coord.x, (int) coord.y, new Color(newColor.r, newColor.b, newColor.g, newColor.a + previousColor.a));
			}

			// Reset color for next point
			_color = new Color(_color.r, _color.g, _color.b, ALPHA_STEP);

			return Task.CompletedTask;
		}
	}
}