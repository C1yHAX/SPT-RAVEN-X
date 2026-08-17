using RavenX.Properties;
using EFT.Weather;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Weather : TriggerFeature
{
	public override string Name => Strings.FeatureWeatherName;
	public override string Description => Strings.FeatureWeatherDescription;

	public override KeyCode Key { get; set; } = KeyCode.None;

	[Configuration.ConfigurationProperty(Order = 10)]
	public float Hour { get; set; } = 12f;

	[Configuration.ConfigurationProperty(Order = 11)]
	public float CloudDensity { get; set; } = -0.7f;

	[Configuration.ConfigurationProperty(Order = 12)]
	public float Fog { get; set; } = 0.004f;

	[Configuration.ConfigurationProperty(Order = 13)]
	public float Rain { get; set; }

	[Configuration.ConfigurationProperty(Order = 14)]
	public float Wind { get; set; }

	[Configuration.ConfigurationProperty(Order = 15)]
	public float Thunder { get; set; }

	protected override void UpdateOnceWhenTriggered()
	{
		ToClearWeather();
	}

	public void ApplyWeather()
	{
		var weatherController = WeatherController.Instance;
		var weatherDebug = weatherController?.WeatherDebug;
		if (weatherDebug == null)
			return;

		if (weatherController!.WeatherCurve != null)
			weatherDebug.CopyParams(weatherController.WeatherCurve);

		weatherDebug.CloudDensity = CloudDensity;
		weatherDebug.Fog = Mathf.Max(0.001f, Fog);
		weatherDebug.Rain = Mathf.Clamp01(Rain);
		weatherDebug.WindMagnitude = Mathf.Clamp01(Wind);
		weatherDebug.LightningThunderProbability = Mathf.Clamp01(Thunder);

		weatherDebug.Enabled = true;
	}

	public void ApplyTime()
	{
		var sky = TOD_Sky.Instance;
		if (sky == null)
			return;

		sky.Components.Time.GameDateTime = null;
		sky.Cycle.Hour = Hour;
	}

	public static void ToClearWeather(bool changeTime = true)
	{
		var weatherController = WeatherController.Instance;
		if (weatherController != null)
		{
			var weatherDebug = weatherController.WeatherDebug;
			weatherDebug.Enabled = true;
			weatherDebug.CloudDensity = -0.7f;
			weatherDebug.Fog = 0.004f;
			weatherDebug.LightningThunderProbability = 0f;
			weatherDebug.Rain = 0f;
		}

		if (!changeTime)
			return;

		var sky = TOD_Sky.Instance;
		if (sky == null)
			return;

		sky.Components.Time.GameDateTime = null;
		sky.Cycle.Hour = 12f;
	}
}
