using Manager;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Classes
{
	[Serializable]
	public class LayerData
	{
		private Dictionary<GameObject, List<Vector3>> layerObjects;
		private Dictionary<string, Vector3[]> destroyedObjects = new();
		private List<Vector3> recordedPositions;

		private bool isRecording = true;
		private int destroyIndex = 0;

		private string layerName;
		private int objIndex = 0;

		public LayerData(string _layername)
		{
			layerObjects = new Dictionary<GameObject, List<Vector3>>();
			recordedPositions = new List<Vector3>();
			layerName = _layername;
		}

		/// <summary>
		/// Add a new object to the layer dictionary
		/// </summary>
		/// <param name="obj"></param>
		public void AddObject(GameObject obj)
		{
			if(!layerObjects.ContainsKey(obj))
			{
				layerObjects.Add(obj, new List<Vector3>());
			}
		}

		/// <summary>
		/// Removes given object to the layer dictionary
		/// </summary>
		/// <param name="obj"></param>
		public void RemoveObject(GameObject obj)
		{
			if(layerObjects.ContainsKey(obj))
			{
				layerObjects.Remove(obj);
			}
		}

		/// <summary>
		/// Adds a destroyed object to the dictionary
		/// </summary>
		/// <param name="obj"></param>
		public void AddObjectToDestroyedDictionary(GameObject obj)
		{
			if(layerObjects.ContainsKey(obj))
			{
				List<Vector3> tempList = layerObjects[obj];
				string destroyedObjName = obj.name + "_" + "_Destroyed_" + destroyIndex;
				destroyedObjects.Add(destroyedObjName, new Vector3[tempList.Count]);
				tempList.CopyTo(destroyedObjects[destroyedObjName]);
				destroyIndex++;
			}
		}

		/// <summary>
		/// Updates the position of the object in the dictionary. Clears out empty and null objects as well
		/// </summary>
		public void UpdateObject()
		{
			List<GameObject> toRemove = new();

			foreach(GameObject obj in layerObjects.Keys)
			{
				if(obj == null)
				{
					toRemove.Add(obj);
				}
			}

			foreach(GameObject obj in toRemove)
			{
				RemoveObject(obj);
			}

			foreach((GameObject obj, List<Vector3> posList) in layerObjects)
			{
				if(obj != null)
				{
					if(!posList.Contains(obj.transform.position))
					{
						posList.Add(obj.transform.position);
					}
				}
			}
		}

		/// <summary>
		/// A Function that can be used to create heatmaps at run time. This function will create a heatmap for both
		/// dictionarys and the total on the layer then save them to the given path
		/// </summary>
		public async void CreateRuntimeHeatmapCreationLayer()
		{
			string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{HeatmapManager.Instance.GetTimeStamp()}/{layerName}";

			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			if(layerObjects.Count > 0)
			{
				foreach((GameObject obj, List<Vector3> posList) in layerObjects)
				{
					if(obj != null)
					{
						Vector3[] positionArray = posList.ToArray();
						await ClusterHeatMap.CreateClusteredHeatmap(positionArray, HeatmapManager.Instance.GetPointRadius(), HeatmapManager.Instance.GetEpsilon(), HeatmapManager.Instance.GetMinPoints(), HeatmapManager.Instance.GetIs2DMode());
						Texture2D heatmapImage = ClusterHeatMap.GetFinalTexture();
						string objName = obj.name;

						while(File.Exists(directoryPath + "/" + objName + "_" + objIndex + ".png"))
						{
							objIndex++;
						}
						
						foreach(Vector3 t in positionArray)
						{
							HeatmapManager.Instance.AddHeatmapEvent(obj.name, t);
							recordedPositions.Add(t);
						}


						File.WriteAllBytes(directoryPath + "/" + objName + "_" + objIndex + ".png", heatmapImage.EncodeToPNG());
					}
				}
			}

			if(destroyedObjects.Count > 0)
			{
				foreach((string obj, Vector3[] posList) in destroyedObjects)
				{
					await ClusterHeatMap.CreateClusteredHeatmap(posList, HeatmapManager.Instance.GetPointRadius(), HeatmapManager.Instance.GetEpsilon(), HeatmapManager.Instance.GetMinPoints(), HeatmapManager.Instance.GetIs2DMode());
					Texture2D heatmapImage = ClusterHeatMap.GetFinalTexture();
					string objName = obj + "_Destroyed";
					int index = 0;
					
					while(File.Exists(directoryPath + "/" + objName + "_" + index + ".png"))
					{
						index++;
					}
					
					foreach(Vector3 t in posList)
					{
						HeatmapManager.Instance.AddHeatmapEvent(obj, t);
						recordedPositions.Add(t);
					}


					File.WriteAllBytes(directoryPath + "/" + objName + "_" + index + ".png", heatmapImage.EncodeToPNG());
				}
			}

			Vector3[] layerPositionArray = recordedPositions.ToArray();
			await ClusterHeatMap.CreateClusteredHeatmap(layerPositionArray, HeatmapManager.Instance.GetPointRadius(), HeatmapManager.Instance.GetEpsilon(), HeatmapManager.Instance.GetMinPoints(), HeatmapManager.Instance.GetIs2DMode());
			Texture2D heatmapLayerImage = ClusterHeatMap.GetFinalTexture();
			foreach(Vector3 t in layerPositionArray)
			{
				HeatmapManager.Instance.AddHeatmapEvent(layerName, t);
			}
			
			File.WriteAllBytes(directoryPath + $"/{layerName}.png", heatmapLayerImage.EncodeToPNG());
		}

		/// <summary>
		/// A Function that can be used to create heatmaps json logs at run time. This function will create a file for both
		/// dictionarys and the total on the layer then save them to the given path. These files are used in the front
		/// end tool to create the heatmap
		/// </summary>
		public void CreateLogData()
		{
			int index = 0;

			if(layerObjects.Count > 0)
			{
				foreach((GameObject obj, List<Vector3> posList) in layerObjects)
				{
					if(obj != null)
					{
						Vector3[] positionArray = posList.ToArray();
						string objName = obj.name + "_" + index;

						if(HeatmapManager.Instance.GetEventPositionsContainsKey(objName))
						{
							index++;
						}

						foreach(Vector3 t in positionArray)
						{
							HeatmapManager.Instance.AddHeatmapEvent(objName, t);
							recordedPositions.Add(t);
						}
					}
				}
			}


			if(destroyedObjects.Count > 0)
			{
				Debug.Log(layerName + "_" + destroyedObjects.Count);
				foreach((string obj, Vector3[] posList) in destroyedObjects)
				{

					Debug.Log(obj + "::" + posList.Length);

					foreach(Vector3 t in posList)
					{
						HeatmapManager.Instance.AddHeatmapEvent(obj, t);
						recordedPositions.Add(t);
					}
				}
			}

			Vector3[] layerPositionArray = recordedPositions.ToArray();

			foreach(Vector3 t in layerPositionArray)
			{
				HeatmapManager.Instance.AddHeatmapEvent("Total_Layer_" + layerName, t);
			}
		}

		/// <summary>
		/// Returns all positions from the layer
		/// </summary>
		/// <returns></returns>
		public List<Vector3> GetTotalRecordedPositions()
		{
			return recordedPositions;
		}

		/// <summary>
		/// Returns whether a key exists in the active layer
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool Contains(GameObject obj)
		{
			return layerObjects.ContainsKey(obj);
		}
		
		/// <summary>
		/// Returns the layer name
		/// </summary>
		public string GetLayerName()
		{
			return layerName;
		}

		/// <summary>
		/// Returns the record State
		/// </summary>
		public bool GetRecordState() => isRecording;

		/// <summary>
		/// Changes the recording state
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		private void ChangeRecordState(bool state) => isRecording = state;
	}
}