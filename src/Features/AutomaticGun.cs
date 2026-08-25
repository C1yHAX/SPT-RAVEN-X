using System.Diagnostics.CodeAnalysis;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class AutomaticGun : ToggleFeature
{
	public override string Name => Strings.FeatureAutomaticGunName;
	public override string Description => Strings.FeatureAutomaticGunDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public bool OverrideRate { get; set; } = false;

	[ConfigurationProperty(Order = 11)]
	public int Rate { get; set; } = 500;

	private Weapon? _weapon;
	private FireModeComponent? _fireMode;
	private Weapon.EFireMode _originalFireMode;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void FireRatePostfix(Weapon __instance, ref int __result)
	{
		var feature = Active(__instance);
		if (feature is { OverrideRate: true })
			__result = feature.Rate > 0 ? feature.Rate : 1;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void BoltActionPostfix(Weapon __instance, ref bool __result)
	{
		if (Active(__instance) != null)
			__result = false;
	}
#pragma warning restore IDE0060

	private static AutomaticGun? Active(Weapon weapon)
	{
		var feature = FeatureFactory.GetFeature<AutomaticGun>();
		if (feature is not { Enabled: true })
			return null;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid() || !ReferenceEquals(player.HandsController?.Item, weapon))
			return null;

		return feature;
	}

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.FireRate), nameof(FireRatePostfix));
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.BoltAction), nameof(BoltActionPostfix));
		});

		var weapon = GameState.Current?.LocalPlayer?.HandsController?.Item as Weapon;
		if (ReferenceEquals(weapon, _weapon))
		{
			if (_fireMode != null)
				_fireMode.FireMode = Weapon.EFireMode.fullauto;
			return;
		}

		RestoreFireMode();
		if (weapon == null)
			return;

		var fireMode = weapon.GetItemComponent<FireModeComponent>();
		if (fireMode == null)
			return;

		_weapon = weapon;
		_fireMode = fireMode;
		_originalFireMode = fireMode.FireMode;
		fireMode.FireMode = Weapon.EFireMode.fullauto;
	}

	protected override void UpdateWhenDisabled()
	{
		RestoreFireMode();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		RestoreFireMode();
	}

	private void RestoreFireMode()
	{
		if (_fireMode != null)
			_fireMode.FireMode = _originalFireMode;

		_weapon = null;
		_fireMode = null;
	}
}
