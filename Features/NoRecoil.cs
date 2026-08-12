using RavenX.Extensions;
using UnityEngine;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoRecoil : ToggleFeature
{
	public override string Name => Strings.FeatureNoRecoilName;
	public override string Description => Strings.FeatureNoRecoilDescription;

	public override bool Enabled { get; set; } = false;

	[RavenX.Configuration.ConfigurationProperty]
	public float Strength { get; set; } = 1f;

	private float _cameraBaseline;
	private float _handPositionBaseline;
	private float _handRotationBaseline;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		if (player.ProceduralWeaponAnimation == null)
			return;

		var effect = player.ProceduralWeaponAnimation.Shootingg?.CurrentRecoilEffect;
		if (effect == null)
			return;

		var factor = 1f - Mathf.Clamp01(Strength);

		effect.CameraRotationRecoilEffect.Intensity = Scale(effect.CameraRotationRecoilEffect.Intensity, ref _cameraBaseline, factor);
		effect.HandPositionRecoilEffect.Intensity = Scale(effect.HandPositionRecoilEffect.Intensity, ref _handPositionBaseline, factor);
		effect.HandRotationRecoilEffect.Intensity = Scale(effect.HandRotationRecoilEffect.Intensity, ref _handRotationBaseline, factor);
	}

	private static float Scale(float current, ref float baseline, float factor)
	{
		if (current > 0f)
			baseline = current;

		return baseline * factor;
	}
}
