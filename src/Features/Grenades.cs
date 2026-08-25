using System.Collections.Generic;
using Comfort.Common;
using RavenX.Configuration;
using RavenX.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class Grenades : CachableFeature<Throwable>
{
	public override string Name => Properties.Strings.FeatureGrenadesName;
	public override string Description => Properties.Strings.FeatureGrenadesDescription;

	[ConfigurationProperty]
	public Color Color { get; set; } = Color.red;

	public override bool Enabled { get; set; } = false;
	public override float CacheTimeInSec { get; set; } = 0.25f;

	private const CameraEvent RenderEvent = CameraEvent.BeforeImageEffects;
	private readonly List<Renderer> _renderers = [];
	private readonly HashSet<int> _rendererIds = [];
	private Camera? _camera;
	private CommandBuffer? _commands;
	private Material? _material;
	private float _nextRebuild;

	public override void RefreshData(List<Throwable> data)
	{
		var world = Singleton<GameWorld>.Instance;
		var grenades = world?.Grenades;
		if (grenades == null)
			return;

		for (var i = 0; i < grenades.Count; i++)
		{
			var grenade = grenades.GetByIndex(i);
			if (grenade.IsValid())
				data.Add(grenade);
		}
	}

	public override void ProcessData(IReadOnlyList<Throwable> data)
	{
		var state = GameState.Current;
		var camera = state?.Camera;
		var player = state?.LocalPlayer;
		var shader = GameState.OutlineShader;
		if (camera == null || !player.IsValid() || shader == null)
		{
			ResetRuntime();
			return;
		}

		if (player.IsInventoryOpened)
		{
			_commands?.Clear();
			return;
		}

		if (!EnsureRuntime(camera, shader))
			return;

		if (Time.unscaledTime < _nextRebuild)
			return;

		_nextRebuild = Time.unscaledTime + CacheTimeInSec;
		var commands = _commands!;
		var material = _material!;
		commands.Clear();
		material.SetColor(ShaderProperties.FirstOutlineColor, Color);
		material.SetFloat(ShaderProperties.FirstOutlineWidth, 0.02f);
		material.SetColor(ShaderProperties.SecondOutlineColor, Color);
		material.SetFloat(ShaderProperties.SecondOutlineWidth, 0.0025f);
		material.SetFloat(ShaderProperties.ZTest, (float)CompareFunction.Always);
		_rendererIds.Clear();

		foreach (var throwable in data)
		{
			if (!throwable.IsValid())
				continue;

			_renderers.Clear();
			throwable.gameObject.GetComponentsInChildren(true, _renderers);
			foreach (var renderer in _renderers)
			{
				if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || !_rendererIds.Add(renderer.GetInstanceID()))
					continue;

				var subMeshCount = SubMeshCount(renderer);
				for (var subMesh = 0; subMesh < subMeshCount; subMesh++)
					commands.DrawRenderer(renderer, material, subMesh);
			}
		}
	}

	protected override void BeforeRefreshData(IReadOnlyList<Throwable> data)
	{
		_commands?.Clear();
		_nextRebuild = 0f;
	}

	protected override void UpdateWhenDisabled()
	{
		base.UpdateWhenDisabled();
		ResetRuntime();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		ResetRuntime();
	}

	private bool EnsureRuntime(Camera camera, Shader shader)
	{
		if (_camera == camera && _commands != null && _material != null && _material.shader == shader)
			return true;

		ResetRuntime();
		_material = new Material(shader)
		{
			name = "RavenX Grenades",
			hideFlags = HideFlags.HideAndDontSave
		};
		_commands = new CommandBuffer { name = "RavenX Grenades" };
		camera.AddCommandBuffer(RenderEvent, _commands);
		_camera = camera;
		return true;
	}

	private void ResetRuntime()
	{
		if (_camera != null && _commands != null)
			_camera.RemoveCommandBuffer(RenderEvent, _commands);

		if (_commands != null)
		{
			_commands.Clear();
			_commands.Release();
		}

		if (_material != null)
			Destroy(_material);

		_camera = null;
		_commands = null;
		_material = null;
		_nextRebuild = 0f;
		_renderers.Clear();
		_rendererIds.Clear();
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
}
