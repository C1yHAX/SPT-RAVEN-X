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

		var customSpeedFactor = feature.GetShotSpeedFactor();
		speedFactor *= customSpeedFactor;

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
		if (feature is not { MagicBullets: true, MagicBulletExtendFlight: true })
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
	private AmmoTemplate? _ammoTemplate;
	private TrajectoryCalculator? _trajectory;
	private AmmoTemplate? _trajectoryAmmoTemplate;
	private float _trajectorySpeed;

	private float GetShotSpeedFactor()
	{
		var factor = SilentAim ? Mathf.Clamp(SilentAimSpeedFactor, 0.01f, 300f) : 1f;

		if (MagicBullets && MagicBulletBoostSpeed)
			factor = Mathf.Max(factor, Mathf.Clamp(MagicBulletSpeedFactor, 1f, 100f));

		return factor;
	}

	private Vector3 ResolveAimPoint(Vector3 targetPosition, Vector3 origin, float speedFactor)
	{
		var speed = _muzzleVelocity * Mathf.Max(0.01f, speedFactor);
		if (speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed))
			return targetPosition;

		var lead = LeadTarget ? _targetVelocity : Vector3.zero;
		if (lead == Vector3.zero && !CompensateDrop)
			return targetPosition;

		if (!TryPrepareTrajectory(speed))
		{
			var travelTime = Vector3.Distance(origin, targetPosition) / speed;
			var fallback = targetPosition + lead * travelTime;
			if (CompensateDrop)
				fallback.y += -0.5f * Physics.gravity.y * travelTime * travelTime;
			return fallback;
		}

		var aimPoint = targetPosition;
		var time = 0f;

		for (var i = 0; i < 3; i++)
		{
			aimPoint = targetPosition + lead * time;
			time = EstimateTravelTime(origin, aimPoint, speed);
		}

		aimPoint = targetPosition + lead * time;
		if (CompensateDrop)
		{
			Shot.PredictedTrajectoryCalculation(out var position, out _, _trajectory!, time);
			if (!float.IsNaN(position.y) && !float.IsInfinity(position.y))
				aimPoint.y -= position.y;
		}

		return aimPoint;
	}

	private bool TryPrepareTrajectory(float speed)
	{
		if (_ammoTemplate == null || _ammoTemplate.BulletMassGram <= 0f || _ammoTemplate.BulletDiameterMilimeters <= 0f || _ammoTemplate.BallisticCoeficient <= 0f)
		{
			ReleaseTrajectory();
			return false;
		}

		if (_trajectory != null && ReferenceEquals(_trajectoryAmmoTemplate, _ammoTemplate) && Mathf.Approximately(_trajectorySpeed, speed))
			return true;

		ReleaseTrajectory();
		_trajectory = new TrajectoryCalculator();
		_trajectory.Initialize(Vector3.zero, Vector3.forward * speed, _ammoTemplate.BulletMassGram, _ammoTemplate.BulletDiameterMilimeters, _ammoTemplate.BallisticCoeficient, false);
		_trajectoryAmmoTemplate = _ammoTemplate;
		_trajectorySpeed = speed;
		return true;
	}

	private float EstimateTravelTime(Vector3 origin, Vector3 target, float speed)
	{
		var offset = target - origin;
		var distance = new Vector2(offset.x, offset.z).magnitude;
		if (distance <= 0.01f || _trajectory == null)
			return Vector3.Distance(origin, target) / speed;

		var low = 0f;
		var high = Mathf.Max(1f, MagicBullets && MagicBulletExtendFlight ? MagicBulletFlightTime : _ammoTemplate?.AmmoLifeTimeSec ?? 1f);

		for (var i = 0; i < 14; i++)
		{
			var middle = (low + high) * 0.5f;
			Shot.PredictedTrajectoryCalculation(out var position, out _, _trajectory, middle);
			var travelled = new Vector2(position.x, position.z).magnitude;
			if (float.IsNaN(travelled) || float.IsInfinity(travelled))
				return Vector3.Distance(origin, target) / speed;

			if (travelled < distance)
				low = middle;
			else
				high = middle;
		}

		return high;
	}

	private void ReleaseTrajectory()
	{
		if (_trajectory == null)
			return;

		_trajectory.ClearClass();
		_trajectory = null;
		_trajectoryAmmoTemplate = null;
		_trajectorySpeed = 0f;
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		ReleaseTrajectory();
	}

	protected override void Update()
	{
		base.Update();

		HarmonyPatchOnce(harmony =>
		{
			HarmonyPrefix(harmony, typeof(BallisticsCalculator), nameof(BallisticsCalculator.CreateShot), nameof(CreateShotPrefix));
			HarmonyPrefix(harmony, typeof(Player), nameof(Player.ApplyShot), nameof(ApplyShotPrefix));
			HarmonyPrefix(harmony, typeof(Weapon), nameof(Weapon.CreateOpticCalibrationData), nameof(CreateOpticCalibrationDataPrefix));
			HarmonyPostfix(harmony, typeof(Shot), "get_" + nameof(Shot.IsFlyingOutOfTime), nameof(IsFlyingOutOfTimePostfix));
		});

		_silentAimTarget = null;

		if (!SilentAim && !MagicBullets)
			return;

		if (!TryGetNearestTarget(out var player, out var camera, out var nearestTarget))
			return;

		if (player.IsInventoryOpened)
			return;

		if (player.HandsController is not Player.FirearmController controller)
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

		var origin = player.Fireport.position;
		AimAtPosition(player, ResolveAimPoint(nearestTarget.position, origin, GetShotSpeedFactor()), Smoothness);
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

		_ammoTemplate = template;
		_muzzleVelocity = weapon.TotalVelocity;
		_targetVelocity = Vector3.zero;

		Player? nearestHostile = null;

		foreach (var hostile in state.Hostiles)
		{
			if (hostile == null)
				continue;

			if (!hostile.IsAlive())
				continue;

			if (hostile.IsFriendlyTo(localPlayer))
				continue;

			if (!TryGetHeadTransform(hostile, out var hostileTransform))
				continue;

			var destination = hostileTransform.position;
			var screenPosition = camera.WorldPointToVisibleScreenPoint(destination);
			if (screenPosition == Vector2.zero)
				continue;

			if (!IsInFieldOfView(screenPosition))
				continue;

			if (RequireLineOfSight && !camera.IsTransformVisible(hostileTransform))
				continue;

			var distance = Vector3.Distance(camera.transform.position, destination);
			if (MaximumDistance > 0f && distance > MaximumDistance)
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
		if (Event.current.type != EventType.Repaint || !ShowFovCircle || FovRadius <= 0)
			return;

		GUI.depth = 10;

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
