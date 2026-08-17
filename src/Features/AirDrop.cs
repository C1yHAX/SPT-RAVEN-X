using System.Linq;
using EFT.InventoryLogic;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class AirDrop : TriggerFeature
{
	public override string Name => Strings.FeatureAirDropName;
	public override string Description => Strings.FeatureAirDropDescription;

	public override KeyCode Key { get; set; } = KeyCode.None;

	protected override void UpdateOnceWhenTriggered()
	{
		var player = GameState.Current?.LocalPlayer;
		if (player == null)
			return;

		if (TemplateHelper.FindTemplates(KnownTemplateIds.RedSignalFlare).FirstOrDefault() is not AmmoTemplate template)
			return;

		player.HandleFlareSuccessEvent(player.Transform.position, template);
	}
}
