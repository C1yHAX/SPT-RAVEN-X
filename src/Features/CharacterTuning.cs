using System.Diagnostics.CodeAnalysis;
using EFT.HealthSystem;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class CharacterTuning : ToggleFeature
{
	public override string Name => "chartuning";
	public override string Description => "Scale movement speeds and metabolism drain.";

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 5)]
	public float WalkSpeed { get; set; } = 1f;

	[ConfigurationProperty(Order = 6)]
	public float SprintSpeed { get; set; } = 1f;

	[ConfigurationProperty(Order = 10)]
	public float VaultSpeed { get; set; } = 1f;

	[ConfigurationProperty(Order = 11)]
	public float StanceSpeed { get; set; } = 1f;

	[ConfigurationProperty(Order = 12)]
	public float JumpHeight { get; set; } = 1f;

	[ConfigurationProperty(Order = 15)]
	public float HealthRegen { get; set; } = 0f;

	[ConfigurationProperty(Order = 20)]
	public float EnergyDrain { get; set; } = 1f;

	[ConfigurationProperty(Order = 21)]
	public float HydrationDrain { get; set; } = 1f;

	private float _vaultBaseline;
	private float _stanceBaseline;
	private float _walkBaseline;
	private float _sprintBaseline;
	private float _jumpBaseline;
	private float _regenCarry;
	private static readonly EBodyPart[] _bodyParts = (EBodyPart[])System.Enum.GetValues(typeof(EBodyPart));

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void JumpHeightPostfix(MovementContext __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature != null)
			__result = Scale(__result, ref feature._jumpBaseline, feature.JumpHeight);
	}

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void VaultingSpeedPostfix(MovementContext __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature != null)
			__result = Scale(__result, ref feature._vaultBaseline, feature.VaultSpeed);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void TransitionSpeedPostfix(MovementContext __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature != null)
			__result = Scale(__result, ref feature._stanceBaseline, feature.StanceSpeed);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void MaxSpeedPostfix(MovementContext __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature != null)
			__result = Scale(__result, ref feature._walkBaseline, feature.WalkSpeed);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void SprintingSpeedPostfix(MovementContext __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature != null)
			__result = Scale(__result, ref feature._sprintBaseline, feature.SprintSpeed);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void ChangeEnergyPrefix(ActiveHealthController __instance, ref float __0)
	{
		var feature = ActiveFor(__instance);
		if (feature != null && __0 < 0f)
			__0 *= Mathf.Clamp(feature.EnergyDrain, 0f, 2f);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void ChangeHydrationPrefix(ActiveHealthController __instance, ref float __0)
	{
		var feature = ActiveFor(__instance);
		if (feature != null && __0 < 0f)
			__0 *= Mathf.Clamp(feature.HydrationDrain, 0f, 2f);
	}
#pragma warning restore IDE0060

	private static float Scale(float current, ref float baseline, float factor)
	{
		if (current > 0f)
			baseline = current;

		return baseline * Mathf.Max(0.05f, factor);
	}

	private static CharacterTuning? Active(MovementContext context)
	{
		var feature = FeatureFactory.GetFeature<CharacterTuning>();
		if (feature == null || !feature.Enabled)
			return null;

		var player = GameState.Current?.LocalPlayer;
		return player.IsValid() && ReferenceEquals(context, player.MovementContext) ? feature : null;
	}

	private static CharacterTuning? ActiveFor(ActiveHealthController controller)
	{
		var feature = FeatureFactory.GetFeature<CharacterTuning>();
		if (feature == null || !feature.Enabled)
			return null;

		var player = GameState.Current?.LocalPlayer;
		return player.IsValid() && ReferenceEquals(controller, player.ActiveHealthController) ? feature : null;
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		Regenerate(player!);

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.JumpHeight), nameof(JumpHeightPostfix));
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.MaxSpeed), nameof(MaxSpeedPostfix));
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.SprintingSpeed), nameof(SprintingSpeedPostfix));
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.VaultingSpeed), nameof(VaultingSpeedPostfix));
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.TransitionSpeed), nameof(TransitionSpeedPostfix));
			HarmonyPrefix(harmony, typeof(ActiveHealthController), nameof(ActiveHealthController.ChangeEnergy), nameof(ChangeEnergyPrefix));
			HarmonyPrefix(harmony, typeof(ActiveHealthController), nameof(ActiveHealthController.ChangeHydration), nameof(ChangeHydrationPrefix));
		});
	}

	private void Regenerate(Player player)
	{
		if (HealthRegen <= 0f)
		{
			_regenCarry = 0f;
			return;
		}

		var healthController = player.ActiveHealthController;
		if (healthController == null)
			return;

		_regenCarry += HealthRegen * Time.deltaTime;

		var amount = Mathf.Floor(_regenCarry);
		if (amount < 1f)
			return;

		_regenCarry -= amount;

		foreach (var bodyPart in _bodyParts)
		{
			if (bodyPart == EBodyPart.Common)
				continue;

			if (healthController.IsBodyPartDestroyed(bodyPart))
				continue;

			healthController.ChangeHealth(bodyPart, amount, default);
		}
	}

	protected override void UpdateWhenDisabled()
	{
		_regenCarry = 0f;
	}
}
