using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using RavenX.UI;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class CrossHair : ToggleFeature
{
	public override string Name => Strings.FeatureCrosshairName;
	public override string Description => Strings.FeatureCrosshairDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty]
	public Color Color { get; set; } = Color.red;

	[ConfigurationProperty]
	public bool HideWhenAiming { get; set; } = true;

	[ConfigurationProperty]
	public float Size { get; set; } = 10f;

	[ConfigurationProperty]
	public float Thickness { get; set; } = 2f;

	protected override void OnGUIWhenEnabled()
	{

		if (Cursor.visible)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		if (player.HandsController == null)
			return;

		if (player.HandsController.IsAiming && HideWhenAiming)
			return;

		var centerx = Screen.width / 2;
		var centery = Screen.height / 2;

		Render.DrawCrosshair(new Vector2(centerx, centery), Size, Color, Thickness);
	}
}
