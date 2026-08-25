using System.Collections.Generic;
using RavenX.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Features;

internal abstract class CachableFeature<T> : ToggleFeature
{
	[ConfigurationProperty(Order = 3)]
	public abstract float CacheTimeInSec { get; set; }

	private List<T> _data = [];
	private List<T> _refreshData = [];
	private bool _refreshing = false;
	private bool _dataCleared = true;
	private bool _refreshRequested = true;
	private float _nextRefreshTime;

#if DEBUG_PERFORMANCE
		private readonly System.Diagnostics.Stopwatch _stopwatch = new();
#endif

	private void Refresh()
	{
		try
		{
			_refreshing = true;

#if DEBUG_PERFORMANCE
			_stopwatch.Restart();
#endif

			_refreshData.Clear();
			RefreshData(_refreshData);
			BeforeRefreshData(_data);
			(_data, _refreshData) = (_refreshData, _data);
			_refreshData.Clear();
			_dataCleared = false;
		}
		catch (System.Exception ex)
		{
			BeforeRefreshData(_refreshData);
			_refreshData.Clear();
			AddConsoleLog(RavenX.Extensions.StringExtensions.Red($"{GetType().Name} refresh failed: {ex.Message}"));
		}
		finally
		{
			_refreshing = false;
			_refreshRequested = false;
			_nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, CacheTimeInSec);

#if DEBUG_PERFORMANCE
			_stopwatch.Stop();
#endif
		}

#if DEBUG_PERFORMANCE
		AddConsoleLog(string.Format(RavenX.Properties.Strings.DebugPerformanceRefreshedFormat, GetType().Name, _stopwatch.ElapsedMilliseconds));
#endif
	}

	protected override void UpdateWhenEnabled()
	{
		if (!_refreshing && (_refreshRequested || Time.unscaledTime >= _nextRefreshTime))
			Refresh();

		if (_refreshing)
			return;

		if (_data.Count > 0)
			ProcessData(_data);
	}

	protected override void OnGUIWhenEnabled()
	{
		if (_refreshing)
			return;

		if (_data.Count > 0)
			ProcessDataOnGUI(_data);
	}

	protected override void UpdateWhenDisabled()
	{
		_refreshRequested = true;

		if (_dataCleared)
			return;

		BeforeRefreshData(_data);
		BeforeRefreshData(_refreshData);
		_data.Clear();
		_refreshData.Clear();
		_dataCleared = true;
	}

	protected virtual void BeforeRefreshData(IReadOnlyList<T> data) { }
	public abstract void RefreshData(List<T> data);
	public virtual void ProcessData(IReadOnlyList<T> data) { }
	public virtual void ProcessDataOnGUI(IReadOnlyList<T> data) { }
}
