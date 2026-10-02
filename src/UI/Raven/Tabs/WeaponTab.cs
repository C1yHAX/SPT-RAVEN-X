using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class WeaponTab : IRavenTab
{
	public string Title => RavenText.L("Weapon");

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
		using (RavenMenu.Card(RavenText.L("Firing")))
		{
			RavenTabHelper.FeatureSwitch<Ammunition>(RavenText.L("Unlimited Ammo"));
			RavenWidgets.Spacer(4f);

			var auto = FeatureFactory.GetFeature<AutomaticGun>();
			if (auto == null)
				return;

			auto.Enabled = RavenWidgets.SwitchRow(auto.Enabled, RavenText.L("Force Full Auto"));
			RavenWidgets.Spacer(4f);

			auto.OverrideRate = RavenWidgets.SwitchRow(auto.OverrideRate, RavenText.L("Custom Fire Rate"));
			RavenWidgets.Spacer(4f);

			var rate = RavenWidgets.Slider(RavenText.L("Fire Rate"), auto.Rate, 100f, 1200f, $"{auto.Rate} rpm");
			auto.Rate = Mathf.RoundToInt(rate);

			if (!auto.OverrideRate)
				GUILayout.Label(RavenText.L("Slider needs Custom Fire Rate on."), RavenTheme.MutedLabel);
		}
	}

	private static void DrawHandlingCard()
	{
		using (RavenMenu.Card(RavenText.L("Handling")))
		{
			var recoil = FeatureFactory.GetFeature<NoRecoil>();
			if (recoil != null)
			{
				recoil.Enabled = RavenWidgets.SwitchRow(recoil.Enabled, RavenText.L("No Recoil"));

				recoil.Strength = RavenWidgets.Slider(RavenText.L("Reduction"), recoil.Strength, 0f, 1f, $"{recoil.Strength * 100f:0}%");
				RavenWidgets.Spacer(6f);
			}

			RavenTabHelper.FeatureCheckbox<NoSway>(RavenText.L("No Sway"));
			RavenTabHelper.FeatureCheckbox<NoMalfunctions>(RavenText.L("No Malfunctions"));

			RavenWidgets.Spacer(10f);

			var tuning = FeatureFactory.GetFeature<WeaponTuning>();
			if (tuning == null)
				return;

			tuning.Enabled = RavenWidgets.SwitchRow(tuning.Enabled, RavenText.L("Handling Override"));
			RavenWidgets.Spacer(4f);
			tuning.OverrideErgonomics = RavenWidgets.Checkbox(tuning.OverrideErgonomics, RavenText.L("Set Ergonomics"));
			tuning.Ergonomics = RavenWidgets.Slider(RavenText.L("Ergonomics"), tuning.Ergonomics, 0f, 100f, $"{tuning.Ergonomics:0}");
			RavenWidgets.Spacer(6f);
			tuning.NoWeight = RavenWidgets.Checkbox(tuning.NoWeight, RavenText.L("Weightless"));
			tuning.NoOverheat = RavenWidgets.Checkbox(tuning.NoOverheat, RavenText.L("No Overheat"));
			GUILayout.Label(RavenText.L("Ergonomics drives aim speed and sway.\nWeight feeds stamina drain."), RavenTheme.MutedLabel);
		}
	}

	private static void DrawGearCard()
	{
		using (RavenMenu.Card(RavenText.L("Gear")))
		{
			RavenTabHelper.FeatureCheckbox<Durability>(RavenText.L("Maximum Durability"));
			RavenTabHelper.FeatureCheckbox<Examine>(RavenText.L("Everything Examined"));
			RavenTabHelper.FeatureCheckbox<InstantResearch>(RavenText.L("Instant Research"));
			GUILayout.Label(RavenText.L("Examined marks items as inspected.\nInstant Research removes the search wait."), RavenTheme.MutedLabel);
		}
	}

	private static void DrawThrowablesCard()
	{
		using (RavenMenu.Card(RavenText.L("Throwables")))
		{

			RavenTabHelper.FeatureTrigger<QuickTrow>(RavenText.L("Quick Throw"), RavenText.L("Throw"));
		}
	}
}
