using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class List : BaseListCommand
{
	public override string Name => Strings.CommandList;
}
