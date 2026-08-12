using System.Collections.Generic;
using System.Linq;
using RavenX.Extensions;
using RavenX.Features;
using RavenX.Properties;
using HarmonyLib;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class SpawnHideoutItems : ConsoleCommandWithoutArgument
{
	public override string Name => Strings.CommandSpawnHideoutItems;

	public override void Execute()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var manager = player.Profile?.WishlistManager;
		if (manager == null)
			return;

		var method = AccessTools
			.GetDeclaredMethods(manager.GetType())
			.FirstOrDefault(m => m.ReturnType == typeof(IEnumerable<MongoID>));

		if (method?.Invoke(manager, []) is not IEnumerable<MongoID> templates)
			return;

		foreach (var template in templates)
			Spawn.SpawnTemplate(template, player, this, _ => true);
	}
}
