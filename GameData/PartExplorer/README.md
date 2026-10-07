# Part Explorer

PartExplorer is a utility mod for KSP1 which is intended to make browsing, inspecting, filtering and comparing the parts that are installed in your game a lot easier. Rather than having to go through the editor's catalog or the individual part configuration files, PartExplorer shows the loaded part database in a searchable and sortable table, including thumbnails, filters by category and mod, integration with the editor's filter options, and a detailed information panel for each part you have selected.

The Details pane gathers a wide variety of information concerning a part, covering fundamental properties such as mass, cost, temperature limits, resources, command capabilities, reaction wheels, SAS, antennas, cargo properties, science modules, and all other data available from supported stock modules. PartExplorer also provides improved support for SCANsat and a configurable ScienceMods system which is able to detect and show information from a number of science-related mods. The various sections and the individual fields can be customized in order that the interface will display only the information you are interested in.

The PartExplorer system also features a strong comparison facility. Up to 50 parts can be chosen directly from the main list and shown side by side in the Compare tab, with each property appearing in its own row. It is possible to hide rows that have identical values, highlight differing values, display low/high indicators and deltas relative to the first part, abbreviate long descriptions to a configurable character limit, and individually compress comparison rows to a single-line height. The widths of the comparison columns can be adjusted between Compact, Normal, and Wide arrangements.

For those who have heavily modified their installation, PartExplorer includes a number of extra features such as a Mods summary page which displays the number of parts loaded and the science support that is recognised, the ability to quickly filter by installed mod, editor-aware filtering, persistent settings, rotating previews of the parts, and the option to insert a part directly into the VAB or SPH. Parts can also be selected with a right-click to carry out quick actions such as adding them to the editor or comparison list and copying their internal part ID or configuration path. The aim is to offer a single, convenient reference tool for use both in regular gameplay and in mod development.

- Parts-list column headings remain pinned while the part rows scroll vertically; horizontal scrolling stays synchronized with the table.
Part Explorer is a KSP 1 part browser and reference window. It can browse the parts supplied by any installed mod, while retaining additional SCANsat-specific information when a part contains SCANsat scanner modules.

## Features


- The Details pane is organized into separate boxed panels for part information, stock modules/resources, science modules, and SCANsat data.
- Select any installed mod and view the parts loaded from that mod.
- Optional **All installed mods** view.
- Filter the selected mod by KSP part category.
- Text filtering by part name, internal ID, mod, category, path, and SCANsat scan data.
- Context-sensitive SCANsat filters for Biome, Altimeter, Visual, Resource, Anomaly, and Requires Daylight. They are only shown when SCANsat is selected or the current part list contains SCANsat scanner parts.
- Sort by the visible table columns.
- Select parts for comparison with the leftmost **Compare** checkbox column. Selections remain intact while sorting and filtering; the Parts tab includes a **Clear** button for clearing all selections.
- Use the **Compare** tab to view selected parts as columns with Details-pane values arranged as comparison rows. The top-level **Filtered only** toggle applies KSP's active editor filters on top of PartExplorer's own filters throughout the mod; hidden comparison selections are retained.
- Compare can optionally **highlight differing rows** and hide identical rows with **Differences only**, making similar parts easier to compare.
- Compare part columns can be switched between **Compact**, **Normal**, and **Wide** widths; the choice is saved between sessions.
- Comparison selection is limited to **50 parts**.
- **Abbreviated Descr** can shorten the Compare Description row; the character limit is configurable in Settings and defaults to **120**.
- Every Compare row has its own compress/expand control, and compressed-row choices are saved between sessions.
- The **Mods** tab summarizes loaded mods, part counts, and detected ScienceMods/stock-science data. Clicking a mod immediately applies that Mod filter and returns to Parts.
- Add optional data columns from the Settings window.
- Settings opens in a separate window. Its former sections are individual tabs: **Interface**, **Details Pane**, **Details Panels**, **Compare Rows**, and **Additional Columns**.
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
- Loaded part information is cached after the first database build, so closing and reopening PartExplorer does not reparse every installed part; the cache automatically rebuilds if KSP replaces the loaded-parts list or its count changes.
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

`GameData/PartExplorer/PluginData/ScienceMods`

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


## Exporting part data

PartExplorer can export the already-cached part database without rescanning KSP parts. Use **Export** on the Parts tab or the Compare tab.

Export scopes include:

- **Current results** — the parts matching the current PartExplorer and, when enabled, editor filters.
- **All parts** — every part in the current PartExplorer cache.
- **Selected parts** — all parts selected with the Compare checkboxes, regardless of current filters.
- **Compare parts** — the parts currently visible on the Compare screen.
- **Current mod** — all cached parts belonging to the mod selected on the Parts tab.

Supported formats are **CSV** and **JSON**. Normal part exports provide selectable fields including title, internal name, path, mod, category, manufacturer, description, mass, cost, entry cost, tech node, bulkhead profiles, crew capacity, temperatures/tolerances, modules, resources, engine data, science data, and SCANsat data. Field selections, export format, export directory, and comparison layout are remembered.

Compare exports use the rows currently enabled under **Settings → Compare Rows**. CSV comparison exports can place parts across columns or down rows. The current **Differences only**, **Mark low/high**, and **Deltas vs first** display choices are reflected in comparison exports.

The default export directory is:

`GameData/PartExplorer/Exports`

The directory is created automatically when needed. Generated filenames identify the scope and date, for example `PartExplorer_AllParts_2026-10-07.csv` or `PartExplorer_Comparison_2026-10-07.json`.

## Editor integration

When used in the VAB or SPH, the Details pane includes **Add Part to Editor** for loaded parts. The selected part is spawned using KSP's normal editor part-spawn mechanism.

## Dependencies

- **ToolbarController**
- **ClickThroughBlocker**

Both are required by the plugin.

## Toolbar

The mod uses ToolbarController and can be placed on the supported KSP/Blizzy toolbar according to the user's ToolbarController configuration.



- Automatically hides the PartExplorer window during scene changes and while KSP is paused; a window that was open before pausing is restored on unpause.


## Version 0.1.72

- The Settings window can now be dragged from anywhere in its unused window area, rather than only from the title bar. Interactive controls continue to receive normal mouse input.

## Version 0.1.71

- Moved the Compare **Export** button to the Column width row and right-aligned it.
- Limited comparison selection to 50 parts.
- Added **Abbreviated Descr** with a configurable maximum description length (default 120 characters).
- Added per-row Compare compression/expansion with saved row state.
- Moved Settings into a separate tabbed window.
