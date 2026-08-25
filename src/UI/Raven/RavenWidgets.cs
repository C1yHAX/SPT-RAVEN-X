using System;
using System.Collections.Generic;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven;

public static class RavenWidgets
{
	private static readonly List<Action> _overlays = [];
	private static readonly List<Action> _nextLayout = [];
	private static readonly HashSet<int> _pendingClicks = [];
	private static readonly Dictionary<int, object> _pendingValues = [];
	private static readonly Dictionary<object, int> _dropdownPicks = [];
	private static GUIStyle? _scratch;
	private static object? _openDropdown;
	private static GUIStyle? _rowStyle;
	private static GUIStyle? _rowStyleMuted;
	private static bool _drawingDropdown;

	public static Vector2 LayoutOrigin { get; set; } = Vector2.zero;

	public static GUIStyle RowLabel(bool highlighted)
	{
		if (highlighted)
			return _rowStyle ??= new GUIStyle(RavenTheme.Label) { wordWrap = false, clipping = TextClipping.Clip };

		return _rowStyleMuted ??= new GUIStyle(RavenTheme.MutedLabel) { wordWrap = false, clipping = TextClipping.Clip };
	}

	public static void BeginFrame()
	{
		_overlays.Clear();

		if (Event.current.type != EventType.Layout || _nextLayout.Count == 0)
			return;

		var actions = _nextLayout.ToArray();
		_nextLayout.Clear();

		foreach (var action in actions)
			action();
	}

	public static void EndFrame()
	{
		for (var i = 0; i < _overlays.Count; i++)
			_overlays[i]();

		_overlays.Clear();

		if (Event.current.type != EventType.Layout)
			return;

		_pendingClicks.Clear();
		_pendingValues.Clear();
	}

	public static void RunNextLayout(Action action)
	{
		_nextLayout.Add(action);
	}

	public static void CloseDropdowns()
	{
		_openDropdown = null;
	}

	public static void ResetInteraction()
	{
		_openDropdown = null;
		_capturing = null;
		_overlays.Clear();
		_nextLayout.Clear();
		_pendingClicks.Clear();
		_pendingValues.Clear();
		_dropdownPicks.Clear();
		GUIUtility.hotControl = 0;
		GUIUtility.keyboardControl = 0;
	}

