using System.IO;
using RavenX.Configuration;
using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class ConfigTab : IRavenTab
{
	public string Title => "Config";

	private string _status = string.Empty;

	public void Draw()
	{
		RavenTabHelper.BeginColumns();

		RavenTabHelper.BeginColumn(320f);
		DrawSettingsCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(560f);
		DrawBindingsCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private void DrawSettingsCard()
	{
		using (RavenMenu.Card("Settings"))
		{
			var path = Context.ConfigFile;

			GUILayout.Label(Path.GetFileName(path), RavenTheme.Label);
			GUILayout.Label(Path.GetDirectoryName(path) ?? string.Empty, RavenTheme.MutedLabel);
			GUILayout.Label(File.Exists(path) ? "File present" : "Not saved yet", RavenTheme.MutedLabel);

			RavenWidgets.Spacer(10f);

			GUILayout.BeginHorizontal();
			var save = RavenWidgets.OutlineButton("SAVE", 90f);
			GUILayout.Space(8f);
			var load = RavenWidgets.OutlineButton("LOAD", 90f);
			GUILayout.EndHorizontal();

			if (save)
				_status = ConfigurationManager.Save(path, Context.Features.Value) ? "Saved." : "Save failed.";

			if (load)
				_status = ConfigurationManager.Load(path, Context.Features.Value) ? "Loaded." : "Load failed.";

			if (_status.Length > 0)
			{
				RavenWidgets.Spacer(8f);
				GUILayout.Label(_status, RavenTheme.MutedLabel);
			}
		}
	}

	private static void DrawBindingsCard()
	{
		using (RavenMenu.Card("Key Bindings"))
		{
			GUILayout.Label("Click a bind, then press a key. Escape clears it, right-click cancels.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(8f);

			foreach (var feature in Context.Features.Value)
			{
				if (GetKey(feature) is not { } current)
					continue;

				var bound = RavenWidgets.KeyBind(feature.Name, current, feature);
				if (bound != current)
					SetKey(feature, bound);
			}
		}
	}

	private static KeyCode? GetKey(Feature feature) => feature switch
	{
		ToggleFeature toggle => toggle.Key,
		TriggerFeature trigger => trigger.Key,
		HoldFeature hold => hold.Key,
		_ => null
	};

	private static void SetKey(Feature feature, KeyCode key)
	{
		switch (feature)
		{
			case ToggleFeature toggle:
				toggle.Key = key;
				break;
			case TriggerFeature trigger:
				trigger.Key = key;
				break;
			case HoldFeature hold:
				hold.Key = key;
				break;
		}
	}
}
