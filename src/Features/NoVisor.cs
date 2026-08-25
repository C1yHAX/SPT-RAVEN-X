using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoVisor : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureNoVisorName;
	public override string Description => Properties.Strings.FeatureNoVisorDescription;

	public override bool Enabled { get; set; } = false;

	private VisorEffect? _component;
	private float _originalIntensity;

	protected override void UpdateWhenEnabled()
	{
		var component = GameState.Current?.Camera?.GetComponent<VisorEffect>();
		if (component == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_component, component))
		{
			Restore();
			_component = component;
			_originalIntensity = component.Intensity;
		}

		component.Intensity = 0f;
	}

	protected override void UpdateWhenDisabled()
	{
		Restore();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		Restore();
	}

	private void Restore()
	{
		if (_component != null)
			_component.Intensity = _originalIntensity;

		_component = null;
		_originalIntensity = 0f;
	}
}
