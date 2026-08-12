using System.Text.RegularExpressions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class UnTrack : BaseTrackCommand
{
	public override string Name => Strings.CommandUnTrack;

	public override void Execute(Match match)
	{
		var matchGroup = match.Groups[ValueGroup];
		if (matchGroup is not { Success: true })
			return;

		TrackList.ShowTrackList(this, LootItemsFeature, LootItemsFeature.UnTrack(matchGroup.Value));
	}
}
