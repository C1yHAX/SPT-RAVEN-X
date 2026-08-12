using System.Text.RegularExpressions;
using RavenX.Configuration;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class SaveTrackList : BaseTrackListCommand
{
	public override string Name => Strings.CommandSaveTrackList;

	public override void Execute(Match match)
	{
		if (!TryGetTrackListFilename(match, out var filename))
			return;

		ConfigurationManager.SavePropertyValue(filename, LootItemsFeature, nameof(LootItemsFeature.TrackedNames));
	}
}
