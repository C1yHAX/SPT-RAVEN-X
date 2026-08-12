using System.Diagnostics.CodeAnalysis;
using EFT.HealthSystem;
using RavenX.Extensions;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoFallDamage : ToggleFeature
{
	public override string Name => "nofall";
	public override string Description => "Falling from any height causes no damage.";

	public override bool Enabled { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool HandleFallPrefix(ActiveHealthController __instance, ref float __result)
	{
		var feature = FeatureFactory.GetFeature<NoFallDamage>();
		if (feature == null || !feature.Enabled)
			return true;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid() || !ReferenceEquals(__instance, player.ActiveHealthController))
			return true;

		__result = 0f;
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
			HarmonyPrefix(harmony, typeof(ActiveHealthController), nameof(ActiveHealthController.HandleFall), nameof(HandleFallPrefix));
		});
	}
}
