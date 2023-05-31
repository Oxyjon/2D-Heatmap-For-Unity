using Manager;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SFB;

using System;

using TMPro;

namespace UI
{
	[RequireComponent(typeof(Button))]
	public class TextFileMultipleOpenButton : MonoBehaviour
	{
		private string storedJson;

		private Button button;

		private HeatmapCreatorFrontEnd parentCreator;


		void Start()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
			parentCreator = GetComponentInParent<HeatmapCreatorFrontEnd>();
		}

		/// <summary>
		/// Opens the panel and adds files to use for heatmap creation.
		/// </summary>
		private void OnClick()
		{
			var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", true);
			if(paths.Length > 0)
			{
				var urlArr = new List<string>(paths.Length);
				for(int i = 0; i < paths.Length; i++)
				{
					urlArr.Add(new Uri(paths[i]).AbsoluteUri);
				}
				StartCoroutine(OutputRoutine(urlArr.ToArray()));
			}
		}

		/// <summary>
		/// Loads all the json data into the heatmap creator.
		/// </summary>
		/// <param name="urlArr"></param>
		/// <returns></returns>
		private IEnumerator OutputRoutine(string[] urlArr)
		{
			string outputText = "";
			for(int i = 0; i < urlArr.Length; i++)
			{
				WWW loader = new WWW(urlArr[i]);
				yield return loader;
				HeatmapManager.Instance.LoadHeatmapDataFromJson(loader.text);
				int lastIndexOccurence = urlArr[i].LastIndexOf('/');
				if(lastIndexOccurence != -1)
				{
					parentCreator.AddFileNameToList(urlArr[i].Substring(lastIndexOccurence + 1));
				}
				outputText += loader.text;
			}

			storedJson = outputText;
		}


		public void ResetButton()
		{
			storedJson = string.Empty;
		}
	}
}