using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Speed : HoldFeature
{
	public override string Name => Strings.FeatureSpeedName;
	public override string Description => Strings.FeatureSpeedDescription;

	public override KeyCode Key { get; set; } = KeyCode.None;

	[ConfigurationProperty]
	public float Intensity { get; set; } = 2.0f;

	protected override void UpdateWhenHold()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		if (player.IsInventoryOpened || FeatureFactory.GetFeature<RavenUI>()?.Enabled == true)
			return;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		player.Transform.position += Intensity * Time.deltaTime * camera.transform.forward;
	}
}
