using System;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT.Animations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoSway : ToggleFeature
{
	public override string Name => Strings.FeatureNoSwayName;
	public override string Description => Strings.FeatureNoSwayDescription;

	public override bool Enabled { get; set; } = false;

	private object? _animation;
	private Action? _apply;
	private Action? _restore;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		var weaponAnimation = player.IsValid() ? player.ProceduralWeaponAnimation : null;
		if (weaponAnimation == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_animation, weaponAnimation))
		{
			Restore();

			var motionReact = weaponAnimation.MotionReact;
			var breath = weaponAnimation.Breath;
			var walk = weaponAnimation.Walk;
			var shooting = weaponAnimation.Shootingg;
			var forceReact = weaponAnimation.ForceReact;
			if (motionReact == null || breath == null || walk == null || shooting?.AimingConfig == null || forceReact == null)
				return;

			var motionIntensity = motionReact.Intensity;
			var swayFactors = motionReact.SwayFactors;
			var velocity = motionReact.Velocity;
			var breathIntensity = breath.Intensity;
			var walkIntensity = walk.Intensity;
			var aimingIntensity = shooting.AimingConfig.AimProceduralIntensity;
			var forceIntensity = forceReact.Intensity;
			var walkEnabled = (weaponAnimation.Mask & EProceduralAnimationMask.Walking) != 0;

			_animation = weaponAnimation;
			_apply = () =>
			{
				motionReact.Intensity = 0f;
				motionReact.SwayFactors = Vector3.zero;
				motionReact.Velocity = Vector3.zero;
				breath.Intensity = 0f;
				walk.Intensity = 0f;
				shooting.AimingConfig.AimProceduralIntensity = 0f;
				forceReact.Intensity = 0f;
				weaponAnimation.WalkEffectorEnabled = false;
			};
			_restore = () =>
			{
				motionReact.Intensity = motionIntensity;
				motionReact.SwayFactors = swayFactors;
				motionReact.Velocity = velocity;
				breath.Intensity = breathIntensity;
				walk.Intensity = walkIntensity;
				shooting.AimingConfig.AimProceduralIntensity = aimingIntensity;
				forceReact.Intensity = forceIntensity;
				weaponAnimation.WalkEffectorEnabled = walkEnabled;
			};
		}

		_apply?.Invoke();
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
		_animation = null;
		_apply = null;
		_restore = null;
	}
}
