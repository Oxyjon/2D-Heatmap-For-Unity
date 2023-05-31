using System.Collections.Generic;

using UnityEngine;

namespace Utils
{
	public class DBSCAN
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="points"></param>
		/// <param name="epsilon"></param>
		/// <param name="minPoints"></param>
		/// <returns></returns>
		public static List<List<int>> CreateClusters(Vector2[] points, float epsilon, int minPoints)
	{
		List<List<int>> clusters = new();
		int pointCount = points.Length;
		int[] pointCluster = new int[pointCount];

		epsilon *= pointCount / 1000f;
		minPoints = (int) (minPoints * (pointCount / 1000f));


		int clusterIndex = 1;

		for(int i = 0; i < pointCount; i++)
		{
			if(pointCluster[i] == 0)
			{
				List<int> neighbours = GetNeighbours(points, i, epsilon);

				if(neighbours.Count < minPoints)
				{
					pointCluster[i] = -1;
				}
				else
				{
					List<int> cluster = new();
					ExpandCluster(points, pointCluster, i, neighbours, cluster, clusterIndex, epsilon, minPoints);
					clusters.Add(cluster);
					clusterIndex++;
				}
			}
		}
		
		foreach(List<int> cluster in clusters)
		{
			Vector2 average = Vector2.zero;

			for(int j = 0; j < cluster.Count; j++)
			{
				average += points[cluster[j]];
			}

			average /= cluster.Count;

			points[cluster[0]] = average;
		}
		


		return clusters;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="points"></param>
	/// <param name="pointIndex"></param>
	/// <param name="epsilon"></param>
	/// <returns></returns>
	private static List<int> GetNeighbours(Vector2[] points, int pointIndex, float epsilon)
	{
		List<int> neighbours = new();
		for(int i = 0; i < points.Length; i++)
		{
			if(i != pointIndex)
			{
				float distance = Vector2.Distance(points[pointIndex], points[i]);
				if(distance <= epsilon / 10)
				{
					neighbours.Add(i);
				}
			}
		}

		return neighbours;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="points"></param>
	/// <param name="pointCluster"></param>
	/// <param name="pointIndex"></param>
	/// <param name="neighbours"></param>
	/// <param name="cluster"></param>
	/// <param name="clusterIndex"></param>
	/// <param name="epsilon"></param>
	/// <param name="minPoints"></param>
	private static void ExpandCluster(Vector2[] points, int[] pointCluster, int pointIndex, List<int> neighbours, List<int> cluster, int clusterIndex, float epsilon, int minPoints)
	{
		cluster.Add(pointIndex);
		pointCluster[pointIndex] = clusterIndex;

		for(int i = 0; i < neighbours.Count; i++)
		{
			int neighbourIndex = neighbours[i];
			if(pointCluster[neighbourIndex] == 0)
			{
				List<int> neighbourNeighbours = GetNeighbours(points, neighbourIndex, epsilon);
				if(neighbourNeighbours.Count >= minPoints)
				{
					ExpandCluster(points, pointCluster, neighbourIndex, neighbourNeighbours, cluster, clusterIndex, epsilon, minPoints);
				}
				else
				{
					pointCluster[neighbourIndex] = -1;
				}
			}

			if(pointCluster[neighbourIndex] < 1)
			{
				pointCluster[neighbourIndex] = clusterIndex;
				cluster.Add(neighbourIndex);
			}
		}
	}
	}
}