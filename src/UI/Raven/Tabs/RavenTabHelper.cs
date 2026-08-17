using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal static class RavenTabHelper
{
	public static void FeatureSwitch<T>(string label) where T : ToggleFeature
	{
		var feature = FeatureFactory.GetFeature<T>();
		if (feature == null)
		{
			GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
			GUILayout.Label(label, RavenTheme.MutedLabel, GUILayout.ExpandWidth(true));
			GUILayout.Label("n/a", RavenTheme.ValueLabel, GUILayout.Width(40f));
			GUILayout.EndHorizontal();
			return;
		}

		feature.Enabled = RavenWidgets.SwitchRow(feature.Enabled, label);
	}

	public static void FeatureCheckbox<T>(string label) where T : ToggleFeature
	{
		var feature = FeatureFactory.GetFeature<T>();
		if (feature == null)
		{
			GUILayout.Label(label, RavenTheme.MutedLabel, GUILayout.Height(RavenTheme.RowHeight));
			return;
		}

		feature.Enabled = RavenWidgets.Checkbox(feature.Enabled, label);
	}

	public static void FeatureTrigger<T>(string label, string caption = "Run") where T : TriggerFeature
	{
		var feature = FeatureFactory.GetFeature<T>();

		GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight + 6f));
		GUILayout.Label(label, feature == null ? RavenTheme.MutedLabel : RavenTheme.Label, GUILayout.ExpandWidth(true));

		if (feature == null)
		{
			GUILayout.Label("n/a", RavenTheme.ValueLabel, GUILayout.Width(40f));
			GUILayout.EndHorizontal();
			return;
		}

		if (feature.Key != KeyCode.None)
			GUILayout.Label(feature.Key.ToString(), RavenTheme.ValueLabel, GUILayout.Width(70f));

		var fire = RavenWidgets.OutlineButton(caption, 58f);
		GUILayout.EndHorizontal();

		if (fire)
			feature.Trigger();
	}

	public static void KeyRow(string label, KeyCode key)
	{
		GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
		GUILayout.Label(label, RavenTheme.Label, GUILayout.ExpandWidth(true));
		GUILayout.Label(key == KeyCode.None ? "unbound" : key.ToString(), RavenTheme.ValueLabel, GUILayout.Width(90f));
		GUILayout.EndHorizontal();
	}

	private const float ColumnGap = 12f;

	private static float _rowUsed;
	private static bool _rowOpen;

	public static void BeginColumns()
	{
		_rowUsed = 0f;
		_rowOpen = false;
		GUILayout.BeginVertical();
	}

	public static void EndColumns()
	{
		CloseRow();
		GUILayout.EndVertical();
	}

	public static void BeginColumn(float width)
	{
		var available = RavenMenu.ContentWidth;

		if (_rowOpen && _rowUsed + width > available)
			CloseRow();

		if (!_rowOpen)
		{
			GUILayout.BeginHorizontal();
			_rowOpen = true;
			_rowUsed = 0f;
		}

		_rowUsed += width + ColumnGap;
		GUILayout.BeginVertical(GUILayout.Width(width));
	}

	public static void EndColumn()
	{
		GUILayout.EndVertical();
		GUILayout.Space(ColumnGap);
	}

	private static void CloseRow()
	{
		if (!_rowOpen)
			return;

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		_rowOpen = false;
		_rowUsed = 0f;
	}
}
