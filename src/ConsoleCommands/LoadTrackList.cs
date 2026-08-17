using System.Text.RegularExpressions;
using RavenX.Configuration;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class LoadTrackList : BaseTrackListCommand
{
	public override string Name => Strings.CommandLoadTrackList;

	public override void Execute(Match match)
	{
		if (!TryGetTrackListFilename(match, out var filename))
			return;

		ConfigurationManager.LoadPropertyValue(filename, LootItemsFeature, nameof(Features.LootItems.TrackedNames));
	}
}
