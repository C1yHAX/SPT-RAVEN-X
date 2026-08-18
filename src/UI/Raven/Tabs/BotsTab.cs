using System;
using Comfort.Common;
using System.Linq;
using RavenX.ConsoleCommands;
using RavenX.Extensions;
using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class BotsTab : IRavenTab
{
	public string Title => "Bots";

	private string _filter = string.Empty;
	private string _filterApplied = string.Empty;
	private int _sub;
	private string _liveFilter = string.Empty;

	private string _requestedType = string.Empty;

	private static readonly BotDifficulty[] _difficulties = [BotDifficulty.easy, BotDifficulty.normal, BotDifficulty.hard, BotDifficulty.impossible];
	private static readonly string[] _difficultyNames = ["Easy", "Normal", "Hard", "Impossible"];

	public void Draw()
	{
		_sub = RavenWidgets.SubTabBar(["Live Entities", "Spawn"], _sub);

		RavenTabHelper.BeginColumns(3);

		if (_sub == 0)
		{
			RavenTabHelper.BeginColumn();
			DrawLiveCard();
			RavenTabHelper.EndColumn();

			RavenTabHelper.BeginColumn();
			DrawActionsCard();
			RavenTabHelper.EndColumn();
		}
		else
		{
			RavenTabHelper.BeginColumn();
			DrawSpawnCard();
			RavenTabHelper.EndColumn();
		}

		RavenTabHelper.EndColumns();
	}

	private Vector2 _spawnScroll;
	private Vector2 _liveScroll;

	private void DrawSpawnCard()
	{
		using (RavenMenu.Card("Spawn Bot"))
		{
			var inRaid = GameState.Current?.LocalPlayer.IsValid() == true;

			_filter = RavenWidgets.TextField(_filter, "filter, e.g. boss or usec");
			RavenWidgets.Spacer(8f);

			if (!inRaid)
			{
				GUILayout.Label("Spawning needs an active raid.", RavenTheme.MutedLabel);
				return;
			}

			var names = SpawnBot.GetBotNames();

			// Taken from the layout pass, not from the live text, so typing cannot change
			// how many rows this card draws partway through a frame.
			if (Event.current.type == EventType.Layout)
				_filterApplied = _filter.Trim();

			var needle = _filterApplied;

			if (needle.Length > 0)
				names = [.. names.Where(n => n.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)];

			if (names.Length == 0)
			{
				GUILayout.Label("No bot type matches.", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.Label($"{names.Length} roles", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(4f);

			_spawnScroll = GUILayout.BeginScrollView(_spawnScroll, false, true, GUILayout.Height(360f));

			foreach (var name in names)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
				GUILayout.Label(name, RavenWidgets.RowLabel(true), GUILayout.ExpandWidth(true));

				if (RavenWidgets.SmallButton("spawn", 54f))
				{
					_requestedType = name;
					FeatureFactory.GetFeature<BotSpawning>()?.Request(name);
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}

		DrawSpawnReport();
	}

	private void DrawSpawnReport()
	{
		var spawning = FeatureFactory.GetFeature<BotSpawning>();
		if (spawning == null)
			return;

		using (RavenMenu.Card("Bot Spawn"))
		{
			spawning.Enabled = RavenWidgets.SwitchRow(spawning.Enabled, "Place At Distance");
			RavenWidgets.Spacer(4f);

			if (spawning.Enabled)
			{
				spawning.Distance = RavenWidgets.Slider("Spawn Distance", spawning.Distance, 5f, 200f, $"{spawning.Distance:0}m");
				RavenWidgets.Spacer(6f);
				spawning.RandomDirection = RavenWidgets.Checkbox(spawning.RandomDirection, "Random Direction");
				GUILayout.Label("Off drops them ahead of you.", RavenTheme.MutedLabel);
			}
			else
			{
				GUILayout.Label("Off lets the map decide, which can be\nhundreds of metres away.", RavenTheme.MutedLabel);
			}

			RavenWidgets.Spacer(6f);

			var difficulty = RavenWidgets.Dropdown("Difficulty", Array.IndexOf(_difficulties, spawning.Difficulty), _difficultyNames, spawning);
			if (difficulty >= 0 && difficulty < _difficulties.Length)
				spawning.Difficulty = _difficulties[difficulty];

			if (_requestedType.Length == 0)
				return;

			RavenWidgets.Spacer(10f);
			Row("Type", _requestedType);
			Row("Status", spawning.Status);

			if (spawning.LastDistance > 0f)
				Row("Distance", $"{spawning.LastDistance:0}m");
		}
	}

	private static string RoleOf(Player bot)
	{
		return bot.Profile?.Info?.Settings?.Role.ToString() ?? bot.GetHostileType().ToString();
	}

	private static void Row(string label, string value)
	{
		GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
		GUILayout.Label(label, RavenTheme.MutedLabel, GUILayout.Width(84f));
		GUILayout.Label(value, RavenTheme.Label, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
	}

	private static void DrawActionsCard()
	{
		using (RavenMenu.Card("Actions"))
		{
			var freeze = FeatureFactory.GetFeature<FreezeBots>();
			if (freeze != null)
			{
				freeze.Enabled = RavenWidgets.SwitchRow(freeze.Enabled, "Freeze AI");

				if (freeze.Enabled && freeze.SuspendedCount > 0)
					GUILayout.Label($"{freeze.SuspendedCount} bot(s) suspended.", RavenTheme.MutedLabel);

				RavenWidgets.Spacer(6f);
			}

			var friendly = FeatureFactory.GetFeature<FriendlyBots>();
			if (friendly != null)
			{
				friendly.Enabled = RavenWidgets.SwitchRow(friendly.Enabled, "Friendly");

				if (friendly.Enabled && friendly.GroupCount > 0)
					GUILayout.Label($"Allied with {friendly.GroupCount} group(s).", RavenTheme.MutedLabel);

				GUILayout.Label("Bots already hunting you keep their target\nuntil they lose it.", RavenTheme.MutedLabel);
				RavenWidgets.Spacer(8f);
			}

			RavenTabHelper.FeatureTrigger<GatherBots>("Teleport All To Me", "Gather");
			RavenTabHelper.FeatureTrigger<KillAllBots>("Kill All", "Kill");

			var killed = FeatureFactory.GetFeature<KillAllBots>();
			if (killed is { LastKilledCount: > 0 })
				GUILayout.Label($"Last run killed {killed.LastKilledCount} bot(s).", RavenTheme.MutedLabel);

			var gather = FeatureFactory.GetFeature<GatherBots>();
			if (gather is { LastMovedCount: > 0 })
				GUILayout.Label($"Last run moved {gather.LastMovedCount} bot(s).", RavenTheme.MutedLabel);
		}
	}

	private void DrawLiveCard()
	{
		using (RavenMenu.Card("In Raid"))
		{
			var state = GameState.Current;
			var player = state?.LocalPlayer;

			if (state == null || !player.IsValid())
			{
				GUILayout.Label("Not in a raid.", RavenTheme.MutedLabel);
				return;
			}

			_liveFilter = RavenWidgets.TextField(_liveFilter, "search by role");
			RavenWidgets.Spacer(6f);

			var origin = player.Transform.position;
			var needle = _liveFilter.Trim();

			var hostiles = state.Hostiles
				.Where(h => h.IsAlive())
				.Where(h => needle.Length == 0 || RoleOf(h).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
				.Select(h => (bot: h, distance: Vector3.Distance(origin, h.Transform.position)))
				.OrderBy(x => x.distance)
				.ToArray();

			if (hostiles.Length == 0)
			{
				GUILayout.Label(needle.Length == 0 ? "Nobody else alive." : "No role matches.", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.Label($"{hostiles.Length} alive", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(6f);

			_liveScroll = GUILayout.BeginScrollView(_liveScroll, false, true, GUILayout.Height(360f));

			foreach (var (bot, distance) in hostiles)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight));
				GUILayout.Label(RoleOf(bot), RavenWidgets.RowLabel(true), GUILayout.ExpandWidth(true));
				GUILayout.Label($"{distance:0}m", RavenTheme.ValueLabel, GUILayout.Width(60f));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}
	}
}
