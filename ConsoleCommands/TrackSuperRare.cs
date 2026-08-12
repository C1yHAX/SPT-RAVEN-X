using RavenX.Properties;
using JetBrains.Annotations;
using JsonType;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class TrackSuperRare : BaseTrackCommand
{
	public override string Name => Strings.CommandTrackSuperRare;
	protected override ELootRarity? Rarity => ELootRarity.Superrare;
}
