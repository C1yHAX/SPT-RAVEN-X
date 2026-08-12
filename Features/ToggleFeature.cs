using RavenX.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal abstract class ToggleFeature : Feature
{
	[ConfigurationProperty(Order = 1)]
	public virtual bool Enabled { get; set; } = true;

	[ConfigurationProperty(Order = 2)]
	public virtual KeyCode Key { get; set; } = KeyCode.None;

	protected virtual void Update()
	{
		if (Key != KeyCode.None && !UI.Raven.RavenWidgets.IsCapturingKey && Input.GetKeyUp(Key))
			Enabled = !Enabled;

		if (Enabled)
			UpdateWhenEnabled();

		if (!Enabled)
			UpdateWhenDisabled();
	}

	[UsedImplicitly]
	private void OnGUI()
	{
		if (Enabled)
			OnGUIWhenEnabled();
	}

	protected virtual void UpdateWhenEnabled() { }

	protected virtual void UpdateWhenDisabled() { }

	protected virtual void OnGUIWhenEnabled() { }
}
