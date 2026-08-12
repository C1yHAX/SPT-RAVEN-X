using System.Diagnostics.CodeAnalysis;
using RavenX.Extensions;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class SilentMovement : ToggleFeature
{
	public override string Name => "silent";
	public override string Description => "Move and carry gear without making noise.";

	public override bool Enabled { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void NoisePostfix(MovementContext __instance, ref float __result)
	{
		if (NoInertia.IsLocal<SilentMovement>(__instance))
			__result = 0f;
	}
#pragma warning restore IDE0060

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.CovertEquipmentNoise), nameof(NoisePostfix));
			HarmonyPostfix(harmony, typeof(MovementContext), "get_" + nameof(MovementContext.CovertMovementVolumeBySpeed), nameof(NoisePostfix));
		});
	}
}
