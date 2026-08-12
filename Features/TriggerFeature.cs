using RavenX.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal abstract class TriggerFeature : Feature
{
	[ConfigurationProperty(Order = 2)]
	public virtual KeyCode Key { get; set; } = KeyCode.None;

	[UsedImplicitly]
	private void Update()
	{
		if (Key != KeyCode.None && !UI.Raven.RavenWidgets.IsCapturingKey && Input.GetKeyUp(Key))
			UpdateOnceWhenTriggered();
	}

	public void Trigger()
	{
		UpdateOnceWhenTriggered();
	}

	protected virtual void UpdateOnceWhenTriggered() { }
}
