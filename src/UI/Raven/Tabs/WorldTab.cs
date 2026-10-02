using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

using Weather = RavenX.Features.Weather;

internal class WorldTab : IRavenTab
{
	public string Title => RavenText.L("World");

	public void Draw()
	{
		RavenTabHelper.BeginColumns(3);

		RavenTabHelper.BeginColumn();
		DrawEventsCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawInteractionCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawSkyCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static void DrawEventsCard()
	{
		using (RavenMenu.Card(RavenText.L("Events")))
		{
			GUILayout.Label(RavenText.L("Fires once at your current position."), RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);

			RavenTabHelper.FeatureTrigger<AirDrop>(RavenText.L("Air Drop"), RavenText.L("Call"));
			RavenTabHelper.FeatureTrigger<Mortar>(RavenText.L("Mortar Strike"), RavenText.L("Call"));
			RavenTabHelper.FeatureTrigger<Train>(RavenText.L("Summon Train"), RavenText.L("Call"));
			RavenTabHelper.FeatureTrigger<Weather>(RavenText.L("Clear Weather"), RavenText.L("Apply"));
		}
	}

	private static void DrawSkyCard()
	{
		using (RavenMenu.Card(RavenText.L("Time & Weather")))
		{
			var weather = FeatureFactory.GetFeature<Weather>();
			if (weather == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			weather.Hour = RavenWidgets.Slider(RavenText.L("Time Of Day"), weather.Hour, 0f, 24f, FormatHour(weather.Hour));
			RavenWidgets.Spacer(4f);

			if (RavenWidgets.OutlineButton(RavenText.L("SET TIME"), 110f))
				weather.ApplyTime();

			RavenWidgets.Spacer(10f);

			weather.CloudDensity = RavenWidgets.Slider(RavenText.L("Clouds"), weather.CloudDensity, -1f, 1f, $"{weather.CloudDensity:0.00}");
			RavenWidgets.Spacer(6f);
			weather.Fog = RavenWidgets.Slider(RavenText.L("Fog"), weather.Fog, 0f, 0.5f, $"{weather.Fog:0.000}");
			RavenWidgets.Spacer(6f);
			weather.Rain = RavenWidgets.Slider(RavenText.L("Rain"), weather.Rain, 0f, 1f, $"{weather.Rain:0.00}");
			RavenWidgets.Spacer(6f);
			weather.Wind = RavenWidgets.Slider(RavenText.L("Wind"), weather.Wind, 0f, 1f, $"{weather.Wind:0.00}");
			RavenWidgets.Spacer(6f);
			weather.Thunder = RavenWidgets.Slider(RavenText.L("Thunder"), weather.Thunder, 0f, 1f, $"{weather.Thunder:0.00}");

			RavenWidgets.Spacer(8f);

			GUILayout.BeginHorizontal();
			if (RavenWidgets.OutlineButton(RavenText.L("APPLY"), 100f))
				weather.ApplyWeather();

			GUILayout.Space(8f);

			if (RavenWidgets.OutlineButton(RavenText.L("CLEAR"), 100f))
				weather.Trigger();

			GUILayout.EndHorizontal();
		}
	}

	private static string FormatHour(float hour)
	{
		var h = Mathf.FloorToInt(hour) % 24;
		var m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
		return $"{h:00}:{m:00}";
	}

	private static void DrawInteractionCard()
	{
		using (RavenMenu.Card(RavenText.L("Interaction")))
		{
			RavenTabHelper.FeatureTrigger<WorldInteractiveObjects>(RavenText.L("Open Doors & Readers"), RavenText.L("Open"));
			RavenWidgets.Spacer(6f);

			var interact = FeatureFactory.GetFeature<Interact>();
			if (interact == null)
				return;

			interact.Enabled = RavenWidgets.SwitchRow(interact.Enabled, RavenText.L("Custom Reach"));
			RavenWidgets.Spacer(4f);
			interact.Distance = RavenWidgets.Slider(RavenText.L("Distance"), interact.Distance, 0.5f, 20f, $"{interact.Distance:0.#}m");
		}
	}
}
