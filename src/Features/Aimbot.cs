using System.Diagnostics.CodeAnalysis;
using Comfort.Common;
using EFT.Ballistics;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using RavenX.UI;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Aimbot : HoldFeature
{
	public override string Name => Strings.FeatureAimbotName;
	public override string Description => Strings.FeatureAimbotDescription;

	public override KeyCode Key { get; set; } = KeyCode.Slash;

	[ConfigurationProperty(Order = 10)]
	public float MaximumDistance { get; set; } = 200f;

	[ConfigurationProperty(Order = 11)]
	public float Smoothness { get; set; } = 0.085f;

	[ConfigurationProperty(Order = 15)]
	public bool ElevationAdjustment { get; set; } = true;

	[ConfigurationProperty(Order = 20)]
	public float FovRadius { get; set; } = 0f;

	[ConfigurationProperty(Order = 21)]
	public bool ShowFovCircle { get; set; } = false;

	[ConfigurationProperty(Order = 22)]
	public Color FovCircleColor { get; set; } = Color.white;

	[ConfigurationProperty(Order = 23)]
	public float FovCircleThickness { get; set; } = 1f;

	[ConfigurationProperty(Order = 30)]
	public bool SilentAim { get; set; } = false;

	[ConfigurationProperty(Order = 31)]
	public float SilentAimNextShotDelay { get; set; } = 0.25f;

	[ConfigurationProperty(Order = 32)]
	public float SilentAimSpeedFactor { get; set; } = 100f;

	[ConfigurationProperty(Order = 40)]
	public bool MagicBullets { get; set; } = false;

	[ConfigurationProperty(Order = 41)]
	public float MagicBulletSpeedFactor { get; set; } = 1f;

	[ConfigurationProperty(Order = 46)]
	public bool MagicBulletBoostSpeed { get; set; } = true;

	[ConfigurationProperty(Order = 45)]
	public float MagicBulletFlightTime { get; set; } = 12f;

	[ConfigurationProperty(Order = 47)]
	public bool MagicBulletExtendFlight { get; set; } = true;

	[ConfigurationProperty(Order = 42)]
	public bool CompensateDrop { get; set; } = true;

	[ConfigurationProperty(Order = 43)]
	public bool LeadTarget { get; set; } = true;

	[ConfigurationProperty(Order = 44)]
	public bool RequireLineOfSight { get; set; } = false;

#pragma warning disable IDE0060
	[UsedImplicitly]
	protected static bool CreateShotPrefix(object ammo, Vector3 origin, ref Vector3 direction, int fireIndex, string player, Item weapon, ref float speedFactor, int fragmentIndex)
	{
		var feature = FeatureFactory.GetFeature<Aimbot>();
		if (feature == null || (!feature.SilentAim && !feature.MagicBullets) || feature._silentAimTarget == null)
			return true;

		var world = Singleton<GameWorld>.Instance;
		if (world == null)
			return true;

		var localPlayer = world.GetEverExistedBridgeByProfileID(player)?.iPlayer;
		if (localPlayer == null)
			return true;

		if (!localPlayer.IsYourPlayer)
			return true;

		if (feature.SilentAim)
			speedFactor = feature.SilentAimSpeedFactor;
		else if (feature.MagicBullets && feature.MagicBulletBoostSpeed)
			speedFactor = Mathf.Max(1f, feature.MagicBulletSpeedFactor);

		direction = (feature.ResolveAimPoint(feature._silentAimTarget.position, origin, speedFactor) - origin).normalized;

		return true;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool ApplyShotPrefix(DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, object shotId, Player? __instance)
	{
		var feature = FeatureFactory.GetFeature<Aimbot>();
		if (feature == null || (!feature.SilentAim && !feature.MagicBullets) || feature._silentAimTarget == null)
			return true;

		if (damageInfo.Player?.iPlayer is { IsYourPlayer: true } && __instance is { IsYourPlayer: true })
			return false;

		return true;
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static void IsFlyingOutOfTimePostfix(Shot __instance, ref bool __result)
	{
		if (!__result)
			return;

		var feature = FeatureFactory.GetFeature<Aimbot>();
		if (feature == null || (!feature.SilentAim && !feature.MagicBullets))
			return;

		if (!feature.MagicBulletExtendFlight)
			return;

		if (__instance.Player?.iPlayer is not { IsYourPlayer: true })
			return;

		__result = __instance.TimeSinceShot > Mathf.Max(__instance.AmmoLifeTime, feature.MagicBulletFlightTime);
	}

	[UsedImplicitly]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected static bool CreateOpticCalibrationDataPrefix(Weapon? __instance, ref AmmoTemplate ammoTemplate)
	{
		var feature = FeatureFactory.GetFeature<Aimbot>();
		if (feature == null || !feature.ElevationAdjustment)
			return true;

		if (__instance?.GetCurrentMagazine() is { } mag && mag.FirstRealAmmo() is Ammo { AmmoTemplate: { } magazineTemplate })
		{
			ammoTemplate = magazineTemplate;
		}
		else if (__instance?.Chambers is { Length: > 0 } slots && slots[0]?.ContainedItem is Ammo { AmmoTemplate: { } chamberedTemplate })
		{
			ammoTemplate = chamberedTemplate;
		}

		return true;
	}

#pragma warning restore IDE0060

	private Transform? _silentAimTarget = null;
	private float _silentAimNextShotTime = 0f;
	private float _muzzleVelocity = 0f;
	private Vector3 _targetVelocity = Vector3.zero;

	private Vector3 ResolveAimPoint(Vector3 targetPosition, Vector3 origin, float speedFactor)
	{
		var speed = _muzzleVelocity * Mathf.Max(0.01f, speedFactor);
		if (speed <= 0f)
			return targetPosition;

		var lead = LeadTarget ? _targetVelocity : Vector3.zero;
		var drop = CompensateDrop ? -Physics.gravity.y : 0f;

		if (lead == Vector3.zero && drop <= 0f)
			return targetPosition;

		var aimPoint = targetPosition;

		for (var i = 0; i < 4; i++)
		{
			var travelTime = Vector3.Distance(origin, aimPoint) / speed;
			aimPoint = targetPosition + lead * travelTime;
			aimPoint.y += 0.5f * drop * travelTime * travelTime;
		}

		return aimPoint;
	}
	protected override void Update()
	{
		base.Update();

		if (!SilentAim && !MagicBullets)
			return;

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(BallisticsCalculator), nameof(BallisticsCalculator.CreateShot), nameof(CreateShotPrefix));
			HarmonyPrefix(harmony, typeof(Player), nameof(Player.ApplyShot), nameof(ApplyShotPrefix));
			HarmonyPrefix(harmony, typeof(Weapon), nameof(Weapon.CreateOpticCalibrationData), nameof(CreateOpticCalibrationDataPrefix));
			HarmonyPostfix(harmony, typeof(Shot), "get_" + nameof(Shot.IsFlyingOutOfTime), nameof(IsFlyingOutOfTimePostfix));
		});

		_silentAimTarget = null;

		if (!TryGetNearestTarget(out var player, out var camera, out var nearestTarget))
			return;

		if (player.IsInventoryOpened)
			return;

		if (player.HandsController is not Player.FirearmController controller)
			return;

		if (RequireLineOfSight && !camera.IsTransformVisible(nearestTarget))
			return;

		_silentAimTarget = nearestTarget;

		if (!SilentAim)
			return;

		if (_silentAimNextShotTime > Time.time)
			return;

		controller.SetTriggerPressed(true);
		_silentAimNextShotTime = Time.time + SilentAimNextShotDelay;
		controller.SetTriggerPressed(false);
	}

	protected override void UpdateWhenHold()
	{
		if (!TryGetNearestTarget(out var player, out _, out var nearestTarget))
			return;

		var speedFactor = SilentAim
			? SilentAimSpeedFactor
			: MagicBullets && MagicBulletBoostSpeed
				? MagicBulletSpeedFactor
				: 1f;

		var origin = player.Fireport.position;
		AimAtPosition(player, ResolveAimPoint(nearestTarget.position, origin, speedFactor), Smoothness);
	}

	private bool TryGetNearestTarget([NotNullWhen(true)] out Player? localPlayer, [NotNullWhen(true)] out Camera? camera, [NotNullWhen(true)] out Transform? nearestTarget)
	{
		localPlayer = null;
		camera = null;
		nearestTarget = null;
		var nearestTargetDistance = float.MaxValue;

		var state = GameState.Current;
		if (state == null)
			return false;

		camera = state.Camera;
		if (camera == null)
			return false;

		localPlayer = state.LocalPlayer;
		if (localPlayer == null)
			return false;

		if (localPlayer.HandsController == null || localPlayer.HandsController.Item is not Weapon weapon)
			return false;

		var template = weapon.CurrentAmmoTemplate;
		if (template == null)
			return false;

		_muzzleVelocity = template.InitialSpeed;
		_targetVelocity = Vector3.zero;

		Player? nearestHostile = null;

		foreach (var hostile in state.Hostiles)
		{
			if (hostile == null)
				continue;

			if (!hostile.IsAlive())
				continue;

			if (!TryGetHeadTransform(hostile, out var hostileTransform))
				continue;

			var destination = hostileTransform.position;
			var screenPosition = camera.WorldPointToVisibleScreenPoint(destination);
			if (screenPosition == Vector2.zero)
				continue;

			if (!IsInFieldOfView(screenPosition))
				continue;

			var distance = Vector3.Distance(camera.transform.position, destination);
			if (distance > MaximumDistance)
				continue;

			if (distance >= nearestTargetDistance)
				continue;

			nearestTargetDistance = distance;
			nearestHostile = hostile;
			nearestTarget = hostileTransform;
		}

		if (nearestHostile != null)
			_targetVelocity = nearestHostile.Velocity;

		return nearestTarget != null;
	}

	[UsedImplicitly]
	protected void OnGUI()
	{
		if (!ShowFovCircle || FovRadius <= 0)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		if (player.HandsController == null || player.HandsController.Item is not Weapon)
			return;

		Render.DrawCircle(Render.ScreenCenter, FovRadius, FovCircleColor, FovCircleThickness, 48);
	}

	private bool IsInFieldOfView(Vector2 screenPosition)
	{
		if (FovRadius <= 0f)
			return true;

		var distance = Vector2.Distance(Render.ScreenCenter, screenPosition);
		return distance <= FovRadius;
	}

	private static void AimAtPosition(Player player, Vector3 targetPosition, float smoothness)
	{
		var firingAngle = player.Fireport.position - player.Fireport.up * 1f;
		var normalized = (targetPosition - firingAngle).normalized;
		var quaternion = Quaternion.LookRotation(normalized);
		var euler = quaternion.eulerAngles;

		if (euler.x > 180f)
			euler.x -= 360f;

		var playerRotation = player.MovementContext.Rotation;
		var smoothAngle = GetSmoothAngle(playerRotation, new Vector2(euler.y, euler.x), smoothness);
		player.MovementContext.Rotation = smoothAngle;
	}

	private static Vector2 GetSmoothAngle(Vector2 fromAngle, Vector2 toAngle, float smoothness)
	{
		var delta = fromAngle - toAngle;
		NormalizeAngle(ref delta);
		var smoothedDelta = Vector2.Scale(delta, new Vector2(smoothness, smoothness));
		toAngle = fromAngle - smoothedDelta;
		return toAngle;
	}

	private static void NormalizeAngle(ref Vector2 angle)
	{
		var newX = angle.x switch
		{
			<= -180f => angle.x + 360f,
			> 180f => angle.x - 360f,
			_ => angle.x
		};

		var newY = angle.y switch
		{
			> 90f => angle.y - 180f,
			<= -90f => angle.y + 180f,
			_ => angle.y
		};

		angle = new Vector2(newX, newY);
	}

	private static bool TryGetHeadTransform(Player player, [NotNullWhen(true)] out Transform? transform)
	{
		transform = null;

		var bones = player.PlayerBones;
		if (bones == null)
			return false;

		transform = bones.Head.Original;
		return true;
	}
}
