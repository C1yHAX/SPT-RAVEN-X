using RavenX.Properties;
using JetBrains.Annotations;
using JsonType;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class ListSuperRare : BaseListCommand
{
	public override string Name => Strings.CommandListSuperRare;
	protected override ELootRarity? Rarity => ELootRarity.Superrare;
}
