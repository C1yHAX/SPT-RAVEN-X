using System;
using Comfort.Common;
using EFT.Ballistics;
using EFT.InventoryLogic;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Ammunition : ToggleFeature
{
	public override string Name => Strings.FeatureAmmunitionName;
	public override string Description => Strings.FeatureAmmunitionDescription;

	public override bool Enabled { get; set; } = false;

	[UsedImplicitly]
	private static void ShootPostfix(Shot shot)
	{
		var feature = FeatureFactory.GetFeature<Ammunition>();
		if (feature == null || !feature.Enabled)
			return;

		if (shot.Weapon is not Weapon weapon)
			return;

		var ammo = shot.Ammo;
		if (ammo == null)
			return;

		var player = shot.Player.iPlayer;
		if (player is not { IsYourPlayer: true })
			return;

		var magazine = weapon.GetCurrentMagazine();
		if (magazine != null)
		{
			if (magazine is CylinderMagazine cylinderMagazine)
			{

				foreach (var slot in cylinderMagazine.Camoras)
					slot.Add(CreateAmmo(ammo), false, true);
			}
			else
			{
				var cartridges = magazine.Cartridges;
				cartridges?.Add(CreateAmmo(ammo), false);
			}
		}
		else
		{

			foreach (var slot in weapon.Chambers)
				slot.Add(CreateAmmo(ammo), false, true);
		}
	}

	private static Item CreateAmmo(Item ammo)
	{
		var instantiated = Singleton<ItemFactory>.Instantiated;
		if (!instantiated)
			return ammo;

		var instance = Singleton<ItemFactory>.Instance;
		var itemId = Guid.NewGuid().ToString("N").Substring(0, 24);
		return instance.CreateItem(itemId, ammo.TemplateId, null) ?? ammo;
	}

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(BallisticsCalculator), nameof(BallisticsCalculator.Shoot), nameof(ShootPostfix), [typeof(Shot)]);
		});
	}
}
