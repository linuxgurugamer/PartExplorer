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

