using System.IO;
using System.Linq;
using RavenX.ConsoleCommands;
using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class MiscTab : IRavenTab
{
	public string Title => "Misc";

	private string _status = string.Empty;

	public void Draw()
	{
		RavenTabHelper.BeginColumns();

		RavenTabHelper.BeginColumn(300f);
		DrawDiagnosticsCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(300f);
		DrawActiveCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private void DrawDiagnosticsCard()
	{
		using (RavenMenu.Card("Diagnostics"))
		{
			GUILayout.Label("Writes scene and object dumps for analysis.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(8f);

			if (RavenWidgets.OutlineButton("DUMP", 90f))
			{

				new Dump().Execute();
				_status = $"Written to {Path.Combine(Context.UserPath, "Dumps")}";
			}

			if (_status.Length > 0)
			{
				RavenWidgets.Spacer(8f);
				GUILayout.Label(_status, RavenTheme.MutedLabel);
			}
		}
	}

	private static void DrawActiveCard()
	{
		using (RavenMenu.Card("Active Features"))
		{
			var toggles = Context.ToggleableFeatures.Value
				.Where(f => f.Enabled)
				.OrderBy(f => f.Name)
				.ToArray();

			if (toggles.Length == 0)
			{
				GUILayout.Label("Nothing enabled.", RavenTheme.MutedLabel);
				return;
			}

			foreach (var feature in toggles)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
				GUILayout.Label(feature.Name, RavenTheme.Label, GUILayout.ExpandWidth(true));
				GUILayout.Label("on", RavenTheme.ValueLabel, GUILayout.Width(40f));
				GUILayout.EndHorizontal();
			}
		}
	}
}
