using System.Text;
using EFT.InventoryLogic;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using RavenX.UI;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Hud : ToggleFeature
{
	public override string Name => Strings.FeatureHudName;
	public override string Description => Strings.FeatureHudDescription;

	[ConfigurationProperty]
	public Color Color { get; set; } = Color.white;

	[ConfigurationProperty]
	public bool ShowCompass { get; set; } = true;

	private static readonly string[] _directions = [
		Strings.DirectionNorth,
		Strings.DirectionNorthEast,
		Strings.DirectionEast,
		Strings.DirectionSouthEast,
		Strings.DirectionSouth,
		Strings.DirectionSouthWest,
		Strings.DirectionWest,
		Strings.DirectionNorthWest,
		Strings.DirectionNorth
	];

	[ConfigurationProperty]
	public bool ShowCoordinates { get; set; } = false;

	private readonly StringBuilder _sb = new();
	protected override void OnGUIWhenEnabled()
	{
		if (Event.current.type != EventType.Repaint)
			return;

		var player = GameState.Current?.LocalPlayer;
		if (!player.IsValid())
			return;

		var camera = GameState.Current?.Camera;
		if (camera == null)
			return;

		_sb.Clear();

		if (ShowCompass)
		{
			var forward = camera.transform.forward;
			forward.y = 0;

			if (forward.sqrMagnitude > 0.0001f)
			{
				var heading = Quaternion.LookRotation(forward).eulerAngles.y;
				_sb.Append(_directions[(int)Mathf.Round(heading % 360 / 45)]);
			}
		}

		if (player.HandsController?.Item is Weapon weapon)
		{
			if (_sb.Length > 0)
				_sb.Append(Strings.FeatureHudSeparator);

			var mag = weapon.GetCurrentMagazine();
			_sb.Append(string.Format(Strings.FeatureHudWeaponFormat, mag?.Count ?? 0, weapon.ChamberAmmoCount, mag?.MaxCount ?? 0, weapon.SelectedFireMode));
		}

		if (ShowCoordinates)
		{
			if (_sb.Length > 0)
				_sb.Append(Strings.FeatureHudSeparator);

			var position = player.Transform.position;
			_sb.Append(string.Format(Strings.FeatureHudCoordinatesFormat, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)));
		}

		if (_sb.Length == 0)
			return;

		Render.DrawString(new Vector2(Screen.width / 2f, Screen.height - 16f), _sb.ToString(), Color);
	}
}
