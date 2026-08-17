using System;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.UI.Raven;

public static class RavenLogo
{
	private const string Base64 =
		"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" +
		"80IRLJgX/g+qc6DXDresZwAAAABJRU5ErkJggg==";

	private static Texture2D? _texture;

	public static Texture2D? Texture
	{
		get
		{
			if (_texture != null)
				return _texture;

			var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
			{
				hideFlags = HideFlags.HideAndDontSave,
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};

			if (!texture.LoadImage(Convert.FromBase64String(Base64)))
				return null;

			return _texture = texture;
		}
	}
}
