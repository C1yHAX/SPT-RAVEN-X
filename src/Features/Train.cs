using System;
using EFT.MovingPlatforms;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Train : TriggerFeature
{
	public override string Name => Strings.FeatureTrainName;
	public override string Description => Strings.FeatureTrainDescription;

	public override KeyCode Key { get; set; } = KeyCode.None;

	protected override void UpdateOnceWhenTriggered()
	{
		var locomotive = FindObjectOfType<Locomotive>();
		if (locomotive == null)
			return;

		locomotive.Init(DateTime.UtcNow);
	}
}
