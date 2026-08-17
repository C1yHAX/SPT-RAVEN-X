using System.Diagnostics.CodeAnalysis;
using RavenX.Extensions;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoInertia : ToggleFeature
{
	public override string Name => "noinertia";
	public override string Description => "Instant acceleration, stopping and direction changes.";

	public override bool Enabled { get; set; } = false;

	private static readonly string[] _properties =
	[
		nameof(MovementContext.Inertia),
		nameof(MovementContext.MoveSideInertia),
		nameof(MovementContext.MoveDiagonalInertia),
		nameof(MovementContext.PoseInertia),
		nameof(MovementContext.WalkInertia),
		nameof(MovementContext.SprintBrakeInertia)
	];

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void InertiaPostfix(MovementContext __instance, ref float __result)
	{
		if (IsLocal<NoInertia>(__instance))
			__result = 0f;
	}
#pragma warning restore IDE0060

	internal static bool IsLocal<T>(MovementContext context) where T : ToggleFeature
	{
		var feature = FeatureFactory.GetFeature<T>();
		if (feature == null || !feature.Enabled)
			return false;

		var player = GameState.Current?.LocalPlayer;
		return player.IsValid() && ReferenceEquals(context, player.MovementContext);
	}

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{
			foreach (var property in _properties)
				HarmonyPostfix(harmony, typeof(MovementContext), "get_" + property, nameof(InertiaPostfix));
		});
	}
}
