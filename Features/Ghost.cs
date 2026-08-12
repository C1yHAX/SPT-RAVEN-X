using System.Diagnostics.CodeAnalysis;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Ghost : ToggleFeature
{
	public override string Name => Strings.FeatureGhostName;
	public override string Description => Strings.FeatureGhostDescription;

	public override bool Enabled { get; set; } = false;

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private static bool CheckLookEnemy(EnemyInfo __instance)
	{
		var feature = FeatureFactory.GetFeature<Ghost>();
		if (feature == null || !feature.Enabled)
			return true;

		if (__instance.Person is not { IsYourPlayer: true })
			return true;

		var groupInfo = __instance.GroupInfo;
		groupInfo.Clear();
		groupInfo.IsHaveSeen = false;
		groupInfo.EnemyLastPosition = Vector3.zero;
		groupInfo.EnemyLastVisiblePosition = Vector3.zero;
		groupInfo.EnemyWeaponRootLastPos = Vector3.zero;
		groupInfo.EnemyLastSeenTimeSense = 0f;
		groupInfo.EnemyLastSeenTimeReal = 0f;

		var memory = __instance.Owner.Memory;
		memory.GoalTarget.Clear();
		memory.GoalEnemy = null;
		memory.LastEnemy = null;

		__instance.SetVisible(false);
		__instance.SetCanShoot(false);
		__instance.SetIgnoreState();

		return false;
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(EnemyInfo), nameof(EnemyInfo.CheckLookEnemy), nameof(CheckLookEnemy));
		});
	}
}
