using Manager;

using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace Editor
{
	public class HeatmapCameraOverviewWindow : EditorWindow
	{
		#if UNITY_EDITOR
		private Camera heatmapCamera;
		private HeatmapManager heatmapManager;

		[MenuItem("Hydra/HeatmapCamera")]
		
		//Default function to show the editor window
		private static void ShowWindow()
		{
			var window = GetWindow<HeatmapCameraOverviewWindow>();
			window.titleContent = new GUIContent("HeatmapCameraView");
			window.Show();
		}

		//Gets manager and camera and displays what it's seeing in the editor window
		private void OnGUI()
		{
			heatmapManager = FindObjectOfType<HeatmapManager>();
			heatmapCamera = heatmapManager.GetComponentInChildren<Camera>();

			if(heatmapCamera != null)
			{
				heatmapCamera.Render();

				Rect rect = GUILayoutUtility.GetRect(heatmapCamera.pixelWidth, heatmapCamera.pixelHeight);
				
				Rect newRect = new Rect(rect.x, rect.y, heatmapCamera.pixelWidth, heatmapCamera.pixelHeight);
				EditorGUI.DrawPreviewTexture(newRect, heatmapCamera.targetTexture);
			}
		}
		
		//Updates the window
		private void Update()
		{
			Repaint();
		}
		
		#endif
	}
}