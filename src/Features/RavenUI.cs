using System.Collections.Generic;
using RavenX.Configuration;
using RavenX.UI.Raven;
using RavenX.UI.Raven.Tabs;
using EFT.InputSystem;
using EFT.UI;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using EFT;

#nullable enable

namespace RavenX.Features;

[UsedImplicitly]
internal class RavenUI : ToggleFeature
{
	public override string Name => "raven";
	public override string Description => "RAVEN-X menu.";

	[ConfigurationProperty(Skip = true)]
	public override bool Enabled { get; set; } = false;

	public override KeyCode Key { get; set; } = KeyCode.Insert;

	private RavenMenu? _menu;
	private bool _wasEnabled;
	private readonly HashSet<EventSystem> _suspendedEventSystems = [];

	private RavenMenu Menu => _menu ??= BuildMenu();

	private static RavenMenu BuildMenu()
	{
		var menu = new RavenMenu();
		menu.Register(new VisualsTab());
		menu.Register(new AimbotTab());
		menu.Register(new PlayerTab());
		menu.Register(new WeaponTab());
		menu.Register(new WorldTab());
		menu.Register(new ItemsTab());
		menu.Register(new BotsTab());
		menu.Register(new ExfilsTab());
		menu.Register(new HotspotsTab());
		menu.Register(new MiscTab());
		menu.Register(new ConfigTab());
		return menu;
	}

	protected override void Update()
	{
		base.Update();

		if (_wasEnabled == Enabled)
			return;

		_wasEnabled = Enabled;
		if (!Enabled)
			Menu.OnClosed();
	}

	protected override void UpdateWhenEnabled()
	{
		SetupInputNode();
		SuspendGameUi();
	}

	protected override void UpdateWhenDisabled()
	{
		UI.Render.MenuArea = Rect.zero;

		RestoreGameUi();
	}

	private void SuspendGameUi()
	{
		var current = EventSystem.current;
		if (current == null || !current.enabled || _suspendedEventSystems.Contains(current))
			return;

		current.enabled = false;
		_suspendedEventSystems.Add(current);
	}

	private void RestoreGameUi()
	{
		foreach (var eventSystem in _suspendedEventSystems)
		{
			if (eventSystem != null)
				eventSystem.enabled = true;
		}

		_suspendedEventSystems.Clear();
	}

	[UsedImplicitly]
	private void OnDestroy()
	{
		RestoreGameUi();
		_menu?.OnClosed();
	}

	protected override void OnGUIWhenEnabled()
	{
		SetupInputNode();
		Menu.Draw();
	}

#if EFT_LIVE
	protected
#else
	public
#endif
	override ETranslateResult TranslateCommand(ECommand command)
	{
		return Enabled ? ETranslateResult.BlockAll : ETranslateResult.Ignore;
	}

#if EFT_LIVE
	protected
#else
	public
#endif
	override void TranslateAxes(ref float[] axes)
	{
		if (Enabled)
			axes = null!;
	}

#if EFT_LIVE
	protected
#else
	public
#endif
	override ECursorResult ShouldLockCursor()
	{
		return Enabled ? ECursorResult.ShowCursor : ECursorResult.Ignore;
	}

	private void SetupInputNode()
	{
		var player = GameState.Current?.LocalPlayer;
		if (player == null)
			return;

		if (!player.TryGetComponent<PlayerOwner>(out var owner))
			return;

		if (owner.InputTree.Contains(this))
			return;

		owner.InputTree.Add(this);
	}
}
