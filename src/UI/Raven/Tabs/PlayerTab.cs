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
	public string Title => RavenText.L("Player");

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
		using (RavenMenu.Card(RavenText.L("Survival")))
		{
			var health = FeatureFactory.GetFeature<Health>();
			if (health != null)
			{
				health.Enabled = RavenWidgets.SwitchRow(health.Enabled, RavenText.L("God Mode"));
				RavenWidgets.Spacer(4f);
				health.VitalsOnly = RavenWidgets.Checkbox(health.VitalsOnly, RavenText.L("Vitals Only"));
				health.RemoveNegativeEffects = RavenWidgets.Checkbox(health.RemoveNegativeEffects, RavenText.L("Remove Negative Effects"));
				health.FoodWater = RavenWidgets.Checkbox(health.FoodWater, RavenText.L("Keep Food & Water"));
				RavenWidgets.Spacer(4f);
			}

			RavenTabHelper.FeatureCheckbox<Stamina>(RavenText.L("Unlimited Stamina"));

			var tuning = FeatureFactory.GetFeature<CharacterTuning>();
			if (tuning == null)
				return;

			RavenWidgets.Spacer(8f);
			tuning.Enabled = RavenWidgets.SwitchRow(tuning.Enabled, RavenText.L("Tuning"));
			RavenWidgets.Spacer(4f);
			tuning.WalkSpeed = RavenWidgets.Slider(RavenText.L("Walk Speed"), tuning.WalkSpeed, 0.5f, 5f, $"{tuning.WalkSpeed:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.SprintSpeed = RavenWidgets.Slider(RavenText.L("Sprint Speed"), tuning.SprintSpeed, 0.5f, 5f, $"{tuning.SprintSpeed:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.JumpHeight = RavenWidgets.Slider(RavenText.L("Jump Height"), tuning.JumpHeight, 0.5f, 5f, $"{tuning.JumpHeight:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.HealthRegen = RavenWidgets.Slider(RavenText.L("Health Regen"), tuning.HealthRegen, 0f, 25f, tuning.HealthRegen <= 0f ? "off" : $"{tuning.HealthRegen:0} hp/s per part");
			RavenWidgets.Spacer(6f);
			tuning.EnergyDrain = RavenWidgets.Slider(RavenText.L("Energy Drain"), tuning.EnergyDrain, 0f, 2f, $"{tuning.EnergyDrain:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.HydrationDrain = RavenWidgets.Slider(RavenText.L("Hydration Drain"), tuning.HydrationDrain, 0f, 2f, $"{tuning.HydrationDrain:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.VaultSpeed = RavenWidgets.Slider(RavenText.L("Vault Speed"), tuning.VaultSpeed, 0.25f, 5f, $"{tuning.VaultSpeed:0.00}x");
			RavenWidgets.Spacer(6f);
			tuning.StanceSpeed = RavenWidgets.Slider(RavenText.L("Stance Speed"), tuning.StanceSpeed, 0.25f, 5f, $"{tuning.StanceSpeed:0.00}x");
		}
	}

	private static void DrawMovementCard()
	{
		using (RavenMenu.Card(RavenText.L("Movement")))
		{
			var speed = FeatureFactory.GetFeature<Speed>();
			if (speed != null)
			{

				RavenTabHelper.KeyRow(RavenText.L("Speed Boost (hold)"), speed.Key);
				speed.Intensity = RavenWidgets.Slider(RavenText.L("Intensity"), speed.Intensity, 0.5f, 20f, $"{speed.Intensity:0.#}x");
				RavenWidgets.Spacer(6f);
			}

			RavenTabHelper.FeatureCheckbox<NoCollision>(RavenText.L("No Collision"));
			RavenTabHelper.FeatureCheckbox<Ghost>(RavenText.L("Ghost Mode"));
			RavenTabHelper.FeatureCheckbox<NoInertia>(RavenText.L("No Inertia"));
			RavenTabHelper.FeatureCheckbox<SilentMovement>(RavenText.L("Silent Movement"));
			RavenTabHelper.FeatureCheckbox<NoFallDamage>(RavenText.L("No Fall Damage"));

			RavenWidgets.Spacer(8f);

			var fly = FeatureFactory.GetFeature<Fly>();
			if (fly == null)
				return;

			fly.Enabled = RavenWidgets.SwitchRow(fly.Enabled, RavenText.L("Fly"));
			RavenWidgets.Spacer(4f);
			fly.Speed = RavenWidgets.Slider(RavenText.L("Fly Speed"), fly.Speed, 1f, 30f, $"{fly.Speed:0.#}");
			RavenWidgets.Spacer(6f);
			fly.FastSpeed = RavenWidgets.Slider(RavenText.L("Boost Speed"), fly.FastSpeed, 5f, 80f, $"{fly.FastSpeed:0.#}");
			RavenWidgets.Spacer(6f);
			fly.LandOnExit = RavenWidgets.Checkbox(fly.LandOnExit, RavenText.L("Land When Disabled"));
			GUILayout.Label($"WASD to move, {fly.UpKey}/{fly.DownKey} for height,\n{fly.FastKey} to boost.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawCameraCard()
	{
		using (RavenMenu.Card(RavenText.L("Camera")))
		{
			var camera = FeatureFactory.GetFeature<FreeCamera>();
			if (camera != null)
			{
				camera.Enabled = RavenWidgets.SwitchRow(camera.Enabled, RavenText.L("Free Camera"));
				RavenWidgets.Spacer(4f);

				if (camera.Enabled)
				{
					if (FreeCamera.TryGetTargetPosition(out var target))
					{
						GUILayout.Label($"Target  X {target.x:0} · Y {target.y:0} · Z {target.z:0}", RavenTheme.MutedLabel);

						if (RavenWidgets.OutlineButton(RavenText.L("TELEPORT HERE"), 150f))
							FreeCamera.TeleportToCamera();
					}
					else
					{
						GUILayout.Label(RavenText.L("Camera position not available yet."), RavenTheme.MutedLabel);
					}

					RavenTabHelper.KeyRow(RavenText.L("Teleport key"), camera.Teleport);
					RavenWidgets.Spacer(6f);
				}

				camera.MovementSpeed = RavenWidgets.Slider(RavenText.L("Move Speed"), camera.MovementSpeed, 1f, 50f, $"{camera.MovementSpeed:0.#}");
				RavenWidgets.Spacer(6f);
				camera.FastMovementSpeed = RavenWidgets.Slider(RavenText.L("Fast Speed"), camera.FastMovementSpeed, 1f, 100f, $"{camera.FastMovementSpeed:0.#}");
				RavenWidgets.Spacer(6f);
				camera.FreeLookSensitivity = RavenWidgets.Slider(RavenText.L("Look Sensitivity"), camera.FreeLookSensitivity, 0.1f, 10f, $"{camera.FreeLookSensitivity:0.##}");
				RavenWidgets.Spacer(8f);
			}

			var fov = FeatureFactory.GetFeature<FovChanger>();
			if (fov == null)
				return;

			fov.Enabled = RavenWidgets.SwitchRow(fov.Enabled, RavenText.L("FOV Changer"));
			RavenWidgets.Spacer(4f);
			fov.Fov = RavenWidgets.Slider(RavenText.L("Field Of View"), fov.Fov, 40f, 120f, $"{fov.Fov:0}");
			RavenWidgets.Spacer(6f);
			fov.CameraOffset = RavenWidgets.Slider(RavenText.L("Camera Offset"), fov.CameraOffset, -1f, 1f, $"{fov.CameraOffset:0.##}");
		}
	}

	private static void DrawActionsCard()
	{
		using (RavenMenu.Card(RavenText.L("Actions")))
		{
			RavenTabHelper.FeatureTrigger<Skills>(RavenText.L("Max All Skills"), RavenText.L("Apply"));
			RavenTabHelper.FeatureTrigger<SelfHeal>(RavenText.L("Self Heal"), RavenText.L("Heal"));
		}
	}
}
