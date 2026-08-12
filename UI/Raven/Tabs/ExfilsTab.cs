using System.Linq;
using Comfort.Common;
using RavenX.Extensions;
using RavenX.Features;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven.Tabs;

internal class ExfilsTab : IRavenTab
{
	public string Title => "Exfils";

	private string _status = string.Empty;

	public void Draw()
	{
		RavenTabHelper.BeginColumns();

		RavenTabHelper.BeginColumn(560f);
		DrawListCard();
		RavenTabHelper.EndColumn();

		RavenTabHelper.EndColumns();
	}

	private void DrawListCard()
	{
		using (RavenMenu.Card("Extraction Points"))
		{
			var player = GameState.Current?.LocalPlayer;
			var world = Singleton<GameWorld>.Instance;

			if (!player.IsValid() || world?.ExfiltrationController == null)
			{
				GUILayout.Label("Not in a raid.", RavenTheme.MutedLabel);
				return;
			}

			var profile = player.Profile;
			var side = profile?.Info?.Side ?? EPlayerSide.Usec;

			var points = ExfiltrationPoints.GetExfiltrationPoints(side, world);
			if (points == null || points.Length == 0)
			{
				GUILayout.Label("This map reports no extraction points.", RavenTheme.MutedLabel);
				return;
			}

			var eligible = profile != null
				? ExfiltrationPoints.GetEligibleExfiltrationPoints(side, world, profile) ?? []
				: [];

			var origin = player.Transform.position;
			var ordered = points
				.Where(p => p.IsValid())
				.Select(p => (point: p, distance: Vector3.Distance(origin, p.transform.position)))
				.OrderBy(x => x.distance)
				.ToArray();

			foreach (var (point, distance) in ordered)
			{
				var name = point.Settings.Name.Localized();
				var isEligible = eligible.Contains(point);

				GUILayout.BeginHorizontal(GUILayout.Height(RavenTheme.RowHeight + 6f));

				GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				GUILayout.Label(name, isEligible ? RavenTheme.Label : RavenTheme.MutedLabel);
				GUILayout.Label($"{distance:0}m  ·  {ExfiltrationPoints.GetStatus(point.Status)}", RavenTheme.MutedLabel);
				GUILayout.EndVertical();

				if (RavenWidgets.SmallButton("teleport", 68f))
				{

					player.Teleport(point.transform.position + Vector3.up * 0.5f, false);
					_status = $"Teleported to {name}.";
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
