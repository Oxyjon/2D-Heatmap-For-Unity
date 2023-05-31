using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Utils;

namespace Classes
{
	public static class ClusterHeatMap
	{
		private const float ALPHA_STEP = 0.01f; //The starting alpha of the points

		private const int LINE_WIDTH = 1; //The line widths of the circles for the points
		public static ConcurrentDictionary<Vector2, Color> pixelAlpha = new();
		private static Texture2D finalTexture;
		private static int totalPointsCount;
		private static int currentPointCount;

		public static Texture2D GetFinalTexture() => finalTexture;
		public static int GetTotalPointsCount => totalPointsCount;
		public static int GetCurrentPointCount => currentPointCount;

		/// <summary>
		/// Creates a Cluster heatmap from a list of world positions.
		/// This function assigns the conversion camera based on the mode whether it is 2D or 3D.
		/// It then takes the world positions and converts them to screen positions.
		/// It then takes the positions and creates clusters based on epsilon and minPoints.
		/// Which will then eventually give you a heatmap texture image
		/// </summary>
		/// <param name="worldPoints"></param>
		/// <param name="radius"></param>
		/// <param name="eps"></param>
		/// <param name="minPts"></param>
		/// <param name="is2D"></param>
		/// <returns></returns>
		public static async Task<Task> CreateClusteredHeatmap(Vector3[] worldPoints, int radius, float eps, int minPts, bool is2D)
		{
			Camera camera = Camera.main;

			if(!is2D)
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


			// Convert world to screen points
			Vector2[] points = new Vector2[worldPoints.Length];
			for(int i = 0; i < worldPoints.Length; i++)
			{
				points[i] = camera!.WorldToScreenPoint(worldPoints[i]);
			}

			// Cluster points using DBSCAN
			List<List<int>> clusters = DBSCAN.CreateClusters(points, eps, minPts);

			// Generate texture
			finalTexture = new(camera!.pixelWidth, camera!.pixelHeight, TextureFormat.ARGB32, false);

			// Set texture to alpha-fied state
			finalTexture.SetPixels(Helper.CreateColorArray(new Color(1f, 1f, 1f, 0f), finalTexture.width * finalTexture.height), 0);

			Color color = new(1f, 1f, 1f, ALPHA_STEP);
			totalPointsCount = clusters.Count - 1;

			for(int i = 0; i < clusters.Count; i++)
			{
				List<int> cluster = clusters[i];
				await Task.Delay(1);
				await GenerateClusterTexture(cluster, points, radius, finalTexture, color);
				currentPointCount = i;
			}

			finalTexture.Apply();

			finalTexture.SetPixels(Helper.GradientColorizePixels(finalTexture.GetPixels(0)), 0);

			finalTexture.Apply();

			return Task.CompletedTask;
		}

		/// <summary>
		/// The function assigns all the pixels on the heatmap based on the cluster points
		/// It uses a radius to create a circle around the point and then assigns the pixels
		/// It will assign the colors and scale based on the alpha. The more points in a cluster the more alpha it will
		/// have, meaning it will be darker(more red).
		/// It sets the colors and then overwrites the surrounding pixels back to defaults for the next pixel.
		/// </summary>
		/// <param name="cluster"></param>
		/// <param name="points"></param>
		/// <param name="radius"></param>
		/// <param name="mapTexture"></param>
		/// <param name="_color"></param>
		/// <returns></returns>
		private static Task GenerateClusterTexture(List<int> cluster, Vector2[] points, int radius, Texture2D mapTexture, Color _color)
		{
			foreach(int index in cluster)
			{
				Vector2 point = points[index];
				pixelAlpha.Clear();

				for(int r = 0; r < radius; r += LINE_WIDTH)
				{
					//phi formula to generate the circumference
					for(float phi = 0; phi < 360; phi += 0.1f)
					{
						//Calculate the X and Y coordinates of the point
						int x = (int) (point.x + r * Mathf.Cos(phi * Mathf.Deg2Rad));
						int y = (int) (point.y + r * Mathf.Sin(phi * Mathf.Deg2Rad));
						for(int y2 = y; y2 > y - LINE_WIDTH; y2--)
						{
							for(int x2 = x; x2 < x + LINE_WIDTH; x2++)
							{
								Vector2 coord = new(x2, y2);
								if(pixelAlpha.ContainsKey(coord))
									pixelAlpha[coord] = _color;
								else
									pixelAlpha.TryAdd(new Vector2(x2, y2), _color);
							}
						}
					}

					_color = new Color(_color.r, _color.g, _color.b, _color.a - ALPHA_STEP / ((float) radius / LINE_WIDTH));
				}

				foreach(KeyValuePair<Vector2, Color> pair in pixelAlpha)
				{
					Color previousColor = mapTexture.GetPixel((int) pair.Key.x, (int) pair.Key.y);
					mapTexture.SetPixel((int) pair.Key.x, (int) pair.Key.y, new Color(pair.Value.r, pair.Value.b, pair.Value.g, pair.Value.a + previousColor.a));
				}

				_color = new Color(_color.r, _color.g, _color.b, ALPHA_STEP);
			}

			return Task.CompletedTask;
		}
	}
}