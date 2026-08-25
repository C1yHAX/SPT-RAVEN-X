using System;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Stamina : ToggleFeature
{
	public override string Name => Strings.FeatureStaminaName;
	public override string Description => Strings.FeatureStaminaDescription;

	public override bool Enabled { get; set; } = false;

	private object? _parameters;
	private Action? _restore;

	[UsedImplicitly]
	protected static bool ConsumePrefix(object __instance)
	{
		var feature = FeatureFactory.GetFeature<Stamina>();
		if (feature is not { Enabled: true })
			return true;

		var stamina = GameState.Current?.LocalPlayer?.Physical?.Stamina;
		return !ReferenceEquals(__instance, stamina);
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		var playerPhysical = player.IsValid() ? player.Physical : null;
		var parameters = playerPhysical?.StaminaParameters;
		var stamina = playerPhysical?.Stamina;
		if (parameters == null || stamina == null)
		{
			Restore();
			return;
		}

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, stamina.GetType(), nameof(stamina.Consume), nameof(ConsumePrefix));
		});

		if (!ReferenceEquals(_parameters, parameters))
		{
			Restore();
			var aimDrainRate = parameters.AimDrainRate;
			var aimRangeFinderDrainRate = parameters.AimRangeFinderDrainRate;
			var sprintDrainRate = parameters.SprintDrainRate;
			var jumpConsumption = parameters.JumpConsumption;
			var proneConsumption = parameters.ProneConsumption;
			var aimConsumptionByPose = parameters.AimConsumptionByPose;
			var overweightConsumptionByPose = parameters.OverweightConsumptionByPose;
			var crouchConsumption = parameters.CrouchConsumption;
			var standupConsumption = parameters.StandupConsumption;
			var walkConsumption = parameters.WalkConsumption;
			var oxygenRestoration = parameters.OxygenRestoration;
			var exhaustedMeleeSpeed = parameters.ExhaustedMeleeSpeed;
			var baseRestorationRate = parameters.BaseRestorationRate;
			var staminaExhaustionCausesJiggle = parameters.StaminaExhaustionCausesJiggle;
			var staminaExhaustionRocksCamera = parameters.StaminaExhaustionRocksCamera;
			var staminaExhaustionStartsBreathSound = parameters.StaminaExhaustionStartsBreathSound;

			_parameters = parameters;
			_restore = () =>
			{
				parameters.AimDrainRate = aimDrainRate;
				parameters.AimRangeFinderDrainRate = aimRangeFinderDrainRate;
				parameters.SprintDrainRate = sprintDrainRate;
				parameters.JumpConsumption = jumpConsumption;
				parameters.ProneConsumption = proneConsumption;
				parameters.AimConsumptionByPose = aimConsumptionByPose;
				parameters.OverweightConsumptionByPose = overweightConsumptionByPose;
				parameters.CrouchConsumption = crouchConsumption;
				parameters.StandupConsumption = standupConsumption;
				parameters.WalkConsumption = walkConsumption;
				parameters.OxygenRestoration = oxygenRestoration;
				parameters.ExhaustedMeleeSpeed = exhaustedMeleeSpeed;
				parameters.BaseRestorationRate = baseRestorationRate;
				parameters.StaminaExhaustionCausesJiggle = staminaExhaustionCausesJiggle;
				parameters.StaminaExhaustionRocksCamera = staminaExhaustionRocksCamera;
				parameters.StaminaExhaustionStartsBreathSound = staminaExhaustionStartsBreathSound;
			};
		}

		parameters.AimDrainRate = 0f;
		parameters.AimRangeFinderDrainRate = 0f;
		parameters.SprintDrainRate = 0f;
		parameters.JumpConsumption = 0f;
		parameters.ProneConsumption = 0f;
		parameters.AimConsumptionByPose = Vector3.zero;
		parameters.OverweightConsumptionByPose = Vector3.zero;
		parameters.CrouchConsumption = Vector2.zero;
		parameters.StandupConsumption = Vector2.zero;
		parameters.WalkConsumption = Vector2.zero;
		parameters.OxygenRestoration = 10000f;
		parameters.ExhaustedMeleeSpeed = 10000f;
		parameters.BaseRestorationRate = parameters.Capacity;
		parameters.StaminaExhaustionCausesJiggle = false;
		parameters.StaminaExhaustionRocksCamera = false;
		parameters.StaminaExhaustionStartsBreathSound = false;
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
		_parameters = null;
		_restore = null;
	}
}