	public static void Fill(Rect rect, Color color)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var previous = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, RavenTheme.White);
		GUI.color = previous;
	}

	public static void Rounded(Rect rect, float radius, Color fill, Color border, float borderWidth = 1f)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var slice = Mathf.CeilToInt(radius) + 1;
		_scratch ??= new GUIStyle();
		_scratch.normal.background = RavenTheme.RoundedTexture(radius, fill, border, borderWidth);
		_scratch.border = new RectOffset(slice, slice, slice, slice);
		_scratch.Draw(rect, GUIContent.none, false, false, false, false);
	}

	private static bool Clicked(Rect rect, int id, bool allowWithDropdown = false)
	{
		var e = Event.current;

		if (e.type == EventType.Layout)
			return _pendingClicks.Remove(id);

		if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition))
			return false;

		if (_openDropdown != null && !_drawingDropdown && !allowWithDropdown)
			return false;

		_pendingClicks.Add(id);
		e.Use();
		return false;
	}

	public static bool Click(Rect rect)
	{
		return Clicked(rect, GUIUtility.GetControlID(FocusType.Passive));
	}

	private static bool TryTakeValue<T>(int id, ref T value)
	{
		if (Event.current.type != EventType.Layout || !_pendingValues.TryGetValue(id, out var pending) || pending is not T next)
			return false;

		_pendingValues.Remove(id);
		value = next;
		return true;
	}

	private static bool Hovered(Rect rect)
	{
		return rect.Contains(Event.current.mousePosition);
	}

	public static void Section(string title)
	{
		GUILayout.Label(title.ToUpperInvariant(), RavenTheme.SectionLabel);
	}

	public static void Spacer(float height = 8f)
	{
		GUILayout.Space(height);
	}

	public static bool Checkbox(bool value, string label, Color? swatch = null)
	{
		var rect = GUILayoutUtility.GetRect(GUIContent.none, RavenTheme.Label, GUILayout.Height(RavenTheme.RowHeight));
		var id = GUIUtility.GetControlID(FocusType.Passive);

		const float switchWidth = 30f;
		const float switchHeight = 15f;

		var toggle = new Rect(rect.x, rect.y + (rect.height - switchHeight) / 2f, switchWidth, switchHeight);

		if (Clicked(rect, id))
			value = !value;

		DrawSwitch(toggle, value);

		var textX = toggle.xMax + 9f;

		if (swatch.HasValue)
		{
			var chip = new Rect(textX, rect.y + (rect.height - 13f) / 2f, 13f, 13f);
			Rounded(chip, 2f, swatch.Value, swatch.Value);
			textX = chip.xMax + 9f;
		}

		GUI.Label(new Rect(textX, rect.y, rect.width - textX + rect.x, rect.height), label, RavenTheme.Label);
		return value;
	}

	public static bool Switch(bool value, float width = 34f, float height = 18f)
	{
		var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
		return SwitchAt(rect, value);
	}

	public static bool SwitchAt(Rect rect, bool value)
	{
		var id = GUIUtility.GetControlID(FocusType.Passive);

		if (Clicked(rect, id))
			value = !value;

		DrawSwitch(rect, value);
		return value;
	}

	public static void DrawSwitch(Rect rect, bool value)
	{
		var radius = rect.height / 2f;
		Rounded(rect, radius, value ? RavenTheme.Accent : RavenTheme.ControlBackground, value ? RavenTheme.Accent : RavenTheme.ControlBorder);

		var inset = 2.5f;
		var diameter = rect.height - inset * 2f;
		var knobX = value ? rect.xMax - diameter - inset : rect.x + inset;
		Rounded(new Rect(knobX, rect.y + inset, diameter, diameter), diameter / 2f, Color.white, Color.white);
	}

	public static bool SwitchRow(bool value, string label)
	{
		GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
		GUILayout.Label(label, RavenTheme.Label, GUILayout.ExpandWidth(true));
		var result = Switch(value);
		GUILayout.EndHorizontal();
		return result;
	}

	public static float Slider(string label, float value, float min, float max, string? display = null)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, RavenTheme.Label, GUILayout.ExpandWidth(true));
		GUILayout.Label(display ?? value.ToString("0.##"), RavenTheme.ValueLabel, GUILayout.Width(70f));
		GUILayout.EndHorizontal();

		var rect = GUILayoutUtility.GetRect(GUIContent.none, RavenTheme.Label, GUILayout.Height(16f));
		return SliderAt(new Rect(rect.x, rect.y + 6f, rect.width, 4f), value, min, max);
	}

	private static float SliderAt(Rect track, float value, float min, float max)
	{
		var id = GUIUtility.GetControlID(FocusType.Passive);
		var e = Event.current;
		var hit = new Rect(track.x, track.y - 8f, track.width, track.height + 16f);
		TryTakeValue(id, ref value);

		if (_openDropdown == null && e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition))
		{
			GUIUtility.hotControl = id;
			e.Use();
		}
		else if (e.type == EventType.MouseUp && GUIUtility.hotControl == id)
		{
			GUIUtility.hotControl = 0;
			e.Use();
		}

		if (GUIUtility.hotControl == id && (e.type is EventType.MouseDrag or EventType.MouseDown))
		{
			var t = Mathf.Clamp01((e.mousePosition.x - track.x) / Mathf.Max(1f, track.width));
			_pendingValues[id] = Mathf.Lerp(min, max, t);
			e.Use();
		}

		var normalized = max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f;

		Rounded(track, track.height / 2f, RavenTheme.AccentTrack, RavenTheme.AccentTrack);
		if (normalized > 0f)
			Rounded(new Rect(track.x, track.y, track.width * normalized, track.height), track.height / 2f, RavenTheme.Accent, RavenTheme.Accent);

		var knob = new Rect(track.x + track.width * normalized - 6f, track.center.y - 6f, 12f, 12f);
		Rounded(knob, 6f, Color.white, Color.white);

		return value;
	}

	public static int Dropdown(string label, int index, string[] options, object key)
	{
		GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight + 6f));
		GUILayout.Label(label, RavenTheme.Label, GUILayout.ExpandWidth(true));

		var rect = GUILayoutUtility.GetRect(140f, 26f, GUILayout.Width(140f), GUILayout.Height(26f));
		GUILayout.EndHorizontal();

		if (Event.current.type == EventType.Layout && _dropdownPicks.TryGetValue(key, out var picked))
		{
			_dropdownPicks.Remove(key);
			index = picked;
		}

		var isOpen = ReferenceEquals(_openDropdown, key);
		var id = GUIUtility.GetControlID(FocusType.Passive);

		if (Clicked(rect, id, isOpen))
		{
			_openDropdown = isOpen ? null : key;
			isOpen = !isOpen;
		}

		Rounded(rect, RavenTheme.ControlRadius, RavenTheme.ControlBackground, isOpen ? RavenTheme.Accent : RavenTheme.ControlBorder);

		var current = index >= 0 && index < options.Length ? options[index] : string.Empty;
		GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 30f, rect.height), current, RavenTheme.Label);
		DrawChevron(new Rect(rect.xMax - 20f, rect.y, 12f, rect.height), isOpen);

		if (!isOpen)
			return index;

		var captured = new Rect(rect.x + LayoutOrigin.x, rect.y + LayoutOrigin.y, rect.width, rect.height);
		var selected = index;
		_overlays.Add(() =>
		{
			var choice = DrawDropdownList(captured, options, selected);
			if (choice < 0)
				return;

			_dropdownPicks[key] = choice;
			_openDropdown = null;
		});

		return index;
	}

	private static int DrawDropdownList(Rect anchor, string[] options, int index)
	{
		const float itemHeight = 24f;

		var height = options.Length * itemHeight + 6f;
		var top = anchor.yMax + 3f;

		if (top + height > Screen.height)
			top = Mathf.Max(0f, anchor.y - 3f - height);

		var list = new Rect(anchor.x, top, anchor.width, height);

		Rounded(list, RavenTheme.ControlRadius, RavenTheme.ControlBackground, RavenTheme.Accent);

		var picked = -1;
		_drawingDropdown = true;

		try
		{
			for (var i = 0; i < options.Length; i++)
			{
				var item = new Rect(list.x + 3f, list.y + 3f + i * itemHeight, list.width - 6f, itemHeight);
				var id = GUIUtility.GetControlID(FocusType.Passive);

				if (Hovered(item))
					Rounded(item, 3f, RavenTheme.AccentTrack, RavenTheme.AccentTrack);

				GUI.Label(new Rect(item.x + 7f, item.y, item.width - 14f, item.height), options[i], RowLabel(i == index));

				if (Clicked(item, id, true))
					picked = i;
			}
		}
		finally
		{
			_drawingDropdown = false;
		}

		if (Event.current.type == EventType.MouseDown && !list.Contains(Event.current.mousePosition) && !anchor.Contains(Event.current.mousePosition))
		{
			_openDropdown = null;
			Event.current.Use();
		}

		return picked;
	}

	private static void DrawChevron(Rect rect, bool up)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var c = rect.center;
		var dy = up ? -2f : 2f;
		Render.DrawLine(new Vector2(c.x - 4f, c.y - dy), new Vector2(c.x, c.y + dy), 1.5f, RavenTheme.TextSecondary);
		Render.DrawLine(new Vector2(c.x, c.y + dy), new Vector2(c.x + 4f, c.y - dy), 1.5f, RavenTheme.TextSecondary);
		GUI.color = Color.white;
	}

	public static string TextField(string value, string placeholder = "", float height = 26f)
	{
		var rect = GUILayoutUtility.GetRect(GUIContent.none, RavenTheme.Label, GUILayout.Height(height));
		var id = GUIUtility.GetControlID(FocusType.Passive);
		TryTakeValue(id, ref value);
		Rounded(rect, RavenTheme.ControlRadius, RavenTheme.ControlBackground, RavenTheme.ControlBorder);

		var inner = new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height);
		var previousEnabled = GUI.enabled;
		if (_openDropdown != null && Event.current.type == EventType.MouseDown)
			GUI.enabled = false;

		var result = GUI.TextField(inner, value, RavenTheme.TextInput);
		GUI.enabled = previousEnabled;

		if (Event.current.type != EventType.Layout && result != value)
			_pendingValues[id] = result;

		if (placeholder.Length > 0 && value.Length == 0)
			GUI.Label(inner, placeholder, RavenTheme.MutedLabel);

		return value;
	}

	public static bool SmallButton(string caption, float width = 54f)
	{
		var rect = GUILayoutUtility.GetRect(width, 20f, GUILayout.Width(width), GUILayout.Height(20f));
		var clicked = Clicked(rect, GUIUtility.GetControlID(FocusType.Passive));

		Rounded(rect, 3f, Hovered(rect) ? RavenTheme.AccentTrack : RavenTheme.ControlBackground, RavenTheme.ControlBorder);
		GUI.Label(rect, caption, RavenTheme.SmallButton);

		return clicked;
	}

	private static object? _capturing;

	public static bool IsCapturingKey => _capturing != null;

	public static KeyCode KeyBind(string label, KeyCode current, object key)
	{
		GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight + 4f));
		GUILayout.Label(label, RavenTheme.Label, GUILayout.ExpandWidth(true));

		var rect = GUILayoutUtility.GetRect(104f, 24f, GUILayout.Width(104f), GUILayout.Height(24f));
		GUILayout.EndHorizontal();
		var id = GUIUtility.GetControlID(FocusType.Passive);
		TryTakeValue(id, ref current);

		var listening = ReferenceEquals(_capturing, key);

		if (Clicked(rect, id))
		{
			_capturing = listening ? null : key;
			listening = !listening;
		}

		if (listening)
		{
			var e = Event.current;

			if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
			{
				_pendingValues[id] = e.keyCode == KeyCode.Escape ? KeyCode.None : e.keyCode;
				_capturing = null;
				listening = false;
				e.Use();
			}
			else if (e.type == EventType.MouseDown && e.button == 1)
			{
				_capturing = null;
				listening = false;
				e.Use();
			}
		}

		Rounded(rect, RavenTheme.ControlRadius, RavenTheme.ControlBackground, listening ? RavenTheme.Accent : RavenTheme.ControlBorder);

		var caption = listening ? "press a key" : current == KeyCode.None ? "unbound" : current.ToString();
		GUI.Label(rect, caption, listening ? RavenTheme.Label : RavenTheme.MutedLabel);

		return current;
	}

	public static void CancelKeyCapture()
	{
		_capturing = null;
	}

	public static int SubTabBar(string[] labels, int index)
	{
		if (labels.Length == 0)
			return index;

		var row = GUILayoutUtility.GetRect(GUIContent.none, RavenTheme.Label, GUILayout.Height(30f), GUILayout.ExpandWidth(true));
		var width = row.width / labels.Length;
		var previous = GUI.contentColor;

		for (var i = 0; i < labels.Length; i++)
		{
			var rect = new Rect(row.x + i * width + 2f, row.y, width - 4f, row.height);
			var active = i == index;
			var id = GUIUtility.GetControlID(FocusType.Passive);

			if (Clicked(rect, id))
				index = i;

			Rounded(rect, RavenTheme.ControlRadius,
				active ? RavenTheme.AccentTrack : RavenTheme.ControlBackground,
				active ? RavenTheme.Accent : RavenTheme.ControlBorder);

			GUI.contentColor = active ? Color.white : Hovered(rect) ? RavenTheme.TextPrimary : RavenTheme.TextSecondary;
			GUI.Label(rect, labels[i], RavenTheme.SubTab);
		}

		GUI.contentColor = previous;
		Spacer(10f);

		return index;
	}

	public static int TabBar(Rect area, string[] tabs, int index, out int rows)
	{
		var x = area.x;
		var y = area.y;

		rows = 1;

		var previous = GUI.contentColor;

		for (var i = 0; i < tabs.Length; i++)
		{
			var content = new GUIContent(tabs[i]);

			var width = Mathf.Ceil(RavenTheme.Tab.CalcSize(content).x) + 2f;

			if (x > area.x && x + width > area.xMax)
			{
				x = area.x;
				y += area.height;
				rows++;
			}

			var rect = new Rect(x, y, width, area.height);
			var id = GUIUtility.GetControlID(FocusType.Passive);

			if (Clicked(rect, id))
				index = i;

			GUI.contentColor = i == index
				? Color.white
				: Hovered(rect) ? RavenTheme.TextPrimary : RavenTheme.TextSecondary;

			GUI.Label(rect, content, RavenTheme.Tab);

			if (i == index)
				Fill(new Rect(rect.x + 6f, rect.yMax - 2f, rect.width - 12f, 2f), RavenTheme.Accent);

			x += width;
		}

		GUI.contentColor = previous;

		Fill(new Rect(area.x, y + area.height - 1f, area.width, 1f), RavenTheme.PanelBorder);
		return index;
	}

	public static void StatusDot(Rect rect, Color color)
	{
		Rounded(new Rect(rect.x, rect.center.y - 3f, 6f, 6f), 3f, color, color);
	}

	public static bool OutlineButton(string caption, float width = 78f)
	{
		var rect = GUILayoutUtility.GetRect(width, 28f, GUILayout.Width(width), GUILayout.Height(28f));
		var clicked = Clicked(rect, GUIUtility.GetControlID(FocusType.Passive));

		Rounded(rect, RavenTheme.ControlRadius, new Color(0f, 0f, 0f, 0f), Hovered(rect) ? RavenTheme.AccentHover : RavenTheme.Accent);
		GUI.Label(rect, caption, RavenTheme.OutlineButton);

		return clicked;
	}

	public static bool LayoutButton(string caption, GUIStyle style, params GUILayoutOption[] options)
	{
		var id = GUIUtility.GetControlID(FocusType.Passive);
		var clicked = Event.current.type == EventType.Layout && _pendingClicks.Remove(id);
		var previousEnabled = GUI.enabled;

		if (_openDropdown != null && Event.current.type == EventType.MouseDown)
			GUI.enabled = false;

		if (GUILayout.Button(caption, style, options))
			_pendingClicks.Add(id);

		GUI.enabled = previousEnabled;

		return clicked;
	}
}
