using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.ConsoleCommands;

[UsedImplicitly]
internal class Track : BaseTrackCommand
{
	public override string Name => Strings.CommandTrack;
}
