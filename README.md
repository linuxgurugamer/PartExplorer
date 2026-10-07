# Parts Reference

- Parts-list column headings remain pinned while the part rows scroll vertically; horizontal scrolling stays synchronized with the table.
Parts Reference is a KSP 1 part browser and reference window. It can browse the parts supplied by any installed mod, while retaining additional SCANsat-specific information when a part contains SCANsat scanner modules.

## Features


- The Details pane is organized into separate boxed panels for part information, stock modules/resources, science modules, and SCANsat data.
- Select any installed mod and view the parts loaded from that mod.
- Optional **All installed mods** view.
- Filter the selected mod by KSP part category.
- Text filtering by part name, internal ID, mod, category, path, and SCANsat scan data.
- Context-sensitive SCANsat filters for Biome, Altimeter, Visual, Resource, Anomaly, and Requires Daylight. They are only shown when SCANsat is selected or the current part list contains SCANsat scanner parts.
- Sort by the visible table columns.
- Select parts for comparison with the leftmost **Compare** checkbox column. Selections remain intact while sorting and filtering; the Parts tab includes a **Clear** button for clearing all selections.
- Use the **Compare** tab to view selected parts as columns with Details-pane values arranged as comparison rows. The top-level **Filtered only** toggle applies KSP's active editor filters on top of PartsReference's own filters throughout the mod; hidden comparison selections are retained.
- Compare can optionally **highlight differing rows** and hide identical rows with **Differences only**, making similar parts easier to compare.
- Compare part columns can be switched between **Compact**, **Normal**, and **Wide** widths; the choice is saved between sessions.
- The **Mods** tab summarizes loaded mods, part counts, and detected ScienceMods/stock-science data. Clicking a mod immediately applies that Mod filter and returns to Parts.
- Add optional data columns from the Settings page.
- The Settings page uses a three-column option layout for detail-field and additional-column choices.
- View part details including the part description, mod, category, cost, mass, temperatures/tolerances, cargo information, command capabilities, transmitters, probe control points, reaction wheels, SAS, stored resources, and SCANsat fields when applicable.
- Extensible science-module details loaded from `PluginData/ScienceMods` configuration files.
- Automatically list input/output resources for configured science modules.
- Optional display of the loaded part path.
- Settings can independently hide ID, Cost, Mass, Max temperature, Impact tolerance, and G tolerance from the Part Information panel.
- Static part thumbnails for the bundled SCANsat reference parts.
- Hover over the image area to display a larger rotating 3D preview of the loaded KSP part.
- In the VAB/SPH, add the selected part directly to the editor scene.
- Draggable/resizable window with a draggable pane splitter.
- ToolbarController support for stock and Blizzy toolbar placement.
- ClickThroughBlocker support so mouse clicks on the reference window do not pass through to KSP.
- Optional alternate KSP skin.
- Window, pane, column, filter, mod, category, and skin choices are saved between sessions.

## Mod selection

The **Mod** selector is populated from the first folder in each loaded part's KSP `partUrl`. This normally corresponds to the top-level folder under `GameData`, such as `Squad`, `SCANsat`, or the folder used by another installed part mod.

Choose **All installed mods** to browse all loaded parts together.

Parts whose KSP category is `none` are ignored.

## Category filter

The **Category** selector provides **Any** plus the KSP categories that are actually present in the currently selected mod. Changing the mod automatically refreshes the available category choices.

## Stock part details

When present on the loaded part, the Details pane also shows stock KSP information for:

- **Cargo Part Info** — packed volume, stackable quantity, and related cargo values.
- **Command** — minimum crew, hibernation settings, and command-module resource consumption.
- **Data Transmitter** — antenna type/power, packet settings, required resource, range, and combinability.
- **Probe Control Point** — minimum crew and multi-hop capability.
- **Reaction Wheel** — pitch, yaw, and roll torque plus its resource consumption.
- **SAS** — SAS service level.
- **Resources** — each resource stored by the part, showing current/default amount and maximum capacity from the loaded part configuration.

These values are read from KSP's loaded part configuration, so ModuleManager changes are reflected. They are also included in text searches.

## SCANsat data

When a loaded part contains SCANsat scanner modules, the window also reads the live scanner configuration after KSP and ModuleManager have finished loading the part. This includes scan types, altitude ranges, FOV, daylight requirement, EC usage, and SCANsat science information where available.

For parts that do not contain SCANsat scanner modules, the SCANsat-specific details and Scan Types section are not shown.

If live loaded-part data is unavailable, the plugin falls back to its embedded SCANsat reference data.

## Filters

The SCANsat-specific filters are only shown when either **SCANsat** is selected in the Mod selector or the current part list (after Mod, Category, and text filtering) contains at least one part with SCANsat scanner modules. Hidden SCANsat filters are not applied to non-SCANsat part lists; their saved choices are retained for the next time SCANsat data is relevant.

The available SCANsat filters are:

- **Biome:** Any / Yes / No
- **Altimeter:** Any / Low / High
- **Visual:** Any / Low / High
- **Resource:** Any / Low / High
- **Anomaly:** Any / Yes / No
- **Requires Daylight:** Any / Yes / No

