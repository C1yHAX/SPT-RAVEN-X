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

	private Weapon? _lastWeapon;
	private int _lastFireIndex = -1;

	[UsedImplicitly]
	private static void ShootPostfix(Shot shot)
	{
		var feature = FeatureFactory.GetFeature<Ammunition>();
		if (feature == null || !feature.Enabled)
			return;

		feature.Refill(shot);
	}

	private void Refill(Shot shot)
	{
		if (shot.Parent != null)
			return;

		if (shot.Weapon is not Weapon weapon)
			return;

		if (ReferenceEquals(_lastWeapon, weapon) && _lastFireIndex == shot.FireIndex)
			return;

		var ammo = shot.Ammo;
		if (ammo == null)
			return;

		var player = shot.Player.iPlayer;
		if (player is not { IsYourPlayer: true })
			return;

		_lastWeapon = weapon;
		_lastFireIndex = shot.FireIndex;
		var replacement = CreateAmmo(ammo);
		if (replacement == null)
			return;

		var magazine = weapon.GetCurrentMagazine();
		if (magazine != null)
		{
			if (magazine is CylinderMagazine cylinderMagazine)
			{
				foreach (var slot in cylinderMagazine.Camoras)
				{
					if (slot.ContainedItem != null)
						continue;

					slot.Add(replacement, false, true);
					break;
				}
			}
			else
			{
				var cartridges = magazine.Cartridges;
				cartridges?.Add(replacement, false);
			}
		}
		else
		{
			foreach (var slot in weapon.Chambers)
			{
				if (slot.ContainedItem != null)
					continue;

				slot.Add(replacement, false, true);
				break;
			}
		}
	}

	private static Item? CreateAmmo(Item ammo)
	{
		var instantiated = Singleton<ItemFactory>.Instantiated;
		if (!instantiated)
			return null;

		var instance = Singleton<ItemFactory>.Instance;
		var itemId = Guid.NewGuid().ToString("N").Substring(0, 24);
		return instance.CreateItem(itemId, ammo.TemplateId, null);
	}

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(BallisticsCalculator), nameof(BallisticsCalculator.Shoot), nameof(ShootPostfix), [typeof(Shot)]);
		});
	}
}
