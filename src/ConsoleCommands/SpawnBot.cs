using System;
using System.Linq;
using System.Text.RegularExpressions;
using Comfort.Common;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

using RavenX.Extensions;

[UsedImplicitly]
internal class SpawnBot : ConsoleCommandWithArgument
{
	public const string MatchAll = "*";

	public override string Pattern => RequiredArgumentPattern;
	public override string Name => Strings.CommandSpawnBot;

	public override void Execute(Match match)
	{
		var matchGroup = match.Groups[ValueGroup];
		if (matchGroup is not { Success: true })
			return;

		var search = matchGroup.Value.Trim();
		var bots = FindBots(search);

		switch (bots.Length)
		{
			case 0:
				AddConsoleLog(Strings.ErrorNoBotFound.Red());
				return;

			case > 1 when search != MatchAll:
				foreach (var bot in bots)
					AddConsoleLog(string.Format(Strings.CommandSpawnBotEnumerateFormat, bot.Green()));

				AddConsoleLog(string.Format(Strings.ErrorTooManyBotsFormat, bots.Length.ToString().Cyan()));
				return;
		}

		SpawnBots(bots);
	}

	internal static void SpawnBots(string[] bots)
	{
		var instance = Singleton<IBotGame>.Instance;
		if (instance == null)
			return;

		var controller = instance.BotsController;
		var spawner = controller?.BotSpawner;

		if (spawner == null)
			return;

		foreach (var bot in bots)
			spawner.SpawnBotByTypeForce(1, (WildSpawnType)Enum.Parse(typeof(WildSpawnType), bot), BotDifficulty.normal, null);
	}

	private static string[] FindBots(string search)
	{
		var names = GetBotNames();

		if (search == MatchAll)
			return names;

		var exactMatch = names
			.Where(n => n.Equals(search, StringComparison.OrdinalIgnoreCase))
			.ToArray();

		if (exactMatch.Length == 1)
			return exactMatch;

		return [.. names.Where(n => n.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)];
	}

	internal static string[] GetBotNames()
	{
		var filter = new[] { "test", "event", "spirit", "shooterbtr" };

		return [.. Enum
			.GetNames(typeof(WildSpawnType))
			.Where(n => !filter.Any(f => n.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
			.OrderBy(n => n)];
	}
}
