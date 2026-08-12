using System;
using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using EFT.HealthSystem;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Health : ToggleFeature
{
	public override string Name => Strings.FeatureHealthName;
	public override string Description => Strings.FeatureHealthDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty]
	public bool VitalsOnly { get; set; } = false;

	[ConfigurationProperty]
	public bool RemoveNegativeEffects { get; set; } = true;

	[ConfigurationProperty]
	public bool FoodWater { get; set; } = true;

	private static readonly Array _bodyParts = Enum.GetValues(typeof(EBodyPart));

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool ApplyDamagePrefix(EBodyPart bodyPart, ActiveHealthController? __instance, ref float __result)
	{
		if (__instance == null)
			return true;

		if (UseBuiltinDamageLogic(__instance.Player, bodyPart))
			return true;

		__result = 0f;
		return false;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool ReceiveDamagePrefix(float damage, EBodyPart part, EDamageType type, float absorbed, MaterialType special, Player? __instance)
	{
		return UseBuiltinDamageLogic(__instance, part);
	}
#pragma warning restore IDE0060

	protected static bool UseBuiltinDamageLogic(Player? player, EBodyPart bodyPart)
	{
		var feature = FeatureFactory.GetFeature<Health>();
		if (feature == null || !feature.Enabled)
			return true;

		if (player == null || !player.IsYourPlayer)
			return true;

		if (feature.VitalsOnly && bodyPart is not (EBodyPart.Chest or EBodyPart.Head))
			return true;

		return false;
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var healthController = player.ActiveHealthController;
		if (healthController == null)
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage), nameof(ApplyDamagePrefix));
			HarmonyPrefix(harmony, typeof(Player), nameof(Player.ReceiveDamage), nameof(ReceiveDamagePrefix));
		});

		foreach (EBodyPart bodyPart in _bodyParts)
		{
			if (bodyPart == EBodyPart.Common)
				continue;

			if (VitalsOnly && bodyPart is not (EBodyPart.Chest or EBodyPart.Head))
				continue;

			if (healthController.IsBodyPartBroken(bodyPart) || healthController.IsBodyPartDestroyed(bodyPart))
				healthController.RestoreBodyPart(bodyPart, 1);

			if (RemoveNegativeEffects)
			{
				healthController.RemoveNegativeEffects(bodyPart);
				healthController.RemoveNegativeEffects(EBodyPart.Common);
			}

			var bodyPartHealth = healthController.GetBodyPartHealth(bodyPart);
			if (bodyPartHealth.AtMaximum)
				continue;

			if (!VitalsOnly)
				healthController.RestoreFullHealth();

			healthController.ChangeHealth(bodyPart, bodyPartHealth.Maximum, default);

			break;
		}

		if (!healthController.Energy.AtMaximum && FoodWater)
			healthController.ChangeEnergy(healthController.Energy.Maximum);

		if (!healthController.Hydration.AtMaximum && FoodWater)
			healthController.ChangeHydration(healthController.Hydration.Maximum);
	}
}
