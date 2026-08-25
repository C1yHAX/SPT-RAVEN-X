using System;
using System.Collections.Generic;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Chams : ToggleFeature
{
	public override string Name => "chams";
	public override string Description => "Colour characters through walls.";

	public override bool Enabled { get; set; } = false;

	[ConfigurationProperty(Order = 30)]
	public float Opacity { get; set; } = 0.85f;

	[ConfigurationProperty(Order = 31)]
	public float MaximumDistance { get; set; } = 0f;

	[ConfigurationProperty(Order = 80)]
	public bool ShowCorpses { get; set; } = false;

	[ConfigurationProperty(Order = 81)]
	public Color CorpseColor { get; set; } = new(0.85f, 0.35f, 0.35f, 0.85f);

	[ConfigurationProperty(Order = 82)]
	public bool ShowLoot { get; set; } = false;

	[ConfigurationProperty(Order = 83)]
	public Color LootColor { get; set; } = new(0.30f, 0.85f, 0.90f, 0.85f);

	private const float RefreshInterval = 0.2f;
	private const CameraEvent RenderEvent = CameraEvent.BeforeImageEffects;

	private readonly Dictionary<string, MaterialPair> _styles = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _usedStyles = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<int> _rendererIds = [];
	private readonly List<string> _staleStyles = [];
	private readonly List<Renderer> _rendererBuffer = [];

	private List<RoleSetting> _roleColors = [];
	private Dictionary<string, RoleSetting>? _roleIndex;
	private Shader? _shader;
	private Camera? _camera;
	private CommandBuffer? _commands;
	private float _nextRefresh;

	private sealed class MaterialPair
	{
		public Material Visible = null!;
		public Material Occluded = null!;
	}

	[ConfigurationProperty(Browsable = false)]
	public List<RoleSetting> RoleColors
	{
		get => _roleColors;
		set
		{
			_roleColors = value ?? [];
			_roleIndex = null;
		}
	}

	protected override void UpdateWhenEnabled()
	{
		var state = GameState.Current;
		var player = state?.LocalPlayer;

		if (state == null || !player.IsValid())
		{
			ResetRuntime();
			return;
		}

		if (player!.IsInventoryOpened)
		{
			_commands?.Clear();
			_nextRefresh = 0f;
			return;
		}

		var camera = state.Camera != null ? state.Camera : Camera.main;
		if (camera == null || !EnsureCommandBuffer(camera))
		{
			ResetRuntime();
			return;
		}

		if (Time.unscaledTime < _nextRefresh)
			return;

		_nextRefresh = Time.unscaledTime + RefreshInterval;
		Rebuild(state, player);
	}

	protected override void UpdateWhenDisabled()
	{
		ResetRuntime();
	}

	private void OnDestroy()
	{
		ResetRuntime();
	}

	public RoleSetting RoleFor(string key) => RoleCatalog.Resolve(RoleColors, ref _roleIndex, key);

	private void Rebuild(GameStateSnapshot state, Player player)
	{
		var commands = _commands;
		if (commands == null)
			return;

		commands.Clear();
		_usedStyles.Clear();
		var origin = player.Transform.position;

		foreach (var hostile in state.Hostiles)
		{
			if (hostile == null || !hostile.IsAlive() || TooFar(origin, hostile.Transform.position))
				continue;

			var roleKey = RoleCatalog.KeyOf(hostile);
			var role = RoleFor(roleKey);
			if (!role.Enabled)
				continue;

			var visible = role.Visible;
			var occluded = role.Occluded;
			visible.a = Opacity;
			occluded.a = Opacity;

			var style = StyleFor($"player:{roleKey}", visible, occluded);
			DrawPlayer(commands, hostile, style);
		}

		if (ShowCorpses || ShowLoot)
			DrawWorld(commands, origin);

		RemoveUnusedStyles();
	}

	private bool TooFar(Vector3 origin, Vector3 position)
	{
		return MaximumDistance > 0f && (position - origin).sqrMagnitude > MaximumDistance * MaximumDistance;
	}

	private void DrawPlayer(CommandBuffer commands, Player hostile, MaterialPair style)
	{
		_rendererBuffer.Clear();

		var body = hostile.PlayerBody;
		if (body == null)
			return;

		try
		{
			body.GetRenderersNonAlloc(_rendererBuffer);
		}
		catch
		{
			return;
		}

		DrawRenderers(commands, style);
	}

	private void DrawWorld(CommandBuffer commands, Vector3 origin)
	{
		var world = Comfort.Common.Singleton<GameWorld>.Instance;
		if (world == null)
			return;

		var lootItems = world.LootItems;
		var count = lootItems.Count;

		for (var i = 0; i < count; i++)
		{
			var lootItem = lootItems.GetByIndex(i);
			if (!lootItem.IsValid())
				continue;

			var isCorpse = lootItem is EFT.Interactive.Corpse;
			if (isCorpse ? !ShowCorpses : !ShowLoot)
				continue;

			if (TooFar(origin, lootItem.transform.position))
				continue;

			var visible = isCorpse ? CorpseColor : LootColor;
			var occluded = RoleCatalog.OccludedFrom(visible);
			visible.a = Opacity;
			occluded.a = Opacity;

			var style = StyleFor(isCorpse ? "world:corpse" : "world:loot", visible, occluded);
			_rendererBuffer.Clear();
			lootItem.gameObject.GetComponentsInChildren(true, _rendererBuffer);
			DrawRenderers(commands, style);
		}
	}

	private void DrawRenderers(CommandBuffer commands, MaterialPair style)
	{
		_rendererIds.Clear();

		foreach (var renderer in _rendererBuffer)
		{
			if (renderer == null || renderer is not SkinnedMeshRenderer && renderer is not MeshRenderer)
				continue;

			if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !_rendererIds.Add(renderer.GetInstanceID()))
				continue;

			var subMeshCount = SubMeshCount(renderer);
			for (var subMesh = 0; subMesh < subMeshCount; subMesh++)
			{
				commands.DrawRenderer(renderer, style.Occluded, subMesh);
				commands.DrawRenderer(renderer, style.Visible, subMesh);
			}
		}
	}

	private static int SubMeshCount(Renderer renderer)
	{
		if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
			return skinned.sharedMesh.subMeshCount;

		if (renderer is MeshRenderer)
		{
			var filter = renderer.GetComponent<MeshFilter>();
			if (filter != null && filter.sharedMesh != null)
				return filter.sharedMesh.subMeshCount;
		}

		return renderer.sharedMaterials.Length;
	}

	private MaterialPair StyleFor(string key, Color visible, Color occluded)
	{
		_usedStyles.Add(key);

		if (!_styles.TryGetValue(key, out var style))
		{
			style = new MaterialPair
			{
				Visible = CreateMaterial(_shader!, $"RavenChamVisible:{key}", CompareFunction.LessEqual),
				Occluded = CreateMaterial(_shader!, $"RavenChamOccluded:{key}", CompareFunction.Greater)
			};

			_styles[key] = style;
		}

		style.Visible.color = visible;
		style.Occluded.color = occluded;
		return style;
	}

	private void RemoveUnusedStyles()
	{
		_staleStyles.Clear();

		foreach (var key in _styles.Keys)
		{
			if (!_usedStyles.Contains(key))
				_staleStyles.Add(key);
		}

		foreach (var key in _staleStyles)
		{
			DestroyStyle(_styles[key]);
			_styles.Remove(key);
		}
	}

	private bool EnsureCommandBuffer(Camera camera)
	{
		_shader ??= Shader.Find("Hidden/Internal-Colored");
		if (_shader == null)
			return false;

		if (_camera == camera && _commands != null)
			return true;

		ReleaseCommandBuffer();

		_commands = new CommandBuffer { name = "RavenX Chams" };
		camera.AddCommandBuffer(RenderEvent, _commands);
		_camera = camera;
		_nextRefresh = 0f;
		return true;
	}

	private void ResetRuntime()
	{
		ReleaseCommandBuffer();

		foreach (var style in _styles.Values)
			DestroyStyle(style);

		_styles.Clear();
		_usedStyles.Clear();
		_staleStyles.Clear();
		_rendererIds.Clear();
		_rendererBuffer.Clear();
		_nextRefresh = 0f;
	}

	private void ReleaseCommandBuffer()
	{
		if (_camera != null && _commands != null)
			_camera.RemoveCommandBuffer(RenderEvent, _commands);

		if (_commands != null)
		{
			_commands.Clear();
			_commands.Release();
			_commands = null;
		}

		_camera = null;
	}

	private static void DestroyStyle(MaterialPair style)
	{
		if (style.Visible != null)
			UnityEngine.Object.Destroy(style.Visible);

		if (style.Occluded != null)
			UnityEngine.Object.Destroy(style.Occluded);
	}

	private static Material CreateMaterial(Shader shader, string name, CompareFunction zTest)
	{
		var material = new Material(shader)
		{
			name = name,
			hideFlags = HideFlags.HideAndDontSave,
			renderQueue = 3000
		};

		material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
		material.SetInt("_Cull", (int)CullMode.Back);
		material.SetInt("_ZWrite", 0);
		material.SetInt("_ZTest", (int)zTest);
		return material;
	}
}
