using System.Collections.Generic;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven;

public static class RavenTheme
{

	public static readonly Color WindowBackground = Hex(0x12141A, 0.97f);
	public static readonly Color PanelBackground = Hex(0x171A21);
	public static readonly Color PanelBorder = Hex(0x23262F);
	public static readonly Color HeaderBackground = Hex(0x14161C);
	public static readonly Color ControlBackground = Hex(0x1E212A);
	public static readonly Color ControlBorder = Hex(0x2C303B);

	public static readonly Color Accent = Hex(0x7B5CFA);
	public static readonly Color AccentHover = Hex(0x8F74FF);
	public static readonly Color AccentTrack = Hex(0x2A2740);

	public static readonly Color TextPrimary = Hex(0xE8EAED);
	public static readonly Color TextSecondary = Hex(0x8B909A);
	public static readonly Color TextSection = Hex(0x9AA0AA);
	public static readonly Color Online = Hex(0x4ADE80);

	public const float PanelRadius = 6f;
	public const float ControlRadius = 4f;
	public const float RowHeight = 24f;
	public const float PanelPadding = 14f;
	public const float TabHeight = 38f;
	public const float HeaderHeight = 62f;

	public static GUIStyle Window { get; private set; } = null!;
	public static GUIStyle Panel { get; private set; } = null!;
	public static GUIStyle Control { get; private set; } = null!;
	public static GUIStyle Title { get; private set; } = null!;
	public static GUIStyle Subtitle { get; private set; } = null!;
	public static GUIStyle SectionLabel { get; private set; } = null!;
	public static GUIStyle Label { get; private set; } = null!;
	public static GUIStyle MutedLabel { get; private set; } = null!;
	public static GUIStyle ValueLabel { get; private set; } = null!;
	public static GUIStyle TitleAccent { get; private set; } = null!;
	public static GUIStyle TextInput { get; private set; } = null!;
	public static GUIStyle SmallButton { get; private set; } = null!;
	public static GUIStyle SubtitleCentered { get; private set; } = null!;
	public static GUIStyle Tab { get; private set; } = null!;
	public static GUIStyle SubTab { get; private set; } = null!;
	public static GUIStyle OutlineButton { get; private set; } = null!;
	public static GUIStyle DropdownButton { get; private set; } = null!;

	private static bool _built;
	private static readonly List<Texture2D> _textures = [];
	private static readonly Dictionary<long, Texture2D> _roundedCache = [];

	public static void EnsureBuilt()
	{
		if (_built)
			return;

		var panel = RoundedTexture(PanelRadius, PanelBackground, PanelBorder);
		var control = RoundedTexture(ControlRadius, ControlBackground, ControlBorder);

		Window = new GUIStyle(GUI.skin.box)
		{
			normal = { background = RoundedTexture(PanelRadius + 2f, WindowBackground, PanelBorder) },
			border = Slice(PanelRadius + 2f),
			padding = new RectOffset(0, 0, 0, 0),
			margin = new RectOffset(0, 0, 0, 0)
		};

		Panel = new GUIStyle
		{
			normal = { background = panel },
			border = Slice(PanelRadius),
			padding = new RectOffset((int)PanelPadding, (int)PanelPadding, (int)PanelPadding, (int)PanelPadding)
		};

		Control = new GUIStyle
		{
			normal = { background = control },
			border = Slice(ControlRadius),
			padding = new RectOffset(8, 8, 4, 4)
		};

		Title = new GUIStyle(GUI.skin.label)
		{
			fontSize = 22,
			fontStyle = FontStyle.Bold,
			normal = { textColor = TextPrimary },
			padding = new RectOffset(0, 0, 0, 0)
		};

		Subtitle = new GUIStyle(GUI.skin.label)
		{
			fontSize = 10,
			normal = { textColor = TextSecondary },
			padding = new RectOffset(0, 0, 0, 0)
		};

		SectionLabel = new GUIStyle(GUI.skin.label)
		{
			fontSize = 11,
			fontStyle = FontStyle.Bold,
			normal = { textColor = TextSection },
			padding = new RectOffset(0, 0, 0, 6)
		};

		Label = new GUIStyle(GUI.skin.label)
		{
			fontSize = 12,
			normal = { textColor = TextPrimary },
			padding = new RectOffset(0, 0, 0, 0)
		};

		MutedLabel = new GUIStyle(Label) { normal = { textColor = TextSecondary } };

		ValueLabel = new GUIStyle(Label)
		{
			alignment = TextAnchor.MiddleRight,
			normal = { textColor = TextSecondary }
		};

		TextInput = new GUIStyle(GUI.skin.textField)
		{
			fontSize = 12,
			normal = { background = null, textColor = TextPrimary },
			focused = { background = null, textColor = TextPrimary },
			hover = { background = null, textColor = TextPrimary },
			active = { background = null, textColor = TextPrimary },
			border = new RectOffset(0, 0, 0, 0),
			padding = new RectOffset(0, 0, 0, 0),
			margin = new RectOffset(0, 0, 0, 0)
		};

		SmallButton = new GUIStyle(GUI.skin.label)
		{
			fontSize = 11,
			alignment = TextAnchor.MiddleCenter,
			normal = { textColor = TextSecondary },
			padding = new RectOffset(0, 0, 0, 0)
		};

		SubTab = new GUIStyle(GUI.skin.label)
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleCenter,
			wordWrap = false,
			clipping = TextClipping.Overflow,
			normal = { textColor = Color.white }
		};

