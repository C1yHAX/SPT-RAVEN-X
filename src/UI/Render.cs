using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI;

public static class Render
{
	private static GUIStyle? _stringStyle = null;
	private static int _styleFontSize = -1;

	public static int FontSize { get; set; } = 0;
	public static float OutlineThickness { get; set; } = 0f;
	public static Color OutlineColor { get; set; } = new(0f, 0f, 0f, 0.85f);

	private static GUIStyle StringStyle
	{
		get
		{
			if (_stringStyle != null && _styleFontSize == FontSize)
				return _stringStyle;

			_stringStyle = new GUIStyle(GUI.skin.label);

			if (FontSize > 0)
				_stringStyle.fontSize = FontSize;

			_styleFontSize = FontSize;
			return _stringStyle;
		}
	}

	public static Vector2 ScreenCenter => new(Screen.width / 2f, Screen.height / 2f);

	public static bool DrawingMenu { get; set; }

	public static Rect MenuArea { get; set; }

	private static int _inventoryFrame = -1;
	private static bool _inventoryOpen;

	private static bool InventoryOpen
	{
		get
		{
			if (_inventoryFrame == Time.frameCount)
				return _inventoryOpen;

			_inventoryFrame = Time.frameCount;

			var player = Features.GameState.Current?.LocalPlayer;
			_inventoryOpen = player != null && player.IsInventoryOpened;

			return _inventoryOpen;
		}
	}

	private static bool Behind(Rect rect)
	{
		if (DrawingMenu)
			return false;

		if (InventoryOpen)
			return true;

		var area = MenuArea;
		return area.width > 0f && area.Overlaps(rect);
	}

	public static Color Color
	{
		get { return GUI.color; }
		set { GUI.color = value; }
	}

	public static Vector2 DrawString(Vector2 position, string label, Color color, bool centered = true)
	{
		Color = color;
		return DrawString(position, label, centered);
	}

	public static void GetContentAndSize(string label, out GUIContent content, out Vector2 size)
	{
		content = new GUIContent(label);
		size = StringStyle.CalcSize(content);
	}

	public static Vector2 DrawString(Vector2 position, string label, bool centered = true)
	{
		GetContentAndSize(label, out var content, out var size);
		var upperLeft = centered ? position - size / 2f : position;
		var rect = new Rect(upperLeft, size);

		if (Behind(rect))
			return size;

		if (OutlineThickness > 0f)
		{
			var fill = Color;
			Color = OutlineColor;

			var offset = OutlineThickness;
			GUI.Label(new Rect(rect.x - offset, rect.y, rect.width, rect.height), content, StringStyle);
			GUI.Label(new Rect(rect.x + offset, rect.y, rect.width, rect.height), content, StringStyle);
			GUI.Label(new Rect(rect.x, rect.y - offset, rect.width, rect.height), content, StringStyle);
			GUI.Label(new Rect(rect.x, rect.y + offset, rect.width, rect.height), content, StringStyle);

			Color = fill;
		}

		GUI.Label(rect, content, StringStyle);
		return size;
	}

	public static void DrawCrosshair(Vector2 position, float size, Color color, float thickness)
	{
		var rect = new Rect(position.x - size, position.y - size, size * 2f + thickness, size * 2f + thickness);
		if (Behind(rect))
			return;

		Color = color;
		var texture = Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(position.x - size, position.y, size * 2 + thickness, thickness), texture);
		GUI.DrawTexture(new Rect(position.x, position.y - size, thickness, size * 2 + thickness), texture);
	}

	public static void DrawFilledBox(float x, float y, float w, float h, Color color)
	{
		var rect = new Rect(x, y, w, h);
		if (Behind(rect))
			return;

		Color = color;
		GUI.DrawTexture(rect, Texture2D.whiteTexture);
	}

	public static void DrawPlayer(Vector2 position, float size, Color color, float thickness)
	{
		var forward = new Vector2(position.x, position.y - size * 2.5f);
		DrawCircle(position, size, color, thickness, 8);
		DrawLine(position, forward, thickness, color);
		DrawLine(new Vector2(position.x - size / 2, position.y - size * 1.25f), forward, thickness, color);
		DrawLine(new Vector2(position.x + size / 2, position.y - size * 1.25f), forward, thickness, color);
	}

	public static void DrawBox(float x, float y, float w, float h, float thickness, Color color)
	{
		if (Behind(new Rect(x, y, w + thickness, h + thickness)))
			return;

		Color = color;
		var texture = Texture2D.whiteTexture;
		GUI.DrawTexture(new Rect(x, y, w + thickness, thickness), texture);
		GUI.DrawTexture(new Rect(x, y, thickness, h + thickness), texture);
		GUI.DrawTexture(new Rect(x + w, y, thickness, h + thickness), texture);
		GUI.DrawTexture(new Rect(x, y + h, w + thickness, thickness), texture);
	}

	public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, float thickness, Color color)
	{
		if (Behind(Rect.MinMaxRect(
				Mathf.Min(lineStart.x, lineEnd.x), Mathf.Min(lineStart.y, lineEnd.y),
				Mathf.Max(lineStart.x, lineEnd.x), Mathf.Max(lineStart.y, lineEnd.y))))
			return;

		Color = color;

		var vector = lineEnd - lineStart;
		float pivot = Mathf.Rad2Deg * Mathf.Atan(vector.y / vector.x);
		if (vector.x < 0f)
			pivot += 180f;

		thickness = Mathf.Max(thickness, 1f);
		int yOffset = (int)Mathf.Ceil(thickness / 2);

		GUIUtility.RotateAroundPivot(pivot, lineStart);
		GUI.DrawTexture(new Rect(lineStart.x, lineStart.y - yOffset, vector.magnitude, thickness), Texture2D.whiteTexture);
		GUIUtility.RotateAroundPivot(-pivot, lineStart);
	}

	public static void DrawCircle(Vector2 center, float radius, Color color, float width, int segmentsPerQuarter)
	{
		int totalSegments = segmentsPerQuarter * 4;
		float step = 1f / totalSegments;
		var lastV = center + new Vector2(radius, 0);

		for (int i = 1; i <= totalSegments; ++i)
		{
			float t = i * step;
			var currentV = center + new Vector2(
				radius * Mathf.Cos(2 * Mathf.PI * t),
				radius * Mathf.Sin(2 * Mathf.PI * t)
			);
			DrawLine(lastV, currentV, width, color);
			lastV = currentV;
		}
	}
}
