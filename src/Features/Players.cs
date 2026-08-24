using System;
using System.Collections.Generic;
using System.Linq;
using EFT.CameraControl;
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

public class PlayerColor(Color color, Color borderColor, Color infoColor) : IFeature
{
	[ConfigurationProperty(Order = 1)]
	public Color Color { get; set; } = color;

	[ConfigurationProperty(Order = 2)]
	public Color BorderColor { get; set; } = borderColor;

	[ConfigurationProperty(Order = 3)]
	public Color InfoColor { get; set; } = infoColor;

	public string Name => nameof(PlayerColor);
}

public class ShootableColor(Color color, Color borderColor) : IFeature
{
	[ConfigurationProperty(Order = 1)]
	public Color Color { get; set; } = color;

	[ConfigurationProperty(Order = 2)]
	public Color BorderColor { get; set; } = borderColor;

	public string Name => nameof(ShootableColor);
}

[UsedImplicitly]
internal class Players : ToggleFeature
{
	public override string Name => Strings.FeaturePlayersName;
	public override string Description => Strings.FeaturePlayersDescription;

	[ConfigurationProperty(Order = 10)]
	public PlayerColor BearColors { get; set; } = new(Color.blue, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor UsecColors { get; set; } = new(Color.green, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor ScavColors { get; set; } = new(Color.yellow, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor BossColors { get; set; } = new(Color.red, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor CultistColors { get; set; } = new(Color.yellow, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor ScavRaiderColors { get; set; } = new(Color.yellow, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor ScavAssaultColors { get; set; } = new(Color.yellow, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor MarksmanColors { get; set; } = new(Color.yellow, Color.red, Color.red);

	[ConfigurationProperty(Order = 10)]
	public PlayerColor RogueUsecColors { get; set; } = new(Color.gray, Color.red, Color.red);

	[ConfigurationProperty(Order = 20)]
	public bool ShowBoxes { get; set; } = true;

	[ConfigurationProperty(Order = 21)]
	public float BoxThickness { get; set; } = 2f;

	[ConfigurationProperty(Order = 40)]
	public bool ShowInfos { get; set; } = true;

	[ConfigurationProperty(Order = 41)]
	public bool ShowRole { get; set; } = true;

	[ConfigurationProperty(Order = 89)]
	public bool ShowNames { get; set; } = false;

	[ConfigurationProperty(Order = 90)]
	public bool ShowWeapons { get; set; } = true;

	[ConfigurationProperty(Order = 91)]
	public bool ShowDistance { get; set; } = true;

	[ConfigurationProperty(Order = 92)]
	public bool ShowHealthText { get; set; } = true;

	[ConfigurationProperty(Order = 93)]
	public bool ShowHealthBar { get; set; } = false;

	[ConfigurationProperty(Order = 94)]
	public bool ShowSnapLines { get; set; } = false;

	[ConfigurationProperty(Order = 95)]
	public Color SnapLineColor { get; set; } = new(0.48f, 0.36f, 0.98f, 0.65f);

	[ConfigurationProperty(Order = 96)]
	public float SnapLineThickness { get; set; } = 1.4f;

	[ConfigurationProperty(Order = 50)]
	public bool ShowSkeletons { get; set; } = false;

	[ConfigurationProperty(Order = 51)]
	public float SkeletonThickness { get; set; } = 2;

	[ConfigurationProperty(Order = 60)]
	public bool ShowShootable { get; set; } = false;

	[ConfigurationProperty(Order = 61)]
	public ShootableColor ShootableColors { get; set; } = new(Color.green, Color.red);

	[ConfigurationProperty(Order = 62)]
	public bool ShowNotShootable { get; set; } = false;

	[ConfigurationProperty(Order = 63)]
	public ShootableColor NotShootableColors { get; set; } = new(Color.red, Color.blue);

	[ConfigurationProperty(Order = 64)]
	public bool PerLimbVisibility { get; set; } = true;

	[ConfigurationProperty(Order = 65, Browsable = false)]
	public List<RoleSetting> RoleColors { get; set; } = [];

	private Dictionary<string, RoleSetting>? _roleIndex;

	public RoleSetting RoleFor(Player player) => RoleFor(RoleCatalog.KeyOf(player));

	public RoleSetting RoleFor(string key) => RoleCatalog.Resolve(RoleColors, ref _roleIndex, key);

	[ConfigurationProperty(Order = 19)]
	public float MaximumDistance { get; set; } = 0f;

	[ConfigurationProperty(Order = 97)]
	public int TextSize { get; set; } = 0;

	[ConfigurationProperty(Order = 98)]
	public float TextOutline { get; set; } = 0f;

	private static Camera? _opticCamera;
	private static (Vector2 center, float radius) _scopeParameters;

	[UsedImplicitly]
	protected void OnGUI()
	{
		Render.FontSize = TextSize;
		Render.OutlineThickness = TextOutline;

		var snapshot = GameState.Current;
		if (snapshot == null)
			return;

		if (snapshot.MapMode)
			return;

		var hostiles = snapshot.Hostiles;

		var player = snapshot.LocalPlayer;
		if (player == null)
			return;

		var camera = snapshot.Camera;
		if (camera == null)
			return;

		if (!Enabled)
			return;

		var isAiming = AimingCheck(camera, player);

		foreach (var ennemy in hostiles)
		{
			if (!ennemy.IsValid())
				continue;

			var role = RoleFor(ennemy);
			if (!role.Enabled)
				continue;

			var defaults = GetPlayerColors(ennemy);
			var playerColors = new PlayerColor(role.Visible, defaults.BorderColor, role.Visible);
			var borderColor = playerColors.BorderColor;

			var position = ennemy.Transform.position;
			var screenPosition = isAiming ? ScopePointToScreenPoint(camera, position) : camera.WorldPointToVisibleScreenPoint(position);
			if (screenPosition == Vector2.zero)
				continue;

			var distance = Mathf.Round(Vector3.Distance(camera.transform.position, position));
			if (MaximumDistance > 0 && distance > MaximumDistance)
				continue;

			var playerBones = ennemy.PlayerBones;
			if (playerBones == null)
				continue;

			var headScreenPosition = isAiming
				? ScopePointToScreenPoint(camera, playerBones.Head.position)
				: camera.WorldPointToVisibleScreenPoint(playerBones.Head.position);
			var leftShoulderScreenPosition = isAiming
				? ScopePointToScreenPoint(camera, playerBones.LeftShoulder.position)
				: camera.WorldPointToVisibleScreenPoint(playerBones.LeftShoulder.position);

			if (headScreenPosition == Vector2.zero || leftShoulderScreenPosition == Vector2.zero)
				continue;

			if (ShowShootable)
			{
				var bonesToCheck = GetBonesToCheck(playerBones);
				var anyVisible = bonesToCheck.Any(bone => IsTransformVisibleCached(bone.transform, camera.IsTransformVisible));

				borderColor = anyVisible
					? ShootableColors.BorderColor
					: ShowNotShootable ? NotShootableColors.BorderColor : role.Occluded;

				if (ShowSkeletons)
				{
					foreach (var bone in bonesToCheck)
					{
						var exposed = PerLimbVisibility
							? IsTransformVisibleCached(bone.transform, camera.IsTransformVisible)
							: anyVisible;

						var bonesColor = exposed ? ShootableColors.Color : ShowNotShootable ? NotShootableColors.Color : role.Occluded;
						Bones.RenderBones(ennemy, bone.bones, SkeletonThickness, bonesColor, camera, isAiming);
					}

					var headExposed = PerLimbVisibility
						? IsTransformVisibleCached(bonesToCheck[0].transform, camera.IsTransformVisible)
						: anyVisible;

					var color = headExposed ? ShootableColors.Color : ShowNotShootable ? NotShootableColors.Color : role.Occluded;
					Bones.RenderHead(ennemy, SkeletonThickness, color, camera, isAiming);
					if (distance < 75f)
						Bones.RenderFingers(ennemy, SkeletonThickness, color, camera, isAiming);
				}

				ClearTransformCache();
			}
			else if (ShowSkeletons)
				Bones.RenderBones(ennemy, SkeletonThickness, playerColors.Color, camera, isAiming, distance);

			var heightOffset = Mathf.Abs(headScreenPosition.y - leftShoulderScreenPosition.y);

			var boxHeight = Mathf.Abs(headScreenPosition.y - screenPosition.y) + heightOffset * 3f;
			var boxWidth = boxHeight * 0.62f;

			var boxPositionX = screenPosition.x - boxWidth / 2f;
			var boxPositionY = headScreenPosition.y - heightOffset * 2f;

			if (ShowBoxes)
				Render.DrawBox(boxPositionX, boxPositionY, boxWidth, boxHeight, BoxThickness, borderColor);

			if (ShowSnapLines)
				Render.DrawLine(new Vector2(Screen.width / 2f, Screen.height), new Vector2(screenPosition.x, boxPositionY + boxHeight), SnapLineThickness, SnapLineColor);

			var ennemyHealthController = ennemy.HealthController;
			var ennemyHandController = ennemy.HandsController;

			if (!ShowInfos || ennemyHealthController is not { IsAlive: true })
				continue;

			var bodyPartHealth = ennemyHealthController.GetBodyPartHealth(EBodyPart.Common);
			var currentPlayerHealth = bodyPartHealth.Current;
			var maximumPlayerHealth = bodyPartHealth.Maximum;

			var healthFraction = maximumPlayerHealth > 0f ? currentPlayerHealth / maximumPlayerHealth : 0f;

			if (ShowHealthBar)
				DrawHealthBar(boxPositionX, boxPositionY, boxWidth, healthFraction);

			var weaponText = ShowWeapons && ennemyHandController != null && ennemyHandController.Item is Weapon weapon
				? weapon.ShortName.Localized()
				: string.Empty;

			var distanceText = ShowDistance
				? string.Format(Strings.FeaturePointOfInterestsDistanceFormat, distance)
				: string.Empty;

			var infoText = ShowWeapons && ShowDistance && ShowHealthText
				? string.Format(Strings.FeaturePlayersFormat, weaponText, Mathf.Round(healthFraction * 100f), distanceText).Trim()
				: JoinParts(weaponText, ShowHealthText ? $"{Mathf.Round(healthFraction * 100f)}%" : string.Empty, distanceText);

			var textY = boxPositionY - 20f;

			if (ShowRole)
			{
				var label = RoleCatalog.LabelOf(RoleCatalog.KeyOf(ennemy));
				if (label.Length > 0)
				{
					Render.DrawString(new Vector2(boxPositionX, textY), label, playerColors.InfoColor, false);
					textY -= 16f;
				}
			}

			if (ShowNames)
			{
				var nickname = ennemy.Profile?.Info?.Nickname ?? string.Empty;
				if (nickname.Length > 0)
				{
					Render.DrawString(new Vector2(boxPositionX, textY), nickname, playerColors.InfoColor, false);
					textY -= 16f;
				}
			}

			if (infoText.Length > 0)
				Render.DrawString(new Vector2(boxPositionX, textY), infoText, playerColors.InfoColor, false);
		}
	}

	private static string JoinParts(params string[] parts)
	{
		return string.Join(" ", parts.Where(p => p.Length > 0).ToArray());
	}

	private static void DrawHealthBar(float boxX, float boxY, float boxWidth, float fraction)
	{
		fraction = Mathf.Clamp01(fraction);

		const float height = 3f;
		var y = boxY - 6f;

		Render.DrawBox(boxX, y, boxWidth, height, 1f, new Color(0f, 0f, 0f, 0.55f));

		if (fraction <= 0f)
			return;

		var color = Color.Lerp(new Color(0.86f, 0.24f, 0.24f), new Color(0.24f, 0.80f, 0.36f), fraction);
		Render.DrawBox(boxX, y, boxWidth * fraction, height, height, color);
	}

	private static (Transform transform, string[] bones)[] GetBonesToCheck(PlayerBones playerBones)
	{
		return
		[
			(playerBones.Head.Original.transform, [Bones.Neck, Bones.Head]),
			(playerBones.Neck.transform, [Bones.RCollarbone, Bones.Spine3, Bones.LCollarbone, Bones.Spine3, Bones.Spine3, Bones.Neck]),
			(playerBones.Spine1.transform, [Bones.Pelvis, Bones.Spine1, Bones.Spine1, Bones.Spine2, Bones.Spine2, Bones.Spine3]),
			(playerBones.Upperarms[0].transform, [Bones.LCollarbone, Bones.LForearm1, Bones.LForearm1, Bones.LForearm2]),
			(playerBones.Upperarms[1].transform, [Bones.RCollarbone, Bones.RForearm1, Bones.RForearm1, Bones.RForearm2]),
			(playerBones.Forearms[0].transform, [Bones.LForearm2, Bones.LForearm3, Bones.LForearm3, Bones.LPalm]),
			(playerBones.Forearms[1].transform, [Bones.RForearm2, Bones.RForearm3, Bones.RForearm3, Bones.RPalm]),
			(playerBones.LeftThigh1.Original.transform, [Bones.Pelvis, Bones.LThigh1, Bones.LThigh1, Bones.LThigh2]),
			(playerBones.RightThigh1.Original.transform, [Bones.Pelvis, Bones.RThigh1, Bones.RThigh1, Bones.RThigh2]),
			(playerBones.LeftThigh2.Original.transform, [Bones.LThigh2, Bones.LCalf, Bones.LCalf, Bones.LFoot, Bones.LFoot, Bones.LToe]),
			(playerBones.RightThigh2.Original.transform, [Bones.RThigh2, Bones.RCalf, Bones.RCalf, Bones.RFoot, Bones.RFoot, Bones.RToe])
		];
	}

	private readonly Dictionary<Transform, bool> _cache = [];

	private bool IsTransformVisibleCached(Transform value, Func<Transform, bool> isVisibleFunc)
	{
		if (_cache.TryGetValue(value, out bool isVisible))
		{
			return isVisible;
		}

		isVisible = isVisibleFunc(value);
		_cache[value] = isVisible;
		return isVisible;
	}

	private void ClearTransformCache()
	{
		_cache.Clear();
	}

	public static bool IsScoped()
	{
		var player = GameState.Current?.LocalPlayer;
		if (player == null)
			return false;

		if (player.HandsController is not { IsAiming: true })
			return false;

		var aimingMod = player.ProceduralWeaponAnimation?.CurrentAimingMod;
		if (aimingMod == null || aimingMod.ScopesCount <= 0)
			return false;

		return aimingMod.GetCurrentOpticZoom() > 1;
	}

	private static bool AimingCheck(Camera camera, Player player)
	{
		var handsController = player.HandsController;
		if (handsController == null)
			return false;

		var weaponAnimation = player.ProceduralWeaponAnimation;
		if (weaponAnimation == null)
			return false;

		var aimingMod = weaponAnimation.CurrentAimingMod;
		if (aimingMod == null)
			return false;

		if (aimingMod.ScopesCount <= 0)
			return false;

		var zoom = aimingMod.GetCurrentOpticZoom();
		var isAiming = handsController.IsAiming;

		if (isAiming && zoom <= 1)
			isAiming = false;

		var currentOptic = weaponAnimation.HandsContainer.Weapon.GetComponentInChildren<OpticSight>();
		if (isAiming && currentOptic != null)
			GetScopeParameters(camera, currentOptic);

		if (_opticCamera != null)
			return isAiming;

		_opticCamera = Camera.allCameras.FirstOrDefault(c => c.name == "BaseOpticCamera(Clone)");

		return isAiming;
	}

	public PlayerColor GetPlayerColors(Player player)
	{
		var hostileType = player.GetHostileType();
		return GetPlayerColors(hostileType);
	}

	public PlayerColor GetPlayerColors(HostileType hostileType)
	{
		return hostileType switch
		{
			HostileType.Bear => BearColors,
			HostileType.Usec => UsecColors,
			HostileType.Scav => ScavColors,
			HostileType.Boss => BossColors,
			HostileType.Cultist => CultistColors,
			HostileType.ScavRaider => ScavRaiderColors,
			HostileType.ScavAssault => ScavAssaultColors,
			HostileType.Marksman => MarksmanColors,
			HostileType.RogueUsec => RogueUsecColors,
			_ => ScavColors,
		};
	}

	public static Vector2 ScopePointToScreenPoint(Camera camera, Vector3 worldPoint, bool clamp = false)
	{
		if (_opticCamera == null || !GetCameraOffset(camera, out var scale, out var cameraOffset))
			return camera.WorldPointToScreenPoint(worldPoint);

		var scopePoint = (Vector2)_opticCamera.WorldToScreenPoint(worldPoint) + cameraOffset;
		scopePoint.y = Screen.height - scopePoint.y * scale;
		scopePoint.x *= scale;

		if (clamp)
			return ClampPointToScope(scopePoint);

		var distance = Vector2.Distance(_scopeParameters.center, scopePoint);
		if (distance <= _scopeParameters.radius)
			return scopePoint;

		return Vector2.zero;
	}

	private static bool GetCameraOffset(Camera camera, out float scale, out Vector2 cameraOffset)
	{
		scale = 0f;
		cameraOffset = Vector2.zero;

		if (_opticCamera == null)
			return false;

		scale = Screen.height / (float)camera.scaledPixelHeight;
		cameraOffset = new Vector2(
			camera.pixelWidth / 2 - _opticCamera.pixelWidth / 2,
			camera.pixelHeight / 2 - _opticCamera.pixelHeight / 2);

		return true;
	}
	private static Vector2 ClampPointToScope(Vector2 scopePoint)
	{
		var distance = Vector2.Distance(_scopeParameters.center, scopePoint);

		var clampedPoint = scopePoint;

		if (distance > _scopeParameters.radius)
		{
			var clampedVector = (scopePoint - _scopeParameters.center).normalized * _scopeParameters.radius;
			clampedPoint = _scopeParameters.center + clampedVector;
		}

		return clampedPoint;
	}

	private static void GetScopeParameters(Camera camera, OpticSight currentOptic)
	{
		var opticTransform = currentOptic.LensRenderer.transform;
		var lensMesh = currentOptic.LensRenderer.GetComponent<MeshFilter>().mesh;
		var lensUpperRight = opticTransform.TransformPoint(lensMesh.bounds.max);
		var lensUpperLeft = opticTransform.TransformPoint(new Vector3(lensMesh.bounds.min.x, 0, lensMesh.bounds.max.z));

		var lensUpperRight3D = camera.WorldPointToScreenPoint(lensUpperRight);
		var lensUpperLeft3D = camera.WorldPointToScreenPoint(lensUpperLeft);
		_scopeParameters.radius = Vector2.Distance(lensUpperRight3D, lensUpperLeft3D) / 2;
		_scopeParameters.center = camera.WorldPointToScreenPoint(opticTransform.position);
	}
}