These filters combine with the selected mod, category, and text filter.

## Science module definitions

Additional science PartModules are defined by `.cfg` files in:

`GameData/PartsReference/PluginData/ScienceMods`

Each definition identifies a `ModuleName` and one or more fields to display. For example:

```text
{
    ModuleName = MyScienceModule

    expField_1 = experimentID
    expField_1_Title = Experiment ID

    expField_2 = experimentValue
    expField_2_TitleField = experimentTitle
}
```

`expField_#` is the field to read from the part's `MODULE` configuration. `expField_#_Title` supplies a fixed display label. `expField_#_TitleField` can instead name another field in the module whose value supplies the label. Field numbers determine display order.

For every matched science module, the plugin also checks for `INPUT_RESOURCE`, `OUTPUT_RESOURCE`, `INPUTRESOURCE`, `OUTPUTRESOURCE`, `RESOURCE`, and supported field-based resource declarations. Resource names and rate/ratio/amount values are shown when available.

ScienceMods definitions are reloaded whenever the part database is refreshed, so configuration files can be added or edited without recompiling the plugin. Configured science-module fields and resources are also included in text searches.

### Mods with bundled science definitions

The package currently includes explicit science-module definitions for:

- **SCANsat** — `SCANexperiment`
- **Wild Blue Industries WBIScience 1.5.1**
- **Nehemiah Engineering Orbital Science 0.10.0**
- **DMagic Orbital Science 1.4.3**
- **Station Science Continued 2.6.0**
- **LTech Continued 1-0.5.3.2**
- **James Webb for Kerbal 1.12.x**
- **Impact 1.9.2**
- **ExoInstruments 0.5.1**
- **Tarsier Space Technology Continued 7.13**
- **Microbiology Expansion 0.2.0**

The package also includes reusable definitions for the stock KSP science modules:

- `ModuleScienceExperiment`
- `ModuleScienceLab`
- `ModuleScienceConverter`

Because they use those stock modules, the following scanned mods are also covered without needing separate custom definition files:

- **KrakenScience 1.0**
- **Mkerb Inc. Science Instruments 1.1**
- **KDEX 2.0.2**
- **Interkosmos 0.5**
- **Planetside Exploration Technologies 1.0.2**

Some mods use a mixture of custom and stock science modules. Their custom modules are defined in the corresponding ScienceMods file, while their stock modules are handled by the reusable stock definitions above.

## Editor integration

When used in the VAB or SPH, the Details pane includes **Add Part to Editor** for loaded parts. The selected part is spawned using KSP's normal editor part-spawn mechanism.

## Dependencies

- **ToolbarController**
- **ClickThroughBlocker**

Both are required by the plugin.

## Toolbar

The mod uses ToolbarController and can be placed on the supported KSP/Blizzy toolbar according to the user's ToolbarController configuration.

## 0.1.43

- Replaced the toolbar icon with the new Parts + Information design.
- Fixed the right-edge resize handle so it remains attached to the actual window edge when resizing left to the effective minimum width.


## 0.1.44

- Added a 102 x 102 PartsReference icon to the upper-right of the window header. The size is 1.5 times the 68 px Low/High filter button width.

## 0.1.45

- Fixed intermittent Mod/Category dropdown clicks and incorrect selections by removing the duplicate manual popup input handler. Dropdown rows now use the popup scroll view's own `GUI.Button` event handling, while the underlying window remains disabled for the complete popup event.
- Clicking outside an open dropdown, including on its selector button, now closes it without passing the click through to controls underneath.


## 0.1.46

- Replaced the full-height right-edge resize bar with a compact lower-right corner resize handle.
- The corner handle resizes both window width and height while respecting the existing minimum size and screen bounds.
- Window height is now saved and restored along with window width.


## 0.1.47

- Added a leftmost Compare checkbox column to the Parts list.
- Added a Compare tab that places selected parts across the top of a scrollable comparison table.
- Comparison rows are generated from the same information shown in Details, including general part information, stock module/resource data, configured science-module data, and SCANsat fields when present.
- Missing values are shown as `—`, and comparison selections remain stable while sorting or filtering the Parts list.


## 0.1.48

- Centered the Close button at the bottom of the window.
- Enlarged the lower-right resize handle.
- Removed the text heading above the comparison-selection checkbox column.
- Added hover tooltips to comparison-selection toggles.
- Compare-table value cells now explicitly use normal-weight text.
- Compare row descriptions no longer include their section name prefix.
- Part names in the Compare table are clickable in the VAB/SPH and use the existing editor part-spawn behavior.

## 0.1.49

- Added a Parts-tab **Clear** button that clears all comparison-selection toggles.
- Added a note above the Compare table explaining that part-name buttons add parts to the scene in the VAB/SPH.
- Changed the default Details setting for **ID** to disabled.
- Left-aligned the comparison-selection toggles and reduced the unused space to their left.
- Removed the Refresh button. Live part data is refreshed when PartsReference is opened.
- Moved the live-data status text to the bottom footer, to the left of the centered Close button.
- Added a **Compare: filtered only** toggle to the right of Category. When enabled, Compare uses only selected parts visible under the current filters; when disabled, it uses all selected parts. Hidden selections are preserved.


