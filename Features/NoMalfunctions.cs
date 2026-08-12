using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoMalfunctions : ToggleFeature
{
	public override string Name => Strings.FeatureNoMalfunctionsName;
	public override string Description => Strings.FeatureNoMalfunctionsDescription;

	public override bool Enabled { get; set; } = false;

	protected override void UpdateWhenEnabled()
	{
		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		if (player.HandsController is not Player.FirearmController controller)
			return;

		var template = controller.Item?.Template;
		if (template == null)
			return;

		template.AllowFeed = false;
		template.AllowJam = false;
		template.AllowMisfire = false;
		template.AllowOverheat = false;
		template.AllowSlide = false;
	}
}
