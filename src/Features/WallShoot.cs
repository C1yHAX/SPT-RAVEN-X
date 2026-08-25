using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class WallShoot : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureWallShootName;
	public override string Description => Properties.Strings.FeatureFeatureWallShootDescription;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool IsPenetratedPrefix(Shot shot, Vector3 hitPoint, BallisticCollider __instance, ref bool __result)
	{
		if (!Applies(shot))
			return true;

		shot.PenetrationPower = Mathf.Max(shot.PenetrationPower, 1000f);
		__result = true;
		return false;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool DeflectsPrefix(Shot shot, ref bool __result)
	{
		if (!Applies(shot))
			return true;

		__result = false;
		return false;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool ShotResultPrefix(Shot __instance, ref bool __result)
	{
		if (!Applies(__instance))
			return true;

		__result = false;
		return false;
	}
#pragma warning restore IDE0060

	private static bool Applies(Shot? shot)
	{
		var feature = FeatureFactory.GetFeature<WallShoot>();
		return feature is { Enabled: true } && shot?.Player?.iPlayer is { IsYourPlayer: true };
	}

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(BallisticCollider), nameof(BallisticCollider.IsPenetrated), nameof(IsPenetratedPrefix));
			HarmonyPrefix(harmony, typeof(BodyPartCollider), nameof(BodyPartCollider.IsPenetrated), nameof(IsPenetratedPrefix));
			HarmonyPrefix(harmony, typeof(BallisticCollider), nameof(BallisticCollider.Deflects), nameof(DeflectsPrefix));
			HarmonyPrefix(harmony, typeof(BodyPartCollider), nameof(BodyPartCollider.Deflects), nameof(DeflectsPrefix));
			HarmonyPrefix(harmony, typeof(Shot), "IsBulletFragmented", nameof(ShotResultPrefix));
			HarmonyPrefix(harmony, typeof(Shot), nameof(Shot.CheckTrajectoryDeviationChance), nameof(ShotResultPrefix));
			HarmonyPrefix(harmony, typeof(Shot), nameof(Shot.IsShotDeflectedByHeavyArmor), nameof(ShotResultPrefix));
		});
	}
}
