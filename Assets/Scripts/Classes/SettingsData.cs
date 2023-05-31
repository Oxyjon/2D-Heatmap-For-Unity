namespace Classes
{
	[System.Serializable]
	public class SettingsData
	{
		public bool is2DMode;
		public int textureWidth;
		public int textureHeight;
		public float camPosX;
		public float camPosY;
		public float camPosZ;
		public float camRotX;
		public float camRotY;
		public float camRotZ;
		public float camRotW;
		public float camOrthSize;
		
		public SettingsData(bool _is2DMode, int _textureWidth, int _textureHeight, 
			float _camPosX, float _camPosY, float _camPosZ,
			float _camRotX, float _camRotY, float _camRotZ, float _camRotW, float _camOrthSize)
		{
			is2DMode = _is2DMode;
			textureWidth = _textureWidth;
			textureHeight = _textureHeight;
			camPosX = _camPosX;
			camPosY = _camPosY;
			camPosZ = _camPosZ;
			camRotX = _camRotX;
			camRotY = _camRotY;
			camRotZ = _camRotZ;
			camRotW = _camRotW;
			camOrthSize = _camOrthSize;
		}
	}
}