		TitleAccent = new GUIStyle(Title) { normal = { textColor = Accent } };
		SubtitleCentered = new GUIStyle(Subtitle) { alignment = TextAnchor.MiddleCenter };

		Tab = new GUIStyle(GUI.skin.label)
		{
			fontSize = 12,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter,
			wordWrap = false,
			clipping = TextClipping.Overflow,
			normal = { textColor = Color.white },
			padding = new RectOffset(14, 14, 0, 0)
		};

		OutlineButton = new GUIStyle(GUI.skin.label)
		{
			fontSize = 11,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter,
			normal = { background = RoundedTexture(ControlRadius, new Color(0f, 0f, 0f, 0f), Accent), textColor = TextPrimary },
			border = Slice(ControlRadius),
			padding = new RectOffset(10, 10, 6, 6)
		};

		DropdownButton = new GUIStyle(GUI.skin.label)
		{
			fontSize = 12,
			alignment = TextAnchor.MiddleLeft,
			normal = { background = control, textColor = TextPrimary },
			border = Slice(ControlRadius),
			padding = new RectOffset(10, 22, 0, 0)
		};

		_built = true;
	}

	public static Texture2D White => Texture2D.whiteTexture;

	private static RectOffset Slice(float radius)
	{
		var b = Mathf.CeilToInt(radius) + 1;
		return new RectOffset(b, b, b, b);
	}

	public static Texture2D RoundedTexture(float radius, Color fill, Color border, float borderWidth = 1f)
	{

		var key = CacheKey(radius, fill, border, borderWidth);
		if (_roundedCache.TryGetValue(key, out var cached) && cached != null)
			return cached;

		var texture = BuildRoundedTexture(radius, fill, border, borderWidth);
		_roundedCache[key] = texture;
		return texture;
	}

	private static long CacheKey(float radius, Color fill, Color border, float borderWidth)
	{
		static long Q(float value, float scale) => (long)Mathf.RoundToInt(value * scale);

		var hash = 17L;
		hash = hash * 31 + Q(radius, 4f);
		hash = hash * 31 + Q(borderWidth, 4f);
		hash = hash * 31 + Q(fill.r, 255f); hash = hash * 31 + Q(fill.g, 255f);
		hash = hash * 31 + Q(fill.b, 255f); hash = hash * 31 + Q(fill.a, 255f);
		hash = hash * 31 + Q(border.r, 255f); hash = hash * 31 + Q(border.g, 255f);
		hash = hash * 31 + Q(border.b, 255f); hash = hash * 31 + Q(border.a, 255f);
		return hash;
	}

	private static Texture2D BuildRoundedTexture(float radius, Color fill, Color border, float borderWidth)
	{
		var r = Mathf.Max(1f, radius);
		var size = Mathf.CeilToInt(r * 2f) + 3;
		var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
		{
			hideFlags = HideFlags.HideAndDontSave,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};

		var center = (size - 1) / 2f;
		var pixels = new Color[size * size];

		for (var y = 0; y < size; y++)
		{
			for (var x = 0; x < size; x++)
			{

				var dx = Mathf.Abs(x - center) - (center - r);
				var dy = Mathf.Abs(y - center) - (center - r);
				var distance = dx <= 0f || dy <= 0f
					? Mathf.Max(Mathf.Max(dx, dy) - r, -r)
					: Mathf.Sqrt(dx * dx + dy * dy) - r;

				var inside = Mathf.Clamp01(0.5f - distance);
				var onBorder = borderWidth > 0f ? Mathf.Clamp01(distance + borderWidth + 0.5f) * inside : 0f;

				var color = Color.Lerp(fill, border, onBorder);
				color.a *= inside;
				pixels[y * size + x] = color;
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();

		_textures.Add(texture);
		return texture;
	}

	public static void Reset()
	{
		foreach (var texture in _textures)
		{
			if (texture != null)
				Object.Destroy(texture);
		}

		_textures.Clear();
		_roundedCache.Clear();
		_built = false;
	}

	private static Color Hex(int rgb, float alpha = 1f)
	{
		return new Color(
			((rgb >> 16) & 0xFF) / 255f,
			((rgb >> 8) & 0xFF) / 255f,
			(rgb & 0xFF) / 255f,
			alpha);
	}
}
