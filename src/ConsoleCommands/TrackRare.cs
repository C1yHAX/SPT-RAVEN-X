using RavenX.Properties;
using JetBrains.Annotations;
using JsonType;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class TrackRare : BaseTrackCommand
{
	public override string Name => Strings.CommandTrackRare;
	protected override ELootRarity? Rarity => ELootRarity.Rare;
}
