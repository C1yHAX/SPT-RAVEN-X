using System.Diagnostics.CodeAnalysis;
using EFT.InventoryLogic;
using RavenX.Properties;
using JetBrains.Annotations;
using static EFT.Player;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Examine : ToggleFeature
{
	public override string Name => Strings.FeatureExamineName;
	public override string Description => Strings.FeatureExamineDescription;

	public override bool Enabled { get; set; } = false;

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool ExaminedPrefix(ref bool __result)
	{
		var feature = FeatureFactory.GetFeature<Examine>();
		if (feature == null || !feature.Enabled)
			return true;

		__result = true;
		return false;
	}

#pragma warning disable IDE0060
	[UsedImplicitly]
	protected static bool SinglePlayerInventoryControllerConstructorPrefix(Player player, Profile profile, bool isBot, ref bool examined)
	{
		var feature = FeatureFactory.GetFeature<Examine>();
		if (feature == null || !feature.Enabled)
			return true;

		examined = true;
		return true;
	}
#pragma warning restore IDE0060

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(Profile), nameof(Profile.Examined), nameof(ExaminedPrefix), [typeof(MongoID)]);
			HarmonyPrefix(harmony, typeof(Profile), nameof(Profile.Examined), nameof(ExaminedPrefix), [typeof(Item)]);
			HarmonyConstructorPrefix(harmony, typeof(SinglePlayerInventoryController), nameof(SinglePlayerInventoryControllerConstructorPrefix), [typeof(Player), typeof(Profile), typeof(bool), typeof(bool)]);
		});
	}
}
