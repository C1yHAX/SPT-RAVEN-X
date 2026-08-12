using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using EFT.HealthSystem;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class InstantKill : ToggleFeature
{
	public override string Name => "instakill";
	public override string Description => "Every hit you land kills instantly.";

	public override bool Enabled { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void ApplyDamagePrefix(EBodyPart bodyPart, ref float damage, DamageInfo damageInfo, ActiveHealthController? __instance)
	{
		var feature = FeatureFactory.GetFeature<InstantKill>();
		if (feature == null || !feature.Enabled)
			return;

		if (__instance == null)
			return;

		if (damageInfo.Player?.iPlayer is not { IsYourPlayer: true })
			return;

		var victim = __instance.Player;
		if (victim == null || victim.IsYourPlayer)
			return;

		damage = 100000f;
	}
#pragma warning restore IDE0060

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage), nameof(ApplyDamagePrefix));
		});
	}
}
