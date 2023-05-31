using Manager;

using System.Collections.Generic;

using UnityEngine;

namespace Classes
{
	[System.Serializable]
	public class EventData
	{
		public float xPos;
		public float yPos;
		public float zPos;
		public string eventName;

		public EventData(string _eventName, float _xPos, float _yPos, float _zPos)
		{
			eventName = _eventName;
			xPos = _xPos;
			yPos = _yPos;
			zPos = _zPos;
		}

		/// <summary>
		/// Loads Event data into the provided EventData object List
		/// </summary>
		/// <param name="_eventData"></param>
		public static void LoadData(List<EventData> _eventData)
		{
			foreach(EventData data in _eventData)
			{
				HeatmapManager.Instance.AddDataToLoadList(new Vector3(data.xPos, data.yPos, data.zPos));
			}
		}
	}
}