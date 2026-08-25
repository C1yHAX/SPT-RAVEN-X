using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

using Stamina = RavenX.Features.Stamina;
using Speed = RavenX.Features.Speed;
using FreeCamera = RavenX.Features.FreeCamera;

internal class PlayerTab : IRavenTab
{
	public string Title => "Player";

	public void Draw()
	{
		RavenTabHelper.BeginColumns(3);

		RavenTabHelper.BeginColumn();
		DrawSurvivalCard();
		DrawActionsCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawMovementCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawCameraCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static void DrawSurvivalCard()
	{
		using (RavenMenu.Card("Survival"))
		{
			var health = FeatureFactory.GetFeature<Health>();
			if (health != null)
			{
				health.Enabled = RavenWidgets.SwitchRow(health.Enabled, "God Mode");
				RavenWidgets.Spacer(4f);
				health.VitalsOnly = RavenWidgets.Checkbox(health.VitalsOnly, "Vitals Only");
				health.RemoveNegativeEffects = RavenWidgets.Checkbox(health.RemoveNegativeEffects, "Remove Negative Effects");
				health.FoodWater = RavenWidgets.Checkbox(health.FoodWater, "Keep Food & Water");
				RavenWidgets.Spacer(4f);
			}

			RavenTabHelper.FeatureCheckbox<Stamina>("Unlimited Stamina");

			var tuning = FeatureFactory.GetFeature<CharacterTuning>();
			if (tuning == null)
				return;

			RavenWidgets.Spacer(8f);
			tuning.Enabled = RavenWidgets.SwitchRow(tuning.Enabled, "Tuning");
			RavenWidgets.Spacer(4f);
			tuning.WalkSpeed = RavenWidgets.Slider("Walk Speed", tuning.WalkSpeed, 0.5f, 5f, $"{tuning.WalkSpeed:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.SprintSpeed = RavenWidgets.Slider("Sprint Speed", tuning.SprintSpeed, 0.5f, 5f, $"{tuning.SprintSpeed:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.JumpHeight = RavenWidgets.Slider("Jump Height", tuning.JumpHeight, 0.5f, 5f, $"{tuning.JumpHeight:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.HealthRegen = RavenWidgets.Slider("Health Regen", tuning.HealthRegen, 0f, 25f, tuning.HealthRegen <= 0f ? "off" : $"{tuning.HealthRegen:0} hp/s per part");
			RavenWidgets.Spacer(6f);
			tuning.EnergyDrain = RavenWidgets.Slider("Energy Drain", tuning.EnergyDrain, 0f, 2f, $"{tuning.EnergyDrain:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.HydrationDrain = RavenWidgets.Slider("Hydration Drain", tuning.HydrationDrain, 0f, 2f, $"{tuning.HydrationDrain:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.VaultSpeed = RavenWidgets.Slider("Vault Speed", tuning.VaultSpeed, 0.25f, 5f, $"{tuning.VaultSpeed:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.StanceSpeed = RavenWidgets.Slider("Stance Speed", tuning.StanceSpeed, 0.25f, 5f, $"{tuning.StanceSpeed:0.00}x");
		}
	}

	private static void DrawMovementCard()
	{
		using (RavenMenu.Card("Movement"))
		{
			var speed = FeatureFactory.GetFeature<Speed>();
			if (speed != null)
			{

				RavenTabHelper.KeyRow("Speed Boost (hold)", speed.Key);
				speed.Intensity = RavenWidgets.Slider("Intensity", speed.Intensity, 0.5f, 20f, $"{speed.Intensity:0.#}x");
				RavenWidgets.Spacer(6f);
			}

			RavenTabHelper.FeatureCheckbox<NoCollision>("No Collision");
			RavenTabHelper.FeatureCheckbox<Ghost>("Ghost Mode");
			RavenTabHelper.FeatureCheckbox<NoInertia>("No Inertia");
			RavenTabHelper.FeatureCheckbox<SilentMovement>("Silent Movement");
			RavenTabHelper.FeatureCheckbox<NoFallDamage>("No Fall Damage");

			RavenWidgets.Spacer(8f);

			var fly = FeatureFactory.GetFeature<Fly>();
			if (fly == null)
				return;

			fly.Enabled = RavenWidgets.SwitchRow(fly.Enabled, "Fly");
			RavenWidgets.Spacer(4f);
			fly.Speed = RavenWidgets.Slider("Fly Speed", fly.Speed, 1f, 30f, $"{fly.Speed:0.#}");
			RavenWidgets.Spacer(6f);
			fly.FastSpeed = RavenWidgets.Slider("Boost Speed", fly.FastSpeed, 5f, 80f, $"{fly.FastSpeed:0.#}");
			RavenWidgets.Spacer(6f);
			fly.LandOnExit = RavenWidgets.Checkbox(fly.LandOnExit, "Land When Disabled");
			GUILayout.Label($"WASD to move, {fly.UpKey}/{fly.DownKey} for height,\n{fly.FastKey} to boost.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawCameraCard()
	{
		using (RavenMenu.Card("Camera"))
		{
			var camera = FeatureFactory.GetFeature<FreeCamera>();
			if (camera != null)
			{
				camera.Enabled = RavenWidgets.SwitchRow(camera.Enabled, "Free Camera");
				RavenWidgets.Spacer(4f);

				if (camera.Enabled)
				{
					if (FreeCamera.TryGetTargetPosition(out var target))
					{
						GUILayout.Label($"Target  X {target.x:0} · Y {target.y:0} · Z {target.z:0}", RavenTheme.MutedLabel);

						if (RavenWidgets.OutlineButton("TELEPORT HERE", 150f))
							FreeCamera.TeleportToCamera();
					}
					else
					{
						GUILayout.Label("Camera position not available yet.", RavenTheme.MutedLabel);
					}

					RavenTabHelper.KeyRow("Teleport key", camera.Teleport);
					RavenWidgets.Spacer(6f);
				}

				camera.MovementSpeed = RavenWidgets.Slider("Move Speed", camera.MovementSpeed, 1f, 50f, $"{camera.MovementSpeed:0.#}");
				RavenWidgets.Spacer(6f);
				camera.FastMovementSpeed = RavenWidgets.Slider("Fast Speed", camera.FastMovementSpeed, 1f, 100f, $"{camera.FastMovementSpeed:0.#}");
				RavenWidgets.Spacer(6f);
				camera.FreeLookSensitivity = RavenWidgets.Slider("Look Sensitivity", camera.FreeLookSensitivity, 0.1f, 10f, $"{camera.FreeLookSensitivity:0.##}");
				RavenWidgets.Spacer(8f);
			}

			var fov = FeatureFactory.GetFeature<FovChanger>();
			if (fov == null)
				return;

			fov.Enabled = RavenWidgets.SwitchRow(fov.Enabled, "FOV Changer");
			RavenWidgets.Spacer(4f);
			fov.Fov = RavenWidgets.Slider("Field Of View", fov.Fov, 40f, 120f, $"{fov.Fov:0}");
			RavenWidgets.Spacer(6f);
			fov.CameraOffset = RavenWidgets.Slider("Camera Offset", fov.CameraOffset, -1f, 1f, $"{fov.CameraOffset:0.##}");
		}
	}

	private static void DrawActionsCard()
	{
		using (RavenMenu.Card("Actions"))
		{
			RavenTabHelper.FeatureTrigger<Skills>("Max All Skills", "Apply");
			RavenTabHelper.FeatureTrigger<SelfHeal>("Self Heal", "Heal");
		}
	}
}
