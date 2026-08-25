using System.Collections.Generic;
using System.Linq;
using RavenX.UI.Raven.Tabs;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven;

public interface IRavenTab
{
	string Title { get; }
	void Draw();
}

public class RavenMenu
{
	public const string Version = "v1.3.0";

	private readonly List<IRavenTab> _tabs = [];
	private Rect _window = new(90, 60, 980, 660);
	private string? _tabError;
	private string? _pendingTabError;
	private bool _hasPendingTabError;
	private Vector2 _scroll;
	private int _index;
	private bool _dragging;
	private Vector2 _dragOffset;

	public IReadOnlyList<IRavenTab> Tabs => _tabs;

	public int SelectedIndex
	{
		get => _index;
		set => _index = Mathf.Clamp(value, 0, Mathf.Max(0, _tabs.Count - 1));
	}

	public void Register(IRavenTab tab)
	{
		_tabs.Add(tab);
	}

	public void Clear()
	{
		_tabs.Clear();
		_index = 0;
	}

	public void Draw()
	{
		RavenTheme.EnsureBuilt();
		RavenWidgets.BeginFrame();

		var previousDepth = GUI.depth;
		var previousColor = GUI.color;
		var previousContentColor = GUI.contentColor;
		var previousBackgroundColor = GUI.backgroundColor;
		var previousEnabled = GUI.enabled;
		Render.DrawingMenu = true;

		try
		{
			GUI.depth = -1000;

			if (Event.current.type == EventType.Layout && _hasPendingSize)
			{
				_window.width = _pendingSize.x;
				_window.height = _pendingSize.y;
				_hasPendingSize = false;
			}

			ClampWindowToScreen();
			HandleDrag();
			ClampWindowToScreen();

			Render.MenuArea = _window;
			GUI.Box(_window, GUIContent.none, RavenTheme.Window);

			HandleResize();
			DrawHeader();
			DrawTabs();
			ClampWindowToScreen();
			Render.MenuArea = _window;
			DrawContent();
		}
		finally
		{
			RavenTabHelper.ForceClose();

			try
			{
				RavenWidgets.EndFrame();
			}
			finally
			{
				Render.DrawingMenu = false;
				GUI.depth = previousDepth;
				GUI.color = previousColor;
				GUI.contentColor = previousContentColor;
				GUI.backgroundColor = previousBackgroundColor;
				GUI.enabled = previousEnabled;
			}
		}
	}

	public void OnClosed()
	{
		RavenWidgets.ResetInteraction();
		_dragging = false;
		_resizing = false;
		_hasPendingSize = false;
	}

	private bool _resizing;
	private Vector2 _resizeStart;
	private Vector2 _sizeStart;
	private Vector2 _pendingSize;
	private bool _hasPendingSize;

	private const float MinWidth = 420f;
	private const float MinHeight = 300f;
	private const float MinContentHeight = 90f;
	private const float GripSize = 18f;

	private float MinimumHeight => MinHeight + (_tabsHeight - RavenTheme.TabHeight);

	private void ClampWindowToScreen()
	{
		var maxWidth = Mathf.Max(240f, Screen.width - 16f);
		var maxHeight = Mathf.Max(200f, Screen.height - 16f);
		var minWidth = Mathf.Min(MinWidth, maxWidth);
		var minHeight = Mathf.Min(MinimumHeight, maxHeight);

		_window.width = Mathf.Clamp(_window.width, minWidth, maxWidth);
		_window.height = Mathf.Clamp(_window.height, minHeight, maxHeight);
		_window.x = Mathf.Clamp(_window.x, 8f, Mathf.Max(8f, Screen.width - _window.width - 8f));
		_window.y = Mathf.Clamp(_window.y, 8f, Mathf.Max(8f, Screen.height - _window.height - 8f));
	}

	private void HandleResize()
	{
		var e = Event.current;
		var grip = new Rect(_window.xMax - GripSize, _window.yMax - GripSize, GripSize, GripSize);

		switch (e.type)
		{
			case EventType.MouseDown when e.button == 0 && grip.Contains(e.mousePosition):
				_resizing = true;
				_resizeStart = e.mousePosition;
				_sizeStart = new Vector2(_window.width, _window.height);
				e.Use();
				break;

			case EventType.MouseDrag when _resizing:
				var delta = e.mousePosition - _resizeStart;
				var maxWidth = Mathf.Max(Mathf.Min(MinWidth, Screen.width - 16f), Screen.width - _window.x - 8f);
				var maxHeight = Mathf.Max(Mathf.Min(MinimumHeight, Screen.height - 16f), Screen.height - _window.y - 8f);
				_pendingSize = new Vector2(
					Mathf.Clamp(_sizeStart.x + delta.x, Mathf.Min(MinWidth, maxWidth), maxWidth),
					Mathf.Clamp(_sizeStart.y + delta.y, Mathf.Min(MinimumHeight, maxHeight), maxHeight));
				_hasPendingSize = true;
				e.Use();
				break;

			case EventType.MouseUp when _resizing:
				_resizing = false;
				e.Use();
				break;
		}

		if (Event.current.type != EventType.Repaint)
			return;

		var c = grip.center;
		for (var i = 0; i < 3; i++)
		{
			var o = 4f + i * 4f;
			Render.DrawLine(new Vector2(c.x + 6f - o, c.y + 6f), new Vector2(c.x + 6f, c.y + 6f - o), 1.2f, RavenTheme.TextSecondary);
		}

		GUI.color = Color.white;
	}

