namespace FigmaImporter.Editor
{
	public static class ColorUtils
	{
		public static UnityEngine.Color ConvertToUnityColor(Color color)
		{
			return new UnityEngine.Color(color.r, color.g, color.b, color.a);
		}
	}
}