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
	private EventSystem? _suspendedEventSystem;

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

		if (RavenWidgets.IsCapturingKey)
			return;

		base.Update();

		if (_wasEnabled == Enabled)
			return;

		_wasEnabled = Enabled;
		if (!Enabled)
			Menu.OnClosed();
	}

	protected override void UpdateWhenEnabled()
	{
		// Registering here rather than only while drawing means the node is in the
		// tree before the first frame is rendered, so the look axes are already
		// blocked as the menu appears instead of one frame later.
		SetupInputNode();
		SuspendGameUi();
	}

	protected override void UpdateWhenDisabled()
	{
		// Hand the screen back, or the overlays would stay hidden where the closed
		// window used to be.
		UI.Render.MenuArea = Rect.zero;

		RestoreGameUi();
	}

	// The game's own screens are driven by Unity's event system, which never passes
	// through the input tree. Without this a click meant for the menu also lands on
	// whatever sits behind it.
	private void SuspendGameUi()
	{
		if (_suspendedEventSystem != null)
			return;

		var current = EventSystem.current;
		if (current == null || !current.enabled)
			return;

		current.enabled = false;
		_suspendedEventSystem = current;
	}

	private void RestoreGameUi()
	{
		if (_suspendedEventSystem == null)
			return;

		if (_suspendedEventSystem)
			_suspendedEventSystem.enabled = true;

		_suspendedEventSystem = null;
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
		// Everything is held back while the menu is up, not just shooting. The key
		// that closes it is read straight from Input, so this cannot lock you in.
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
