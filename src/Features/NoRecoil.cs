using System;
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

	private object? _effect;
	private Action<float>? _apply;
	private Action? _restore;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		var effect = player.IsValid() ? player.ProceduralWeaponAnimation?.Shootingg?.CurrentRecoilEffect : null;
		if (effect == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_effect, effect))
		{
			Restore();
			var camera = effect.CameraRotationRecoilEffect;
			var handPosition = effect.HandPositionRecoilEffect;
			var handRotation = effect.HandRotationRecoilEffect;
			var cameraIntensity = camera.Intensity;
			var handPositionIntensity = handPosition.Intensity;
			var handRotationIntensity = handRotation.Intensity;

			_effect = effect;
			_apply = factor =>
			{
				camera.Intensity = cameraIntensity * factor;
				handPosition.Intensity = handPositionIntensity * factor;
				handRotation.Intensity = handRotationIntensity * factor;
			};
			_restore = () =>
			{
				camera.Intensity = cameraIntensity;
				handPosition.Intensity = handPositionIntensity;
				handRotation.Intensity = handRotationIntensity;
			};
		}

		_apply?.Invoke(1f - Mathf.Clamp01(Strength));
	}

	protected override void UpdateWhenDisabled()
	{
		Restore();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		Restore();
	}

	private void Restore()
	{
		_restore?.Invoke();
		_effect = null;
		_apply = null;
		_restore = null;
	}
}
