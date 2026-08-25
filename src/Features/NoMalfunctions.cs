using System.Diagnostics.CodeAnalysis;
using EFT.InventoryLogic;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoMalfunctions : ToggleFeature
{
	public override string Name => Strings.FeatureNoMalfunctionsName;
	public override string Description => Strings.FeatureNoMalfunctionsDescription;

	public override bool Enabled { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void AllowedPostfix(Weapon __instance, ref bool __result)
	{
		var feature = FeatureFactory.GetFeature<NoMalfunctions>();
		if (feature is not { Enabled: true })
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid() || !ReferenceEquals(player.HandsController?.Item, __instance))
			return;

		__result = false;
	}
#pragma warning restore IDE0060

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.AllowFeed), nameof(AllowedPostfix));
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.AllowJam), nameof(AllowedPostfix));
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.AllowMisfire), nameof(AllowedPostfix));
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.AllowOverheat), nameof(AllowedPostfix));
			HarmonyPostfix(harmony, typeof(Weapon), "get_" + nameof(Weapon.AllowSlide), nameof(AllowedPostfix));
		});
	}
}
