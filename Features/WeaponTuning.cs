using System.Diagnostics.CodeAnalysis;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class WeaponTuning : ToggleFeature
{
	public override string Name => "tuning";
	public override string Description => "Override weapon ergonomics and weight.";

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public bool OverrideErgonomics { get; set; } = true;

	[ConfigurationProperty(Order = 11)]
	public float Ergonomics { get; set; } = 100f;

	[ConfigurationProperty(Order = 20)]
	public bool NoWeight { get; set; } = false;

	[ConfigurationProperty(Order = 30)]
	public bool NoOverheat { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void ErgonomicsPostfix(Player.FirearmController __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature is { OverrideErgonomics: true })
			__result = Mathf.Clamp(feature.Ergonomics, 0f, 100f);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void ErgonomicWeightPostfix(Player.FirearmController __instance, ref float __result)
	{
		var feature = Active(__instance);
		if (feature is { NoWeight: true })
			__result = 0f;
	}
#pragma warning restore IDE0060

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void AllowOverheatPostfix(Weapon __instance, ref bool __result)
	{
		var feature = FeatureFactory.GetFeature<WeaponTuning>();
		if (feature is not { Enabled: true, NoOverheat: true })
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid() || !ReferenceEquals(__instance, player.HandsController?.Item))
			return;

		__result = false;
	}

	private static WeaponTuning? Active(Player.FirearmController controller)
	{
		var feature = FeatureFactory.GetFeature<WeaponTuning>();
		if (feature == null || !feature.Enabled)
			return null;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return null;

		return ReferenceEquals(controller, player.HandsController) ? feature : null;
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(Player.FirearmController), "get_" + nameof(Player.FirearmController.TotalErgonomics), nameof(ErgonomicsPostfix));
			HarmonyPostfix(harmony, typeof(Player.FirearmController), "get_" + nameof(Player.FirearmController.ErgonomicWeight), nameof(ErgonomicWeightPostfix));
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.AllowOverheat), nameof(AllowOverheatPostfix));
		});
	}
}
