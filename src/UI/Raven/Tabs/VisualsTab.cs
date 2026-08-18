using RavenX.Features;
using UnityEngine;
using EFT;
using JsonType;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

using Map = RavenX.Features.Map;
using ThermalVision = RavenX.Features.ThermalVision;
using Quests = RavenX.Features.Quests;

internal class VisualsTab : IRavenTab
{
	public string Title => "Visuals";

	public void Draw()
	{
		var players = FeatureFactory.GetFeature<Players>();

		// Three columns that each stack several cards, rather than six that wrap onto
		// a second row. A wrapped row starts below the tallest card of the row above
		// it, which is what left the large empty blocks.
		RavenTabHelper.BeginColumns(3);

		RavenTabHelper.BeginColumn();
		DrawPlayersCard(players);
		DrawOtherCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawChamsCard();
		DrawRolesCard(players);
		DrawFiltersCard(players);
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn();
		DrawWorldCard();
		DrawRenderCard(players);
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private static void DrawPlayersCard(Players? players)
	{
		using (RavenMenu.Card("Players ESP"))
		{
			if (players == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			players.Enabled = RavenWidgets.SwitchRow(players.Enabled, "Enable");
			RavenWidgets.Spacer(4f);

			players.ShowBoxes = RavenWidgets.Checkbox(players.ShowBoxes, "Show Boxes");
			players.ShowInfos = RavenWidgets.Checkbox(players.ShowInfos, "Show Info");
			players.ShowSkeletons = RavenWidgets.Checkbox(players.ShowSkeletons, "Show Skeleton");
			players.ShowCharms = RavenWidgets.Checkbox(players.ShowCharms, "Show Charms");
			players.XRayVision = RavenWidgets.Checkbox(players.XRayVision, "X-Ray Vision");
			players.ShowShootable = RavenWidgets.Checkbox(players.ShowShootable, "Show Shootable", players.ShootableColors.Color);
			players.ShowNotShootable = RavenWidgets.Checkbox(players.ShowNotShootable, "Show Blocked", players.NotShootableColors.Color);

			if (players.ShowShootable)
			{
				players.PerLimbVisibility = RavenWidgets.Checkbox(players.PerLimbVisibility, "Per-Limb Visibility");

				if (players.PerLimbVisibility && !players.ShowSkeletons)
					GUILayout.Label("Needs Show Skeleton to be visible.", RavenTheme.MutedLabel);
			}
			players.ShowSnapLines = RavenWidgets.Checkbox(players.ShowSnapLines, "Snap Lines", players.SnapLineColor);

			RavenWidgets.Spacer(8f);
			RavenWidgets.Section("Readout");

			players.ShowNames = RavenWidgets.Checkbox(players.ShowNames, "Names");
			players.ShowWeapons = RavenWidgets.Checkbox(players.ShowWeapons, "Weapon");
			players.ShowDistance = RavenWidgets.Checkbox(players.ShowDistance, "Distance");
			players.ShowHealthBar = RavenWidgets.Checkbox(players.ShowHealthBar, "HP Bar");

			if (players.ShowHealthBar)
			{
				players.ShowHealthText = false;
			}
			else
			{
				players.ShowHealthText = RavenWidgets.Checkbox(players.ShowHealthText, "Health Value");
			}

			if (!players.ShowInfos)
				GUILayout.Label("Enable Show Info to see these.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawFiltersCard(Players? players)
	{
		using (RavenMenu.Card("Filters"))
		{
			if (players == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			var distance = players.MaximumDistance;
			players.MaximumDistance = RavenWidgets.Slider(
				"Max Distance", distance, 0f, 1000f,
				distance <= 0f ? "unlimited" : $"{distance:0}m");
		}
	}

	private static Vector2 _roleScroll;
	private static int _roleGroup;
	private static Vector2 _chamScroll;
	private static int _chamGroup;

	private static void DrawRolesCard(Players? players)
	{
		using (RavenMenu.Card("ESP Roles"))
		{
			if (players == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			DrawRoleList(players.RoleFor, ref _roleGroup, ref _roleScroll, players);

			GUILayout.Label("Colour marks the role. Blocked limbs\nuse a dimmed shade of it.", RavenTheme.MutedLabel);
		}
	}

	private static void DrawRoleList(System.Func<string, RoleSetting> resolve, ref int group, ref Vector2 scroll, object key)
	{
		var groups = RoleCatalog.Groups;
		var names = new string[groups.Count + 1];
		names[0] = "All";

		for (var i = 0; i < groups.Count; i++)
			names[i + 1] = groups[i];

		group = Mathf.Clamp(RavenWidgets.Dropdown("Group", group, names, key), 0, names.Length - 1);

		RavenWidgets.Spacer(6f);

		GUILayout.BeginHorizontal();
		var enableAll = RavenWidgets.OutlineButton("ALL ON", 82f);
		GUILayout.Space(6f);
		var disableAll = RavenWidgets.OutlineButton("ALL OFF", 82f);
		GUILayout.EndHorizontal();

		RavenWidgets.Spacer(6f);

		scroll = GUILayout.BeginScrollView(scroll, false, true, GUILayout.Height(230f));

		foreach (var definition in RoleCatalog.Definitions)
		{
			if (group > 0 && definition.Group != names[group])
				continue;

			var setting = resolve(definition.Key);

			if (enableAll)
				setting.Enabled = true;
			else if (disableAll)
				setting.Enabled = false;

			setting.Enabled = RavenWidgets.Checkbox(setting.Enabled, definition.Label, setting.Visible);
		}

		GUILayout.EndScrollView();
	}

	private static void DrawChamsCard()
	{
		using (RavenMenu.Card("Chams"))
		{
			var chams = FeatureFactory.GetFeature<Chams>();
			if (chams == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			chams.Enabled = RavenWidgets.SwitchRow(chams.Enabled, "Enable");
			RavenWidgets.Spacer(4f);

			DrawRoleList(chams.RoleFor, ref _chamGroup, ref _chamScroll, chams);

			RavenWidgets.Spacer(8f);
			chams.Opacity = RavenWidgets.Slider("Opacity", chams.Opacity, 0.15f, 1f, $"{chams.Opacity * 100f:0}%");

			RavenWidgets.Spacer(6f);
			var distance = chams.MaximumDistance;
			chams.MaximumDistance = RavenWidgets.Slider(
				"Max Distance", distance, 0f, 800f,
				distance <= 0f ? "unlimited" : $"{distance:0}m");

			RavenWidgets.Spacer(8f);
			RavenWidgets.Section("World");

			chams.ShowCorpses = RavenWidgets.Checkbox(chams.ShowCorpses, "Bodies", chams.CorpseColor);
			chams.ShowLoot = RavenWidgets.Checkbox(chams.ShowLoot, "Loot", chams.LootColor);

			RavenWidgets.Spacer(6f);
			GUILayout.Label("Second colour marks the parts\nthat are behind cover.", RavenTheme.MutedLabel);
		}
	}

	// Every control here is drawn unconditionally, including the warning line, which
	// carries empty text when there is nothing to warn about. A control that appears
	// only sometimes would change the count between the layout and repaint passes.
	private static string LootHint(LootItems loot)
	{
		// Kept to one line: the label has a fixed height, so a wrapping message is cut off.
		if (loot.MaximumPrice > 0 && loot.MaximumPrice < loot.MinimumPrice)
			return "Upper limit is below the lower one.";

		if ((loot.MinimumPrice > 0 || loot.MaximumPrice > 0) && !HandbookCatalog.Ready)
			return "Prices are not read yet, so they are ignored.";

		if (loot.TrackedNames.Count > 0)
			return $"Limited to the {loot.TrackedNames.Count} tracked items.";

		return string.Empty;
	}

	private static void DrawLootFilters(LootItems loot)
	{
		var from = RavenWidgets.Slider("Price from", loot.MinimumPrice, 0f, 200000f,
			loot.MinimumPrice > 0 ? $"{loot.MinimumPrice}" : "any");
		loot.MinimumPrice = Mathf.RoundToInt(from / 500f) * 500;

		var to = RavenWidgets.Slider("Price to", loot.MaximumPrice, 0f, 200000f,
			loot.MaximumPrice > 0 ? $"{loot.MaximumPrice}" : "no limit");
		loot.MaximumPrice = Mathf.RoundToInt(to / 500f) * 500;

		// Always drawn and always the same height: the sliders above write their values
		// during the drag, so sizing this from the text would leave the layout pass and
		// the repaint pass of that frame disagreeing about how tall it is.
		GUILayout.Label(LootHint(loot), RavenTheme.MutedLabel, GUILayout.Height(RavenTheme.RowHeight));

		var rank = LootItems.RarityRank(loot.MinimumRarity);
		var picked = RavenWidgets.Dropdown("Rarity", rank, RarityOptions, loot);
		if (picked != rank)
			loot.MinimumRarity = RarityFromRank(picked);

		var distance = RavenWidgets.Slider("Maximum distance", loot.MaximumDistance, 0f, 500f,
			loot.MaximumDistance > 0 ? $"{loot.MaximumDistance:0} m" : "unlimited");
		loot.MaximumDistance = Mathf.Round(distance / 10f) * 10f;
	}

	private static readonly string[] RarityOptions = ["any", "common and up", "rare and up", "superrare only"];

	private static ELootRarity RarityFromRank(int rank) => rank switch
	{
		3 => ELootRarity.Superrare,
		2 => ELootRarity.Rare,
		1 => ELootRarity.Common,
		_ => ELootRarity.Not_exist
	};

	private static void DrawWorldCard()
	{
		using (RavenMenu.Card("Loot & World ESP"))
		{
			var loot = FeatureFactory.GetFeature<LootItems>();
			if (loot != null)
			{
				loot.Enabled = RavenWidgets.SwitchRow(loot.Enabled, "Loot ESP");
				GUILayout.Label("Tags the individual items.", RavenTheme.MutedLabel);
				RavenWidgets.Spacer(4f);
				loot.ShowPrices = RavenWidgets.Checkbox(loot.ShowPrices, "Show Prices");
				loot.TrackWishlist = RavenWidgets.Checkbox(loot.TrackWishlist, "Track Wishlist");
				loot.TrackAutoWishlist = RavenWidgets.Checkbox(loot.TrackAutoWishlist, "Track Auto Wishlist");

				RavenWidgets.Spacer(6f);
				RavenWidgets.Section("Also list what is inside");
				loot.SearchInsideContainers = RavenWidgets.Checkbox(loot.SearchInsideContainers, "Containers");
				loot.SearchInsideCorpses = RavenWidgets.Checkbox(loot.SearchInsideCorpses, "Bodies");
				loot.SearchInsideLivingAI = RavenWidgets.Checkbox(loot.SearchInsideLivingAI, "Living AI");

				RavenWidgets.Spacer(6f);
				RavenWidgets.Section("Only show");
				DrawLootFilters(loot);
				RavenWidgets.Spacer(8f);
			}

			var stash = FeatureFactory.GetFeature<LootableContainers>();
			if (stash != null)
			{
				stash.Enabled = RavenWidgets.SwitchRow(stash.Enabled, "Object Markers");
				GUILayout.Label("Tags the object itself, not its contents.", RavenTheme.MutedLabel);
				RavenWidgets.Spacer(4f);
				stash.ShowContainers = RavenWidgets.Checkbox(stash.ShowContainers, "Stashes & Airdrops", stash.Color);
				stash.ShowCorpses = RavenWidgets.Checkbox(stash.ShowCorpses, "Bodies", stash.CorpseColor);
				GUILayout.Label("Markers draw through walls.", RavenTheme.MutedLabel);
				RavenWidgets.Spacer(8f);
			}

			var exfils = FeatureFactory.GetFeature<ExfiltrationPoints>();
			if (exfils != null)
			{
				exfils.Enabled = RavenWidgets.SwitchRow(exfils.Enabled, "Exfil ESP");
				RavenWidgets.Spacer(4f);
				exfils.ShowEligible = RavenWidgets.Checkbox(exfils.ShowEligible, "Show Eligible", exfils.EligibleColor);
				exfils.ShowNotEligible = RavenWidgets.Checkbox(exfils.ShowNotEligible, "Show Not Eligible", exfils.NotEligibleColor);
				RavenWidgets.Spacer(8f);
			}

			RavenTabHelper.FeatureSwitch<Quests>("Quest ESP");

			RavenWidgets.Spacer(8f);

			var cull = loot?.CullInScopes ?? false;
			var next = RavenWidgets.Checkbox(cull, "Hide World Markers In Scopes");

			if (next != cull)
			{
				if (loot != null)
					loot.CullInScopes = next;

				if (stash != null)
					stash.CullInScopes = next;

				if (exfils != null)
					exfils.CullInScopes = next;

				var quests = FeatureFactory.GetFeature<Quests>();
				if (quests != null)
					quests.CullInScopes = next;
			}
		}
	}

	private static void DrawOtherCard()
	{
		using (RavenMenu.Card("Other ESP"))
		{
			RavenTabHelper.FeatureCheckbox<Grenades>("Grenades ESP");
			RavenTabHelper.FeatureCheckbox<Hits>("Hit Markers");
			RavenTabHelper.FeatureCheckbox<CrossHair>("Crosshair");
			RavenTabHelper.FeatureCheckbox<Hud>("HUD");
			RavenTabHelper.FeatureCheckbox<Radar>("Radar");
			RavenTabHelper.FeatureCheckbox<Map>("Map");
			RavenTabHelper.FeatureCheckbox<NightVision>("Night Vision");

			var nvg = FeatureFactory.GetFeature<NightVision>();
			if (nvg is { Enabled: true })
			{
				nvg.FullScreen = RavenWidgets.Checkbox(nvg.FullScreen, "Full Screen", nvg.Tint);
				nvg.Intensity = RavenWidgets.Slider("Gain", nvg.Intensity, 0f, 1f, $"{nvg.Intensity:0.00}");
				RavenWidgets.Spacer(4f);
				nvg.Noise = RavenWidgets.Slider("Grain", nvg.Noise, 0f, 1f, nvg.Noise <= 0f ? "off" : $"{nvg.Noise:0.00}");
				RavenWidgets.Spacer(4f);
			}
			RavenTabHelper.FeatureCheckbox<ThermalVision>("Thermal Vision");
			RavenTabHelper.FeatureCheckbox<NoVisor>("No Visor");
			RavenTabHelper.FeatureCheckbox<NoFlash>("No Flash");
			RavenTabHelper.FeatureCheckbox<NoGrass>("No Ground Detail");
		}
	}

	private static void DrawRenderCard(Players? players)
	{
		using (RavenMenu.Card("Render"))
		{
			if (players == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			players.BoxThickness = RavenWidgets.Slider("Box Thickness", players.BoxThickness, 1f, 6f, $"{players.BoxThickness:0.#}px");
			RavenWidgets.Spacer(6f);
			players.SkeletonThickness = RavenWidgets.Slider("Skeleton Thickness", players.SkeletonThickness, 1f, 6f, $"{players.SkeletonThickness:0.#}px");
			RavenWidgets.Spacer(6f);
			players.SnapLineThickness = RavenWidgets.Slider("Snap Line Thickness", players.SnapLineThickness, 0.5f, 6f, $"{players.SnapLineThickness:0.#}px");

			RavenWidgets.Spacer(8f);
			RavenWidgets.Section("Text");

			players.TextSize = Mathf.RoundToInt(RavenWidgets.Slider("Text Size", players.TextSize, 0f, 32f, players.TextSize <= 0 ? "default" : $"{players.TextSize}"));
			RavenWidgets.Spacer(6f);
			players.TextOutline = RavenWidgets.Slider("Text Outline", players.TextOutline, 0f, 3f, players.TextOutline <= 0f ? "off" : $"{players.TextOutline:0.#}px");

			var radar = FeatureFactory.GetFeature<Radar>();
			if (radar == null)
				return;

			RavenWidgets.Spacer(6f);
			radar.RadarRange = RavenWidgets.Slider("Radar Range", radar.RadarRange, 50f, 1000f, $"{radar.RadarRange:0}m");
		}
	}
}
