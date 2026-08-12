using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal class ShaderCache : MonoBehaviour
{
	public Dictionary<Renderer, Shader?> Cache { get; } = [];

	[UsedImplicitly]
	public void OnDestroy()
	{
		Cache.Clear();
	}
}
