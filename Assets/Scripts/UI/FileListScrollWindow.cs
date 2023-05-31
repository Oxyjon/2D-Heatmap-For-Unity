using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace UI
{
	public class FileListScrollWindow : MonoBehaviour
	{
		[SerializeField] private Transform scrollContent;

		[SerializeField] private GameObject fileContainerPrefab;

		/// <summary>
		/// Creates a new container item for the given file.
		/// </summary>
		/// <param name="_filenames"></param>
		public void AddItemToList(List<string> _filenames)
		{
			foreach(string file in _filenames)
			{
				GameObject confirmation = Instantiate(fileContainerPrefab, scrollContent);

				TextMeshProUGUI textMesh = confirmation.GetComponentInChildren<TextMeshProUGUI>();

				textMesh.text = file;
			}
		}
		
		/// <summary>
		/// Removes all items from the container
		/// </summary>
		public void RemoveAllChildren()
		{
			foreach(Transform child in scrollContent)
			{
				Destroy(child.gameObject);
			}
		}

		/// <summary>
		/// toggles the window on
		/// </summary>
		public void DisplayWindow()
		{
			gameObject.SetActive(true);
		}
		

	}
}