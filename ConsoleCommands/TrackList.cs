using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class TrackList : ConsoleCommandWithoutArgument
{
	public override string Name => Strings.CommandTrackList;

	public override void Execute()
	{
		ShowTrackList(this, LootItemsFeature);
	}

	internal static void ShowTrackList(ConsoleCommand command, Features.LootItems lootItems, bool changed = false)
	{
		if (changed)
			command.AddConsoleLog(Strings.CommandTrackListUpdated);

		foreach (var templateId in lootItems.Wishlist)
			command.AddConsoleLog(string.Format(Strings.CommandTrackListWishListEnumerateFormat, ((MongoID)templateId).LocalizedShortName()));

		foreach (var item in lootItems.TrackedNames)
		{
			var extra = item.Rarity.HasValue ? string.Format(Strings.CommandTrackListRarityFormat, item.Rarity.Value.Color()) : string.Empty;
			command.AddConsoleLog(string.Format(Strings.CommandTrackListEnumerateFormat, item.Color.HasValue ? item.Name.Color(item.Color.Value) : item.Name, extra));
		}
	}
}
