using Classes;

using Manager;

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;


namespace UI
{
	public class HeatmapCreatorFrontEnd : MonoBehaviour
	{
		//Properties
		private bool isCreatingHeatmap;
		private bool isTrafficHeatmap;

		//Cluster
		[SerializeField] private GameObject clusterPanel;
		[SerializeField] private TMP_InputField[] clusterInputFields;

		[SerializeField] private Toggle clusterToggle;

		//Traffic
		[SerializeField] private GameObject trafficPanel;
		[SerializeField] private TMP_InputField trafficInputField;

		[SerializeField] private Toggle trafficToggle;

		//Files
		private List<string> fileNames = new();
		[SerializeField] private TextMeshProUGUI fileContainerGui;

		[SerializeField] private TMP_InputField fileNameInput;

		//Progress
		[SerializeField] private TextMeshProUGUI progressText;
		[SerializeField] private Button submitButton;
		[SerializeField] private Button clearButton;
		[SerializeField] private TextFileMultipleOpenButton uploadButton;
		[SerializeField] private TextFileSingleOpenButton settingsButton;
		[SerializeField] private Image settingsImage;
		[SerializeField] private Image progressImage;
		private readonly float loadingRotationSpeed = 100;
		private FileListScrollWindow fileListScrollWindow;

		private void Awake()
		{
			clusterInputFields = clusterPanel.GetComponentsInChildren<TMP_InputField>();
			trafficInputField = trafficPanel.GetComponentInChildren<TMP_InputField>();
			fileListScrollWindow = GetComponentInChildren<FileListScrollWindow>();
			FeedbackUI.SetText("");
		}

		private void Start()
		{
			submitButton.onClick.AddListener(OnButtonSubmit);
			clearButton.onClick.AddListener(OnButtonClear);

			HeatmapManager.Instance.UpdateInputTrafficField(trafficInputField);
			HeatmapManager.Instance.UpdateClusterTrafficField(clusterInputFields);
			HeatmapManager.Instance.ResetValues();
			trafficToggle.onValueChanged.AddListener((_) => Toggle());
			clusterToggle.onValueChanged.AddListener((_) => Toggle());

			Toggle();
			OnButtonClear();
			ClearFileNames();
			progressImage.gameObject.SetActive(false);
		}

		private void Update()
		{
			if(isCreatingHeatmap)
			{
				if(isTrafficHeatmap)
				{
					FeedbackUI.SetText("Processing Data.. Please Wait");
					progressText.text = TrafficHeatmap.GetCurrentPointCount + "/" + TrafficHeatmap.GetTotalPointsCount;
					progressImage.transform.RotateAround(Vector3.zero, new Vector3(0, 0, -1), Time.deltaTime * loadingRotationSpeed);
					if(TrafficHeatmap.GetCurrentPointCount == TrafficHeatmap.GetTotalPointsCount)
					{
						OnButtonClear();
						StartCoroutine(FeedbackUI.FlashMessage($"Completed! \n {HeatmapManager.Instance.GetSavedDirectoryText()}", 3));
						isCreatingHeatmap = false;
						submitButton.interactable = true;
						progressText.gameObject.SetActive(false);
					}
				}
				else
				{
					FeedbackUI.SetText("Processing Data.. Please Wait");
					progressText.text = ClusterHeatMap.GetCurrentPointCount + "/" + ClusterHeatMap.GetTotalPointsCount;
					progressImage.transform.RotateAround(Vector3.zero, new Vector3(0, 0, -1), Time.deltaTime * loadingRotationSpeed);
					if(ClusterHeatMap.GetCurrentPointCount == ClusterHeatMap.GetTotalPointsCount)
					{
						OnButtonClear();
						StartCoroutine(FeedbackUI.FlashMessage($"Completed! \n {HeatmapManager.Instance.GetSavedDirectoryText()}", 3));
						isCreatingHeatmap = false;
						submitButton.interactable = true;
						progressText.gameObject.SetActive(false);
					}
				}
			}
			else
			{
				submitButton.interactable = true;
				progressText.gameObject.SetActive(false);
			}
		}
		
		/// <summary>
		/// Toggles being the two versions of the heatmap
		/// </summary>
		private void Toggle()
		{
			isTrafficHeatmap = !isTrafficHeatmap;

			if(isTrafficHeatmap)
			{
				clusterToggle.SetIsOnWithoutNotify(false);
				trafficToggle.SetIsOnWithoutNotify(true);
				trafficPanel.SetActive(true);
				clusterPanel.SetActive(false);
			}
			else
			{
				clusterToggle.SetIsOnWithoutNotify(true);
				trafficToggle.SetIsOnWithoutNotify(false);
				trafficPanel.SetActive(false);
				clusterPanel.SetActive(true);
			}
		}

		/// <summary>
		/// Creates the heatmap based on the input fields and type
		/// </summary>
		private void OnButtonSubmit()
		{
			if(HeatmapManager.Instance.AreSettingsLoaded())
			{
				if(HeatmapManager.Instance.LoadedDataListNotEmpty())
				{
					if(fileNameInput.text != "")
					{
						if(isTrafficHeatmap)
						{
							HeatmapManager.Instance.ApplyInputTraffic(trafficInputField);
							HeatmapManager.Instance.CreateTrafficHeatmapFromData(fileNameInput.text);
						}
						else
						{
							HeatmapManager.Instance.ApplyClusterInput(clusterInputFields);
							HeatmapManager.Instance.CreateClusterHeatmapFromData(fileNameInput.text);
						}

						isCreatingHeatmap = true;
						submitButton.interactable = false;
						progressText.gameObject.SetActive(true);
						progressImage.gameObject.SetActive(true);
					}
					else
					{
						FeedbackUI.SetText("File Name is blank. Please Name The File");
					}
				}
				else
				{
					FeedbackUI.SetText("No Data For Heatmap Creation. Please Upload A File");
				}
			}
			else
			{
				FeedbackUI.SetText("No Settings For Heatmap Creation. Please Upload Settings");
			}
		}

		/// <summary>
		/// Clears all the input fields and data being held
		/// </summary>
		private void OnButtonClear()
		{
			HeatmapManager.Instance.ResetLoadedDataList();
			ClearFileNames();

			if(isTrafficHeatmap)
			{
				HeatmapManager.Instance.ResetTrafficValues();
				HeatmapManager.Instance.UpdateInputTrafficField(trafficInputField);
			}
			else
			{
				HeatmapManager.Instance.ResetClusterValues();
				HeatmapManager.Instance.UpdateClusterTrafficField(clusterInputFields);
			}

			uploadButton.ResetButton();
			HeatmapManager.Instance.ResetSaveSettings();
			FeedbackUI.SetText("");
			ToggleSettingsImage(false);
		}

		/// <summary>
		/// Adds a name to the file name list and create a container to hold the name
		/// </summary>
		/// <param name="_filename"></param>
		public void AddFileNameToList(string _filename)
		{
			fileListScrollWindow.RemoveAllChildren();
			fileNames.Add(_filename);

			fileListScrollWindow.AddItemToList(fileNames);
		}

		/// <summary>
		/// Clears the file name list and the container that holds the names
		/// </summary>
		private void ClearFileNames()
		{
			fileListScrollWindow.RemoveAllChildren();
			fileNames.Clear();
		}

		/// <summary>
		/// Toggles the settings image on and off
		/// </summary>
		/// <param name="_status"></param>
		public void ToggleSettingsImage(bool _status)
		{
			settingsImage.gameObject.SetActive(_status);
		}
		
		
	}
}