## 0.1.50

- Changed **Filtered only** from a Compare-only option into a global PartsReference filter.
- When enabled in the VAB/SPH, parts must pass both PartsReference's own Mod/Category/text/SCANsat filters and KSP's active editor filters.
- KSP editor filtering includes exclusion/game-mode filters, availability, active stock/custom/mod categorizer filters, and the editor search filter. Greyed-but-visible parts are not excluded solely for being greyed out.
- Existing Mod and Category selections are retained even when the combined filters produce no matching parts.
- Compare respects the same combined filtering when **Filtered only** is enabled while retaining hidden selections.
- Removed the **Clear** button from the Compare pane; selections are cleared from the Parts tab.

## 0.1.51

- Optimized **Filtered only** editor filtering to eliminate continuous UI lag on large mod installs.
- KSP's editor filter chain is no longer rebuilt from `OnGUI()` every rendered frame; PartsReference polls KSP's lightweight filter keys and rebuilds the cache only when the active editor filters change.
- Added fast `AvailablePart` lookup dictionaries by part path, internal ID, and localized title instead of repeatedly scanning the complete loaded-part list.
- The filtered/sorted PartsReference result is shared by the Parts and Details panes for the duration of each rendered frame.
- Editor filter changes remain responsive while avoiding repeated expensive work during Unity IMGUI Layout/Repaint events.


## 0.1.53

- The remove **X** beside each part in the Compare header is now red for better visibility.

## 0.1.52

- Added a small **X** beside each part name in the Compare header; clicking it removes that part from the comparison selection.
- Fixed SCANsat filter controls appearing when the current combined PartsReference/editor-filter result contains no SCANsat scanner parts.
- SCANsat filter visibility is now determined from the current pre-SCANsat-filter part set after Mod, Category, text, and optional editor filters are applied.


## 0.1.54
- Tightened the Compare remove X button.
- Fixed cumulative Compare header-column drift by allocating each part header to the exact same fixed width as its data column.

## 0.1.55
- Compare table columns now have a consistent 5 px gap between them while retaining exact header/data alignment.


## 0.1.57
- Removed the extra vertical space above and below the pinned Parts-table column headers.


## 0.1.58
- Added a persistent **Highlight differences** option to the Compare tab. Rows whose displayed values differ between selected parts receive a tinted data-cell background while value text remains normal weight.
- Added a persistent **Differences only** option that hides rows whose displayed values are identical across all selected parts.
- Missing values are compared as the displayed em dash (`—`), so a missing value versus a real value counts as a difference.

## 0.1.59
- Widened the **Highlight differences** option on the Compare tab and made the difference highlight brighter.
- Added persistent **Details panels** settings for Part Information, Cargo Part Info, Command, Data Transmitter, Probe Control Point, Reaction Wheel, SAS, Resources, Science Modules, SCANsat, Scan Types, and Altitude Range.
- Added persistent **Compare rows** settings for the same information groups. Compare visibility is independent of Details-panel visibility.
- All new section visibility options default to enabled for compatibility with existing behavior.

## 0.1.60
- Changed Compare difference highlighting to a high-visibility bright yellow fill.
- Added the selected-part count directly to the **Compare** tab caption.
- Added **Select visible** and **Clear visible** controls on the Parts pane; the existing **Clear** button still removes all comparison selections.
- Added persistent **Mark low/high** numeric comparison mode for simple numeric rows with matching units. Lowest values are marked `▼ LOW` and highest values are marked `▲ HIGH`.
- Added persistent **Deltas vs first** numeric comparison mode, showing each comparable numeric value's delta from the first selected part.
- Numeric comparison is deliberately limited to simple single-number values with matching units so compound/multi-value text is not misinterpreted.
## 0.1.61
- Added persistent **Compact / Normal / Wide** Compare part-column widths (180 / 230 / 320 px). Header buttons and data cells use the same selected width so alignment and horizontal scrolling remain synchronized.
- Added an **Installed Mods** summary tab listing every mod represented by loaded PartsReference parts, its loaded part count, and how many parts expose recognized ScienceMods/stock-science data.
- Clicking a mod name on the Mods page sets the PartsReference Mod filter to that mod, clears the Category selection, and returns to the Parts tab.

## 0.1.62
- Widened the **Differences only** and **Mark low/high** controls on the Compare tab.
- Added a right-click context menu to Parts rows with **Add to editor**, **Add/remove from Compare**, **Copy part ID**, and **Copy path** actions.
- Context-menu actions are modal so clicks do not fall through to the Parts list or window behind the menu.

## 0.1.63
- Fixed the lower-right resize handle losing the drag when the mouse moves quickly.
- The resize grip now captures the IMGUI mouse control for the full drag and calculates size from the original mouse-down position rather than accumulating per-event deltas.