	private void HandleDrag()
	{
		if (_resizing)
			return;

		var e = Event.current;
		var grip = new Rect(_window.x, _window.y, _window.width, RavenTheme.HeaderHeight);

		switch (e.type)
		{
			case EventType.MouseDown when e.button == 0 && grip.Contains(e.mousePosition):
				_dragging = true;
				_dragOffset = e.mousePosition - new Vector2(_window.x, _window.y);
				e.Use();
				break;

			case EventType.MouseDrag when _dragging:
				_window.x = e.mousePosition.x - _dragOffset.x;
				_window.y = e.mousePosition.y - _dragOffset.y;
				e.Use();
				break;

			case EventType.MouseUp when _dragging:
				_dragging = false;
				e.Use();
				break;
		}

	}

	private void DrawHeader()
	{
		var header = new Rect(_window.x, _window.y, _window.width, RavenTheme.HeaderHeight);

		DrawLogo(new Rect(header.x + 20f, header.y + 12f, 38f, 38f));

		const string head = "RAVEN-";
		var headWidth = RavenTheme.Title.CalcSize(new GUIContent(head)).x;
		GUI.Label(new Rect(header.x + 66f, header.y + 10f, 320f, 28f), head, RavenTheme.Title);
		GUI.Label(new Rect(header.x + 66f + headWidth, header.y + 10f, 60f, 28f), "X", RavenTheme.TitleAccent);
		GUI.Label(new Rect(header.x + 68f, header.y + 34f, 320f, 14f), "TACTICAL SYSTEM", RavenTheme.Subtitle);

		var toggle = new Rect(header.xMax - 116f, header.y + 11f, 90f, 26f);
		RavenWidgets.Rounded(toggle, RavenTheme.ControlRadius, new Color(0f, 0f, 0f, 0f), RavenTheme.Accent);
		GUI.Label(toggle, "INSERT", RavenTheme.OutlineButton);
		GUI.Label(new Rect(toggle.x, toggle.yMax + 1f, 90f, 14f), "Toggle Menu", RavenTheme.SubtitleCentered);
	}

	private static void DrawLogo(Rect rect)
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var logo = RavenLogo.Texture;
		if (logo == null)
			return;

		var previous = GUI.color;
		GUI.color = RavenTheme.Accent;
		GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);
		GUI.color = previous;
	}

	private void DrawTabs()
	{
		var area = new Rect(_window.x + 18f, _window.y + RavenTheme.HeaderHeight, _window.width - 36f, RavenTheme.TabHeight);
		var titles = _tabs.Select(t => t.Title.ToUpperInvariant()).ToArray();

		if (titles.Length == 0)
			return;

		_index = Mathf.Clamp(_index, 0, titles.Length - 1);
		_index = RavenWidgets.TabBar(area, titles, _index, out var rows);

		_tabsHeight = RavenTheme.TabHeight * Mathf.Max(1, rows);
	}

	private float _tabsHeight = RavenTheme.TabHeight;

	private void DrawContent()
	{
		var top = _window.y + RavenTheme.HeaderHeight + _tabsHeight;

		var height = Mathf.Max(MinContentHeight, _window.height - (top - _window.y) - 42f);
		var content = new Rect(_window.x + 18f, top + 12f, _window.width - 36f, height);

		ContentWidth = Mathf.Max(1f, content.width - 20f);
		RavenWidgets.LayoutOrigin = new Vector2(content.x - _scroll.x, content.y - _scroll.y);

		if (Event.current.type == EventType.Layout && _hasPendingTabError)
		{
			_tabError = _pendingTabError;
			_hasPendingTabError = false;
		}

		var areaOpen = false;
		var scrollOpen = false;
		try
		{
			GUILayout.BeginArea(content);
			areaOpen = true;
			_scroll = GUILayout.BeginScrollView(_scroll, false, true);
			scrollOpen = true;

			if (_tabError != null)
				GUILayout.Label(_tabError, RavenTheme.MutedLabel);

			if (_index >= 0 && _index < _tabs.Count)
				DrawTab(_tabs[_index]);
		}
		finally
		{
			if (scrollOpen)
				GUILayout.EndScrollView();

			if (areaOpen)
				GUILayout.EndArea();
		}

		var footer = new Rect(_window.x + 22f, _window.yMax - 26f, _window.width - 44f, 16f);
		RavenWidgets.StatusDot(new Rect(footer.x + 96f, footer.y, 6f, footer.height), RavenTheme.Online);
		GUI.Label(footer, $"RAVEN-X {Version}  |", RavenTheme.Subtitle);
		GUI.Label(new Rect(footer.x + 108f, footer.y, 200f, footer.height), "EFT | ONLINE", RavenTheme.Subtitle);
	}

	private void DrawTab(IRavenTab tab)
	{
		try
		{
			tab.Draw();

			if (Event.current.type == EventType.Layout && _tabError != null && !_hasPendingTabError)
				QueueTabError(null);
		}
		catch (System.Exception ex)
		{
			QueueTabError($"{tab.Title} could not be drawn: {ex.Message}");
			RavenTabHelper.ForceClose();
		}
	}

	private void QueueTabError(string? message)
	{
		_pendingTabError = message;
		_hasPendingTabError = true;
	}

	internal static float ContentWidth { get; private set; } = 900f;

	public static CardScope Card(string title, float width = 0f)
	{
		return new CardScope(title, width);
	}

	public readonly struct CardScope : System.IDisposable
	{
		public CardScope(string title, float width)
		{
			if (width > 0f)
				GUILayout.BeginVertical(RavenTheme.Panel, GUILayout.Width(width));
			else
				GUILayout.BeginVertical(RavenTheme.Panel);

			if (!string.IsNullOrEmpty(title))
				RavenWidgets.Section(title);
		}

		public void Dispose()
		{
			GUILayout.EndVertical();
			GUILayout.Space(10f);
		}
	}
}
