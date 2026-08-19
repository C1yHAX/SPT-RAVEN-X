# Changelog

## 1.1.0

### Added

- **Loot filters.** Under *Visuals → Loot & World ESP → Only show*: a price range, a
  rarity floor and a distance limit. Nothing set shows every item; setting a price
  shows exactly what falls in that range.
- **Resizable window.** Drag the grip in the bottom right corner. The cards fit
  themselves to the width that is there, and the tab bar wraps onto a second row
  rather than running off the edge.
- **Separate switches for the magic bullet helpers.** Flight time and muzzle boost
  can each be turned off without turning off the other.
- The aimbot's distance ceiling now goes to 1500 m. A target beyond it is skipped
  outright, which left magic bullets doing nothing at all.

### Fixed

- **Loot ESP showed nothing unless items were tracked by name.** It was an
  allow-list, and it stopped before it even looked when the list was empty.
- **Prices were never shown.** The item's own CreditsPrice is zero for most of the
  game's items; the figures come from the handbook now.
- **Markers for gear carried by a living bot trailed about three seconds behind.**
  They follow the carrier now.
- **The overlays drew on top of the menu and across the looting screen.**
- **Large empty blocks between the cards.** More columns were requested than fit,
  so the surplus wrapped onto a row that started below the tallest card above it.
- **A failure in one tab could take down every interface in the game**, not just
  the window, by leaving a clip pushed for good.
- **One mistyped key in the settings file reset every setting after it.** Only
  JsonException was caught, and Enum.Parse raises ArgumentException.
- The overlays kept drawing the last positions they saw after a raid ended.
- A dropdown choice was applied on the wrong pass, so a list whose length depends
  on the choice could break the layout.
- The game no longer reacts to clicks or turns the camera while the menu is open.
  Only shooting was held back before.

## 1.0.0

First release.
