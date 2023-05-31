using Manager;

using SFB;

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;


namespace UI
{
	[RequireComponent(typeof(Button))]
	public class TextFileSingleOpenButton : MonoBehaviour
	{
		private string output;
		
		private HeatmapCreatorFrontEnd parentCreator;
		void Start()
		{
			Button button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
			parentCreator = GetComponentInParent<HeatmapCreatorFrontEnd>();
		}
		/// <summary>
		/// Opens the panel and adds files to use for heatmap creation.
		/// </summary>
		private void OnClick()
		{
			string[] paths = StandaloneFileBrowser.OpenFilePanel("Title", "", "json", false);
			if(paths.Length > 0)
			{
				StartCoroutine(OutputRoutine(new Uri(paths[0]).AbsoluteUri));
			}
		}

		/// <summary>
		/// Loads all the json data into the heatmap creator.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		[Obsolete("Obsolete")]
		private IEnumerator OutputRoutine(string url)
		{
			var loader = new WWW(url);
			yield return loader;
			output = loader.text;
			HeatmapManager.Instance.LoadSettingDataFromJson(output);
			parentCreator.ToggleSettingsImage(true);
		}
	}
}