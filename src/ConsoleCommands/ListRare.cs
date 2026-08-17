using RavenX.Properties;
using JetBrains.Annotations;
using JsonType;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class ListRare : BaseListCommand
{
	public override string Name => Strings.CommandListRare;
	protected override ELootRarity? Rarity => ELootRarity.Rare;
}
