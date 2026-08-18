using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class WeaponTab : IRavenTab
{
	public string Title => "Weapon";

	public void Draw()
	{
		RavenTabHelper.BeginColumns(3);

		RavenTabHelper.BeginColumn();
		DrawFiringCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawHandlingCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawGearCard();
		DrawThrowablesCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static void DrawFiringCard()
	{
		using (RavenMenu.Card("Firing"))
		{
			RavenTabHelper.FeatureSwitch<Ammunition>("Unlimited Ammo");
			RavenWidgets.Spacer(4f);

			var auto = FeatureFactory.GetFeature<AutomaticGun>();
			if (auto == null)
				return;

			auto.Enabled = RavenWidgets.SwitchRow(auto.Enabled, "Force Full Auto");
			RavenWidgets.Spacer(4f);

			auto.OverrideRate = RavenWidgets.SwitchRow(auto.OverrideRate, "Custom Fire Rate");
			RavenWidgets.Spacer(4f);

			var rate = RavenWidgets.Slider("Fire Rate", auto.Rate, 100f, 1200f, $"{auto.Rate} rpm");
			auto.Rate = Mathf.RoundToInt(rate);

			if (!auto.OverrideRate)
				GUILayout.Label("Slider needs Custom Fire Rate on.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawHandlingCard()
	{
		using (RavenMenu.Card("Handling"))
		{
			var recoil = FeatureFactory.GetFeature<NoRecoil>();
			if (recoil != null)
			{
				recoil.Enabled = RavenWidgets.SwitchRow(recoil.Enabled, "No Recoil");

				recoil.Strength = RavenWidgets.Slider("Reduction", recoil.Strength, 0f, 1f, $"{recoil.Strength * 100f:0}%");
				RavenWidgets.Spacer(6f);
			}

			RavenTabHelper.FeatureCheckbox<NoSway>("No Sway");
			RavenTabHelper.FeatureCheckbox<NoMalfunctions>("No Malfunctions");

			RavenWidgets.Spacer(10f);

			var tuning = FeatureFactory.GetFeature<WeaponTuning>();
			if (tuning == null)
				return;

			tuning.Enabled = RavenWidgets.SwitchRow(tuning.Enabled, "Handling Override");
			RavenWidgets.Spacer(4f);
			tuning.OverrideErgonomics = RavenWidgets.Checkbox(tuning.OverrideErgonomics, "Set Ergonomics");
			tuning.Ergonomics = RavenWidgets.Slider("Ergonomics", tuning.Ergonomics, 0f, 100f, $"{tuning.Ergonomics:0}");
			RavenWidgets.Spacer(6f);
			tuning.NoWeight = RavenWidgets.Checkbox(tuning.NoWeight, "Weightless");
			tuning.NoOverheat = RavenWidgets.Checkbox(tuning.NoOverheat, "No Overheat");
			GUILayout.Label("Ergonomics drives aim speed and sway.\nWeight feeds stamina drain.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawGearCard()
	{
		using (RavenMenu.Card("Gear"))
		{
			RavenTabHelper.FeatureCheckbox<Durability>("Maximum Durability");
			RavenTabHelper.FeatureCheckbox<Examine>("Everything Examined");
			RavenTabHelper.FeatureCheckbox<InstantResearch>("Instant Research");
			GUILayout.Label("Examined marks items as inspected.\nInstant Research removes the search wait.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawThrowablesCard()
	{
		using (RavenMenu.Card("Throwables"))
		{

			RavenTabHelper.FeatureTrigger<QuickTrow>("Quick Throw", "Throw");
		}
	}
}
