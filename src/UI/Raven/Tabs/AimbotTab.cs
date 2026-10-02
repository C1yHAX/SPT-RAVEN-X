using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class AimbotTab : IRavenTab
{
	public string Title => RavenText.L("Aimbot");

	public void Draw()
	{
		var aimbot = FeatureFactory.GetFeature<Aimbot>();

		RavenTabHelper.BeginColumns(3);

		RavenTabHelper.BeginColumn();
		DrawAimCard(aimbot);
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawSilentCard(aimbot);
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawFovCard(aimbot);
		DrawPenetrationCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static void DrawAimCard(Aimbot? aimbot)
	{
		using (RavenMenu.Card(RavenText.L("Aim")))
		{
			if (aimbot == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
			GUILayout.Label(RavenText.L("Hold Key"), RavenTheme.Label, GUILayout.ExpandWidth(true));
			GUILayout.Label(aimbot.Key.ToString(), RavenTheme.ValueLabel, GUILayout.Width(90f));
			GUILayout.EndHorizontal();

			RavenWidgets.Spacer(4f);
			var distance = aimbot.MaximumDistance;
			aimbot.MaximumDistance = RavenWidgets.Slider(RavenText.L("Max Distance"), distance, 0f, 1500f, distance <= 0f ? "unlimited" : $"{distance:0}m");
			GUILayout.Label(RavenText.L("Zero allows targets at any distance.\nThe limit also applies to Magic Bullets."), RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.Smoothness = RavenWidgets.Slider(RavenText.L("Smoothness"), aimbot.Smoothness, 0f, 1f, $"{aimbot.Smoothness:0.###}");
			RavenWidgets.Spacer(6f);
			aimbot.ElevationAdjustment = RavenWidgets.Checkbox(aimbot.ElevationAdjustment, RavenText.L("Elevation Adjustment"));
		}
	}

	private static void DrawSilentCard(Aimbot? aimbot)
	{
		using (RavenMenu.Card(RavenText.L("Silent Aim")))
		{
			if (aimbot == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			aimbot.SilentAim = RavenWidgets.SwitchRow(aimbot.SilentAim, RavenText.L("Enable"));
			RavenWidgets.Spacer(4f);
			aimbot.SilentAimSpeedFactor = RavenWidgets.Slider(RavenText.L("Speed Factor"), aimbot.SilentAimSpeedFactor, 1f, 300f, $"{aimbot.SilentAimSpeedFactor:0}");
			RavenWidgets.Spacer(6f);
			aimbot.SilentAimNextShotDelay = RavenWidgets.Slider(RavenText.L("Shot Delay"), aimbot.SilentAimNextShotDelay, 0f, 2f, $"{aimbot.SilentAimNextShotDelay:0.00}s");

			RavenWidgets.Spacer(10f);
			RavenWidgets.Section(RavenText.L("Magic Bullets"));

			aimbot.MagicBullets = RavenWidgets.Checkbox(aimbot.MagicBullets, RavenText.L("Bend Own Shots"));
			GUILayout.Label(RavenText.L("Redirects the rounds you fire.\nNever pulls the trigger for you."), RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.MagicBulletExtendFlight = RavenWidgets.Checkbox(aimbot.MagicBulletExtendFlight, RavenText.L("Extend Flight Time"));
			aimbot.MagicBulletFlightTime = RavenWidgets.Slider(RavenText.L("Flight Time"), aimbot.MagicBulletFlightTime, 1f, 30f, $"{aimbot.MagicBulletFlightTime:0}s");
			GUILayout.Label(RavenText.L("Keeps the round alive past the\nammo's own expiry. Raises range."), RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.MagicBulletBoostSpeed = RavenWidgets.Checkbox(aimbot.MagicBulletBoostSpeed, RavenText.L("Boost Muzzle Speed"));
			aimbot.MagicBulletSpeedFactor = RavenWidgets.Slider(RavenText.L("Muzzle Boost"), aimbot.MagicBulletSpeedFactor, 1f, 100f, $"{aimbot.MagicBulletSpeedFactor:0}x");
			GUILayout.Label(RavenText.L("Leave at 1x. Higher speeds bleed\noff to drag and cut range."), RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.CompensateDrop = RavenWidgets.Checkbox(aimbot.CompensateDrop, RavenText.L("Compensate Drop"));
			aimbot.LeadTarget = RavenWidgets.Checkbox(aimbot.LeadTarget, RavenText.L("Lead Moving Targets"));
			aimbot.RequireLineOfSight = RavenWidgets.Checkbox(aimbot.RequireLineOfSight, RavenText.L("Require Line Of Sight"));
			GUILayout.Label(RavenText.L("Off bends shots through cover.\nPair it with Wall Penetration."), RavenTheme.MutedLabel);
		}
	}

	private static void DrawFovCard(Aimbot? aimbot)
	{
		using (RavenMenu.Card(RavenText.L("Field Of View")))
		{
			if (aimbot == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			var radius = aimbot.FovRadius;
			aimbot.FovRadius = RavenWidgets.Slider(RavenText.L("FOV Radius"), radius, 0f, 600f, radius <= 0f ? "off" : $"{radius:0}px");
			RavenWidgets.Spacer(6f);
			aimbot.ShowFovCircle = RavenWidgets.Checkbox(aimbot.ShowFovCircle, RavenText.L("Show FOV Circle"), aimbot.FovCircleColor);
			RavenWidgets.Spacer(2f);
			aimbot.FovCircleThickness = RavenWidgets.Slider(RavenText.L("Circle Thickness"), aimbot.FovCircleThickness, 1f, 6f, $"{aimbot.FovCircleThickness:0.#}px");
		}
	}

	private static void DrawPenetrationCard()
	{
		using (RavenMenu.Card(RavenText.L("Penetration")))
		{
			RavenTabHelper.FeatureSwitch<WallShoot>(RavenText.L("Wall Shoot"));
			GUILayout.Label(RavenText.L("Shoot through walls with maximum penetration\nand no ricochet or deviation."), RavenTheme.MutedLabel);
			RavenWidgets.Spacer(10f);

			RavenTabHelper.FeatureSwitch<InstantKill>(RavenText.L("Instant Kill"));
			GUILayout.Label(RavenText.L("Any hit you land is lethal.\nNever applies to yourself."), RavenTheme.MutedLabel);
		}
	}
}
