using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comfort.Common;
using RavenX.Configuration;
using RavenX.Extensions;
using RavenX.Properties;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal class GameState : CachableFeature<GameStateSnapshot>
{
	public override string Name => Strings.FeatureGameStateName;
	public override string Description => Strings.FeatureGameStateDescription;

	public static GameStateSnapshot? Current { get; private set; }

	public override float CacheTimeInSec { get; set; } = 2f;

	[ConfigurationProperty(Skip = true)]
	public override bool Enabled { get; set; } = true;

	[ConfigurationProperty(Skip = true)]
	public override KeyCode Key { get; set; } = KeyCode.None;

	public static Shader? OutlineShader { get; private set; }

	[UsedImplicitly]
	private void Awake()
	{

		if (OutlineShader != null)
			return;

		var filename = Path.Combine(Application.dataPath, "outline");
		if (!File.Exists(filename))
			return;

		var bundle = AssetBundle.LoadFromFile(filename);
		if (bundle == null)
			return;

		OutlineShader = bundle.LoadAsset<Shader>("assets/outline.shader");
	}

	public override void RefreshData(List<GameStateSnapshot> data)
	{
		var snapshot = new GameStateSnapshot();
		var world = Singleton<GameWorld>.Instance;

		if (world == null)
		{
			Current = null;
			return;
		}

		var players = world
			.RegisteredPlayers?
			.OfType<Player>();

		if (players == null)
		{
			Current = null;
			return;
		}

		var hostiles = new List<Player>();
		snapshot.Hostiles = hostiles;

		foreach (var player in players)
		{
			if (player.IsYourPlayer)
			{
				snapshot.LocalPlayer = player;
				continue;
			}

			if (!player.IsAlive())
				continue;

			hostiles.Add(player);
		}

		snapshot.Camera = Camera.main;

		Current = snapshot;
		data.Add(snapshot);
	}
}

public class GameStateSnapshot
{
	public Camera? Camera { get; set; }
	public Camera? MapCamera { get; set; }
	public Player? LocalPlayer { get; set; }
	public IEnumerable<Player> Hostiles { get; set; } = [];
	public bool MapMode { get; set; } = false;
}
