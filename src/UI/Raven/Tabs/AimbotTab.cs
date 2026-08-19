using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class AimbotTab : IRavenTab
{
	public string Title => "Aimbot";

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
		using (RavenMenu.Card("Aim"))
		{
			if (aimbot == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
			GUILayout.Label("Hold Key", RavenTheme.Label, GUILayout.ExpandWidth(true));
			GUILayout.Label(aimbot.Key.ToString(), RavenTheme.ValueLabel, GUILayout.Width(90f));
			GUILayout.EndHorizontal();

			RavenWidgets.Spacer(4f);
			aimbot.MaximumDistance = RavenWidgets.Slider("Max Distance", aimbot.MaximumDistance, 10f, 1500f, $"{aimbot.MaximumDistance:0}m");
			GUILayout.Label("Nothing past this is touched at all,\nnot even by Magic Bullets.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.Smoothness = RavenWidgets.Slider("Smoothness", aimbot.Smoothness, 0f, 1f, $"{aimbot.Smoothness:0.###}");
			RavenWidgets.Spacer(6f);
			aimbot.ElevationAdjustment = RavenWidgets.Checkbox(aimbot.ElevationAdjustment, "Elevation Adjustment");
		}
	}

	private static void DrawSilentCard(Aimbot? aimbot)
	{
		using (RavenMenu.Card("Silent Aim"))
		{
			if (aimbot == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			aimbot.SilentAim = RavenWidgets.SwitchRow(aimbot.SilentAim, "Enable");
			RavenWidgets.Spacer(4f);
			aimbot.SilentAimSpeedFactor = RavenWidgets.Slider("Speed Factor", aimbot.SilentAimSpeedFactor, 1f, 300f, $"{aimbot.SilentAimSpeedFactor:0}");
			RavenWidgets.Spacer(6f);
			aimbot.SilentAimNextShotDelay = RavenWidgets.Slider("Shot Delay", aimbot.SilentAimNextShotDelay, 0f, 2f, $"{aimbot.SilentAimNextShotDelay:0.00}s");

			RavenWidgets.Spacer(10f);
			RavenWidgets.Section("Magic Bullets");

			aimbot.MagicBullets = RavenWidgets.Checkbox(aimbot.MagicBullets, "Bend Own Shots");
			GUILayout.Label("Redirects the rounds you fire.\nNever pulls the trigger for you.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.MagicBulletExtendFlight = RavenWidgets.Checkbox(aimbot.MagicBulletExtendFlight, "Extend Flight Time");
			aimbot.MagicBulletFlightTime = RavenWidgets.Slider("Flight Time", aimbot.MagicBulletFlightTime, 1f, 30f, $"{aimbot.MagicBulletFlightTime:0}s");
			GUILayout.Label("Keeps the round alive past the\nammo's own expiry. Raises range.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.MagicBulletBoostSpeed = RavenWidgets.Checkbox(aimbot.MagicBulletBoostSpeed, "Boost Muzzle Speed");
			aimbot.MagicBulletSpeedFactor = RavenWidgets.Slider("Muzzle Boost", aimbot.MagicBulletSpeedFactor, 1f, 100f, $"{aimbot.MagicBulletSpeedFactor:0}x");
			GUILayout.Label("Leave at 1x. Higher speeds bleed\noff to drag and cut range.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);
			aimbot.CompensateDrop = RavenWidgets.Checkbox(aimbot.CompensateDrop, "Compensate Drop");
			aimbot.LeadTarget = RavenWidgets.Checkbox(aimbot.LeadTarget, "Lead Moving Targets");
			aimbot.RequireLineOfSight = RavenWidgets.Checkbox(aimbot.RequireLineOfSight, "Require Line Of Sight");
			GUILayout.Label("Off bends shots through cover.\nPair it with Wall Penetration.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawFovCard(Aimbot? aimbot)
	{
		using (RavenMenu.Card("Field Of View"))
		{
			if (aimbot == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			var radius = aimbot.FovRadius;
			aimbot.FovRadius = RavenWidgets.Slider("FOV Radius", radius, 0f, 600f, radius <= 0f ? "off" : $"{radius:0}px");
			RavenWidgets.Spacer(6f);
			aimbot.ShowFovCircle = RavenWidgets.Checkbox(aimbot.ShowFovCircle, "Show FOV Circle", aimbot.FovCircleColor);
			RavenWidgets.Spacer(2f);
			aimbot.FovCircleThickness = RavenWidgets.Slider("Circle Thickness", aimbot.FovCircleThickness, 1f, 6f, $"{aimbot.FovCircleThickness:0.#}px");
		}
	}

	private static void DrawPenetrationCard()
	{
		using (RavenMenu.Card("Penetration"))
		{
			RavenTabHelper.FeatureSwitch<WallShoot>("Wall Shoot");
			GUILayout.Label("Shoot through walls with maximum penetration\nand no ricochet or deviation.", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(10f);

			RavenTabHelper.FeatureSwitch<InstantKill>("Instant Kill");
			GUILayout.Label("Any hit you land is lethal.\nNever applies to yourself.", RavenTheme.MutedLabel);
		}
	}
}
