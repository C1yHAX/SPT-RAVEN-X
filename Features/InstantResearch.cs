using System.Diagnostics.CodeAnalysis;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class InstantResearch : ToggleFeature
{
	public override string Name => "instasearch";
	public override string Description => "Search containers and corpses without the wait.";

	public override bool Enabled { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void LuckySearchPostfix(SkillManager __instance, ref float __result)
	{
		if (IsLocal(__instance))
			__result = 1f;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void SearchDoublePostfix(SkillManager __instance, ref bool __result)
	{
		if (IsLocal(__instance))
			__result = true;
	}
#pragma warning restore IDE0060

	private static bool IsLocal(SkillManager skillManager)
	{
		var feature = FeatureFactory.GetFeature<InstantResearch>();
		if (feature == null || !feature.Enabled)
			return false;

		var player = GameState.Current?.LocalPlayer;
		return player.IsValid() && ReferenceEquals(skillManager, player.Skills);
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{

			HarmonyPostfix(harmony, typeof(SkillManager), "get_" + nameof(SkillManager.AttentionEliteLuckySearchValue), nameof(LuckySearchPostfix));
			HarmonyPostfix(harmony, typeof(SkillManager), "get_" + nameof(SkillManager.IsSearchDouble), nameof(SearchDoublePostfix));
		});
	}
}
