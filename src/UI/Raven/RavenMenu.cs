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
	public const string Version = "v1.0.0";

	private readonly List<IRavenTab> _tabs = [];
	private Rect _window = new(90, 60, 980, 660);
	private static string? _tabError;
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

		// Claim the area for this frame and mark ourselves exempt, so the overlays skip
		// whatever would land on the window while our own grip still draws.
		Render.MenuArea = _window;
		Render.DrawingMenu = true;
		GUI.depth = 0;

		// The new size is taken here and nowhere else. Writing it while the mouse drags
		// would leave the repaint pass laying out against a different width than the
		// layout pass used, and the column wrap would then open a row in one pass and
		// not the other.
		if (Event.current.type == EventType.Layout && _hasPendingSize)
		{
			_window.width = _pendingSize.x;
			_window.height = _pendingSize.y;
			_hasPendingSize = false;
		}

		HandleDrag();

		GUI.Box(_window, GUIContent.none, RavenTheme.Window);

		HandleResize();

		DrawHeader();
		DrawTabs();
		DrawContent();

		RavenWidgets.EndFrame();

		Render.DrawingMenu = false;
	}

	public void OnClosed()
	{
		RavenWidgets.CloseDropdowns();
		RavenWidgets.CancelKeyCapture();
		_dragging = false;
		_resizing = false;
	}

	private bool _resizing;
	private Vector2 _resizeStart;
	private Vector2 _sizeStart;
	private Vector2 _pendingSize;
	private bool _hasPendingSize;

	private const float MinWidth = 520f;
	private const float MinHeight = 320f;
	private const float MinContentHeight = 90f;
	private const float GripSize = 18f;

	// Every extra tab row pushes the content down, so the floor has to move with it.
	private float MinimumHeight => MinHeight + (_tabsHeight - RavenTheme.TabHeight);

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
				_pendingSize = new Vector2(
					Mathf.Clamp(_sizeStart.x + delta.x, MinWidth, Screen.width - _window.x - 8f),
					Mathf.Clamp(_sizeStart.y + delta.y, MinimumHeight, Screen.height - _window.y - 8f));
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

		_window.x = Mathf.Clamp(_window.x, -_window.width + 120f, Screen.width - 120f);
		_window.y = Mathf.Clamp(_window.y, 0f, Screen.height - RavenTheme.HeaderHeight);
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

		// Floored: a tab bar wrapped onto several rows eats into the height, and a
		// negative area would take the scroll view with it.
		var height = Mathf.Max(MinContentHeight, _window.height - (top - _window.y) - 42f);
		var content = new Rect(_window.x + 18f, top + 12f, _window.width - 36f, height);

		ContentWidth = content.width - 20f;
		RavenWidgets.LayoutOrigin = new Vector2(content.x - _scroll.x, content.y - _scroll.y);

		GUILayout.BeginArea(content);
		_scroll = GUILayout.BeginScrollView(_scroll, false, true);

		try
		{
			if (_tabError != null)
				GUILayout.Label(_tabError, RavenTheme.MutedLabel);

			if (_index >= 0 && _index < _tabs.Count)
				DrawTab(_tabs[_index]);

			GUILayout.EndScrollView();
		}
		catch (System.Exception)
		{
			// Never leave the area open: the clip would stay pushed for every later frame.
		}
		finally
		{
			GUILayout.EndArea();
		}

		var footer = new Rect(_window.x + 22f, _window.yMax - 26f, _window.width - 44f, 16f);
		RavenWidgets.StatusDot(new Rect(footer.x + 96f, footer.y, 6f, footer.height), RavenTheme.Online);
		GUI.Label(footer, $"RAVEN-X {Version}  |", RavenTheme.Subtitle);
		GUI.Label(new Rect(footer.x + 108f, footer.y, 200f, footer.height), "EFT | ONLINE", RavenTheme.Subtitle);
	}

	// A tab that throws mid-layout leaves the IMGUI layout stack unbalanced, which
	// takes the whole window down with it — including the tab bar, so the menu can
	// no longer be closed or switched away from. Keep the damage inside one tab.
	private static void DrawTab(IRavenTab tab)
	{
		try
		{
			tab.Draw();
			_tabError = null;
		}
		catch (System.Exception ex)
		{
			// Drawing anything here would add a control the layout pass never counted,
			// which unbalances IMGUI just as badly as the original failure. Record it
			// and let the next frame render the message from the start.
			_tabError = $"{tab.Title} could not be drawn: {ex.Message}";
			RavenTabHelper.ForceClose();
		}
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
