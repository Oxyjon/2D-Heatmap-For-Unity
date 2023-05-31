using System;
using System.Collections;

using TMPro;

using UnityEngine;

namespace UI
{
	public class FeedbackUI : MonoBehaviour
	{
		private static TextMeshProUGUI feedbackTextGUI;

		private void Awake()
		{
			feedbackTextGUI = GetComponent<TextMeshProUGUI>();
		}

		/// <summary>
		/// Sets the feedback text
		/// </summary>
		/// <param name="message"></param>
		public static void SetText(string message)
		{
			feedbackTextGUI.text = message;
		}
		public static IEnumerator FlashMessage(string _message, int _seconds)
		{
			SetText($"{_message}");
			yield return new WaitForSeconds(_seconds);
			SetText("");
		}

		/// <summary>
		/// Toggle the feedback text
		/// </summary>
		/// <param name="_status"></param>
		public static void ToggleUIVisibility(bool _status)
		{
			feedbackTextGUI.gameObject.SetActive(_status);
		}
	}
}