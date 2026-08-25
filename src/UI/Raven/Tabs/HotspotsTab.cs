using System.Linq;
using RavenX.Extensions;
using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class HotspotsTab : IRavenTab
{
	public string Title => "Hotspots";

	private string _nameInput = string.Empty;
	private string _status = string.Empty;

	public void Draw()
	{
		RavenTabHelper.BeginColumns();

		RavenTabHelper.BeginColumn(300f);
		DrawSaveCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.BeginColumn(440f);
		DrawListCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private void DrawSaveCard()
	{
		using (RavenMenu.Card("Save Position"))
		{
			var hotspots = FeatureFactory.GetFeature<Hotspots>();
			var player = GameState.Current?.LocalPlayer;
			var map = Hotspots.CurrentMap;

			if (hotspots == null || !player.IsValid() || map.Length == 0)
			{
				GUILayout.Label("Not in a raid.", RavenTheme.MutedLabel);
				return;
			}

			GUILayout.Label($"Map: {map}", RavenTheme.MutedLabel);
			var position = player.Transform.position;
			GUILayout.Label($"X {position.x:0} · Y {position.y:0} · Z {position.z:0}", RavenTheme.MutedLabel);
			RavenWidgets.Spacer(8f);

			_nameInput = RavenWidgets.TextField(_nameInput, "name for this spot");
			RavenWidgets.Spacer(6f);

			if (!RavenWidgets.OutlineButton("SAVE HERE", 120f))
				return;

			if (hotspots.Add(_nameInput))
			{
				_status = $"Saved \"{_nameInput.Trim()}\".";
				_nameInput = string.Empty;
			}
			else
			{
				_status = hotspots.LastError ?? "Give the spot a name first.";
			}
		}
	}

	private void DrawListCard()
	{
		using (RavenMenu.Card("Saved Spots"))
		{
			var hotspots = FeatureFactory.GetFeature<Hotspots>();
			if (hotspots == null)
			{
				GUILayout.Label("Feature unavailable", RavenTheme.MutedLabel);
				return;
			}

			if (hotspots.LastError != null)
			{
				GUILayout.Label(hotspots.LastError, RavenTheme.MutedLabel);
				RavenWidgets.Spacer(6f);
			}

			var player = GameState.Current?.LocalPlayer;
			var map = Hotspots.CurrentMap;

			if (!player.IsValid() || map.Length == 0)
			{
				GUILayout.Label($"Not in a raid. {hotspots.All.Count} spot(s) saved in total.", RavenTheme.MutedLabel);
				return;
			}

			var entries = hotspots.ForCurrentMap().ToArray();

			if (entries.Length == 0)
			{
				GUILayout.Label("Nothing saved on this map yet.", RavenTheme.MutedLabel);
				return;
			}

			var origin = player.Transform.position;

			foreach (var hotspot in entries.OrderBy(h => Vector3.Distance(origin, h.Position)).ToArray())
			{
				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight + 4f));

				GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				GUILayout.Label(hotspot.Name, RavenTheme.Label);
				GUILayout.Label($"{Vector3.Distance(origin, hotspot.Position):0}m away", RavenTheme.MutedLabel);
				GUILayout.EndVertical();

				if (RavenWidgets.SmallButton("teleport", 68f))
					_status = Hotspots.TeleportTo(hotspot) ? $"Teleported to {hotspot.Name}." : "Teleport failed.";

				if (RavenWidgets.SmallButton("delete", 58f))
				{
					RavenWidgets.RunNextLayout(() =>
					{
						_status = hotspots.Remove(hotspot)
							? $"Removed {hotspot.Name}."
							: hotspots.LastError ?? $"Unable to remove {hotspot.Name}.";
					});
				}

				GUILayout.EndHorizontal();
				RavenWidgets.Spacer(2f);
			}

			if (_status.Length > 0)
			{
				RavenWidgets.Spacer(6f);
				GUILayout.Label(_status, RavenTheme.MutedLabel);
			}
		}
	}
}
