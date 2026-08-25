#pragma warning disable IDE0079

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Installer;

internal class VersionChecker
{

	private static readonly Dictionary<Version, bool> _versions = [];
	private static readonly HttpClient _client = CreateClient();

	private static HttpClient CreateClient()
	{
		var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("RavenX-Installer");
		return client;
	}
	private static readonly SemaphoreSlim _semaphore = new(1, 1);

	public static async Task<bool> IsVersionSupportedAsync(Version version)
	{
		await _semaphore.WaitAsync();

		try
		{
			if (_versions.TryGetValue(version, out var supported))
				return supported;

			var branch = $"dev-{version}";
			var uri = new Uri($"https://codeload.github.com/C1yHAX/SPT-RAVEN-X/zip/refs/heads/{Uri.EscapeDataString(branch)}");
			using var request = new HttpRequestMessage(HttpMethod.Head, uri);
			using var result = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			var isSupported = result.IsSuccessStatusCode;
			if (isSupported || result.StatusCode == HttpStatusCode.NotFound)
				_versions[version] = isSupported;

			return isSupported;
		}
		catch (Exception e)
		{
#if DEBUG
			Spectre.Console.AnsiConsole.WriteException(e);
#endif
			_ = e;
			return false;
		}
		finally
		{
			_semaphore.Release();
		}

	}

}
