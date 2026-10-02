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
	public string Title => RavenText.L("Visuals");

	public void Draw()
	{
		var players = FeatureFactory.GetFeature<Players>();

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
		using (RavenMenu.Card(RavenText.L("Players ESP")))
		{
			if (players == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			players.Enabled = RavenWidgets.SwitchRow(players.Enabled, RavenText.L("Enable"));
			RavenWidgets.Spacer(4f);

			players.ShowBoxes = RavenWidgets.Checkbox(players.ShowBoxes, RavenText.L("Show Boxes"));
			players.ShowInfos = RavenWidgets.Checkbox(players.ShowInfos, RavenText.L("Show Info"));
			players.ShowSkeletons = RavenWidgets.Checkbox(players.ShowSkeletons, RavenText.L("Show Skeleton"));
			players.ShowShootable = RavenWidgets.Checkbox(players.ShowShootable, RavenText.L("Show Shootable"), players.ShootableColors.Color);
			players.ShowNotShootable = RavenWidgets.Checkbox(players.ShowNotShootable, RavenText.L("Show Blocked"), players.NotShootableColors.Color);

			if (players.ShowShootable)
			{
				players.PerLimbVisibility = RavenWidgets.Checkbox(players.PerLimbVisibility, RavenText.L("Per-Limb Visibility"));

				if (players.PerLimbVisibility && !players.ShowSkeletons)
					GUILayout.Label(RavenText.L("Needs Show Skeleton to be visible."), RavenTheme.MutedLabel);
			}
			players.ShowSnapLines = RavenWidgets.Checkbox(players.ShowSnapLines, RavenText.L("Snap Lines"), players.SnapLineColor);

			RavenWidgets.Spacer(8f);
			RavenWidgets.Section(RavenText.L("Readout"));

			players.ShowNames = RavenWidgets.Checkbox(players.ShowNames, RavenText.L("Names"));
			players.ShowRole = RavenWidgets.Checkbox(players.ShowRole, RavenText.L("Faction"));
			players.ShowWeapons = RavenWidgets.Checkbox(players.ShowWeapons, RavenText.L("Weapon"));
			players.ShowDistance = RavenWidgets.Checkbox(players.ShowDistance, RavenText.L("Distance"));
			players.ShowHealthBar = RavenWidgets.Checkbox(players.ShowHealthBar, RavenText.L("HP Bar"));

			if (players.ShowHealthBar)
			{
				players.ShowHealthText = false;
			}
			else
			{
				players.ShowHealthText = RavenWidgets.Checkbox(players.ShowHealthText, RavenText.L("Health Value"));
			}

			if (!players.ShowInfos)
				GUILayout.Label(RavenText.L("Enable Show Info to see these."), RavenTheme.MutedLabel);
		}
	}

	private static void DrawFiltersCard(Players? players)
	{
		using (RavenMenu.Card(RavenText.L("Filters")))
		{
			if (players == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			var distance = players.MaximumDistance;
			players.MaximumDistance = RavenWidgets.Slider(
				RavenText.L("Max Distance"), distance, 0f, 1000f,
				distance <= 0f ? "unlimited" : $"{distance:0}m");
		}
	}

	private static Vector2 _roleScroll;
	private static int _roleGroup;
	private static Vector2 _chamScroll;
	private static int _chamGroup;

	private static void DrawRolesCard(Players? players)
	{
		using (RavenMenu.Card(RavenText.L("ESP Roles")))
		{
			if (players == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			DrawRoleList(players.RoleFor, ref _roleGroup, ref _roleScroll, players);

			GUILayout.Label(RavenText.L("Colour marks the role. Blocked limbs\nuse a dimmed shade of it."), RavenTheme.MutedLabel);
		}
	}

	private static void DrawRoleList(System.Func<string, RoleSetting> resolve, ref int group, ref Vector2 scroll, object key)
	{
		var groups = RoleCatalog.Groups;
		var names = new string[groups.Count + 1];
		names[0] = RavenText.L("All");

		for (var i = 0; i < groups.Count; i++)
			names[i + 1] = groups[i];

		group = Mathf.Clamp(RavenWidgets.Dropdown(RavenText.L("Group"), group, names, key), 0, names.Length - 1);

		RavenWidgets.Spacer(6f);

		GUILayout.BeginHorizontal();
		var enableAll = RavenWidgets.OutlineButton(RavenText.L("ALL ON"), 82f);
		GUILayout.Space(6f);
		var disableAll = RavenWidgets.OutlineButton(RavenText.L("ALL OFF"), 82f);
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
		using (RavenMenu.Card(RavenText.L("Chams")))
		{
			var chams = FeatureFactory.GetFeature<Chams>();
			if (chams == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			chams.Enabled = RavenWidgets.SwitchRow(chams.Enabled, RavenText.L("Enable"));
			RavenWidgets.Spacer(4f);

			DrawRoleList(chams.RoleFor, ref _chamGroup, ref _chamScroll, chams);

			RavenWidgets.Spacer(8f);
			chams.Opacity = RavenWidgets.Slider(RavenText.L("Opacity"), chams.Opacity, 0.15f, 1f, $"{chams.Opacity * 100f:0}%");

			RavenWidgets.Spacer(6f);
			var distance = chams.MaximumDistance;
			chams.MaximumDistance = RavenWidgets.Slider(
				RavenText.L("Max Distance"), distance, 0f, 800f,
				distance <= 0f ? "unlimited" : $"{distance:0}m");

			RavenWidgets.Spacer(8f);
			RavenWidgets.Section(RavenText.L("World"));

			chams.ShowCorpses = RavenWidgets.Checkbox(chams.ShowCorpses, RavenText.L("Bodies"), chams.CorpseColor);
			chams.ShowLoot = RavenWidgets.Checkbox(chams.ShowLoot, RavenText.L("Loot"), chams.LootColor);

			RavenWidgets.Spacer(6f);
			GUILayout.Label(RavenText.L("Second colour marks the parts\nthat are behind cover."), RavenTheme.MutedLabel);
		}
	}

	private static string LootHint(LootItems loot)
	{
		if (loot.MaximumPrice > 0 && loot.MaximumPrice < loot.MinimumPrice)
			return RavenText.L("The two price limits are applied from low to high.");

		if ((loot.MinimumPrice > 0 || loot.MaximumPrice > 0) && !HandbookCatalog.PricesReady)
			return RavenText.L("Prices are loading. Items without a price stay hidden.");

		return string.Empty;
	}

	private static void DrawLootFilters(LootItems loot)
	{
		var from = RavenWidgets.Slider(RavenText.L("Price from"), loot.MinimumPrice, 0f, 1000000f,
			loot.MinimumPrice > 0 ? $"{loot.MinimumPrice}" : "any");
		loot.MinimumPrice = Mathf.RoundToInt(from / 500f) * 500;

		var to = RavenWidgets.Slider(RavenText.L("Price to"), loot.MaximumPrice, 0f, 1000000f,
			loot.MaximumPrice > 0 ? $"{loot.MaximumPrice}" : "no limit");
		loot.MaximumPrice = Mathf.RoundToInt(to / 500f) * 500;

		GUILayout.Label(LootHint(loot), RavenTheme.MutedLabel, GUILayout.Height(RavenTheme.RowHeight));

		var rank = LootItems.RarityRank(loot.MinimumRarity);
		var picked = RavenWidgets.Dropdown(RavenText.L("Rarity"), rank, RarityOptions, loot);
		if (picked != rank)
			loot.MinimumRarity = RarityFromRank(picked);

		var distance = RavenWidgets.Slider(RavenText.L("Maximum distance"), loot.MaximumDistance, 0f, 500f,
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
		using (RavenMenu.Card(RavenText.L("Loot & World ESP")))
		{
			var loot = FeatureFactory.GetFeature<LootItems>();
			if (loot != null)
			{
				loot.Enabled = RavenWidgets.SwitchRow(loot.Enabled, RavenText.L("Loot ESP"));
				GUILayout.Label(RavenText.L("Tags the individual items."), RavenTheme.MutedLabel);
				RavenWidgets.Spacer(4f);
				loot.ShowPrices = RavenWidgets.Checkbox(loot.ShowPrices, RavenText.L("Show Prices"));
				loot.TrackWishlist = RavenWidgets.Checkbox(loot.TrackWishlist, RavenText.L("Track Wishlist"));
				loot.TrackAutoWishlist = RavenWidgets.Checkbox(loot.TrackAutoWishlist, RavenText.L("Track Auto Wishlist"));

				RavenWidgets.Spacer(6f);
				RavenWidgets.Section(RavenText.L("Also list what is inside"));
				loot.SearchInsideContainers = RavenWidgets.Checkbox(loot.SearchInsideContainers, RavenText.L("Containers"));
				loot.SearchInsideCorpses = RavenWidgets.Checkbox(loot.SearchInsideCorpses, RavenText.L("Bodies"));
				loot.SearchInsideLivingAI = RavenWidgets.Checkbox(loot.SearchInsideLivingAI, RavenText.L("Living AI"));

				RavenWidgets.Spacer(6f);
				RavenWidgets.Section(RavenText.L("Only show"));
				DrawLootFilters(loot);
				RavenWidgets.Spacer(8f);
			}

			var stash = FeatureFactory.GetFeature<LootableContainers>();
			if (stash != null)
			{
				stash.Enabled = RavenWidgets.SwitchRow(stash.Enabled, RavenText.L("Object Markers"));
				GUILayout.Label(RavenText.L("Tags the object itself, not its contents."), RavenTheme.MutedLabel);
				RavenWidgets.Spacer(4f);
				stash.ShowContainers = RavenWidgets.Checkbox(stash.ShowContainers, RavenText.L("Stashes & Airdrops"), stash.Color);
				stash.ShowCorpses = RavenWidgets.Checkbox(stash.ShowCorpses, RavenText.L("Bodies"), stash.CorpseColor);
				GUILayout.Label(RavenText.L("Markers draw through walls."), RavenTheme.MutedLabel);
				RavenWidgets.Spacer(8f);
			}

			var exfils = FeatureFactory.GetFeature<ExfiltrationPoints>();
			if (exfils != null)
			{
				exfils.Enabled = RavenWidgets.SwitchRow(exfils.Enabled, RavenText.L("Exfil ESP"));
				RavenWidgets.Spacer(4f);
				exfils.ShowEligible = RavenWidgets.Checkbox(exfils.ShowEligible, RavenText.L("Show Eligible"), exfils.EligibleColor);
				exfils.ShowNotEligible = RavenWidgets.Checkbox(exfils.ShowNotEligible, RavenText.L("Show Not Eligible"), exfils.NotEligibleColor);
				RavenWidgets.Spacer(8f);
			}

			RavenTabHelper.FeatureSwitch<Quests>(RavenText.L("Quest ESP"));

			RavenWidgets.Spacer(8f);

			var cull = loot?.CullInScopes ?? false;
			var next = RavenWidgets.Checkbox(cull, RavenText.L("Hide World Markers In Scopes"));

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
		using (RavenMenu.Card(RavenText.L("Other ESP")))
		{
			RavenTabHelper.FeatureCheckbox<Grenades>(RavenText.L("Grenades ESP"));
			RavenTabHelper.FeatureCheckbox<Hits>(RavenText.L("Hit Markers"));
			RavenTabHelper.FeatureCheckbox<CrossHair>(RavenText.L("Crosshair"));
			RavenTabHelper.FeatureCheckbox<Hud>("HUD");
			RavenTabHelper.FeatureCheckbox<Radar>(RavenText.L("Radar"));
			RavenTabHelper.FeatureCheckbox<Map>(RavenText.L("Map"));
			RavenTabHelper.FeatureCheckbox<NightVision>(RavenText.L("Night Vision"));

			var nvg = FeatureFactory.GetFeature<NightVision>();
			if (nvg is { Enabled: true })
			{
				nvg.FullScreen = RavenWidgets.Checkbox(nvg.FullScreen, RavenText.L("Full Screen"), nvg.Tint);
				nvg.Intensity = RavenWidgets.Slider(RavenText.L("Gain"), nvg.Intensity, 0f, 1f, $"{nvg.Intensity:0.00}");
				RavenWidgets.Spacer(4f);
				nvg.Noise = RavenWidgets.Slider(RavenText.L("Grain"), nvg.Noise, 0f, 1f, nvg.Noise <= 0f ? "off" : $"{nvg.Noise:0.00}");
				RavenWidgets.Spacer(4f);
			}
			RavenTabHelper.FeatureCheckbox<ThermalVision>(RavenText.L("Thermal Vision"));
			RavenTabHelper.FeatureCheckbox<NoVisor>(RavenText.L("No Visor"));
			RavenTabHelper.FeatureCheckbox<NoFlash>(RavenText.L("No Flash"));
			RavenTabHelper.FeatureCheckbox<NoGrass>(RavenText.L("No Ground Detail"));
		}
	}

	private static void DrawRenderCard(Players? players)
	{
		using (RavenMenu.Card(RavenText.L("Render")))
		{
			if (players == null)
			{
				GUILayout.Label(RavenText.L("Feature unavailable"), RavenTheme.MutedLabel);
				return;
			}

			players.BoxThickness = RavenWidgets.Slider(RavenText.L("Box Thickness"), players.BoxThickness, 1f, 6f, $"{players.BoxThickness:0.#}px");
			RavenWidgets.Spacer(6f);
			players.SkeletonThickness = RavenWidgets.Slider(RavenText.L("Skeleton Thickness"), players.SkeletonThickness, 1f, 6f, $"{players.SkeletonThickness:0.#}px");
			RavenWidgets.Spacer(6f);
			players.SnapLineThickness = RavenWidgets.Slider(RavenText.L("Snap Line Thickness"), players.SnapLineThickness, 0.5f, 6f, $"{players.SnapLineThickness:0.#}px");

			RavenWidgets.Spacer(8f);
			RavenWidgets.Section(RavenText.L("Text"));

			players.TextSize = Mathf.RoundToInt(RavenWidgets.Slider(RavenText.L("Text Size"), players.TextSize, 0f, 32f, players.TextSize <= 0 ? "default" : $"{players.TextSize}"));
			RavenWidgets.Spacer(6f);
			players.TextOutline = RavenWidgets.Slider(RavenText.L("Text Outline"), players.TextOutline, 0f, 3f, players.TextOutline <= 0f ? "off" : $"{players.TextOutline:0.#}px");

			var radar = FeatureFactory.GetFeature<Radar>();
			if (radar == null)
				return;

			RavenWidgets.Spacer(6f);
			radar.RadarRange = RavenWidgets.Slider(RavenText.L("Radar Range"), radar.RadarRange, 50f, 1000f, $"{radar.RadarRange:0}m");
		}
	}
}
