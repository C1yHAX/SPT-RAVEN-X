using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

using Weather = RavenX.Features.Weather;

internal class WorldTab : IRavenTab
{
	public string Title => "World";

	private const float ColumnWidth = 300f;

	public void Draw()
	{
		RavenTabHelper.BeginColumns();

		RavenTabHelper.BeginColumn(ColumnWidth);
		DrawEventsCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(ColumnWidth);
		DrawInteractionCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(ColumnWidth);
		DrawSkyCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static void DrawEventsCard()
	{
		using (RavenMenu.Card("Events"))
		{
			GUILayout.Label("Fires once at your current position.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);

			RavenTabHelper.FeatureTrigger<AirDrop>("Air Drop", "Call");
			RavenTabHelper.FeatureTrigger<Mortar>("Mortar Strike", "Call");
			RavenTabHelper.FeatureTrigger<Train>("Summon Train", "Call");
			RavenTabHelper.FeatureTrigger<Weather>("Clear Weather", "Apply");
		}
	}

	private static void DrawSkyCard()
	{
		using (RavenMenu.Card("Time & Weather"))
		{
			var weather = FeatureFactory.GetFeature<Weather>();
			if (weather == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			weather.Hour = RavenWidgets.Slider("Time Of Day", weather.Hour, 0f, 24f, FormatHour(weather.Hour));
			RavenWidgets.Spacer(4f);

			if (RavenWidgets.OutlineButton("SET TIME", 110f))
				weather.ApplyTime();

			RavenWidgets.Spacer(10f);

			weather.CloudDensity = RavenWidgets.Slider("Clouds", weather.CloudDensity, -1f, 1f, $"{weather.CloudDensity:0.00}");
			RavenWidgets.Spacer(6f);
			weather.Fog = RavenWidgets.Slider("Fog", weather.Fog, 0f, 0.5f, $"{weather.Fog:0.000}");
			RavenWidgets.Spacer(6f);
			weather.Rain = RavenWidgets.Slider("Rain", weather.Rain, 0f, 1f, $"{weather.Rain:0.00}");
			RavenWidgets.Spacer(6f);
			weather.Wind = RavenWidgets.Slider("Wind", weather.Wind, 0f, 1f, $"{weather.Wind:0.00}");
			RavenWidgets.Spacer(6f);
			weather.Thunder = RavenWidgets.Slider("Thunder", weather.Thunder, 0f, 1f, $"{weather.Thunder:0.00}");

			RavenWidgets.Spacer(8f);

			GUILayout.BeginHorizontal();
			if (RavenWidgets.OutlineButton("APPLY", 100f))
				weather.ApplyWeather();

			GUILayout.Space(8f);

			if (RavenWidgets.OutlineButton("CLEAR", 100f))
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
		using (RavenMenu.Card("Interaction"))
		{
			RavenTabHelper.FeatureTrigger<WorldInteractiveObjects>("Open Doors & Readers", "Open");
			RavenWidgets.Spacer(6f);

			var interact = FeatureFactory.GetFeature<Interact>();
			if (interact == null)
				return;

			interact.Enabled = RavenWidgets.SwitchRow(interact.Enabled, "Custom Reach");
			RavenWidgets.Spacer(4f);
			interact.Distance = RavenWidgets.Slider("Distance", interact.Distance, 0.5f, 20f, $"{interact.Distance:0.#}m");
		}
	}
}
