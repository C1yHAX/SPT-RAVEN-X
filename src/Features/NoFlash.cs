using JetBrains.Annotations;
using UnityEngine;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class NoFlash : ToggleFeature
{
	public override string Name => Properties.Strings.FeatureNoFlashName;
	public override string Description => Properties.Strings.FeatureNoFlashDescription;

	public override bool Enabled { get; set; } = false;

	private Camera? _camera;
	private GrenadeFlashScreenEffect? _flash;
	private EyeBurn? _eyeBurn;
	private bool _flashEnabled;
	private bool _eyeBurnEnabled;

	protected override void UpdateWhenEnabled()
	{
		var camera = GameState.Current?.Camera;
		if (camera == null)
		{
			Restore();
			return;
		}

		if (!ReferenceEquals(_camera, camera))
		{
			Restore();
			_camera = camera;
			_flash = camera.GetComponent<GrenadeFlashScreenEffect>();
			_eyeBurn = camera.GetComponent<EyeBurn>();
			_flashEnabled = _flash != null && _flash.enabled;
			_eyeBurnEnabled = _eyeBurn != null && _eyeBurn.enabled;
		}

		if (_flash != null)
		{
			_flash.enabled = false;
			_flash.EffectStrength = 0f;
		}

		if (_eyeBurn != null)
		{
			_eyeBurn.enabled = false;
			_eyeBurn.EyesBurn = false;
		}
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
		if (_flash != null)
		{
			_flash.EffectStrength = 0f;
			_flash.enabled = _flashEnabled;
		}

		if (_eyeBurn != null)
		{
			_eyeBurn.EyesBurn = false;
			_eyeBurn.enabled = _eyeBurnEnabled;
		}

		_camera = null;
		_flash = null;
		_eyeBurn = null;
		_flashEnabled = false;
		_eyeBurnEnabled = false;
	}
}
