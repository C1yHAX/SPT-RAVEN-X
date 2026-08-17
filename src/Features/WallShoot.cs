using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class WallShoot : ToggleFeature
{
	public override string Name => Strings.FeatureWallShootName;
	public override string Description => Strings.FeatureFeatureWallShootDescription;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool IsPenetratedPrefix(Shot shot, Vector3 hitPoint, BallisticCollider __instance, ref bool __result)
	{
		var feature = FeatureFactory.GetFeature<WallShoot>();
		if (feature == null || !feature.Enabled)
			return true;

		var player = shot.Player.iPlayer;
		if (player is not { IsYourPlayer: true })
			return true;

		__result = true;
		__instance.PenetrationChance = 1.0f;
		__instance.PenetrationLevel = 0.0f;
		__instance.RicochetChance = 0.0f;
		__instance.FragmentationChance = 0.0f;
		__instance.TrajectoryDeviationChance = 0.0f;
		__instance.TrajectoryDeviation = 0.0f;

		return false;
	}
#pragma warning restore IDE0060

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(BallisticCollider), nameof(BallisticCollider.IsPenetrated), nameof(IsPenetratedPrefix));
			HarmonyPrefix(harmony, typeof(BodyPartCollider), nameof(BodyPartCollider.IsPenetrated), nameof(IsPenetratedPrefix));
		});
	}
}
