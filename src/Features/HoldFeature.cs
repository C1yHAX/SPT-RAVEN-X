using RavenX.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal abstract class HoldFeature : Feature
{
	[ConfigurationProperty(Order = 2)]
	public virtual KeyCode Key { get; set; } = KeyCode.None;

	[UsedImplicitly]
	protected virtual void Update()
	{
		if (Key != KeyCode.None && !UI.Raven.RavenWidgets.IsCapturingKey && Input.GetKey(Key))
			UpdateWhenHold();
	}

	protected virtual void UpdateWhenHold() { }
}
