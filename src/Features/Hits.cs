using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using EFT.HealthSystem;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.UI;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Hits : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureHitsName;
	public override string Description => Properties.Strings.FeatureHitsDescription;

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 10)]
	public Color HitMarkerColor { get; set; } = new(225f / 255f, 66f / 255f, 33f / 255f, 1f);

	[ConfigurationProperty(Order = 11)]
	public Color ArmorDamageColor { get; set; } = new(0f, 126f / 255f, 1f, 1f);

	[ConfigurationProperty(Order = 12)]
	public Color HealthDamageColor { get; set; } = new(1f, 33f / 255f, 33f / 255f, 1f);

	[ConfigurationProperty(Order = 20)]
	public float DisplayTime { get; set; } = 2f;

	[ConfigurationProperty(Order = 21)]
	public float FadeOutTime { get; set; } = 1f;

	[ConfigurationProperty(Order = 30)]
	public bool ShowHitMarker { get; set; } = true;

	[ConfigurationProperty(Order = 31)]
	public bool ShowArmorDamage { get; set; } = true;

	[ConfigurationProperty(Order = 32)]
	public bool ShowHealthDamage { get; set; } = true;

	private sealed class HitMarker(DamageInfo damageInfo, float healthDamage)
	{
		public readonly float CreatedAt = Time.unscaledTime;
		public readonly bool HasWeapon = damageInfo.Weapon != null;
		public readonly float ArmorDamage = damageInfo.ArmorDamage;
		public readonly float Damage = Mathf.Max(0f, healthDamage);
		public readonly Vector3 HitPoint = damageInfo.HitPoint;
	}

	private readonly List<HitMarker> _hitMarkers = [];

#pragma warning disable IDE0060
	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void ApplyDamagePostfix(DamageInfo damageInfo, ActiveHealthController? __instance, float __result)
	{
		var feature = FeatureFactory.GetFeature<Hits>();
		if (feature is not { Enabled: true } || __instance?.Player is not { IsYourPlayer: false })
			return;

		if (damageInfo.Player?.iPlayer is not { IsYourPlayer: true })
			return;

		feature._hitMarkers.Add(new HitMarker(damageInfo, __result));
	}
#pragma warning restore IDE0060

	protected override void OnGUIWhenEnabled()
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		var displayTime = Mathf.Max(0f, DisplayTime);
		var fadeTime = Mathf.Max(0f, FadeOutTime);
		var lifetime = displayTime + fadeTime;
		var now = Time.unscaledTime;

		for (var i = _hitMarkers.Count - 1; i >= 0; i--)
		{
			var marker = _hitMarkers[i];
			var elapsed = now - marker.CreatedAt;
			if (!marker.HasWeapon || elapsed >= lifetime)
			{
				_hitMarkers.RemoveAt(i);
				continue;
			}

			var worldPoint = camera.WorldToScreenPoint(marker.HitPoint);
			if (worldPoint.z <= 0.01f)
				continue;

			var screenHitPoint = new Vector2(worldPoint.x, Screen.height - worldPoint.y);
			var alpha = fadeTime > 0f && elapsed > displayTime ? 1f - (elapsed - displayTime) / fadeTime : 1f;
			alpha = Mathf.Clamp01(alpha);
			var armorDamage = Mathf.Round(marker.ArmorDamage);
			var healthDamage = Mathf.Round(marker.Damage);

			if (ShowHitMarker)
			{
				var radius = 16f + elapsed * 2f;
				Render.DrawCircle(screenHitPoint, radius, HitMarkerColor.SetAlpha(alpha), 2.98f, 32);
			}

			var offset = 0f;
			if (armorDamage > 0f && ShowArmorDamage)
			{
				offset = 10f;
				Render.DrawString(new Vector2(screenHitPoint.x, screenHitPoint.y - offset), $"{armorDamage}", ArmorDamageColor.SetAlpha(alpha));
			}

			if (healthDamage > 0f && ShowHealthDamage)
				Render.DrawString(new Vector2(screenHitPoint.x, screenHitPoint.y + offset), $"{healthDamage}", HealthDamageColor.SetAlpha(alpha));
		}
	}

	protected override void UpdateWhenEnabled()
	{
		HarmonyPatchOnce(harmony =>
		{
			HarmonyPostfix(harmony, typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage), nameof(ApplyDamagePostfix));
		});
	}

	protected override void UpdateWhenDisabled()
	{
		_hitMarkers.Clear();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		_hitMarkers.Clear();
	}
}
