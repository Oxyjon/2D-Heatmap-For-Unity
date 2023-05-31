using Classes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

using Newtonsoft.Json;

using TMPro;

using UI;

using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Manager
{
	public class HeatmapManager : MonoBehaviour
	{
		//Private Variables
		private Dictionary<LayerMask, LayerData> layerData;
		private const int MAX_LAYERS = 32;
		private static HeatmapManager instance;
		private Dictionary<string, List<Vector3>> eventPositions;
		[SerializeField] private List<Vector3> loadedData = new();
		private Camera heatmapCamera;
		private Texture2D mapTexture;
		private string timeStamp = String.Empty;
		private SettingsData settingsData;
		private string savedDirectory;

		[Header("Recorder Settings")] [SerializeField] [Tooltip("The layers of which objects are recorded")]
		private LayerMask layerMasks;

		[SerializeField] [Tooltip("How often data is recorded")]
		private float recordingFrequency = 3.0f;

		[Header("Camera Settings")] [SerializeField] [Tooltip("The RenderTexture on the camera for screenshots")]
		private RenderTexture renderTexture;

		[SerializeField] [Tooltip("If the game is in 2D or 3D")]
		private bool is2DMode;

		[Header("Traffic Settings")] [SerializeField] [Tooltip("The radius of which points are drawn")]
		private int trafficPointRadius = 5;

		[Header("Cluster Settings")] [SerializeField] [Tooltip("The radius of which points are drawn")]
		private int clusterPointRadius = 15;

		[SerializeField] [Tooltip("The maximum distance between each point")]
		private float clusterEpsilon = 30;

		[SerializeField] [Tooltip("The minimum number of points to determine a cluster")]
		private int clusterMinPoints = 1;

		//Changeable values
		private int tempTrafficPointRadius;
		private int tempClusterPointRadius;
		private float tempClusterEpsilon;
		private int tempClusterMinPoints;


		public static HeatmapManager Instance => instance;

		private static JsonSerializerSettings jsonSettings => new()
		{
			TypeNameHandling = TypeNameHandling.None,
			Formatting = Formatting.Indented,
		};


		private void Awake()
		{
			layerData = new Dictionary<LayerMask, LayerData>();

			if(instance == null)
				instance = this;
			else
				Destroy(gameObject);
		}

		private void Start()
		{
			SetTimeStamp();

			for(int i = 0; i < MAX_LAYERS; i++)
			{
				LayerMask layerMask = 1 << i;
				if(layerMasks == (layerMasks | layerMask))
				{
					GameObject[] objectsInLayer = FindObjectsOfType<GameObject>().Where(go => layerMask == (layerMask | (1 << go.layer))).ToArray();
					string layerName = LayerMask.LayerToName(i);

					LayerData settings = new(layerName);

					foreach(GameObject obj in objectsInLayer)
					{
						settings.AddObject(obj);
					}

					layerData[layerMask] = settings;
				}
			}

			heatmapCamera = GetComponentInChildren<Camera>();


			if(SceneManager.GetActiveScene().name != "FrontEnd")
			{
				mapTexture = is2DMode ? TakeScreenshot2D(heatmapCamera) : TakeScreenshot(heatmapCamera);
				InvokeRepeating(nameof(UpdateHeatmap), 0, recordingFrequency); //Calls UpdateHeatmap every recordingFrequency seconds
				SaveSettingsData();
			}

			//Init values
			ResetValues();
		}


		/// <summary>
		/// Updating the heatmap by layer
		/// </summary>
		private void UpdateHeatmap()
		{
			foreach(KeyValuePair<LayerMask, LayerData> layer in layerData)
			{
				layer.Value.UpdateObject();
			}
		}

		/// <summary>
		/// Logging the destroyed object to the new dictionary
		/// </summary>
		/// <param name="obj"></param>
		public void LogDestroyedObject(GameObject obj)
		{
			foreach(KeyValuePair<LayerMask, LayerData> layer in layerData)
			{
				if(layer.Value.Contains(obj))
				{
					layer.Value.AddObjectToDestroyedDictionary(obj);
				}
			}

			Destroy(obj);
		}

		/// <summary>
		/// Add newly found objects to the dictionary at runtime automatically
		/// </summary>
		private void AddRuntimeObjectsToLayer()
		{
			for(int i = 0; i < MAX_LAYERS; i++)
			{
				LayerMask layerMask = 1 << i;
				if(layerMasks == (layerMasks | layerMask))
				{
					GameObject[] objectsInLayer = FindObjectsOfType<GameObject>().Where(go => layerMask == (layerMask | (1 << go.layer))).ToArray();

					foreach(GameObject obj in objectsInLayer)
					{
						if(!layerData[layerMask].Contains(obj))
						{
							layerData[layerMask].AddObject(obj);
						}
					}
				}
			}
		}

		/// <summary>
		/// Manually adds runtime objects to track
		/// </summary>
		/// <param name="_obj"></param>
		public void AddRuntimeObjectToTrack(GameObject _obj)
		{
			//get layer from object and add to the matching layer
			LayerMask layerMask = 1 << _obj.layer;
			if(layerMasks == (layerMasks | layerMask))
			{
				layerData[layerMask].AddObject(_obj);
			}
		}

		/// <summary>
		/// Creates the JSON log files
		/// </summary>
		private void CreateCombinedDataLog()
		{
			List<Vector3> points = new();
			foreach(KeyValuePair<LayerMask, LayerData> layer in layerData)
			{
				List<Vector3> tempList = layer.Value.GetTotalRecordedPositions();
				foreach(Vector3 pos in tempList)
				{
					points.Add(pos);
					AddHeatmapEvent("Combined", pos);
				}
			}

			SaveHeatmapData();
		}

		/// <summary>
		/// Uses the parameter camera and takes a screenshot of the scene from a overhead view
		/// </summary>
		/// <param name="_cam"></param>
		/// <returns></returns>
		private Texture2D TakeScreenshot(Camera _cam)
		{
			renderTexture.width = _cam.targetTexture.width;
			renderTexture.height = _cam.targetTexture.height;

			_cam.Render();
			RenderTexture.active = _cam.targetTexture;
			Texture2D image = new(_cam.targetTexture.width, _cam.targetTexture.height);
			image.ReadPixels(new Rect(0, 0, _cam.targetTexture.width, _cam.targetTexture.height), 0, 0);
			image.Apply();
			RenderTexture.active = null;
			byte[] bytes = image.EncodeToPNG();
			string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{timeStamp}/Combined";
			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);

			File.WriteAllBytes(directoryPath + $"/Screenshot.png", bytes);

			return image;
		}

		/// <summary>
		/// Uses the parameter camera and takes a screenshot of the scene from a 2D view
		/// </summary>
		/// <param name="_cam"></param>
		/// <returns></returns>
		private Texture2D TakeScreenshot2D(Camera _cam)
		{
			_cam.transform.position = Camera.main.transform.position;
			_cam.transform.rotation = Camera.main.transform.rotation;

			_cam.orthographicSize = Camera.main.orthographicSize;

			renderTexture.width = Screen.width;
			renderTexture.height = Screen.height;

			_cam.Render();
			RenderTexture.active = _cam.targetTexture;

			Texture2D image = new(_cam.targetTexture.width, _cam.targetTexture.height);
			image.ReadPixels(new Rect(0, 0, _cam.targetTexture.width, _cam.targetTexture.height), 0, 0);
			image.Apply();
			RenderTexture.active = null;
			byte[] bytes = image.EncodeToPNG();
			string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{timeStamp}/Combined";
			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);

			File.WriteAllBytes(directoryPath + $"/Screenshot2D.png", bytes);


			return image;
		}

		/// <summary>
		/// Creates all the log files when the application quits via function
		/// </summary>
		public void LogAllData()
		{
			if(SceneManager.GetActiveScene().name != "FrontEnd")
			{
				CancelInvoke(nameof(UpdateHeatmap));
				CreateHeatmapLogsOnExit();
			}
		}

		/// <summary>
		/// Loops through all the layers and creates all the log files
		/// </summary>
		private void CreateHeatmapLogsOnExit()
		{
			foreach(KeyValuePair<LayerMask, LayerData> layer in layerData)
			{
				layer.Value.CreateLogData();
			}

			CreateCombinedDataLog();
		}

		/// <summary>
		/// Adds a custom event to record
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="position"></param>
		public void AddHeatmapEvent(string eventName, Vector3 position)
		{
			if(eventPositions == null)
				eventPositions = new Dictionary<string, List<Vector3>>();

			if(!eventPositions.ContainsKey(eventName))
			{
				eventPositions[eventName] = new List<Vector3>();
			}

			eventPositions[eventName].Add(position);
		}

		/// <summary>
		/// Saves the event data to a JSON file
		/// </summary>
		private void SaveHeatmapData()
		{
			List<EventData> eventDatas = new();

			foreach(KeyValuePair<string, List<Vector3>> data in eventPositions)
			{
				string dataKey = data.Key;
				string filename = "/" + dataKey + ".json";
				string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{timeStamp}/DataFiles";

				if(!Directory.Exists(directoryPath))
					Directory.CreateDirectory(directoryPath);

				foreach(Vector3 pos in data.Value)
				{
					eventDatas.Add(new EventData(data.Key, pos.x, pos.y, pos.z));
				}

				SaveToFile(directoryPath + filename, eventDatas);

				eventDatas.Clear();
			}

			eventPositions.Clear();
		}

		/// <summary>
		/// Saves all the settings data to a JSON file
		/// </summary>
		private void SaveSettingsData()
		{
			//Cache Values
			Vector3 heatmapCameraPos = heatmapCamera.transform.position;
			Quaternion heatmapCameraRotation = heatmapCamera.transform.rotation;
			float heatmapCameraOrthSize = heatmapCamera.orthographicSize;

			//Create Settings Data
			settingsData = new(is2DMode, renderTexture.width, renderTexture.height,
				heatmapCameraPos.x, heatmapCameraPos.y, heatmapCameraPos.z,
				heatmapCameraRotation.x, heatmapCameraRotation.y, heatmapCameraRotation.z, heatmapCameraRotation.w,
				heatmapCameraOrthSize);

			string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{timeStamp}/DataFiles";

			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);

			SaveToFile(directoryPath + "/settings.json", settingsData);
		}

		/// <summary>
		/// Saves the data to a JSON file and writes the file to the path
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <param name="data"></param>
		private void SaveToFile<T>(string path, T data)
		{
			string json = JsonConvert.SerializeObject(data, jsonSettings);

			File.WriteAllText(path, json);
		}

		/// <summary>
		/// Loads the data from a path that contains a JSON file
		/// </summary>
		/// <param name="path"></param>
		public void LoadHeatmapDataFromFile(string path)
		{
			List<EventData> eventDatasList = LoadFromFile<List<EventData>>(path);
			EventData.LoadData(eventDatasList);
		}

		/// <summary>
		/// Loads string data that contains json
		/// </summary>
		/// <param name="json"></param>
		public void LoadHeatmapDataFromJson(string json)
		{
			List<EventData> eventDatasList = LoadFromString<List<EventData>>(json);
			EventData.LoadData(eventDatasList);
		}

		/// <summary>
		/// Loads the data from a path that contains a JSON file
		/// </summary>
		/// <param name="json"></param>
		public void LoadSettingDataFromJson(string json)
		{
			settingsData = LoadFromString<SettingsData>(json);
			if(settingsData != null)
			{
				LoadSettingData(settingsData);
			}
		}

		/// <summary>
		/// Loads setting data into the game	
		/// </summary>
		/// <param name="_settingsData"></param>
		private void LoadSettingData(SettingsData _settingsData)
		{
			is2DMode = _settingsData.is2DMode;
			RenderTexture heatmapCameraTargetTexture = new(_settingsData.textureWidth, _settingsData.textureHeight, 0);
			heatmapCamera.targetTexture = heatmapCameraTargetTexture;
			heatmapCamera.transform.position = new Vector3(_settingsData.camPosX, _settingsData.camPosY, _settingsData.camPosZ);
			heatmapCamera.transform.rotation = new Quaternion(_settingsData.camRotX, _settingsData.camRotY, _settingsData.camRotZ, _settingsData.camRotW);
			heatmapCamera.orthographicSize = _settingsData.camOrthSize;
		}

		/// <summary>
		/// Base Load from string function. Loads the data from a string that contains a JSON file and checks
		/// for valid data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <returns></returns>
		public T LoadFromString<T>(string json)
		{
			Type t = typeof(T);

			try
			{
				return JsonConvert.DeserializeObject<T>(json, jsonSettings);
			}
			catch(JsonException e)
			{
				if(t == typeof(SettingsData))
				{
					StartCoroutine(FeedbackUI.FlashMessage("Event File Imported, Please Upload The Settings Data File", 3));
				}
				else if(t == typeof(EventData))
				{
					StartCoroutine(FeedbackUI.FlashMessage("Settings File Imported, Please Upload Event Data File", 3));
				}
				else
				{
					StartCoroutine(FeedbackUI.FlashMessage("Unknown File Type Imported, Please Event or Settings Data File", 3));
				}

				throw;
			}
		}

		/// <summary>
		/// Base Load from file function. Loads the data from a string that contains a JSON file and checks
		/// for valid data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <returns></returns>
		private T LoadFromFile<T>(string path)
		{
			if(File.Exists(path))
			{
				string json = File.ReadAllText(path);

				Type t = typeof(T);

				try
				{
					return JsonConvert.DeserializeObject<T>(json, jsonSettings);
				}
				catch(JsonException e)
				{
					if(t == typeof(SettingsData))
					{
						StartCoroutine(FeedbackUI.FlashMessage("Event File Imported, Please Upload The Settings Data File", 3));
					}
					else if(t == typeof(EventData))
					{
						StartCoroutine(FeedbackUI.FlashMessage("Settings File Imported, Please Upload Event Data File", 3));
					}
					else
					{
						StartCoroutine(FeedbackUI.FlashMessage("Unknown File Type Imported, Please Event or Settings Data File", 3));
					}

					throw;
				}
			}

			Debug.LogError($"File {path} not found");
			return default;
		}

		/// <summary>
		/// Add position data to load into the list
		/// </summary>
		/// <param name="pos"></param>
		public void AddDataToLoadList(Vector3 pos)
		{
			loadedData.Add(pos);
		}

		/// <summary>
		/// Resets the loaded position data
		/// </summary>
		public void ResetLoadedDataList()
		{
			loadedData.Clear();
		}

		/// <summary>
		/// Resets the save settings
		/// </summary>
		public void ResetSaveSettings()
		{
			settingsData = null;
		}

		/// <summary>
		/// Creates the cluster heatmap from the string data and saves it to a file is the hardcoded directory
		/// </summary>
		/// <param name="fileName"></param>
		public async void CreateClusterHeatmapFromData(string fileName)
		{
			SetTimeStamp();

			Vector3[] positionArray = loadedData.ToArray();
			await ClusterHeatMap.CreateClusteredHeatmap(positionArray, tempClusterPointRadius, tempClusterEpsilon, tempClusterMinPoints, is2DMode);
			Texture2D heatmapImage = ClusterHeatMap.GetFinalTexture();
			byte[] bytes = heatmapImage.EncodeToPNG();
			string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{timeStamp}/LoadedDataHeatMap";
			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			File.WriteAllBytes(directoryPath + $"/{fileName}.png", bytes);

			loadedData.Clear();

			savedDirectory = directoryPath + $"/{fileName}.png";
		}

		/// <summary>
		/// Creates the traffic heatmap from the string data and saves it to a file is the hardcoded directory
		/// </summary>
		/// <param name="fileName"></param>
		public async void CreateTrafficHeatmapFromData(string fileName)
		{
			SetTimeStamp();
			Vector3[] positionArray = loadedData.ToArray();
			await TrafficHeatmap.CreateHeatmap(positionArray, tempTrafficPointRadius, is2DMode);
			Texture2D heatmapImage = TrafficHeatmap.GetFinalTexture();
			byte[] bytes = heatmapImage.EncodeToPNG();
			string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/Hydra/HeatmapData/{SceneManager.GetActiveScene().name}/{timeStamp}/LoadedDataHeatMap";
			if(!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			File.WriteAllBytes(directoryPath + $"/{fileName}.png", bytes);

			loadedData.Clear();

			savedDirectory = directoryPath + $"/{fileName}.png";
		}

		/// <summary>
		/// Sets the timestamp at the start of the session
		/// </summary>
		private void SetTimeStamp()
		{
			timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
		}

		/// <summary>
		/// resets all the values to their default values
		/// </summary>
		public void ResetValues()
		{
			tempTrafficPointRadius = trafficPointRadius;
			tempClusterEpsilon = clusterEpsilon;
			tempClusterPointRadius = clusterPointRadius;
			tempClusterMinPoints = clusterMinPoints;
		}

		/// <summary>
		/// Resets the cluster values to their default values
		/// </summary>
		public void ResetClusterValues()
		{
			tempClusterEpsilon = clusterEpsilon;
			tempClusterPointRadius = clusterPointRadius;
			tempClusterMinPoints = clusterMinPoints;
		}

		/// <summary>
		/// Resets the traffic values to their default values
		/// </summary>
		public void ResetTrafficValues()
		{
			tempTrafficPointRadius = trafficPointRadius;
		}

		/// <summary>
		/// Applies the traffic values to the temp values if valid
		/// </summary>
		/// <param name="_inputField"></param>
		public void ApplyInputTraffic(TMP_InputField _inputField)
		{
			try
			{
				tempTrafficPointRadius = int.Parse(_inputField.text);
			}
			catch(Exception e)
			{
				StartCoroutine(FeedbackUI.FlashMessage("Invalid Input. Please Use Numbers", 3));
				throw;
			}
		}

		/// <summary>
		/// Applies the cluster values to the temp values if valid
		/// </summary>
		/// <param name="_inputFields"></param>
		public void ApplyClusterInput(TMP_InputField[] _inputFields)
		{
			try
			{
				tempClusterPointRadius = int.Parse(_inputFields[0].text);
				tempClusterEpsilon = int.Parse(_inputFields[1].text);
				tempClusterMinPoints = int.Parse(_inputFields[2].text);
			}
			catch(Exception e)
			{
				StartCoroutine(FeedbackUI.FlashMessage("Invalid Inputs", 3));

				throw;
			}
		}

		/// <summary>
		/// shows default values in input field
		/// </summary>
		/// <param name="_inputField"></param>
		public void UpdateInputTrafficField(TMP_InputField _inputField)
		{
			_inputField.text = tempTrafficPointRadius.ToString();
		}

		/// <summary>
		/// shows default values in input field
		/// </summary>
		/// <param name="_inputFields"></param>
		public void UpdateClusterTrafficField(TMP_InputField[] _inputFields)
		{
			_inputFields[0].text = tempClusterPointRadius.ToString();
			_inputFields[1].text = tempClusterEpsilon.ToString();
			_inputFields[2].text = tempClusterMinPoints.ToString();
		}

		//Getters

		public bool GetEventPositionsContainsKey(string key) => eventPositions.ContainsKey(key);
		public bool LoadedDataListNotEmpty() => loadedData.Count > 0;
		public bool AreSettingsLoaded() => settingsData != null;
		public string GetTimeStamp() => timeStamp;
		public bool GetIs2DMode() => is2DMode;
		public int GetPointRadius() => clusterPointRadius;
		public float GetEpsilon() => clusterEpsilon;
		public int GetMinPoints() => clusterMinPoints;
		public string GetSavedDirectoryText() => savedDirectory;
	}
}