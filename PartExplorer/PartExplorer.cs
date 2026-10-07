using ClickThroughFix;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ToolbarControl_NS;
using UnityEngine;

namespace PartExplorer
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class PartExplorerAddon : MonoBehaviour
    {
        private const int WindowId = 921734;
        private const int ModSelectorPopupId = 921735;
        private const int CategorySelectorPopupId = 921736;
        internal const string ModId = "PartExplorer";
        internal const string ModName = "Part Explorer";
        private const string ToolbarButtonId = "PartExplorerButton";
        private const string ToolbarIcon38 = "PartExplorer/PluginData/toolbar_icon";
        private const string ToolbarIcon24 = "PartExplorer/PluginData/toolbar_icon_24";
        private const float MinWindowWidth = 820f;
        private const float MinWindowHeight = 500f;
        private const float MinLeftPaneWidth = 320f;
        private const float MinRightPaneWidth = 240f;
        private const float SplitterWidth = 8f;
        private const float ResizeHandleSize = 28f;
        private static readonly Rect DefaultWindowRect = new Rect(120f, 100f, 940f, 600f);

        private Rect windowRect = DefaultWindowRect;
        private float windowWidth = DefaultWindowRect.width;
        private float windowHeight = DefaultWindowRect.height;
        private float leftPaneWidth = 565f;
        private bool draggingPaneSplitter;
        private bool draggingWindowResize;
        private Vector2 resizeDragStartMouse;
        private float resizeDragStartWidth;
        private float resizeDragStartHeight;
        private bool useAlternateSkin;
        private bool showPartPath;
        private bool showDetailId = false;
        private bool showDetailCost = true;
        private bool showDetailMass = true;
        private bool showDetailMaxTemperature = true;
        private bool showDetailImpactTolerance = true;
        private bool showDetailGTolerance = true;
        private bool windowVisible;
        private bool restoreWindowAfterPause;
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private Vector2 settingsScroll;
        private Vector2 compareScroll;
        private Vector2 modsScroll;
        private string search = string.Empty;
        private int biomeFilter;
        private int altimeterFilter;
        private int visualFilter;
        private int resourceFilter;
        private int anomalyFilter;
        private int daylightFilter;
        private string selectedMod = string.Empty;
        private string selectedCategory = string.Empty;
        private bool showModSelector;
        private bool showCategorySelector;
        private bool filteredOnly;

        // Right-click context menu for Parts rows.  The menu is drawn at the end
        // of the window callback so it stays above the normal table controls.
        private bool showPartContextMenu;
        private PartRecord partContextMenuPart;
        private Vector2 partContextMenuScreenPosition;

        private bool compareHighlightDifferences = true;
        private bool compareDifferencesOnly;
        private bool compareMarkLowHigh;
        private bool compareShowDeltas;
        private CompareColumnWidthMode compareColumnWidthMode = CompareColumnWidthMode.Normal;
        private readonly HashSet<InformationSection> visibleDetailSections = new HashSet<InformationSection>();
        private readonly HashSet<InformationSection> visibleCompareSections = new HashSet<InformationSection>();

        // Editor-filter caching. KSP's filter chain can be expensive with a large
        // mod install, so never rebuild it from OnGUI. Poll only the inexpensive
        // filter-state keys and rebuild the visible-part cache when those keys change.
        private const float EditorFilterStatePollInterval = 0.10f;
        private float nextEditorFilterStatePollTime;
        private string editorFilterStateKey = string.Empty;
        private bool editorFilterCacheValid;
        private HashSet<AvailablePart> editorVisiblePartsCache;

        // Fast AvailablePart lookups. These avoid repeatedly scanning the full
        // PartLoader list for every visible PartExplorer row.
        private readonly Dictionary<string, AvailablePart> availablePartByPath =
            new Dictionary<string, AvailablePart>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AvailablePart> availablePartById =
            new Dictionary<string, AvailablePart>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AvailablePart> availablePartByTitle =
            new Dictionary<string, AvailablePart>(StringComparer.OrdinalIgnoreCase);

        // The Parts and Details panes ask for the same filtered/sorted list several
        // times during one IMGUI frame. Cache it for that frame.
        private int filteredPartsCacheFrame = -1;
        private List<PartRecord> filteredPartsFrameCache;

        private Vector2 modSelectorScroll;
        private Vector2 categorySelectorScroll;
        private Rect modSelectorAnchorScreenRect;
        private Rect categorySelectorAnchorScreenRect;
        private bool modSelectorAnchorValid;
        private bool categorySelectorAnchorValid;
        private int selectedIndex;
        private SortColumn sortColumn = SortColumn.Part;
        private WindowPage currentPage = WindowPage.Parts;
        private readonly HashSet<ExtraColumn> extraColumns = new HashSet<ExtraColumn>();
        private readonly List<string> comparisonPartKeys = new List<string>();
        private KSP.IO.PluginConfiguration configuration;
        private bool sortAscending = true;
        private ToolbarControl toolbarControl;
        private List<PartRecord> activeParts = new List<PartRecord>(SCANsatPartData.EmbeddedReferenceParts);

        // Part-data caching. Building PartRecord objects means walking every loaded
        // KSP part and parsing its stock, science, and SCANsat configuration. That
        // is intentionally done only when the loaded-part database changes, not
        // every time the toolbar button opens the already-populated window.
        private bool partDataCacheValid;
        private object cachedLoadedPartsListReference;
        private int cachedLoadedPartsCount = -1;

        private bool usingLiveData;
        private string dataSourceText = "Loaded KSP part data";
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle rowStyle;
        private GUIStyle selectedRowStyle;
        private GUIStyle smallStyle;
        private GUIStyle rightStyle;
        private GUIStyle wrappedNameStyle;
        private GUIStyle selectedWrappedNameStyle;
        private GUIStyle descriptionStyle;
        private GUIStyle compareHeaderStyle;
        private GUIStyle comparePartButtonStyle;
        private GUIStyle compareLabelStyle;
        private GUIStyle compareCellStyle;
        private GUIStyle compareHighlightedCellTextStyle;
        private GUISkin alternateSkin;
        private bool stylesUseAlternateSkin;
        private readonly Dictionary<string, Texture2D> partTextures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> livePartTextures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private const float ThumbnailSize = 48f;
        private const float ThumbnailColumnWidth = 52f;
        private const float CompareToggleColumnWidth = 26f;
        private const float CompareLabelColumnWidth = 230f;
        private const float ComparePartColumnWidthCompact = 180f;
        private const float ComparePartColumnWidthNormal = 230f;
        private const float ComparePartColumnWidthWide = 320f;
        private const float CompareRemoveButtonWidth = 18f;
        private const float CompareHeaderSpacing = 2f;
        private const float CompareColumnSpacing = 5f;
        private const float CompareMinimumRowHeight = 30f;
        private const float PartsHeaderHeight = 24f;
        private const int LiveThumbnailTextureSize = 96;
        // Low/High filter buttons are 68 px wide; 102 px is exactly 1.5x that width.
        private const float WindowIconSize = 102f;
        private Texture2D windowIconTexture;

        // Hover preview.  This follows VesselPlanner's part-image approach: use the
        // loaded AvailablePart editor icon prefab and render it with a private
        // camera.  The spreadsheet PNG remains the normal thumbnail; the live
        // preview is only rendered while the mouse is over that thumbnail.
        private const int PreviewLayer = 31;
        private const int PreviewTextureSize = 256;
        private const float HoverPreviewDisplaySize = 160f;
        private const float HoverPreviewPadding = 6f;
        private const float HoverPreviewOffset = 10f;
        private const float PreviewDegreesPerSecond = 60f;
        private static readonly Vector3 PreviewSceneOrigin = new Vector3(100000f, 100000f, 100000f);
        private string rotatingPreviewPartKey;
        private GameObject rotatingPreviewRoot;
        private GameObject rotatingPreviewPart;
        private Camera rotatingPreviewCamera;
        private Light rotatingPreviewKeyLight;
        private Light rotatingPreviewFillLight;
        private RenderTexture rotatingPreviewTexture;
        private float rotatingPreviewAngle;
        private Texture hoveredPreviewTexture;
        private Rect hoveredPreviewSourceRect;
        private bool hoveredPreviewActive;

        private enum SortColumn
        {
            Part, Cost, Mass, Ec, Science, Id, Fov, Daylight, MinAltitude, OptimalAltitude, MaxAltitude,
            MaxTemp, ImpactTolerance, GTolerance, Biome, Altimetry, Visual, Resource, Anomaly
        }
        private enum WindowPage { Parts, Compare, Mods, Settings }

        private enum CompareColumnWidthMode
        {
            Compact,
            Normal,
            Wide
        }

        private enum InformationSection
        {
            PartInformation,
            CargoPartInfo,
            Command,
            DataTransmitter,
            ProbeControlPoint,
            ReactionWheel,
            Sas,
            Resources,
            ScienceModules,
            ScanSat,
            ScanTypes,
            AltitudeRange
        }

        private sealed class ComparisonRow
        {
            public string Section = string.Empty;
            public string Label = string.Empty;
            public readonly Dictionary<string, string> Values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class ModSummaryRow
        {
            public string ModName = string.Empty;
            public int PartCount;
            public int ScienceDataPartCount;
        }
        private enum ExtraColumn
        {
            Id, Cost, Ec, Science, Fov, Daylight, MinAltitude, OptimalAltitude, MaxAltitude,
            MaxTemp, ImpactTolerance, GTolerance, Biome, Altimetry, Visual, Resource, Anomaly
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            LoadPartTextures();
            LoadWindowIcon();

            ToolbarControl.RegisterMod(ModId, ModName);
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(
                ToggleWindowOn,
                ToggleWindowOff,
                ApplicationLauncher.AppScenes.ALWAYS,
                ModId,
                ToolbarButtonId,
                ToolbarIcon38,
                ToolbarIcon24,
                ModName);

            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
        }

        private void Start()
        {
        }

        private void Update()
        {
            if (!windowVisible || !filteredOnly || !HighLogic.LoadedSceneIsEditor || EditorPartList.Instance == null)
                return;

            if (Time.realtimeSinceStartup < nextEditorFilterStatePollTime)
                return;

            nextEditorFilterStatePollTime = Time.realtimeSinceStartup + EditorFilterStatePollInterval;
            string currentFilterStateKey = GetEditorFilterStateKey();
            if (!editorFilterCacheValid ||
                !string.Equals(currentFilterStateKey, editorFilterStateKey, StringComparison.Ordinal))
                RebuildEditorFilterCache(currentFilterStateKey);
        }

        private void OnDestroy()
        {
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);
            GameEvents.onGamePause.Remove(OnGamePause);
            GameEvents.onGameUnpause.Remove(OnGameUnpause);

            SaveSettings();
            DestroyRotatingPreview();
            DestroyPartTextures();
            DestroyLivePartTextures();
            DestroyWindowIcon();
        }

        private void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> scenes)
        {
            restoreWindowAfterPause = false;
            ForceHideWindow();
        }

        private void OnGameSceneLoadRequested(GameScenes scene)
        {
            // Hide as soon as KSP requests a scene load so no PartExplorer window
            // carries an open state across scene transitions.
            restoreWindowAfterPause = false;
            ForceHideWindow();
        }

        private void OnGamePause()
        {
            restoreWindowAfterPause = windowVisible;
            ForceHideWindow();
        }

        private void OnGameUnpause()
        {
            if (!restoreWindowAfterPause)
                return;

            restoreWindowAfterPause = false;
            windowVisible = true;
            if (toolbarControl != null)
                toolbarControl.SetTrue(false);
        }

        private void ForceHideWindow()
        {
            windowVisible = false;
            showModSelector = false;
            showCategorySelector = false;
            showPartContextMenu = false;
            draggingPaneSplitter = false;
            draggingWindowResize = false;
            DestroyRotatingPreview();

            if (toolbarControl != null)
                toolbarControl.SetFalse(false);
        }

        private void ToggleWindowOn()
        {
            EnsurePartDataCache();
            windowVisible = true;

            // Build once on open so the first displayed list already reflects the
            // editor filters. Subsequent refreshes happen from Update(), not OnGUI.
            if (filteredOnly && HighLogic.LoadedSceneIsEditor && EditorPartList.Instance != null)
                RebuildEditorFilterCache();
        }

        private void ToggleWindowOff()
        {
            windowVisible = false;
            DestroyRotatingPreview();
        }

        private void OnGUI()
        {
            if (!windowVisible)
                return;

            hoveredPreviewActive = false;
            hoveredPreviewTexture = null;

            if (alternateSkin == null)
                alternateSkin = GUI.skin;

            GUI.skin = useAlternateSkin && alternateSkin != null ? alternateSkin : HighLogic.Skin;
            EnsureStyles();

            float maxWidth = Mathf.Max(MinWindowWidth, Screen.width - Mathf.Max(0f, windowRect.x));
            float maxHeight = Mathf.Max(MinWindowHeight, Screen.height - Mathf.Max(0f, windowRect.y));
            windowWidth = Mathf.Clamp(windowWidth, MinWindowWidth, maxWidth);
            windowHeight = Mathf.Clamp(windowHeight, MinWindowHeight, maxHeight);
            windowRect.width = windowWidth;
            windowRect.height = windowHeight;

            Rect actualWindowRect = ClickThruBlocker.GUILayoutWindow(WindowId, windowRect, DrawWindow, "Part Explorer",
                GUILayout.Width(windowWidth), GUILayout.Height(windowHeight),
                GUILayout.MinWidth(MinWindowWidth), GUILayout.MinHeight(MinWindowHeight));

            // GUILayout can require a larger size than requested.  Keep the stored
            // size synchronized with the actual window so the corner resize handle
            // always remains attached to the visible lower-right edge.
            windowRect = actualWindowRect;
            windowWidth = Mathf.Max(MinWindowWidth, actualWindowRect.width);
            windowHeight = Mathf.Max(MinWindowHeight, actualWindowRect.height);
            windowRect.width = windowWidth;
            windowRect.height = windowHeight;
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - 120f));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - 40f));

        }

        private void EnsureStyles()
        {
            if (titleStyle != null && stylesUseAlternateSkin == useAlternateSkin)
                return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            sectionStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            rowStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            selectedRowStyle = new GUIStyle(rowStyle) { fontStyle = FontStyle.Bold };
            wrappedNameStyle = new GUIStyle(rowStyle) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
            selectedWrappedNameStyle = new GUIStyle(wrappedNameStyle) { fontStyle = FontStyle.Bold };
            descriptionStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            compareHeaderStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            comparePartButtonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            compareLabelStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            compareCellStyle = new GUIStyle(GUI.skin.box) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal };
            compareHighlightedCellTextStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(compareCellStyle.padding.left, compareCellStyle.padding.right,
                    compareCellStyle.padding.top, compareCellStyle.padding.bottom)
            };
            compareHighlightedCellTextStyle.normal.textColor = Color.black;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            rightStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
            stylesUseAlternateSkin = useAlternateSkin;
        }

        private void DrawWindow(int id)
        {
            // Treat an open selector as a modal popup for the entire IMGUI event.
            // Capture this before processing input so that, even when a selection
            // closes the popup during MouseUp, controls underneath remain disabled
            // for the rest of that same event.
            bool selectorModal = showModSelector || showCategorySelector || showPartContextMenu;

            // While a selector is open, draw the normal window controls disabled.
            // The popup itself is drawn last with GUI input restored, so its own
            // scroll view and GUI.Button rows exclusively own the mouse event.
            // This avoids competing manual row-index calculations.

            bool previousGuiEnabled = GUI.enabled;
            if (selectorModal)
                GUI.enabled = false;

            GUILayout.BeginVertical();
            DrawPageTabs();
            GUILayout.Space(4f);

            if (currentPage == WindowPage.Parts)
            {
                GUILayout.BeginHorizontal();
                DrawPartList();
                DrawPaneSplitter();
                DrawDetails();
                GUILayout.EndHorizontal();
            }
            else if (currentPage == WindowPage.Compare)
            {
                DrawComparePage();
            }
            else if (currentPage == WindowPage.Mods)
            {
                DrawModsPage();
            }
            else
            {
                DrawSettingsPage();
            }

            GUILayout.Space(4f);

            // Keep the live-data status on the left while centering Close in the
            // full window width.  Using one layout rect avoids the status text
            // shifting the button away from the true center.
            Rect footerRect = GUILayoutUtility.GetRect(0f, 24f, GUILayout.ExpandWidth(true));
            string footerStatus = dataSourceText + " • " + activeParts.Count.ToString(CultureInfo.InvariantCulture) + " parts";
            Rect statusRect = new Rect(footerRect.x, footerRect.y, Mathf.Max(0f, footerRect.width * 0.5f - 50f), footerRect.height);
            Rect closeRect = new Rect(footerRect.center.x - 40f, footerRect.y, 80f, footerRect.height);
            GUI.Label(statusRect, footerStatus, smallStyle);
            if (GUI.Button(closeRect, "Close"))
            {
                windowVisible = false;
                DestroyRotatingPreview();
                if (toolbarControl != null)
                    toolbarControl.SetFalse(false);
            }
            GUILayout.EndVertical();

            // Draw the enlarged rotating preview inside the window callback so it
            // participates in the same IMGUI window group and cannot disappear
            // behind or outside the KSP window draw order.
            DrawHoveredPreview();

            DrawWindowResizeHandle();
            DrawActiveTooltip();

            // Re-enable GUI input only for the popup itself.  Everything under the
            // popup has already been drawn in a disabled state while a selector is
            // open, so overlapping Name/Mass/list buttons cannot receive the same
            // click.
            GUI.enabled = previousGuiEnabled;

            // Draw dropdowns as the final interactive controls in this window.
            // Their anchor rectangles and Event.current.mousePosition are in the
            // same window-local coordinate system, and drawing them here keeps
            // them above the normal GUILayout content.
            DrawSelectorPopups();
            DrawPartContextMenu();

            // Do not let the window itself acquire a drag while any popup is modal.
            if (!showModSelector && !showCategorySelector && !showPartContextMenu)
                GUI.DragWindow();
        }

        private void DrawPageTabs()
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(currentPage == WindowPage.Parts, "Parts", GUI.skin.button, GUILayout.Width(90f)))
                            currentPage = WindowPage.Parts;
                        string compareTabCaption = "Compare (" + comparisonPartKeys.Count.ToString(CultureInfo.InvariantCulture) + ")";
                        if (GUILayout.Toggle(currentPage == WindowPage.Compare, compareTabCaption, GUI.skin.button, GUILayout.Width(110f)))
                            currentPage = WindowPage.Compare;
                        if (GUILayout.Toggle(currentPage == WindowPage.Mods, "Mods", GUI.skin.button, GUILayout.Width(90f)))
                            currentPage = WindowPage.Mods;
                        if (GUILayout.Toggle(currentPage == WindowPage.Settings, "Settings", GUI.skin.button, GUILayout.Width(90f)))
                            currentPage = WindowPage.Settings;
                    }

                    switch (currentPage)
                    {
                        case WindowPage.Parts:
                            GUILayout.Space(4f);
                            DrawSearchAndSort();
                            break;
                        case WindowPage.Compare:
                            DrawCompareTop();
                            break;
                        case WindowPage.Mods:
                            DrawModsTop();
                            break;
                        case WindowPage.Settings:
                            DrawSettingsTop();
                            break;
                    }

                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    // Decorative PartExplorer icon.  Keep this in the header layout so
                    // it occupies real space and never covers or intercepts controls below.
                    if (windowIconTexture != null)
                        GUILayout.Label(windowIconTexture, GUIStyle.none, GUILayout.Width(WindowIconSize), GUILayout.Height(WindowIconSize));
                    else
                        GUILayout.Space(WindowIconSize);
                }
            }
            if (currentPage == WindowPage.Parts)
                ShowScanSatFilters();
            GUILayout.Space(4f);
        }

        private void DrawSearchAndSort()
        {
            DrawModAndCategorySelectors();
            GUILayout.Space(3f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(42f));
            search = GUILayout.TextField(search ?? string.Empty, GUILayout.MinWidth(220f));
            if (GUILayout.Button("Clear", GUILayout.Width(55f)))
            {
                search = string.Empty;
                biomeFilter = 0;
                altimeterFilter = 0;
                visualFilter = 0;
                resourceFilter = 0;
                anomalyFilter = 0;
                daylightFilter = 0;
                SaveSettings();
            }

            GUILayout.Space(12f);
            GUILayout.Label("Sort:", GUILayout.Width(38f));
            DrawSortButton("Part", SortColumn.Part, 70f);
            DrawSortButton("Cost", SortColumn.Cost, 60f);
            DrawSortButton("Mass", SortColumn.Mass, 60f);
            DrawSortButton("EC/s", SortColumn.Ec, 60f);
            DrawSortButton("Science", SortColumn.Science, 70f);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void ShowScanSatFilters()
        {
            if (ShouldShowScanSatFilters())
            {
                GUILayout.Space(3f);
                GUILayout.BeginHorizontal();
                DrawThreeStateFilter("Biome", ref biomeFilter, "Any", "Yes", "No");
                GUILayout.Space(8f);
                DrawThreeStateFilter("Altimeter", ref altimeterFilter, "Any", "Low", "High");
                GUILayout.Space(8f);
                DrawThreeStateFilter("Visual", ref visualFilter, "Any", "Low", "High");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawThreeStateFilter("Resource", ref resourceFilter, "Any", "Low", "High");
                GUILayout.Space(8f);
                DrawThreeStateFilter("Anomaly", ref anomalyFilter, "Any", "Yes", "No");
                GUILayout.Space(8f);
                DrawThreeStateFilter("Daylight", ref daylightFilter, "Any", "Yes", "No");
                GUILayout.EndHorizontal();
            }
        }

        private void DrawModAndCategorySelectors()
        {
            List<string> mods = GetAvailableMods();
            EnsureSelectedMod(mods);
            List<string> categories = GetAvailableCategories();
            if (!string.IsNullOrEmpty(selectedCategory) && !categories.Contains(selectedCategory))
                selectedCategory = string.Empty;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mod:", GUILayout.Width(42f));
            string modCaption = string.IsNullOrEmpty(selectedMod) ? "All installed mods" : selectedMod;
            if (GUILayout.Button(modCaption, GUILayout.Width(220f)))
            {
                showModSelector = !showModSelector;
                showCategorySelector = false;
            }
            // GetLastRect is already in this window's local coordinate system.
            // Keep it exactly as-is; converting to screen coordinates here causes
            // the popup to drift to the window origin in KSP's nested IMGUI groups.
            modSelectorAnchorScreenRect = GUILayoutUtility.GetLastRect();
            modSelectorAnchorValid = true;

            GUILayout.Space(12f);
            GUILayout.Label("Category:", GUILayout.Width(62f));
            string categoryCaption = string.IsNullOrEmpty(selectedCategory) ? "Any" : FriendlyCategory(selectedCategory);
            if (GUILayout.Button(categoryCaption, GUILayout.Width(145f)))
            {
                showCategorySelector = !showCategorySelector;
                showModSelector = false;
            }
            categorySelectorAnchorScreenRect = GUILayoutUtility.GetLastRect();
            categorySelectorAnchorValid = true;

            GUILayout.Space(12f);
            bool newFilteredOnly = GUILayout.Toggle(
                filteredOnly,
                new GUIContent("Filtered only",
                    "When enabled in the VAB/SPH, PartExplorer shows only parts that pass both its own filters and KSP's active editor filters."),
                GUILayout.Width(110f));
            if (newFilteredOnly != filteredOnly)
            {
                filteredOnly = newFilteredOnly;
                listScroll = Vector2.zero;
                compareScroll = Vector2.zero;
                selectedIndex = 0;
                InvalidateEditorFilterCache();
                SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawSelectorPopups()
        {
            if (showModSelector && modSelectorAnchorValid)
            {
                List<string> items = new List<string> { "All installed mods" };
                items.AddRange(GetAvailableMods());
                int current = string.IsNullOrEmpty(selectedMod)
                    ? 0
                    : Math.Max(0, items.FindIndex(m => string.Equals(m, selectedMod, StringComparison.OrdinalIgnoreCase)));

                int selected = DrawScreenDropDown(
                    ModSelectorPopupId,
                    modSelectorAnchorScreenRect,
                    items,
                    current,
                    ref modSelectorScroll,
                    9);

                if (selected >= 0)
                {
                    selectedMod = selected == 0 ? string.Empty : items[selected];
                    selectedCategory = string.Empty;
                    showModSelector = false;
                    selectedIndex = 0;
                    listScroll = Vector2.zero;
                    SaveSettings();
                }
            }

            if (showCategorySelector && categorySelectorAnchorValid)
            {
                List<string> values = new List<string> { string.Empty };
                values.AddRange(GetAvailableCategories());
                List<string> labels = values
                    .Select(v => string.IsNullOrEmpty(v) ? "Any" : FriendlyCategory(v))
                    .ToList();
                int current = Math.Max(0, values.FindIndex(c =>
                    string.Equals(c ?? string.Empty, selectedCategory ?? string.Empty, StringComparison.OrdinalIgnoreCase)));

                int selected = DrawScreenDropDown(
                    CategorySelectorPopupId,
                    categorySelectorAnchorScreenRect,
                    labels,
                    current,
                    ref categorySelectorScroll,
                    9);

                if (selected >= 0)
                {
                    selectedCategory = values[selected];
                    showCategorySelector = false;
                    selectedIndex = 0;
                    listScroll = Vector2.zero;
                    SaveSettings();
                }
            }
        }

        private int DrawScreenDropDown(int popupId, Rect anchorWindowRect, IList<string> items,
            int selectedIndexValue, ref Vector2 scrollPosition, int visibleItems)
        {
            if (items == null || items.Count == 0)
                return -1;

            const float itemHeight = 24f;
            const float padding = 4f;
            const float scrollBarAllowance = 18f;

            float width = Mathf.Max(anchorWindowRect.width, 160f);
            float bodyHeight = Mathf.Min(items.Count, Math.Max(1, visibleItems)) * itemHeight;
            float height = bodyHeight + padding * 2f;

            float windowLocalWidth = Mathf.Max(1f, windowRect.width);
            float windowLocalHeight = Mathf.Max(1f, windowRect.height);

            float x = anchorWindowRect.xMin;
            float y = anchorWindowRect.yMax + 2f;
            if (x + width > windowLocalWidth - 4f)
                x = Mathf.Max(4f, windowLocalWidth - width - 4f);
            if (y + height > windowLocalHeight - 4f)
                y = Mathf.Max(24f, anchorWindowRect.yMin - height - 2f);

            Rect popupRect = new Rect(x, y, width, height);

            // This method runs inside DrawWindow(), so both popupRect and the
            // selector anchor are window-local.  Draw it late to keep it on top.
            GUI.Box(popupRect, GUIContent.none);

            Rect viewportRect = new Rect(
                popupRect.x + padding,
                popupRect.y + padding,
                popupRect.width - padding * 2f,
                popupRect.height - padding * 2f);
            float viewWidth = Mathf.Max(1f, viewportRect.width - (items.Count > visibleItems ? scrollBarAllowance : 0f));
            Rect viewRect = new Rect(0f, 0f, viewWidth, items.Count * itemHeight);

            scrollPosition = GUI.BeginScrollView(viewportRect, scrollPosition, viewRect, false, items.Count > visibleItems);
            int result = -1;
            for (int i = 0; i < items.Count; i++)
            {
                GUIStyle style = i == selectedIndexValue ? selectedRowStyle : rowStyle;
                Rect itemRect = new Rect(0f, i * itemHeight, viewWidth, itemHeight);
                if (GUI.Button(itemRect, items[i] ?? string.Empty, style))
                    result = i;
            }
            GUI.EndScrollView();

            // The rest of the window was already drawn disabled for this event,
            // so an outside MouseDown can safely close the popup without the click
            // falling through to a control underneath. Clicking the selector button
            // itself also closes the currently open popup.
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 &&
                !popupRect.Contains(e.mousePosition))
            {
                if (popupId == ModSelectorPopupId)
                    showModSelector = false;
                else if (popupId == CategorySelectorPopupId)
                    showCategorySelector = false;
                e.Use();
            }

            return result;
        }

        private void EnsureSelectedMod(List<string> mods)
        {
            if (mods == null || mods.Count == 0)
            {
                selectedMod = string.Empty;
                return;
            }
            if (!string.IsNullOrEmpty(selectedMod) && !mods.Contains(selectedMod, StringComparer.OrdinalIgnoreCase))
                selectedMod = string.Empty;
        }

        private List<string> GetAvailableMods()
        {
            return activeParts
                .Where(p => p != null && !string.IsNullOrEmpty(p.ModName))
                .Select(p => p.ModName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<string> GetAvailableCategories()
        {
            IEnumerable<PartRecord> q = activeParts;
            if (!string.IsNullOrEmpty(selectedMod))
                q = q.Where(p => string.Equals(p.ModName, selectedMod, StringComparison.OrdinalIgnoreCase));
            return q.Where(p => p != null && !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => FriendlyCategory(c), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string FriendlyCategory(string category)
        {
            switch (category)
            {
                case "FuelTank": return "Fuel Tanks";
                case "Engine": return "Engines";
                case "Pods": return "Pods";
                case "Aero": return "Aerodynamics";
                case "Coupling": return "Coupling";
                case "Electrical": return "Electrical";
                case "Ground": return "Ground";
                case "Payload": return "Payload";
                case "Science": return "Science";
                case "Structural": return "Structural";
                case "Thermal": return "Thermal";
                case "Utility": return "Utility";
                case "Control": return "Control";
                default: return string.IsNullOrEmpty(category) ? "Any" : category;
            }
        }

        private void DrawThreeStateFilter(string label, ref int value, string anyCaption, string firstCaption, string secondCaption)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(275f));
            GUILayout.Label(label + ":", GUILayout.Width(68f));
            int old = value;
            if (GUILayout.Toggle(value == 0, anyCaption, GUI.skin.button, GUILayout.Width(60f))) value = 0;
            if (GUILayout.Toggle(value == 1, firstCaption, GUI.skin.button, GUILayout.Width(68f))) value = 1;
            if (GUILayout.Toggle(value == 2, secondCaption, GUI.skin.button, GUILayout.Width(68f))) value = 2;
            GUILayout.EndHorizontal();
            if (old != value)
                SaveSettings();
        }

        private void DrawSortButton(string caption, SortColumn column, float width)
        {
            string suffix = sortColumn == column ? (sortAscending ? " ▲" : " ▼") : string.Empty;
            if (GUILayout.Button(caption + suffix, GUILayout.Width(width)))
            {
                if (sortColumn == column)
                    sortAscending = !sortAscending;
                else
                {
                    sortColumn = column;
                    sortAscending = true;
                }
            }
        }

        private IEnumerable<PartRecord> GetEditorFilteredUniverse()
        {
            IEnumerable<PartRecord> parts = activeParts;
            if (!filteredOnly || !HighLogic.LoadedSceneIsEditor || EditorPartList.Instance == null)
                return parts;

            // Never run KSP's editor filter chain from OnGUI. Update() maintains
            // this cache on a short interval. If a cache is temporarily unavailable,
            // keep the PartExplorer list usable rather than blocking the GUI.
            if (!editorFilterCacheValid || editorVisiblePartsCache == null)
                return parts;

            HashSet<AvailablePart> visibleParts = editorVisiblePartsCache;
            return parts.Where(part =>
            {
                AvailablePart availablePart = FindAvailablePart(part);
                return availablePart != null && visibleParts.Contains(availablePart);
            });
        }

        private string GetEditorFilterStateKey()
        {
            EditorPartList editorPartList = EditorPartList.Instance;
            if (editorPartList == null)
                return string.Empty;

            string excludeKey = editorPartList.ExcludeFilters != null
                ? editorPartList.ExcludeFilters.GetFilterKey() ?? string.Empty
                : string.Empty;
            string categoryKey = editorPartList.CategorizerFilters != null
                ? editorPartList.CategorizerFilters.GetFilterKey() ?? string.Empty
                : string.Empty;
            string amountKey = GetSingleEditorFilterKey(editorPartList.AmountAvailableFilter);
            string searchKey = GetSingleEditorFilterKey(editorPartList.SearchFilterParts);

            return excludeKey + "|" + amountKey + "|" + categoryKey + "|" + searchKey;
        }

        private static string GetSingleEditorFilterKey(EditorPartListFilter<AvailablePart> filter)
        {
            if (filter == null)
                return string.Empty;

            var filters = new EditorPartListFilterList<AvailablePart>();
            filters.AddFilter(filter);
            return filters.GetFilterKey() ?? string.Empty;
        }

        private void RebuildEditorFilterCache(string currentFilterStateKey = null)
        {
            if (!filteredOnly || !HighLogic.LoadedSceneIsEditor || EditorPartList.Instance == null)
            {
                editorFilterStateKey = string.Empty;
                editorFilterCacheValid = false;
                editorVisiblePartsCache = null;
                InvalidateFilteredPartsFrameCache();
                return;
            }

            try
            {
                EditorPartList editorPartList = EditorPartList.Instance;
                List<AvailablePart> visibleParts = PartLoader.LoadedPartsList == null
                    ? new List<AvailablePart>()
                    : PartLoader.LoadedPartsList.Where(part => part != null).ToList();

                // KSP exclusion filters include game-mode restrictions and filters
                // contributed by other mods.
                if (editorPartList.ExcludeFilters != null && editorPartList.ExcludeFilters.Count > 0)
                    visibleParts = editorPartList.ExcludeFilters.GetFilteredList(visibleParts);

                // Apply the normal editor availability gate.
                if (editorPartList.AmountAvailableFilter != null)
                {
                    var amountFilters = new EditorPartListFilterList<AvailablePart>();
                    amountFilters.AddFilter(editorPartList.AmountAvailableFilter);
                    visibleParts = amountFilters.GetFilteredList(visibleParts);
                }

                // Active stock and custom/mod category filters.
                if (editorPartList.CategorizerFilters != null && editorPartList.CategorizerFilters.Count > 0)
                    visibleParts = editorPartList.CategorizerFilters.GetFilteredList(visibleParts);

                // Active editor search filter.
                if (editorPartList.SearchFilterParts != null)
                {
                    var searchFilters = new EditorPartListFilterList<AvailablePart>();
                    searchFilters.AddFilter(editorPartList.SearchFilterParts);
                    visibleParts = searchFilters.GetFilteredList(visibleParts);
                }

                editorVisiblePartsCache = new HashSet<AvailablePart>(visibleParts);
                editorFilterStateKey = currentFilterStateKey ?? GetEditorFilterStateKey();
                editorFilterCacheValid = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to apply editor part filters: " + ex.Message);
                editorVisiblePartsCache = null;
                editorFilterStateKey = string.Empty;
                editorFilterCacheValid = false;
            }

            InvalidateFilteredPartsFrameCache();
        }

        private void InvalidateEditorFilterCache()
        {
            editorFilterStateKey = string.Empty;
            editorFilterCacheValid = false;
            editorVisiblePartsCache = null;
            nextEditorFilterStatePollTime = 0f;
            InvalidateFilteredPartsFrameCache();
        }

        private void InvalidateFilteredPartsFrameCache()
        {
            filteredPartsCacheFrame = -1;
            filteredPartsFrameCache = null;
        }

        private IEnumerable<PartRecord> GetBaseFilteredParts()
        {
            IEnumerable<PartRecord> q = GetEditorFilteredUniverse();
            if (!string.IsNullOrEmpty(selectedMod))
                q = q.Where(p => string.Equals(p.ModName, selectedMod, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(selectedCategory))
                q = q.Where(p => string.Equals(p.Category, selectedCategory, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                q = q.Where(p => p.Part.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Id.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Description.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.ScanSummary.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.ScienceSummary.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.StockSummary.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.ModName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Category.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Path.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return q;
        }

        private bool ShouldShowScanSatFilters()
        {
            // Show SCANsat-specific controls only when the current part universe,
            // after PartExplorer Mod/Category/text filters and the optional KSP
            // editor filters have been applied, still contains at least one SCANsat
            // scanner.  Do not special-case a saved SCANsat mod selection: it may
            // currently have zero visible scanner parts under the active filters.
            return GetBaseFilteredParts().Any(p =>
                p != null && p.ScanSat != null && p.ScanSat.HasScannerModules);
        }

        private List<PartRecord> GetFilteredSorted()
        {
            if (filteredPartsFrameCache != null && filteredPartsCacheFrame == Time.frameCount)
                return filteredPartsFrameCache;

            IEnumerable<PartRecord> q = GetBaseFilteredParts();

            if (ShouldShowScanSatFilters())
            {
                if (biomeFilter == 1) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && !string.IsNullOrEmpty(p.ScanSat.Biome));
                else if (biomeFilter == 2) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && string.IsNullOrEmpty(p.ScanSat.Biome));

                if (altimeterFilter == 1) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && SCANsatPartData.HasResolution(p.ScanSat.Altimetry, false));
                else if (altimeterFilter == 2) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && SCANsatPartData.HasResolution(p.ScanSat.Altimetry, true));

                if (visualFilter == 1) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && SCANsatPartData.HasResolution(p.ScanSat.Visual, false));
                else if (visualFilter == 2) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && SCANsatPartData.HasResolution(p.ScanSat.Visual, true));

                if (resourceFilter == 1) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && SCANsatPartData.HasResolution(p.ScanSat.Resource, false));
                else if (resourceFilter == 2) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && SCANsatPartData.HasResolution(p.ScanSat.Resource, true));

                if (anomalyFilter == 1) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && !string.IsNullOrEmpty(p.ScanSat.Anomaly));
                else if (anomalyFilter == 2) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && string.IsNullOrEmpty(p.ScanSat.Anomaly));

                if (daylightFilter == 1) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && p.ScanSat.RequiresDaylight);
                else if (daylightFilter == 2) q = q.Where(p => p.ScanSat != null && p.ScanSat.HasScannerModules && !p.ScanSat.RequiresDaylight);
            }

            Func<PartRecord, object> key;
            switch (sortColumn)
            {
                case SortColumn.Cost: key = p => p.Cost; break;
                case SortColumn.Mass: key = p => p.Mass; break;
                case SortColumn.Ec: key = p => p.ScanSat != null ? p.ScanSat.EcPerSec : double.MinValue; break;
                case SortColumn.Science: key = p => p.ScanSat != null ? p.ScanSat.Science : double.MinValue; break;
                case SortColumn.Id: key = p => p.Id; break;
                case SortColumn.Fov: key = p => p.ScanSat != null ? p.ScanSat.Fov : double.MinValue; break;
                case SortColumn.Daylight: key = p => p.ScanSat != null && p.ScanSat.HasScannerModules ? (object)p.ScanSat.RequiresDaylight : null; break;
                case SortColumn.MinAltitude: key = p => ParseSortableNumber(p.ScanSat != null ? p.ScanSat.MinAltitudeText : string.Empty); break;
                case SortColumn.OptimalAltitude: key = p => ParseSortableNumber(p.ScanSat != null ? p.ScanSat.OptimalAltitudeText : string.Empty); break;
                case SortColumn.MaxAltitude: key = p => ParseSortableNumber(p.ScanSat != null ? p.ScanSat.MaxAltitudeText : string.Empty); break;
                case SortColumn.MaxTemp: key = p => p.MaxTemp; break;
                case SortColumn.ImpactTolerance: key = p => p.ToleranceMs; break;
                case SortColumn.GTolerance: key = p => p.ToleranceG; break;
                case SortColumn.Biome: key = p => p.ScanSat != null ? p.ScanSat.Biome : string.Empty; break;
                case SortColumn.Altimetry: key = p => p.ScanSat != null ? p.ScanSat.Altimetry : string.Empty; break;
                case SortColumn.Visual: key = p => p.ScanSat != null ? p.ScanSat.Visual : string.Empty; break;
                case SortColumn.Resource: key = p => p.ScanSat != null ? p.ScanSat.Resource : string.Empty; break;
                case SortColumn.Anomaly: key = p => p.ScanSat != null ? p.ScanSat.Anomaly : string.Empty; break;
                default: key = p => p.Part; break;
            }
            q = sortAscending ? q.OrderBy(key) : q.OrderByDescending(key);
            filteredPartsFrameCache = q.ToList();
            filteredPartsCacheFrame = Time.frameCount;
            return filteredPartsFrameCache;
        }

        private void InvalidateStyles()
        {
            titleStyle = null;
            sectionStyle = null;
            rowStyle = null;
            selectedRowStyle = null;
            smallStyle = null;
            rightStyle = null;
            wrappedNameStyle = null;
            selectedWrappedNameStyle = null;
            compareHeaderStyle = null;
            comparePartButtonStyle = null;
            compareLabelStyle = null;
            compareCellStyle = null;
            compareHighlightedCellTextStyle = null;
        }

        private void DrawPaneSplitter()
        {
            Rect rect = GUILayoutUtility.GetRect(SplitterWidth, SplitterWidth,
                GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
            GUI.Box(rect, string.Empty, GUI.skin.box);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                draggingPaneSplitter = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && draggingPaneSplitter)
            {
                float maxLeft = Mathf.Max(MinLeftPaneWidth, windowWidth - MinRightPaneWidth - SplitterWidth - 40f);
                leftPaneWidth = Mathf.Clamp(leftPaneWidth + e.delta.x, MinLeftPaneWidth, maxLeft);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && draggingPaneSplitter)
            {
                draggingPaneSplitter = false;
                SaveSettings();
                e.Use();
            }
        }

        private void DrawWindowResizeHandle()
        {
            Rect rect = new Rect(
                Mathf.Max(0f, windowRect.width - ResizeHandleSize),
                Mathf.Max(0f, windowRect.height - ResizeHandleSize),
                ResizeHandleSize,
                ResizeHandleSize);

            // A compact corner grip replaces the old full-height right-side bar.
            GUI.Box(rect, "///", GUI.skin.box);

            // Capture the mouse for the lifetime of the drag.  Without an IMGUI
            // hot control, a fast cursor movement can leave the small resize grip
            // between events and the drag appears to be lost.
            const int resizeControlHint = 921738;
            int controlId = GUIUtility.GetControlID(resizeControlHint, FocusType.Passive, rect);
            Event e = Event.current;
            EventType eventType = e.GetTypeForControl(controlId);

            if (eventType == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                GUIUtility.hotControl = controlId;
                draggingWindowResize = true;
                resizeDragStartMouse = e.mousePosition;
                resizeDragStartWidth = windowWidth;
                resizeDragStartHeight = windowHeight;
                e.Use();
            }
            else if (eventType == EventType.MouseDrag && draggingWindowResize && GUIUtility.hotControl == controlId)
            {
                Vector2 dragDelta = e.mousePosition - resizeDragStartMouse;
                float maxWidth = Mathf.Max(MinWindowWidth, Screen.width - Mathf.Max(0f, windowRect.x));
                float maxHeight = Mathf.Max(MinWindowHeight, Screen.height - Mathf.Max(0f, windowRect.y));

                windowWidth = Mathf.Clamp(resizeDragStartWidth + dragDelta.x, MinWindowWidth, maxWidth);
                windowHeight = Mathf.Clamp(resizeDragStartHeight + dragDelta.y, MinWindowHeight, maxHeight);
                windowRect.width = windowWidth;
                windowRect.height = windowHeight;

                float maxLeft = Mathf.Max(MinLeftPaneWidth, windowWidth - MinRightPaneWidth - SplitterWidth - 40f);
                leftPaneWidth = Mathf.Clamp(leftPaneWidth, MinLeftPaneWidth, maxLeft);
                e.Use();
            }
            else if (eventType == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                draggingWindowResize = false;
                SaveSettings();
                e.Use();
            }
        }

        private void DrawPartList()
        {
            var rows = GetFilteredSorted();
            float maxLeft = Mathf.Max(MinLeftPaneWidth, windowWidth - MinRightPaneWidth - SplitterWidth - 40f);
            leftPaneWidth = Mathf.Clamp(leftPaneWidth, MinLeftPaneWidth, maxLeft);
            GUILayout.BeginVertical(GUILayout.Width(leftPaneWidth));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Parts", sectionStyle);
            GUILayout.FlexibleSpace();
            bool previousEnabled = GUI.enabled;

            GUI.enabled = previousEnabled && rows.Count > 0;
            if (GUILayout.Button(new GUIContent("Select visible", "Select every part currently shown by the active filters"), GUILayout.Width(95f)))
            {
                foreach (PartRecord visiblePart in rows)
                    SetComparisonSelected(visiblePart, true);
                compareScroll = Vector2.zero;
            }

            bool anyVisibleSelected = rows.Any(IsSelectedForComparison);
            GUI.enabled = previousEnabled && anyVisibleSelected;
            if (GUILayout.Button(new GUIContent("Clear visible", "Clear comparison selection for the parts currently shown by the active filters"), GUILayout.Width(90f)))
            {
                foreach (PartRecord visiblePart in rows)
                    SetComparisonSelected(visiblePart, false);
                compareScroll = Vector2.zero;
            }

            GUI.enabled = previousEnabled && comparisonPartKeys.Count > 0;
            if (GUILayout.Button(new GUIContent("Clear", "Clear all comparison selections"), GUILayout.Width(65f)))
            {
                comparisonPartKeys.Clear();
                compareScroll = Vector2.zero;
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));

            // Keep the column headings pinned above the scrolling part rows.  The
            // header is drawn after the scroll view so it uses the scroll view's
            // current horizontal offset, but its vertical position never changes.
            Rect tableHeaderRect = GUILayoutUtility.GetRect(0f, PartsHeaderHeight,
                GUILayout.ExpandWidth(true), GUILayout.Height(PartsHeaderHeight));

            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(GUILayout.MinWidth(GetTableWidth()));

            if (rows.Count == 0)
            {
                GUILayout.Label("No matching parts.");
            }
            else
            {
                if (selectedIndex >= rows.Count) selectedIndex = rows.Count - 1;
                if (selectedIndex < 0) selectedIndex = 0;

                for (int i = 0; i < rows.Count; i++)
                {
                    PartRecord p = rows[i];
                    GUILayout.BeginHorizontal();
                    Rect rowStartRect = DrawComparisonToggle(p);
                    DrawPartThumbnail(p);
                    GUIStyle nameStyle = i == selectedIndex ? selectedWrappedNameStyle : wrappedNameStyle;
                    if (GUILayout.Button(p.Part, nameStyle, GUILayout.Width(250f), GUILayout.Height(48f)))
                        selectedIndex = i;

                    DrawTableValue(FormatNumber(p.Mass, "0.###"), 58f);
                    if (ShouldShowSortDataColumn())
                        DrawTableValue(GetSortDisplayValue(p), 78f);

                    foreach (ExtraColumn column in extraColumns.OrderBy(c => (int)c))
                        DrawTableValue(GetExtraColumnValue(p, column), GetExtraColumnWidth(column));
                    GUILayout.EndHorizontal();

                    Event rowEvent = Event.current;
                    Rect rowRect = new Rect(rowStartRect.xMin, rowStartRect.yMin, GetTableWidth(), ThumbnailSize);
                    if (rowEvent != null && rowEvent.type == EventType.MouseDown && rowEvent.button == 1 &&
                        rowRect.Contains(rowEvent.mousePosition))
                    {
                        partContextMenuPart = p;
                        partContextMenuScreenPosition = GUIUtility.GUIToScreenPoint(rowEvent.mousePosition);
                        showPartContextMenu = true;
                        showModSelector = false;
                        showCategorySelector = false;
                        rowEvent.Use();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            DrawPinnedTableHeader(tableHeaderRect);

            GUILayout.EndVertical();
            GUILayout.Label(rows.Count.ToString(CultureInfo.InvariantCulture) + " shown • " + comparisonPartKeys.Count.ToString(CultureInfo.InvariantCulture) + " selected for comparison", smallStyle);
            GUILayout.EndVertical();
        }

        private Rect DrawComparisonToggle(PartRecord part)
        {
            Rect cellRect = GUILayoutUtility.GetRect(CompareToggleColumnWidth, ThumbnailSize,
                GUILayout.Width(CompareToggleColumnWidth), GUILayout.Height(ThumbnailSize));
            const float toggleSize = 20f;
            Rect toggleRect = new Rect(
                cellRect.x , //+ 2f,
                cellRect.y + (cellRect.height - toggleSize) * 0.5f,
                toggleSize,
                toggleSize);

            bool selected = IsSelectedForComparison(part);
            string partName = part != null && !string.IsNullOrEmpty(part.Part) ? part.Part : "part";
            string tooltip = selected
                ? "Remove " + partName + " from comparison"
                : "Select " + partName + " for comparison";
            bool newSelected = GUI.Toggle(toggleRect, selected, new GUIContent(string.Empty, tooltip));
            if (newSelected != selected)
                SetComparisonSelected(part, newSelected);

            return cellRect;
        }

        private static string GetComparisonPartKey(PartRecord part)
        {
            if (part == null)
                return string.Empty;
            if (!string.IsNullOrWhiteSpace(part.Path))
                return "path:" + part.Path.Trim();
            if (!string.IsNullOrWhiteSpace(part.Id))
                return "id:" + part.Id.Trim();
            return "name:" + (part.ModName ?? string.Empty) + "|" + (part.Part ?? string.Empty);
        }

        private bool IsSelectedForComparison(PartRecord part)
        {
            string key = GetComparisonPartKey(part);
            return !string.IsNullOrEmpty(key) && comparisonPartKeys.Any(existing =>
                string.Equals(existing, key, StringComparison.OrdinalIgnoreCase));
        }

        private void SetComparisonSelected(PartRecord part, bool selected)
        {
            string key = GetComparisonPartKey(part);
            if (string.IsNullOrEmpty(key))
                return;

            int existingIndex = comparisonPartKeys.FindIndex(existing =>
                string.Equals(existing, key, StringComparison.OrdinalIgnoreCase));
            if (selected)
            {
                if (existingIndex < 0)
                    comparisonPartKeys.Add(key);
            }
            else if (existingIndex >= 0)
            {
                comparisonPartKeys.RemoveAt(existingIndex);
            }
        }

        private List<PartRecord> GetComparisonParts()
        {
            HashSet<string> filteredKeys = null;
            if (filteredOnly)
            {
                filteredKeys = new HashSet<string>(
                    GetFilteredSorted()
                        .Where(part => part != null)
                        .Select(GetComparisonPartKey)
                        .Where(key => !string.IsNullOrEmpty(key)),
                    StringComparer.OrdinalIgnoreCase);
            }

            var result = new List<PartRecord>();
            foreach (string key in comparisonPartKeys)
            {
                if (filteredKeys != null && !filteredKeys.Contains(key))
                    continue;

                PartRecord part = activeParts.FirstOrDefault(candidate =>
                    string.Equals(GetComparisonPartKey(candidate), key, StringComparison.OrdinalIgnoreCase));
                if (part != null)
                    result.Add(part);
            }
            return result;
        }

        private void PruneComparisonSelection()
        {
            var validKeys = new HashSet<string>(
                activeParts.Where(part => part != null).Select(GetComparisonPartKey).Where(key => !string.IsNullOrEmpty(key)),
                StringComparer.OrdinalIgnoreCase);
            comparisonPartKeys.RemoveAll(key => !validKeys.Contains(key));
        }

        private void DrawPartThumbnail(PartRecord part)
        {
            Rect imageRect = GUILayoutUtility.GetRect(ThumbnailColumnWidth, ThumbnailSize,
                GUILayout.Width(ThumbnailColumnWidth), GUILayout.Height(ThumbnailSize));

            Texture normalTexture = null;
            Texture2D texture;
            if (part != null && !string.IsNullOrEmpty(part.Part) &&
                partTextures.TryGetValue(part.Part, out texture) && texture != null)
            {
                normalTexture = texture;
            }

            AvailablePart availablePartForImage = null;
            if (normalTexture == null && part != null)
            {
                availablePartForImage = FindAvailablePart(part);
                normalTexture = GetOrCreateLivePartThumbnail(availablePartForImage);
            }

            bool hovered = imageRect.Contains(Event.current.mousePosition);
            if (hovered && part != null)
            {
                AvailablePart availablePart = availablePartForImage ?? FindAvailablePart(part);
                if (availablePart != null && EnsureRotatingPreview(availablePart))
                {
                    if (Event.current.type == EventType.Repaint)
                        RenderRotatingPreview();

                    if (rotatingPreviewTexture != null)
                    {
                        Vector2 screenPoint = GUIUtility.GUIToScreenPoint(
                            new Vector2(imageRect.xMin, imageRect.yMin));

                        hoveredPreviewActive = true;
                        hoveredPreviewTexture = rotatingPreviewTexture;
                        hoveredPreviewSourceRect = new Rect(
                            screenPoint.x,
                            screenPoint.y,
                            imageRect.width,
                            imageRect.height);
                    }
                }
            }

            // Keep the list thumbnail static.  The rotating model is shown only
            // in the enlarged hover popup.
            if (Event.current.type == EventType.Repaint && normalTexture != null)
                GUI.DrawTexture(imageRect, normalTexture, ScaleMode.ScaleToFit, true);
        }


        private void DrawHoveredPreview()
        {
            if (!hoveredPreviewActive || hoveredPreviewTexture == null)
                return;

            float outerSize = HoverPreviewDisplaySize + HoverPreviewPadding * 2f;

            // hoveredPreviewSourceRect is stored in screen coordinates. Convert
            // back to this window's local coordinates before drawing the popup.
            float sourceXMin = hoveredPreviewSourceRect.xMin - windowRect.x;
            float sourceXMax = hoveredPreviewSourceRect.xMax - windowRect.x;
            float sourceYMin = hoveredPreviewSourceRect.yMin - windowRect.y;

            float x = sourceXMax + HoverPreviewOffset;
            float y = sourceYMin;

            // Keep the popup fully inside the current window. Prefer the right
            // side of the thumbnail, and flip to the left when needed.
            float localWindowWidth = windowRect.width;
            float localWindowHeight = windowRect.height;
            if (x + outerSize > localWindowWidth - 4f)
                x = sourceXMin - HoverPreviewOffset - outerSize;
            x = Mathf.Clamp(x, 4f, Mathf.Max(4f, localWindowWidth - outerSize - 4f));
            y = Mathf.Clamp(y, 24f, Mathf.Max(24f, localWindowHeight - outerSize - 4f));

            Rect outerRect = new Rect(x, y, outerSize, outerSize);
            Rect innerRect = new Rect(
                outerRect.x + HoverPreviewPadding,
                outerRect.y + HoverPreviewPadding,
                HoverPreviewDisplaySize,
                HoverPreviewDisplaySize);

            GUI.Box(outerRect, GUIContent.none);
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(innerRect, hoveredPreviewTexture, ScaleMode.ScaleToFit, true);
        }
        private bool EnsureRotatingPreview(AvailablePart availablePart)
        {
            if (availablePart == null)
                return false;

            string key = !string.IsNullOrEmpty(availablePart.partUrl)
                ? availablePart.partUrl
                : availablePart.name;

            if (rotatingPreviewRoot != null && rotatingPreviewTexture != null &&
                string.Equals(rotatingPreviewPartKey, key, StringComparison.Ordinal))
                return true;

            DestroyRotatingPreview();

            GameObject sourceObject = availablePart.iconPrefab != null
                ? availablePart.iconPrefab
                : (availablePart.partPrefab != null ? availablePart.partPrefab.gameObject : null);
            if (sourceObject == null)
                return false;

            try
            {
                rotatingPreviewPartKey = key;
                rotatingPreviewAngle = 0f;

                rotatingPreviewRoot = new GameObject("PartExplorer_RotatingPreviewRoot");
                rotatingPreviewRoot.hideFlags = HideFlags.HideAndDontSave;
                rotatingPreviewRoot.transform.position = PreviewSceneOrigin;
                rotatingPreviewRoot.layer = PreviewLayer;

                rotatingPreviewPart = (GameObject)UnityEngine.Object.Instantiate(sourceObject);
                rotatingPreviewPart.name = "PartExplorer_RotatingPreviewPart";
                rotatingPreviewPart.hideFlags = HideFlags.HideAndDontSave;
                rotatingPreviewPart.transform.SetParent(rotatingPreviewRoot.transform, false);
                rotatingPreviewPart.transform.localPosition = Vector3.zero;
                rotatingPreviewPart.transform.localRotation = Quaternion.identity;
                SetLayerRecursively(rotatingPreviewPart, PreviewLayer);
                rotatingPreviewPart.SetActive(true);
                ApplyFirstPartVariant(availablePart, rotatingPreviewPart);

                // A preview clone only needs its renderers.  Disable gameplay code
                // and collisions so creating the image cannot affect the editor.
                foreach (MonoBehaviour behaviour in rotatingPreviewPart.GetComponentsInChildren<MonoBehaviour>(true))
                    behaviour.enabled = false;
                foreach (Collider collider in rotatingPreviewPart.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;

                Renderer[] renderers = rotatingPreviewPart.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    DestroyRotatingPreview();
                    return false;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                // Center the actual rendered geometry on the rotation pivot.
                Vector3 offset = rotatingPreviewRoot.transform.position - bounds.center;
                rotatingPreviewPart.transform.position += offset;

                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
                radius = Mathf.Max(radius, 0.1f);

                rotatingPreviewTexture = new RenderTexture(PreviewTextureSize, PreviewTextureSize, 24, RenderTextureFormat.ARGB32);
                rotatingPreviewTexture.name = "PartExplorer_RotatingPreviewTexture";
                rotatingPreviewTexture.hideFlags = HideFlags.HideAndDontSave;
                rotatingPreviewTexture.wrapMode = TextureWrapMode.Clamp;
                rotatingPreviewTexture.Create();

                GameObject cameraObject = new GameObject("PartExplorer_RotatingPreviewCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.layer = PreviewLayer;
                rotatingPreviewCamera = cameraObject.AddComponent<Camera>();
                rotatingPreviewCamera.enabled = false;
                rotatingPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
                rotatingPreviewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                rotatingPreviewCamera.cullingMask = 1 << PreviewLayer;
                rotatingPreviewCamera.orthographic = true;
                rotatingPreviewCamera.orthographicSize = radius * 1.35f;
                rotatingPreviewCamera.aspect = 1f;
                rotatingPreviewCamera.nearClipPlane = 0.01f;
                rotatingPreviewCamera.farClipPlane = Mathf.Max(100f, radius * 20f);
                rotatingPreviewCamera.targetTexture = rotatingPreviewTexture;

                Vector3 center = rotatingPreviewRoot.transform.position;
                float cameraDistance = Mathf.Max(2f, radius * 6f);
                rotatingPreviewCamera.transform.position = center + new Vector3(0f, 0f, -cameraDistance);
                rotatingPreviewCamera.transform.LookAt(center, Vector3.up);

                rotatingPreviewKeyLight = CreatePreviewLight(
                    "PartExplorer_RotatingPreviewKeyLight",
                    new Vector3(35f, -35f, 0f), 1.15f);
                rotatingPreviewFillLight = CreatePreviewLight(
                    "PartExplorer_RotatingPreviewFillLight",
                    new Vector3(330f, 145f, 0f), 0.55f);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to create rotating part preview: " + ex.Message);
                DestroyRotatingPreview();
                return false;
            }
        }

        private Light CreatePreviewLight(string name, Vector3 eulerAngles, float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.layer = PreviewLayer;
            lightObject.transform.rotation = Quaternion.Euler(eulerAngles);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << PreviewLayer;
            return light;
        }

        private void RenderRotatingPreview()
        {
            if (rotatingPreviewRoot == null || rotatingPreviewCamera == null || rotatingPreviewTexture == null)
                return;

            rotatingPreviewAngle = Mathf.Repeat(
                rotatingPreviewAngle + PreviewDegreesPerSecond * Time.unscaledDeltaTime, 360f);
            rotatingPreviewRoot.transform.rotation = Quaternion.Euler(20f, rotatingPreviewAngle, 0f);
            rotatingPreviewCamera.Render();
        }

        private void DestroyRotatingPreview()
        {
            rotatingPreviewPartKey = null;

            // Destroy() is deferred until the end of the frame.  Disable the old
            // preview immediately so switching directly from one hovered part to
            // another cannot render both models/lights for a frame.
            if (rotatingPreviewRoot != null)
                rotatingPreviewRoot.SetActive(false);
            if (rotatingPreviewKeyLight != null)
                rotatingPreviewKeyLight.enabled = false;
            if (rotatingPreviewFillLight != null)
                rotatingPreviewFillLight.enabled = false;
            if (rotatingPreviewCamera != null)
            {
                rotatingPreviewCamera.enabled = false;
                rotatingPreviewCamera.targetTexture = null;
            }

            if (rotatingPreviewTexture != null)
            {
                if (rotatingPreviewTexture.IsCreated())
                    rotatingPreviewTexture.Release();
                Destroy(rotatingPreviewTexture);
                rotatingPreviewTexture = null;
            }

            if (rotatingPreviewKeyLight != null)
                Destroy(rotatingPreviewKeyLight.gameObject);
            if (rotatingPreviewFillLight != null)
                Destroy(rotatingPreviewFillLight.gameObject);
            if (rotatingPreviewCamera != null)
                Destroy(rotatingPreviewCamera.gameObject);
            if (rotatingPreviewRoot != null)
                Destroy(rotatingPreviewRoot);

            rotatingPreviewKeyLight = null;
            rotatingPreviewFillLight = null;
            rotatingPreviewCamera = null;
            rotatingPreviewRoot = null;
            rotatingPreviewPart = null;
        }

        private static void ApplyFirstPartVariant(AvailablePart availablePart, GameObject partObject)
        {
            if (availablePart == null || partObject == null || availablePart.partConfig == null)
                return;

            try
            {
                ConfigNode variantModule = availablePart.partConfig.GetNodes("MODULE")
                    .FirstOrDefault(node => string.Equals(node.GetValue("name"), "ModulePartVariants", StringComparison.Ordinal));
                if (variantModule == null)
                    return;

                ConfigNode[] variants = variantModule.GetNodes("VARIANT");
                if (variants == null || variants.Length == 0)
                    return;

                // Deliberately use only the first VARIANT.  We do not call
                // ModulePartVariants.SetVariant() here because that method applies
                // every TEXTURE entry in the variant.  For thumbnails/previews the
                // requested behavior is to use only the first texture entry.
                ConfigNode firstVariant = variants[0];

                ConfigNode gameObjects = firstVariant.GetNode("GAMEOBJECTS");
                if (gameObjects != null)
                {
                    foreach (ConfigNode.Value value in gameObjects.values)
                    {
                        bool active;
                        if (bool.TryParse(value.value, out active))
                            SetNamedTransformsActive(partObject.transform, value.name, active);
                    }
                }

                ConfigNode[] textures = firstVariant.GetNodes("TEXTURE");
                if (textures != null && textures.Length > 0)
                    ApplyFirstVariantTexture(partObject, textures[0]);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to apply first part variant for preview: " + ex.Message);
            }
        }

        private static void SetNamedTransformsActive(Transform root, string transformName, bool active)
        {
            if (root == null || string.IsNullOrEmpty(transformName))
                return;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (transform != null && string.Equals(transform.name, transformName, StringComparison.Ordinal))
                    transform.gameObject.SetActive(active);
            }
        }

        private static void ApplyFirstVariantTexture(GameObject partObject, ConfigNode textureNode)
        {
            if (partObject == null || textureNode == null)
                return;

            string materialName = textureNode.GetValue("materialName") ?? string.Empty;
            string textureUrl = textureNode.GetValue("_MainTex");
            if (string.IsNullOrEmpty(textureUrl))
                textureUrl = textureNode.GetValue("mainTextureURL");
            if (string.IsNullOrEmpty(textureUrl))
                return;

            Texture2D texture = GameDatabase.Instance.GetTexture(textureUrl, false);
            if (texture == null)
                return;

            Renderer[] renderers = partObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.materials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                        continue;

                    if (!string.IsNullOrEmpty(materialName) &&
                        material.name.IndexOf(materialName, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (material.HasProperty("_MainTex"))
                    {
                        material.mainTexture = texture;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.materials = materials;
            }
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (gameObject == null)
                return;

            gameObject.layer = layer;
            Transform transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        private Texture2D GetOrCreateLivePartThumbnail(AvailablePart availablePart)
        {
            if (availablePart == null)
                return null;

            string key = !string.IsNullOrEmpty(availablePart.partUrl)
                ? availablePart.partUrl
                : availablePart.name;
            if (string.IsNullOrEmpty(key))
                return null;

            Texture2D cached;
            if (livePartTextures.TryGetValue(key, out cached))
                return cached;

            Texture2D generated = CreateLivePartThumbnail(availablePart);
            livePartTextures[key] = generated;
            return generated;
        }

        private Texture2D CreateLivePartThumbnail(AvailablePart availablePart)
        {
            string textureKey = !string.IsNullOrEmpty(availablePart.partUrl)
                ? availablePart.partUrl
                : availablePart.name;

            GameObject sourceObject = availablePart.iconPrefab != null
                ? availablePart.iconPrefab
                : (availablePart.partPrefab != null ? availablePart.partPrefab.gameObject : null);
            if (sourceObject == null)
                return null;

            GameObject root = null;
            GameObject partObject = null;
            GameObject cameraObject = null;
            GameObject keyLightObject = null;
            GameObject fillLightObject = null;
            RenderTexture renderTexture = null;
            RenderTexture oldActive = RenderTexture.active;

            try
            {
                Vector3 origin = PreviewSceneOrigin + new Vector3(0f, 5000f, 0f);
                root = new GameObject("PartExplorer_StaticThumbnailRoot");
                root.hideFlags = HideFlags.HideAndDontSave;
                root.transform.position = origin;
                root.layer = PreviewLayer;

                partObject = (GameObject)UnityEngine.Object.Instantiate(sourceObject);
                partObject.hideFlags = HideFlags.HideAndDontSave;
                partObject.transform.SetParent(root.transform, false);
                partObject.transform.localPosition = Vector3.zero;
                partObject.transform.localRotation = Quaternion.Euler(20f, 25f, 0f);
                SetLayerRecursively(partObject, PreviewLayer);
                partObject.SetActive(true);
                ApplyFirstPartVariant(availablePart, partObject);

                foreach (MonoBehaviour behaviour in partObject.GetComponentsInChildren<MonoBehaviour>(true))
                    behaviour.enabled = false;
                foreach (Collider collider in partObject.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;

                Renderer[] renderers = partObject.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                    return null;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                partObject.transform.position += root.transform.position - bounds.center;

                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
                radius = Mathf.Max(radius, 0.1f);

                renderTexture = new RenderTexture(LiveThumbnailTextureSize, LiveThumbnailTextureSize, 24, RenderTextureFormat.ARGB32);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                renderTexture.Create();

                cameraObject = new GameObject("PartExplorer_StaticThumbnailCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.cullingMask = 1 << PreviewLayer;
                camera.orthographic = true;
                camera.orthographicSize = radius * 1.35f;
                camera.aspect = 1f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = Mathf.Max(100f, radius * 20f);
                camera.targetTexture = renderTexture;

                float distance = Mathf.Max(2f, radius * 6f);
                camera.transform.position = root.transform.position + new Vector3(0f, 0f, -distance);
                camera.transform.LookAt(root.transform.position, Vector3.up);

                keyLightObject = new GameObject("PartExplorer_StaticThumbnailKeyLight");
                keyLightObject.hideFlags = HideFlags.HideAndDontSave;
                Light keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 1.15f;
                keyLight.cullingMask = 1 << PreviewLayer;
                keyLightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

                fillLightObject = new GameObject("PartExplorer_StaticThumbnailFillLight");
                fillLightObject.hideFlags = HideFlags.HideAndDontSave;
                Light fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.intensity = 0.55f;
                fillLight.cullingMask = 1 << PreviewLayer;
                fillLightObject.transform.rotation = Quaternion.Euler(330f, 145f, 0f);

                camera.Render();
                RenderTexture.active = renderTexture;
                Texture2D result = new Texture2D(LiveThumbnailTextureSize, LiveThumbnailTextureSize, TextureFormat.ARGB32, false);
                result.name = "PartExplorer_LiveThumbnail_" + textureKey;
                result.ReadPixels(new Rect(0, 0, LiveThumbnailTextureSize, LiveThumbnailTextureSize), 0, 0, false);
                result.Apply(false, false);
                result.wrapMode = TextureWrapMode.Clamp;
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to create live part thumbnail: " + ex.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = oldActive;
                if (partObject != null) partObject.SetActive(false);
                if (root != null) root.SetActive(false);
                if (renderTexture != null)
                {
                    if (renderTexture.IsCreated()) renderTexture.Release();
                    Destroy(renderTexture);
                }
                if (cameraObject != null) Destroy(cameraObject);
                if (keyLightObject != null) Destroy(keyLightObject);
                if (fillLightObject != null) Destroy(fillLightObject);
                if (root != null) Destroy(root);
            }
        }

        private void DestroyLivePartTextures()
        {
            foreach (Texture2D texture in livePartTextures.Values)
                if (texture != null)
                    Destroy(texture);
            livePartTextures.Clear();
        }

        private void LoadWindowIcon()
        {
            DestroyWindowIcon();

            string path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "PartExplorer", "PluginData", "part_explorer_icon_large.png");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[PartExplorer] Window icon not found: " + path);
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture.name = "PartExplorer_WindowIcon";
                if (texture.LoadImage(bytes))
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                    windowIconTexture = texture;
                }
                else
                {
                    Destroy(texture);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to load window icon " + path + ": " + ex.Message);
            }
        }

        private void DestroyWindowIcon()
        {
            if (windowIconTexture != null)
            {
                Destroy(windowIconTexture);
                windowIconTexture = null;
            }
        }

        private void LoadPartTextures()
        {
            DestroyPartTextures();

            string imageDirectory = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "PartExplorer", "PluginData", "Images");
            if (!Directory.Exists(imageDirectory))
            {
                Debug.LogWarning("[PartExplorer] Image directory not found: " + imageDirectory);
                return;
            }

            foreach (KeyValuePair<string, string> entry in PartImageFiles)
            {
                string path = Path.Combine(imageDirectory, entry.Value);
                if (!File.Exists(path))
                    continue;

                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                    texture.name = "PartExplorer_" + entry.Value;
                    if (texture.LoadImage(bytes))
                    {
                        texture.wrapMode = TextureWrapMode.Clamp;
                        partTextures[entry.Key] = texture;
                    }
                    else
                    {
                        Destroy(texture);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[PartExplorer] Unable to load image " + path + ": " + ex.Message);
                }
            }
        }

        private void DestroyPartTextures()
        {
            foreach (Texture2D texture in partTextures.Values)
                if (texture != null)
                    Destroy(texture);
            partTextures.Clear();
        }

        private static readonly Dictionary<string, string> PartImageFiles = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "MS-1 Multispectral Scanner", "MS-1_Multispectral_Scanner.png" },
            { "MS-R Enhanced Multispectral Scanner", "MS-R_Enhanced_Multispectral_Scanner.png" },
            { "R-3B Radar Altimeter", "R-3B_Radar_Altimeter.png" },
            { "VS-1 High Resolution Imager", "VS-1_High_Resolution_Imager.png" },
            { "R-EO-1 Radar Antenna", "R-EO-1_Radar_Antenna.png" },
            { "SAR-X Antenna", "SAR-X_Antenna.png" },
            { "SAR-C Antenna", "SAR-C_Antenna.png" },
            { "SCAN Been There Done That®", "SCAN_Been_There_Done_That.png" },
            { "VS-3 Advanced High Resolution Imager", "VS-3_Advanced_High_Resolution_Imager.png" },
            { "SCAN-R Resource Mapper", "SCAN-R_Resource_Mapper.png" },
            { "SCAN-R2 Advanced Resource Mapper", "SCAN-R2_Advanced_Resource_Mapper.png" },
            { "SCAN-RX Hyperspectral Resource Mapper", "SCAN-RX_Hyperspectral_Resource_Mapper.png" },
            { "MS-2A Advanced Multispectral Scanner", "MS-2A_Advanced_Multispectral_Scanner.png" },
            { "VS-11 Classified Reconnaissance Imager", "VS-11_Classified_Reconnaissance_Imager.png" },
            { "SAR-L Antenna", "SAR-L_Antenna.png" },
            { "M700 Survey Scanner", "M700_Survey_Scanner.png" },
            { "M4435 Narrow-Band Scanner", "M4435_Narrow-Band_Scanner.png" }
        };

        private void DrawPinnedTableHeader(Rect viewportRect)
        {
            if (viewportRect.width <= 0f || viewportRect.height <= 0f)
                return;

            // Draw in a clipped group and offset by the body scroll position.
            // This keeps the headings horizontally synchronized with the rows while
            // leaving them fixed vertically above the scrolling body.
            GUI.BeginGroup(viewportRect);
            try
            {
                float x = -listScroll.x;
                float y = 0f;

                // The selection-toggle and thumbnail columns intentionally have no
                // headings, but they still consume their exact row widths.
                x += CompareToggleColumnWidth;
                x += ThumbnailColumnWidth;

                DrawPinnedHeaderSortButton(ref x, y, "Name", SortColumn.Part, 250f);
                DrawPinnedHeaderSortButton(ref x, y, "Mass", SortColumn.Mass, 58f);
                if (ShouldShowSortDataColumn())
                    DrawPinnedHeaderSortButton(ref x, y, GetSortColumnCaption(), sortColumn, 78f);

                foreach (ExtraColumn column in extraColumns.OrderBy(c => (int)c))
                    DrawPinnedHeaderSortButton(ref x, y, GetExtraColumnCaption(column),
                        ToSortColumn(column), GetExtraColumnWidth(column));
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawPinnedHeaderSortButton(ref float x, float y, string caption, SortColumn column, float width)
        {
            string suffix = sortColumn == column ? (sortAscending ? " ▲" : " ▼") : string.Empty;
            if (GUI.Button(new Rect(x, y, width, 24f), caption + suffix, GUI.skin.button))
            {
                if (sortColumn == column)
                    sortAscending = !sortAscending;
                else
                {
                    sortColumn = column;
                    sortAscending = true;
                }

                InvalidateFilteredPartsFrameCache();
            }

            x += width;
        }

        private void DrawTableValue(string value, float width)
        {
            GUILayout.Label(string.IsNullOrEmpty(value) ? "—" : value, rightStyle, GUILayout.Width(width), GUILayout.Height(48f));
        }

        private float GetTableWidth()
        {
            float width = CompareToggleColumnWidth + ThumbnailColumnWidth + 250f + 58f + 24f;
            if (ShouldShowSortDataColumn())
                width += 78f;
            foreach (ExtraColumn column in extraColumns)
                width += GetExtraColumnWidth(column);
            return width;
        }

        private string GetSortColumnCaption()
        {
            switch (sortColumn)
            {
                case SortColumn.Cost: return "Cost";
                case SortColumn.Mass: return "Mass";
                case SortColumn.Ec: return "EC/s";
                case SortColumn.Science: return "Science";
                case SortColumn.Id: return "ID";
                case SortColumn.Fov: return "FOV";
                case SortColumn.Daylight: return "Daylight";
                case SortColumn.MinAltitude: return "Min Alt";
                case SortColumn.OptimalAltitude: return "Opt Alt";
                case SortColumn.MaxAltitude: return "Max Alt";
                case SortColumn.MaxTemp: return "Max Temp";
                case SortColumn.ImpactTolerance: return "Impact";
                case SortColumn.GTolerance: return "G Tol";
                case SortColumn.Biome: return "Biome";
                case SortColumn.Altimetry: return "Altimetry";
                case SortColumn.Visual: return "Visual";
                case SortColumn.Resource: return "Resource";
                case SortColumn.Anomaly: return "Anomaly";
                default: return "Part";
            }
        }

        private string GetSortDisplayValue(PartRecord p)
        {
            switch (sortColumn)
            {
                case SortColumn.Cost: return FormatNumber(p.Cost, "0");
                case SortColumn.Mass: return FormatNumber(p.Mass, "0.###");
                case SortColumn.Ec: return HasScanData(p) ? FormatNumber(p.ScanSat.EcPerSec, "0.0#") : string.Empty;
                case SortColumn.Science: return HasScanData(p) ? FormatNumber(p.ScanSat.Science, "0.##") : string.Empty;
                case SortColumn.Id: return p.Id;
                case SortColumn.Fov: return HasScanData(p) ? FormatNumber(p.ScanSat.Fov, "0.##") + "°" : string.Empty;
                case SortColumn.Daylight: return HasScanData(p) ? (p.ScanSat.RequiresDaylight ? "Yes" : "No") : string.Empty;
                case SortColumn.MinAltitude: return HasScanData(p) ? p.ScanSat.MinAltitudeText : string.Empty;
                case SortColumn.OptimalAltitude: return HasScanData(p) ? p.ScanSat.OptimalAltitudeText : string.Empty;
                case SortColumn.MaxAltitude: return HasScanData(p) ? p.ScanSat.MaxAltitudeText : string.Empty;
                case SortColumn.MaxTemp: return FormatNumber(p.MaxTemp, "0");
                case SortColumn.ImpactTolerance: return FormatNumber(p.ToleranceMs, "0.#");
                case SortColumn.GTolerance: return FormatNumber(p.ToleranceG, "0.#");
                case SortColumn.Biome: return HasScanData(p) ? p.ScanSat.Biome : string.Empty;
                case SortColumn.Altimetry: return HasScanData(p) ? p.ScanSat.Altimetry : string.Empty;
                case SortColumn.Visual: return HasScanData(p) ? p.ScanSat.Visual : string.Empty;
                case SortColumn.Resource: return HasScanData(p) ? p.ScanSat.Resource : string.Empty;
                case SortColumn.Anomaly: return HasScanData(p) ? p.ScanSat.Anomaly : string.Empty;
                default: return p.Part;
            }
        }

        private bool ShouldShowSortDataColumn()
        {
            if (sortColumn == SortColumn.Part || sortColumn == SortColumn.Mass)
                return false;

            foreach (ExtraColumn column in extraColumns)
                if (ToSortColumn(column) == sortColumn)
                    return false;

            return true;
        }

        private static SortColumn ToSortColumn(ExtraColumn column)
        {
            switch (column)
            {
                case ExtraColumn.Id: return SortColumn.Id;
                case ExtraColumn.Cost: return SortColumn.Cost;
                case ExtraColumn.Ec: return SortColumn.Ec;
                case ExtraColumn.Science: return SortColumn.Science;
                case ExtraColumn.Fov: return SortColumn.Fov;
                case ExtraColumn.Daylight: return SortColumn.Daylight;
                case ExtraColumn.MinAltitude: return SortColumn.MinAltitude;
                case ExtraColumn.OptimalAltitude: return SortColumn.OptimalAltitude;
                case ExtraColumn.MaxAltitude: return SortColumn.MaxAltitude;
                case ExtraColumn.MaxTemp: return SortColumn.MaxTemp;
                case ExtraColumn.ImpactTolerance: return SortColumn.ImpactTolerance;
                case ExtraColumn.GTolerance: return SortColumn.GTolerance;
                case ExtraColumn.Biome: return SortColumn.Biome;
                case ExtraColumn.Altimetry: return SortColumn.Altimetry;
                case ExtraColumn.Visual: return SortColumn.Visual;
                case ExtraColumn.Resource: return SortColumn.Resource;
                case ExtraColumn.Anomaly: return SortColumn.Anomaly;
                default: return SortColumn.Part;
            }
        }

        private static double ParseSortableNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-")
                return double.MaxValue;

            double result;
            return double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                ? result
                : double.MaxValue;
        }

        private static string GetExtraColumnCaption(ExtraColumn column)
        {
            switch (column)
            {
                case ExtraColumn.Id: return "ID";
                case ExtraColumn.Cost: return "Cost";
                case ExtraColumn.Ec: return "EC/s";
                case ExtraColumn.Science: return "Science";
                case ExtraColumn.Fov: return "FOV";
                case ExtraColumn.Daylight: return "Daylight";
                case ExtraColumn.MinAltitude: return "Min Alt";
                case ExtraColumn.OptimalAltitude: return "Opt Alt";
                case ExtraColumn.MaxAltitude: return "Max Alt";
                case ExtraColumn.MaxTemp: return "Max Temp";
                case ExtraColumn.ImpactTolerance: return "Impact";
                case ExtraColumn.GTolerance: return "G Tol";
                case ExtraColumn.Biome: return "Biome";
                case ExtraColumn.Altimetry: return "Altimetry";
                case ExtraColumn.Visual: return "Visual";
                case ExtraColumn.Resource: return "Resource";
                case ExtraColumn.Anomaly: return "Anomaly";
                default: return column.ToString();
            }
        }

        private static float GetExtraColumnWidth(ExtraColumn column)
        {
            switch (column)
            {
                case ExtraColumn.Id: return 105f;
                case ExtraColumn.Altimetry:
                case ExtraColumn.Visual:
                case ExtraColumn.Resource: return 95f;
                case ExtraColumn.Biome:
                case ExtraColumn.Anomaly: return 75f;
                default: return 72f;
            }
        }

        private static string GetExtraColumnValue(PartRecord p, ExtraColumn column)
        {
            switch (column)
            {
                case ExtraColumn.Id: return p.Id;
                case ExtraColumn.Cost: return p.Cost.ToString("0", CultureInfo.InvariantCulture);
                case ExtraColumn.Ec: return HasScanData(p) ? p.ScanSat.EcPerSec.ToString("0.0#", CultureInfo.InvariantCulture) : string.Empty;
                case ExtraColumn.Science: return HasScanData(p) ? p.ScanSat.Science.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
                case ExtraColumn.Fov: return HasScanData(p) ? p.ScanSat.Fov.ToString("0.##", CultureInfo.InvariantCulture) + "°" : string.Empty;
                case ExtraColumn.Daylight: return HasScanData(p) ? (p.ScanSat.RequiresDaylight ? "Yes" : "No") : string.Empty;
                case ExtraColumn.MinAltitude: return HasScanData(p) ? p.ScanSat.MinAltitudeText : string.Empty;
                case ExtraColumn.OptimalAltitude: return HasScanData(p) ? p.ScanSat.OptimalAltitudeText : string.Empty;
                case ExtraColumn.MaxAltitude: return HasScanData(p) ? p.ScanSat.MaxAltitudeText : string.Empty;
                case ExtraColumn.MaxTemp: return p.MaxTemp.ToString("0", CultureInfo.InvariantCulture);
                case ExtraColumn.ImpactTolerance: return p.ToleranceMs.ToString("0.#", CultureInfo.InvariantCulture);
                case ExtraColumn.GTolerance: return p.ToleranceG.ToString("0.#", CultureInfo.InvariantCulture);
                case ExtraColumn.Biome: return HasScanData(p) ? p.ScanSat.Biome : string.Empty;
                case ExtraColumn.Altimetry: return HasScanData(p) ? p.ScanSat.Altimetry : string.Empty;
                case ExtraColumn.Visual: return HasScanData(p) ? p.ScanSat.Visual : string.Empty;
                case ExtraColumn.Resource: return HasScanData(p) ? p.ScanSat.Resource : string.Empty;
                case ExtraColumn.Anomaly: return HasScanData(p) ? p.ScanSat.Anomaly : string.Empty;
                default: return string.Empty;
            }
        }

        private static bool HasScanData(PartRecord p)
        {
            return p != null && p.ScanSat != null && p.ScanSat.HasScannerModules;
        }

        private void DrawModsTop()
        {
            int modCount = activeParts
                .Where(part => part != null && !string.IsNullOrEmpty(part.ModName))
                .Select(part => part.ModName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            GUILayout.Label("Installed Mods", titleStyle);
            GUILayout.Label(modCount.ToString(CultureInfo.InvariantCulture) + " mods • " +
                activeParts.Count.ToString(CultureInfo.InvariantCulture) +
                " loaded parts. Click a mod name to filter the Parts tab to that mod.", smallStyle);
            GUILayout.Space(6f);
        }

        private void DrawModsPage()
        {
            List<ModSummaryRow> rows = activeParts
                .Where(part => part != null && !string.IsNullOrEmpty(part.ModName))
                .GroupBy(part => part.ModName, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ModSummaryRow
                {
                    ModName = group.Key,
                    PartCount = group.Count(),
                    ScienceDataPartCount = group.Count(part => part.ScienceModules != null && part.ScienceModules.Count > 0)
                })
                .OrderBy(row => row.ModName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Mod", compareHeaderStyle, GUILayout.Width(330f), GUILayout.Height(26f));
                    GUILayout.Label("Parts", compareHeaderStyle, GUILayout.Width(85f), GUILayout.Height(26f));
                    GUILayout.Label("ScienceMods support", compareHeaderStyle, GUILayout.Width(190f), GUILayout.Height(26f));
                    GUILayout.FlexibleSpace();
                }

                modsScroll = GUILayout.BeginScrollView(modsScroll, GUILayout.ExpandHeight(true));
                foreach (ModSummaryRow row in rows)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(new GUIContent(row.ModName, "Filter PartExplorer to " + row.ModName),
                            rowStyle, GUILayout.Width(330f), GUILayout.Height(28f)))
                        {
                            selectedMod = row.ModName;
                            selectedCategory = string.Empty;
                            selectedIndex = 0;
                            listScroll = Vector2.zero;
                            InvalidateFilteredPartsFrameCache();
                            currentPage = WindowPage.Parts;
                            SaveSettings();
                        }

                        GUILayout.Label(row.PartCount.ToString(CultureInfo.InvariantCulture), compareCellStyle,
                            GUILayout.Width(85f), GUILayout.Height(28f));

                        string scienceStatus = row.ScienceDataPartCount > 0
                            ? "Detected (" + row.ScienceDataPartCount.ToString(CultureInfo.InvariantCulture) + " parts)"
                            : "None detected";
                        GUILayout.Label(scienceStatus, compareCellStyle, GUILayout.Width(190f), GUILayout.Height(28f));
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.EndScrollView();
            }
        }

        private void DrawSettingsTop()
        {
            GUILayout.Label("Settings", titleStyle);
            GUILayout.Label("Selected columns are added to the parts table. Click any visible column heading to sort by that column; click it again to reverse the sort order.", smallStyle);
            GUILayout.Space(6f);
        }
        private void DrawSettingsPage()
        {
            settingsScroll = GUILayout.BeginScrollView(settingsScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

            GUILayout.Label("Interface", sectionStyle);
            GUILayout.BeginHorizontal();
            bool newAlternateSkin = GUILayout.Toggle(useAlternateSkin, "Use alternate KSP skin", GUILayout.Width(205f));
            if (newAlternateSkin != useAlternateSkin)
            {
                useAlternateSkin = newAlternateSkin;
                SaveSettings();
            }

            bool newShowPartPath = GUILayout.Toggle(showPartPath, "Show part path in Details", GUILayout.Width(205f));
            if (newShowPartPath != showPartPath)
            {
                showPartPath = newShowPartPath;
                SaveSettings();
            }
            GUILayout.Space(205f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Details pane", sectionStyle);
            GUILayout.Label("Choose which general part fields are shown in the Part Information panel.", smallStyle);
            GUILayout.BeginHorizontal();
            DrawDetailFieldToggle(ref showDetailId, "ID");
            DrawDetailFieldToggle(ref showDetailCost, "Cost");
            DrawDetailFieldToggle(ref showDetailMass, "Mass");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawDetailFieldToggle(ref showDetailMaxTemperature, "Max temperature");
            DrawDetailFieldToggle(ref showDetailImpactTolerance, "Impact tolerance");
            DrawDetailFieldToggle(ref showDetailGTolerance, "G tolerance");
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Details panels", sectionStyle);
            GUILayout.Label("Choose which complete information panels are shown in Details.", smallStyle);
            DrawInformationSectionGrid(visibleDetailSections);

            GUILayout.Space(10f);
            GUILayout.Label("Compare rows", sectionStyle);
            GUILayout.Label("Choose which kinds of information are included in Compare. These choices are independent of Details-panel visibility.", smallStyle);
            DrawInformationSectionGrid(visibleCompareSections);

            GUILayout.Space(10f);
            GUILayout.Label("Additional data columns", sectionStyle);

            ExtraColumn[] columns = (ExtraColumn[])Enum.GetValues(typeof(ExtraColumn));
            for (int i = 0; i < columns.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                DrawColumnToggle(columns[i]);
                if (i + 1 < columns.Length)
                    DrawColumnToggle(columns[i + 1]);
                if (i + 2 < columns.Length)
                    DrawColumnToggle(columns[i + 2]);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8f);
            if (GUILayout.Button("Clear Additional Columns", GUILayout.Width(190f)))
            {
                extraColumns.Clear();
                SaveSettings();
            }
            GUILayout.EndScrollView();
        }

        private void DrawDetailFieldToggle(ref bool value, string caption)
        {
            bool newValue = GUILayout.Toggle(value, caption, GUILayout.Width(205f));
            if (newValue == value)
                return;

            value = newValue;
            SaveSettings();
        }

        private void DrawInformationSectionGrid(HashSet<InformationSection> sections)
        {
            InformationSection[] values = (InformationSection[])Enum.GetValues(typeof(InformationSection));
            for (int i = 0; i < values.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                DrawInformationSectionToggle(sections, values[i]);
                if (i + 1 < values.Length)
                    DrawInformationSectionToggle(sections, values[i + 1]);
                if (i + 2 < values.Length)
                    DrawInformationSectionToggle(sections, values[i + 2]);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawInformationSectionToggle(HashSet<InformationSection> sections, InformationSection section)
        {
            bool oldValue = sections.Contains(section);
            bool newValue = GUILayout.Toggle(oldValue, GetInformationSectionCaption(section), GUILayout.Width(205f));
            if (newValue == oldValue)
                return;

            if (newValue)
                sections.Add(section);
            else
                sections.Remove(section);
            SaveSettings();
        }

        private static string GetInformationSectionCaption(InformationSection section)
        {
            switch (section)
            {
                case InformationSection.PartInformation: return "Part Information";
                case InformationSection.CargoPartInfo: return "Cargo Part Info";
                case InformationSection.Command: return "Command";
                case InformationSection.DataTransmitter: return "Data Transmitter";
                case InformationSection.ProbeControlPoint: return "Probe Control Point";
                case InformationSection.ReactionWheel: return "Reaction Wheel";
                case InformationSection.Sas: return "SAS";
                case InformationSection.Resources: return "Resources";
                case InformationSection.ScienceModules: return "Science Modules";
                case InformationSection.ScanSat: return "SCANsat";
                case InformationSection.ScanTypes: return "Scan Types";
                case InformationSection.AltitudeRange: return "Altitude Range";
                default: return section.ToString();
            }
        }

        private static bool TryGetStockSectionKind(StockDetailSection section, out InformationSection kind)
        {
            string title = section == null ? string.Empty : (!string.IsNullOrEmpty(section.BaseTitle) ? section.BaseTitle : section.Title);
            switch (title)
            {
                case "Cargo Part Info": kind = InformationSection.CargoPartInfo; return true;
                case "Command": kind = InformationSection.Command; return true;
                case "Data Transmitter": kind = InformationSection.DataTransmitter; return true;
                case "Probe Control Point": kind = InformationSection.ProbeControlPoint; return true;
                case "Reaction Wheel": kind = InformationSection.ReactionWheel; return true;
                case "SAS": kind = InformationSection.Sas; return true;
                default:
                    kind = InformationSection.PartInformation;
                    return false;
            }
        }

        private void DrawColumnToggle(ExtraColumn column)
        {
            bool oldValue = extraColumns.Contains(column);
            bool newValue = GUILayout.Toggle(oldValue, GetExtraColumnCaption(column), GUILayout.Width(205f));
            if (newValue == oldValue)
                return;

            if (newValue) extraColumns.Add(column);
            else extraColumns.Remove(column);
            SaveSettings();
        }

        private void LoadSettings()
        {
            visibleDetailSections.Clear();
            visibleCompareSections.Clear();
            foreach (InformationSection section in Enum.GetValues(typeof(InformationSection)))
            {
                visibleDetailSections.Add(section);
                visibleCompareSections.Add(section);
            }

            try
            {
                configuration = KSP.IO.PluginConfiguration.CreateForType<PartExplorerAddon>();
                configuration.load();
                foreach (ExtraColumn column in Enum.GetValues(typeof(ExtraColumn)))
                    if (configuration.GetValue("column_" + column, false))
                        extraColumns.Add(column);

                useAlternateSkin = configuration.GetValue("useAlternateSkin", false);
                showPartPath = configuration.GetValue("showPartPath", false);
                showDetailId = configuration.GetValue("showDetailId", false);
                showDetailCost = configuration.GetValue("showDetailCost", true);
                showDetailMass = configuration.GetValue("showDetailMass", true);
                showDetailMaxTemperature = configuration.GetValue("showDetailMaxTemperature", true);
                showDetailImpactTolerance = configuration.GetValue("showDetailImpactTolerance", true);
                showDetailGTolerance = configuration.GetValue("showDetailGTolerance", true);
                biomeFilter = Mathf.Clamp(configuration.GetValue("filterBiome", 0), 0, 2);
                altimeterFilter = Mathf.Clamp(configuration.GetValue("filterAltimeter", 0), 0, 2);
                visualFilter = Mathf.Clamp(configuration.GetValue("filterVisual", 0), 0, 2);
                resourceFilter = Mathf.Clamp(configuration.GetValue("filterResource", 0), 0, 2);
                anomalyFilter = Mathf.Clamp(configuration.GetValue("filterAnomaly", 0), 0, 2);
                daylightFilter = Mathf.Clamp(configuration.GetValue("filterDaylight", 0), 0, 2);
                selectedMod = configuration.GetValue("selectedMod", string.Empty) ?? string.Empty;
                selectedCategory = configuration.GetValue("selectedCategory", string.Empty) ?? string.Empty;
                filteredOnly = configuration.GetValue("filteredOnly", configuration.GetValue("compareFilteredOnly", false));
                compareHighlightDifferences = configuration.GetValue("compareHighlightDifferences", true);
                compareDifferencesOnly = configuration.GetValue("compareDifferencesOnly", false);
                compareMarkLowHigh = configuration.GetValue("compareMarkLowHigh", false);
                compareShowDeltas = configuration.GetValue("compareShowDeltas", false);
                int savedCompareWidthMode = Mathf.Clamp(configuration.GetValue("compareColumnWidthMode", (int)CompareColumnWidthMode.Normal), 0, 2);
                compareColumnWidthMode = (CompareColumnWidthMode)savedCompareWidthMode;

                foreach (InformationSection section in Enum.GetValues(typeof(InformationSection)))
                {
                    if (!configuration.GetValue("detailsSection_" + section, true))
                        visibleDetailSections.Remove(section);
                    if (!configuration.GetValue("compareSection_" + section, true))
                        visibleCompareSections.Remove(section);
                }

                windowWidth = Mathf.Max(MinWindowWidth, configuration.GetValue("windowWidth", DefaultWindowRect.width));
                windowHeight = Mathf.Max(MinWindowHeight, configuration.GetValue("windowHeight", DefaultWindowRect.height));
                leftPaneWidth = Mathf.Max(MinLeftPaneWidth, configuration.GetValue("leftPaneWidth", 565f));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to load settings: " + ex.Message);
            }
        }

        private void SaveSettings()
        {
            if (configuration == null)
                return;
            try
            {
                foreach (ExtraColumn column in Enum.GetValues(typeof(ExtraColumn)))
                    configuration.SetValue("column_" + column, extraColumns.Contains(column));
                configuration.SetValue("useAlternateSkin", useAlternateSkin);
                configuration.SetValue("showPartPath", showPartPath);
                configuration.SetValue("showDetailId", showDetailId);
                configuration.SetValue("showDetailCost", showDetailCost);
                configuration.SetValue("showDetailMass", showDetailMass);
                configuration.SetValue("showDetailMaxTemperature", showDetailMaxTemperature);
                configuration.SetValue("showDetailImpactTolerance", showDetailImpactTolerance);
                configuration.SetValue("showDetailGTolerance", showDetailGTolerance);
                configuration.SetValue("filterBiome", biomeFilter);
                configuration.SetValue("filterAltimeter", altimeterFilter);
                configuration.SetValue("filterVisual", visualFilter);
                configuration.SetValue("filterResource", resourceFilter);
                configuration.SetValue("filterAnomaly", anomalyFilter);
                configuration.SetValue("filterDaylight", daylightFilter);
                configuration.SetValue("selectedMod", selectedMod ?? string.Empty);
                configuration.SetValue("selectedCategory", selectedCategory ?? string.Empty);
                configuration.SetValue("filteredOnly", filteredOnly);
                configuration.SetValue("compareHighlightDifferences", compareHighlightDifferences);
                configuration.SetValue("compareDifferencesOnly", compareDifferencesOnly);
                configuration.SetValue("compareMarkLowHigh", compareMarkLowHigh);
                configuration.SetValue("compareShowDeltas", compareShowDeltas);
                configuration.SetValue("compareColumnWidthMode", (int)compareColumnWidthMode);
                foreach (InformationSection section in Enum.GetValues(typeof(InformationSection)))
                {
                    configuration.SetValue("detailsSection_" + section, visibleDetailSections.Contains(section));
                    configuration.SetValue("compareSection_" + section, visibleCompareSections.Contains(section));
                }
                configuration.SetValue("windowWidth", windowWidth);
                configuration.SetValue("windowHeight", windowHeight);
                configuration.SetValue("leftPaneWidth", leftPaneWidth);
                configuration.save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to save settings: " + ex.Message);
            }
        }

        private void DrawCompareTop()
        {
            parts = GetComparisonParts();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Compare", sectionStyle);
                GUILayout.Space(12f);

                bool newHighlightDifferences = GUILayout.Toggle(
                    compareHighlightDifferences,
                    new GUIContent("Highlight differences", "Highlight comparison cells when the selected parts do not all have the same displayed value."),
                    GUILayout.Width(195f));
                if (newHighlightDifferences != compareHighlightDifferences)
                {
                    compareHighlightDifferences = newHighlightDifferences;
                    SaveSettings();
                }

                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && parts != null && parts.Count > 1;
                bool newDifferencesOnly = GUILayout.Toggle(
                    compareDifferencesOnly,
                    new GUIContent("Differences only", "Hide comparison rows whose displayed values are identical for all selected parts."),
                    GUILayout.Width(145f));
                if (newDifferencesOnly != compareDifferencesOnly)
                {
                    compareDifferencesOnly = newDifferencesOnly;
                    SaveSettings();
                }

                bool newMarkLowHigh = GUILayout.Toggle(
                    compareMarkLowHigh,
                    new GUIContent("Mark low/high", "For simple numeric rows with matching units, mark the lowest and highest values."),
                    GUILayout.Width(135f));
                if (newMarkLowHigh != compareMarkLowHigh)
                {
                    compareMarkLowHigh = newMarkLowHigh;
                    SaveSettings();
                }

                bool newShowDeltas = GUILayout.Toggle(
                    compareShowDeltas,
                    new GUIContent("Deltas vs first", "For simple numeric rows with matching units, show the numeric delta from the first selected part."),
                    GUILayout.Width(115f));
                GUI.enabled = oldEnabled;
                if (newShowDeltas != compareShowDeltas)
                {
                    compareShowDeltas = newShowDeltas;
                    SaveSettings();
                }

                GUILayout.FlexibleSpace();
                if (parts != null)
                {
                    string compareCountText = filteredOnly && parts.Count != comparisonPartKeys.Count
                        ? parts.Count.ToString(CultureInfo.InvariantCulture) + " compared • " + comparisonPartKeys.Count.ToString(CultureInfo.InvariantCulture) + " selected"
                        : parts.Count.ToString(CultureInfo.InvariantCulture) + " selected";
                    GUILayout.Label(compareCountText, smallStyle);
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Column width:", smallStyle, GUILayout.Width(82f));
                DrawCompareColumnWidthButton("Compact", CompareColumnWidthMode.Compact, 75f);
                DrawCompareColumnWidthButton("Normal", CompareColumnWidthMode.Normal, 75f);
                DrawCompareColumnWidthButton("Wide", CompareColumnWidthMode.Wide, 75f);
                GUILayout.FlexibleSpace();
            }

            if (parts != null && parts.Count == 0)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Space(12f);
                    if (filteredOnly && comparisonPartKeys.Count > 0)
                        GUILayout.Label("No selected parts match the current PartExplorer and editor filters. Turn off Filtered only to compare all selected parts.", descriptionStyle);
                    else
                        GUILayout.Label("Select parts with the checkboxes in the leftmost column of the Parts tab.", descriptionStyle);
                    GUILayout.FlexibleSpace();
                }
                return;
            }

            GUILayout.Label("In the VAB/SPH, click a part-name button below to add that part to the scene.", smallStyle);
            GUILayout.Space(3f);
        }

        private void DrawCompareColumnWidthButton(string caption, CompareColumnWidthMode mode, float width)
        {
            bool selected = compareColumnWidthMode == mode;
            if (GUILayout.Toggle(selected, caption, GUI.skin.button, GUILayout.Width(width)) && !selected)
            {
                compareColumnWidthMode = mode;
                SaveSettings();
            }
        }

        private float GetComparePartColumnWidth()
        {
            switch (compareColumnWidthMode)
            {
                case CompareColumnWidthMode.Compact: return ComparePartColumnWidthCompact;
                case CompareColumnWidthMode.Wide: return ComparePartColumnWidthWide;
                default: return ComparePartColumnWidthNormal;
            }
        }

        List<PartRecord> parts;

        private void DrawComparePage()
        {
            parts = GetComparisonParts();

            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                List<ComparisonRow> rows = BuildComparisonRows(parts);
                if (compareDifferencesOnly && parts.Count > 1)
                    rows = rows.Where(row => IsComparisonRowDifferent(row, parts)).ToList();

                float partColumnWidth = GetComparePartColumnWidth();
                float tableWidth = CompareLabelColumnWidth + partColumnWidth * parts.Count + CompareColumnSpacing * parts.Count;

                compareScroll = GUILayout.BeginScrollView(compareScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
                using (new GUILayout.VerticalScope(GUILayout.Width(tableWidth)))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        float headerHeight = CompareMinimumRowHeight;
                        float partButtonWidth = partColumnWidth - CompareRemoveButtonWidth - CompareHeaderSpacing;
                        foreach (PartRecord part in parts)
                            headerHeight = Mathf.Max(headerHeight,
                                comparePartButtonStyle.CalcHeight(new GUIContent(part.Part ?? string.Empty), partButtonWidth));
                        if (parts.Count > 0)
                        {
                            Rect detailHeaderRect = GUILayoutUtility.GetRect(
                                CompareLabelColumnWidth, headerHeight,
                                GUILayout.Width(CompareLabelColumnWidth), GUILayout.Height(headerHeight));
                            GUI.Label(detailHeaderRect, "Detail", compareHeaderStyle);
                            GUILayout.Space(CompareColumnSpacing);
                        }
                        for (int partIndex = 0; partIndex < parts.Count; partIndex++)
                        {
                            PartRecord part = parts[partIndex];
                            // Reserve exactly the same width as each data cell.  Drawing
                            // the two header buttons inside this fixed rect avoids GUIStyle
                            // margins accumulating from one nested GUILayout group to the next.
                            Rect partHeaderRect = GUILayoutUtility.GetRect(
                                partColumnWidth, headerHeight,
                                GUILayout.Width(partColumnWidth), GUILayout.Height(headerHeight));
                            Rect partButtonRect = new Rect(
                                partHeaderRect.x, partHeaderRect.y,
                                partButtonWidth, partHeaderRect.height);
                            Rect removeButtonRect = new Rect(
                                partHeaderRect.xMax - CompareRemoveButtonWidth, partHeaderRect.y,
                                CompareRemoveButtonWidth, partHeaderRect.height);

                            bool oldEnabled = GUI.enabled;
                            AvailablePart availablePart = FindAvailablePart(part);
                            bool canAdd = HighLogic.LoadedSceneIsEditor && availablePart != null && EditorLogic.fetch != null;
                            GUI.enabled = oldEnabled && canAdd;

                            string tooltip = canAdd
                                ? "Add " + (part.Part ?? "part") + " to the editor"
                                : "Part names can be added from the VAB or SPH";
                            if (GUI.Button(partButtonRect, new GUIContent(part.Part ?? string.Empty, tooltip), comparePartButtonStyle))
                                AddPartToEditor(part, availablePart);

                            GUI.enabled = oldEnabled;

                            string removeTooltip = "Remove " + (part.Part ?? "part") + " from comparison";
                            Color oldContentColor = GUI.contentColor;
                            GUI.contentColor = Color.red;
                            bool removePart = GUI.Button(removeButtonRect, new GUIContent("X", removeTooltip));
                            GUI.contentColor = oldContentColor;
                            if (removePart)
                                SetComparisonSelected(part, false);

                            if (partIndex < parts.Count - 1)
                                GUILayout.Space(CompareColumnSpacing);
                        }
                    }

                    foreach (ComparisonRow row in rows)
                    {
                        // The row's internal Section still distinguishes duplicate labels,
                        // but the visible description is intentionally just the detail label.
                        string rowCaption = row.Label;
                        List<string> displayValues = BuildComparisonDisplayValues(row, parts);
                        float rowHeight = Mathf.Max(CompareMinimumRowHeight,
                            compareLabelStyle.CalcHeight(new GUIContent(rowCaption), CompareLabelColumnWidth));

                        for (int partIndex = 0; partIndex < parts.Count; partIndex++)
                        {
                            rowHeight = Mathf.Max(rowHeight,
                                compareCellStyle.CalcHeight(new GUIContent(displayValues[partIndex]), partColumnWidth));
                        }

                        GUILayout.BeginHorizontal();
                        Rect rowLabelRect = GUILayoutUtility.GetRect(
                            CompareLabelColumnWidth, rowHeight,
                            GUILayout.Width(CompareLabelColumnWidth), GUILayout.Height(rowHeight));
                        GUI.Label(rowLabelRect, rowCaption, compareLabelStyle);
                        if (parts.Count > 0)
                            GUILayout.Space(CompareColumnSpacing);

                        bool rowDiffers = parts.Count > 1 && IsComparisonRowDifferent(row, parts);
                        for (int partIndex = 0; partIndex < parts.Count; partIndex++)
                        {
                            Rect cellRect = GUILayoutUtility.GetRect(
                                partColumnWidth, rowHeight,
                                GUILayout.Width(partColumnWidth), GUILayout.Height(rowHeight));

                            DrawComparisonCell(cellRect, displayValues[partIndex],
                                compareHighlightDifferences && rowDiffers);

                            if (partIndex < parts.Count - 1)
                                GUILayout.Space(CompareColumnSpacing);
                        }
                        GUILayout.EndHorizontal();
                    }

                }
                GUILayout.EndScrollView();
            }
        }

        private void DrawComparisonCell(Rect cellRect, string displayValue, bool highlighted)
        {
            if (!highlighted)
            {
                GUI.Label(cellRect, displayValue, compareCellStyle);
                return;
            }

            // Keep the normal box border, but fill the inside with an intentionally
            // bright yellow so the difference remains obvious with either KSP skin.
            GUI.Box(cellRect, GUIContent.none, compareCellStyle);
            Rect fillRect = new Rect(
                cellRect.x + 2f,
                cellRect.y + 2f,
                Mathf.Max(0f, cellRect.width - 4f),
                Mathf.Max(0f, cellRect.height - 4f));
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 0.05f, 1f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = oldColor;
            GUI.Label(cellRect, displayValue, compareHighlightedCellTextStyle);
        }

        private List<string> BuildComparisonDisplayValues(ComparisonRow row, IList<PartRecord> parts)
        {
            var result = new List<string>(parts.Count);
            for (int i = 0; i < parts.Count; i++)
                result.Add(GetComparisonDisplayValue(row, parts[i]));

            if ((!compareMarkLowHigh && !compareShowDeltas) || parts.Count < 2)
                return result;

            double[] numericValues;
            bool[] hasNumericValue;
            string unit;
            if (!TryGetComparableNumericValues(row, parts, out numericValues, out hasNumericValue, out unit))
                return result;

            double minimum = double.MaxValue;
            double maximum = double.MinValue;
            for (int i = 0; i < numericValues.Length; i++)
            {
                if (!hasNumericValue[i])
                    continue;
                minimum = Math.Min(minimum, numericValues[i]);
                maximum = Math.Max(maximum, numericValues[i]);
            }

            bool hasRange = !NearlyEqual(minimum, maximum);
            bool haveBaseline = hasNumericValue.Length > 0 && hasNumericValue[0];
            double baseline = haveBaseline ? numericValues[0] : 0d;

            for (int i = 0; i < result.Count; i++)
            {
                if (!hasNumericValue[i])
                    continue;

                if (compareMarkLowHigh && hasRange)
                {
                    if (NearlyEqual(numericValues[i], minimum))
                        result[i] += "\n▼ LOW";
                    if (NearlyEqual(numericValues[i], maximum))
                        result[i] += "\n▲ HIGH";
                }

                if (compareShowDeltas && haveBaseline)
                {
                    double delta = numericValues[i] - baseline;
                    result[i] += "\nΔ " + FormatComparisonDelta(delta, unit);
                }
            }

            return result;
        }

        private static bool TryGetComparableNumericValues(ComparisonRow row, IList<PartRecord> parts,
            out double[] values, out bool[] hasValue, out string unit)
        {
            values = new double[parts.Count];
            hasValue = new bool[parts.Count];
            unit = null;
            int numericCount = 0;

            for (int i = 0; i < parts.Count; i++)
            {
                string raw = GetComparisonCellValue(row, parts[i]);
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                double value;
                string currentUnit;
                if (!TryParseSimpleNumericValue(raw, out value, out currentUnit))
                    return false;

                if (unit == null)
                    unit = currentUnit;
                else if (!string.Equals(unit, currentUnit, StringComparison.OrdinalIgnoreCase))
                    return false;

                values[i] = value;
                hasValue[i] = true;
                numericCount++;
            }

            if (unit == null)
                unit = string.Empty;
            return numericCount >= 2;
        }

        private static bool TryParseSimpleNumericValue(string text, out double value, out string unit)
        {
            value = 0d;
            unit = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.Trim();
            int index = 0;
            if (index < trimmed.Length && (trimmed[index] == '+' || trimmed[index] == '-'))
                index++;

            bool sawDigit = false;
            while (index < trimmed.Length && (char.IsDigit(trimmed[index]) || trimmed[index] == ','))
            {
                if (char.IsDigit(trimmed[index]))
                    sawDigit = true;
                index++;
            }

            if (index < trimmed.Length && trimmed[index] == '.')
            {
                index++;
                while (index < trimmed.Length && char.IsDigit(trimmed[index]))
                {
                    sawDigit = true;
                    index++;
                }
            }

            if (!sawDigit)
                return false;

            if (index < trimmed.Length && (trimmed[index] == 'e' || trimmed[index] == 'E'))
            {
                int exponentStart = index;
                index++;
                if (index < trimmed.Length && (trimmed[index] == '+' || trimmed[index] == '-'))
                    index++;
                int exponentDigits = index;
                while (index < trimmed.Length && char.IsDigit(trimmed[index]))
                    index++;
                if (index == exponentDigits)
                    index = exponentStart;
            }

            string numberText = trimmed.Substring(0, index);
            if (!double.TryParse(numberText, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out value))
                return false;

            unit = trimmed.Substring(index).Trim();

            // Avoid pretending compound values such as "10 / 20" or strings that
            // contain additional numeric fields are a single comparable measurement.
            if (unit.Any(char.IsDigit))
                return false;

            return true;
        }

        private static bool NearlyEqual(double first, double second)
        {
            double scale = Math.Max(1d, Math.Max(Math.Abs(first), Math.Abs(second)));
            return Math.Abs(first - second) <= scale * 0.000001d;
        }

        private static string FormatComparisonDelta(double delta, string unit)
        {
            string number = delta > 0d
                ? "+" + delta.ToString("0.###", CultureInfo.InvariantCulture)
                : delta.ToString("0.###", CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(unit))
                return number;
            if (unit.StartsWith("°", StringComparison.Ordinal) || unit.StartsWith("%", StringComparison.Ordinal))
                return number + unit;
            return number + " " + unit;
        }

        private List<ComparisonRow> BuildComparisonRows(IList<PartRecord> parts)
        {
            var rows = new List<ComparisonRow>();

            if (visibleCompareSections.Contains(InformationSection.PartInformation))
            {
                AddGeneralComparisonRow(rows, parts, "Description", part => part.Description, true);
                if (showDetailId)
                    AddGeneralComparisonRow(rows, parts, "ID", part => part.Id);
                if (showPartPath)
                    AddGeneralComparisonRow(rows, parts, "Path", part => string.IsNullOrEmpty(part.Path) ? "Not available" : part.Path);
                if (showDetailCost)
                    AddGeneralComparisonRow(rows, parts, "Cost", part => FormatNumber(part.Cost, "0") + " funds");
                if (showDetailMass)
                    AddGeneralComparisonRow(rows, parts, "Mass", part => FormatNumber(part.Mass, "0.###") + " t");
                if (showDetailMaxTemperature)
                    AddGeneralComparisonRow(rows, parts, "Max temperature", part => FormatNumber(part.MaxTemp, "0") + " K");
                if (showDetailImpactTolerance)
                    AddGeneralComparisonRow(rows, parts, "Impact tolerance", part => FormatNumber(part.ToleranceMs, "0.#") + " m/s");
                if (showDetailGTolerance)
                    AddGeneralComparisonRow(rows, parts, "G tolerance", part => FormatNumber(part.ToleranceG, "0.#") + " g");
            }

            foreach (PartRecord part in parts)
            {
                if (part.StockData != null)
                {
                    foreach (StockDetailSection section in part.StockData.Sections)
                    {
                        if (section == null)
                            continue;

                        InformationSection stockSectionKind;
                        if (TryGetStockSectionKind(section, out stockSectionKind) &&
                            !visibleCompareSections.Contains(stockSectionKind))
                            continue;

                        foreach (StockDetailValue value in section.Values)
                        {
                            if (value != null)
                                AddComparisonValue(rows, part, section.Title, value.Label, value.Value);
                        }
                    }

                    if (visibleCompareSections.Contains(InformationSection.Resources))
                    {
                        foreach (StockResourceInfo resource in part.StockData.Resources)
                        {
                            if (resource != null)
                                AddComparisonValue(rows, part, "Resources", resource.Name,
                                    string.IsNullOrEmpty(resource.DisplayText) ? "Present" : resource.DisplayText);
                        }
                    }
                }

                if (visibleCompareSections.Contains(InformationSection.ScienceModules) && part.ScienceModules != null)
                {
                    foreach (ScienceModuleInfo module in part.ScienceModules)
                    {
                        if (module == null)
                            continue;
                        string moduleName = string.IsNullOrEmpty(module.ModuleName) ? "Science module" : module.ModuleName;
                        string sectionName = "Science Modules / " + moduleName;
                        foreach (ScienceFieldValue field in module.Fields)
                        {
                            if (field != null)
                                AddComparisonValue(rows, part, sectionName, field.Title, field.Value);
                        }
                        foreach (ScienceResourceValue resource in module.InputResources)
                        {
                            if (resource != null)
                                AddComparisonValue(rows, part, sectionName, "Input resource", resource.DisplayText);
                        }
                        foreach (ScienceResourceValue resource in module.OutputResources)
                        {
                            if (resource != null)
                                AddComparisonValue(rows, part, sectionName, "Output resource", resource.DisplayText);
                        }
                    }
                }

                if (part.ScanSat != null && part.ScanSat.HasScannerModules)
                {
                    if (visibleCompareSections.Contains(InformationSection.ScanSat))
                    {
                        AddComparisonValue(rows, part, "SCANsat", "Electric charge", FormatNumber(part.ScanSat.EcPerSec, "0.0#") + " EC/s");
                        AddComparisonValue(rows, part, "SCANsat", "Field of view", FormatNumber(part.ScanSat.Fov, "0.##") + "°");
                        AddComparisonValue(rows, part, "SCANsat", "Requires daylight", part.ScanSat.RequiresDaylight ? "Yes" : "No");
                        AddComparisonValue(rows, part, "SCANsat", "Science", FormatNumber(part.ScanSat.Science, "0.##"));
                    }
                    if (visibleCompareSections.Contains(InformationSection.ScanTypes))
                    {
                        AddComparisonValue(rows, part, "Scan Types", "Biome", part.ScanSat.Biome);
                        AddComparisonValue(rows, part, "Scan Types", "Altimetry", part.ScanSat.Altimetry);
                        AddComparisonValue(rows, part, "Scan Types", "Visual", part.ScanSat.Visual);
                        AddComparisonValue(rows, part, "Scan Types", "Resource", part.ScanSat.Resource);
                        AddComparisonValue(rows, part, "Scan Types", "Anomaly", part.ScanSat.Anomaly);
                    }
                    if (visibleCompareSections.Contains(InformationSection.AltitudeRange))
                    {
                        AddComparisonValue(rows, part, "Altitude Range", "Minimum", part.ScanSat.MinAltitudeText + " km");
                        AddComparisonValue(rows, part, "Altitude Range", "Optimal", part.ScanSat.OptimalAltitudeText + " km");
                        AddComparisonValue(rows, part, "Altitude Range", "Maximum", part.ScanSat.MaxAltitudeText + " km");
                    }
                }
            }

            return rows;
        }

        private void AddGeneralComparisonRow(List<ComparisonRow> rows, IList<PartRecord> parts,
            string label, Func<PartRecord, string> selector, bool omitWhenAllEmpty = false)
        {
            bool anyValue = false;
            foreach (PartRecord part in parts)
            {
                string value = selector(part) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(value))
                    anyValue = true;
                AddComparisonValue(rows, part, "Part Information", label, value);
            }

            if (omitWhenAllEmpty && !anyValue)
                rows.RemoveAll(row => string.Equals(row.Section, "Part Information", StringComparison.Ordinal) &&
                    string.Equals(row.Label, label, StringComparison.Ordinal));
        }

        private void AddComparisonValue(List<ComparisonRow> rows, PartRecord part,
            string section, string label, string value)
        {
            if (part == null || string.IsNullOrEmpty(label))
                return;

            ComparisonRow row = rows.FirstOrDefault(existing =>
                string.Equals(existing.Section, section ?? string.Empty, StringComparison.Ordinal) &&
                string.Equals(existing.Label, label, StringComparison.Ordinal));
            if (row == null)
            {
                row = new ComparisonRow { Section = section ?? string.Empty, Label = label };
                rows.Add(row);
            }

            string key = GetComparisonPartKey(part);
            if (string.IsNullOrEmpty(key))
                return;

            string normalized = value ?? string.Empty;
            string existingValue;
            if (row.Values.TryGetValue(key, out existingValue) && !string.IsNullOrEmpty(existingValue))
            {
                if (!string.IsNullOrEmpty(normalized))
                    row.Values[key] = existingValue + "; " + normalized;
            }
            else
            {
                row.Values[key] = normalized;
            }
        }

        private static string GetComparisonCellValue(ComparisonRow row, PartRecord part)
        {
            if (row == null || part == null)
                return string.Empty;
            string value;
            return row.Values.TryGetValue(GetComparisonPartKey(part), out value) ? value : string.Empty;
        }

        private static string GetComparisonDisplayValue(ComparisonRow row, PartRecord part)
        {
            string value = GetComparisonCellValue(row, part);
            return string.IsNullOrEmpty(value) ? "—" : value;
        }

        private static bool IsComparisonRowDifferent(ComparisonRow row, IList<PartRecord> parts)
        {
            if (row == null || parts == null || parts.Count < 2)
                return false;

            string firstValue = GetComparisonDisplayValue(row, parts[0]);
            for (int i = 1; i < parts.Count; i++)
            {
                if (!string.Equals(firstValue, GetComparisonDisplayValue(row, parts[i]), StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void AddPartToEditor(PartRecord part, AvailablePart availablePart = null)
        {
            if (!HighLogic.LoadedSceneIsEditor || EditorLogic.fetch == null)
                return;

            AvailablePart partToAdd = availablePart ?? FindAvailablePart(part);
            if (partToAdd == null)
                return;

            try
            {
                EditorLogic.fetch.SpawnPart(partToAdd);
                windowVisible = false;
                DestroyRotatingPreview();
                if (toolbarControl != null)
                    toolbarControl.SetFalse(false);
            }
            catch (Exception ex)
            {
                Debug.LogError("[PartExplorer] Unable to add part to editor: " + ex);
            }
        }

        private void DrawPartContextMenu()
        {
            if (!showPartContextMenu || partContextMenuPart == null)
                return;

            const float menuWidth = 220f;
            const float itemHeight = 26f;
            const float padding = 4f;
            const int itemCount = 4;
            float menuHeight = padding * 2f + itemHeight * itemCount;

            Vector2 localPoint = new Vector2(
                partContextMenuScreenPosition.x - windowRect.x,
                partContextMenuScreenPosition.y - windowRect.y);
            float x = Mathf.Clamp(localPoint.x, 4f, Mathf.Max(4f, windowRect.width - menuWidth - 4f));
            float y = Mathf.Clamp(localPoint.y, 24f, Mathf.Max(24f, windowRect.height - menuHeight - 4f));
            Rect menuRect = new Rect(x, y, menuWidth, menuHeight);
            GUI.Box(menuRect, GUIContent.none);

            Rect itemRect = new Rect(menuRect.x + padding, menuRect.y + padding,
                menuRect.width - padding * 2f, itemHeight);

            bool oldEnabled = GUI.enabled;
            AvailablePart availablePart = FindAvailablePart(partContextMenuPart);
            bool canAdd = HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null && availablePart != null;
            GUI.enabled = oldEnabled && canAdd;
            if (GUI.Button(itemRect, new GUIContent("Add to editor",
                canAdd ? "Add this part to the VAB/SPH" : "Available only in the VAB or SPH")))
            {
                PartRecord part = partContextMenuPart;
                showPartContextMenu = false;
                partContextMenuPart = null;
                GUI.enabled = oldEnabled;
                AddPartToEditor(part, availablePart);
                return;
            }

            GUI.enabled = oldEnabled;
            itemRect.y += itemHeight;
            bool selected = IsSelectedForComparison(partContextMenuPart);
            string compareCaption = selected ? "Remove from Compare" : "Add to Compare";
            if (GUI.Button(itemRect, compareCaption))
            {
                SetComparisonSelected(partContextMenuPart, !selected);
                showPartContextMenu = false;
                partContextMenuPart = null;
                return;
            }

            itemRect.y += itemHeight;
            bool hasId = !string.IsNullOrEmpty(partContextMenuPart.Id);
            GUI.enabled = oldEnabled && hasId;
            if (GUI.Button(itemRect, new GUIContent("Copy part ID", hasId ? partContextMenuPart.Id : "No part ID available")))
            {
                GUIUtility.systemCopyBuffer = partContextMenuPart.Id;
                showPartContextMenu = false;
                partContextMenuPart = null;
                GUI.enabled = oldEnabled;
                return;
            }

            itemRect.y += itemHeight;
            bool hasPath = !string.IsNullOrEmpty(partContextMenuPart.Path);
            GUI.enabled = oldEnabled && hasPath;
            if (GUI.Button(itemRect, new GUIContent("Copy path", hasPath ? partContextMenuPart.Path : "No part path available")))
            {
                GUIUtility.systemCopyBuffer = partContextMenuPart.Path;
                showPartContextMenu = false;
                partContextMenuPart = null;
                GUI.enabled = oldEnabled;
                return;
            }
            GUI.enabled = oldEnabled;

            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && !menuRect.Contains(e.mousePosition))
            {
                showPartContextMenu = false;
                partContextMenuPart = null;
                e.Use();
            }
        }

        private void DrawActiveTooltip()
        {
            string tooltip = GUI.tooltip;
            if (string.IsNullOrEmpty(tooltip) || Event.current == null || Event.current.type != EventType.Repaint)
                return;

            GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };

            const float maxWidth = 360f;
            const float padding = 8f;
            GUIContent content = new GUIContent(tooltip);
            float naturalWidth = tooltipStyle.CalcSize(content).x + padding * 2f;
            float width = Mathf.Clamp(naturalWidth, 120f, maxWidth);
            float height = tooltipStyle.CalcHeight(content, width - padding * 2f) + padding * 2f;

            Vector2 mouse = Event.current.mousePosition;
            float x = mouse.x + 16f;
            float y = mouse.y + 18f;
            x = Mathf.Clamp(x, 4f, Mathf.Max(4f, windowRect.width - width - 4f));
            y = Mathf.Clamp(y, 24f, Mathf.Max(24f, windowRect.height - height - 4f));
            GUI.Box(new Rect(x, y, width, height), content, tooltipStyle);
        }

        private void DrawDetails()
        {
            var rows = GetFilteredSorted();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Details", sectionStyle);
            detailScroll = GUILayout.BeginScrollView(detailScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

            if (rows.Count > 0)
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, rows.Count - 1);
                PartRecord p = rows[selectedIndex];

                if (visibleDetailSections.Contains(InformationSection.PartInformation))
                {
                    BeginDetailPanel("Part Information");
                    GUILayout.Label(p.Part, titleStyle);

                    if (!string.IsNullOrWhiteSpace(p.Description))
                    {
                        GUILayout.Space(3f);
                        GUILayout.Label("Description", sectionStyle);
                        GUILayout.Label(p.Description, descriptionStyle, GUILayout.ExpandWidth(true));
                        GUILayout.Space(4f);
                    }

                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        AvailablePart availablePart = FindAvailablePart(p);
                        bool oldEnabled = GUI.enabled;
                        GUI.enabled = oldEnabled && availablePart != null && EditorLogic.fetch != null;
                        if (GUILayout.Button("Add Part to Editor", GUILayout.Height(28f)))
                            AddPartToEditor(p, availablePart);
                        GUI.enabled = oldEnabled;
                        if (availablePart == null)
                            GUILayout.Label("This part is not available in the loaded editor part database.", smallStyle);
                        GUILayout.Space(4f);
                    }

                    if (showDetailId)
                        DrawPair("ID", p.Id);
                    if (showPartPath)
                        DrawPair("Path", string.IsNullOrEmpty(p.Path) ? "Not available" : p.Path);
                    if (showDetailCost)
                        DrawPair("Cost", FormatNumber(p.Cost, "0") + " funds");
                    if (showDetailMass)
                        DrawPair("Mass", FormatNumber(p.Mass, "0.###") + " t");
                    if (showDetailMaxTemperature)
                        DrawPair("Max temperature", FormatNumber(p.MaxTemp, "0") + " K");
                    if (showDetailImpactTolerance)
                        DrawPair("Impact tolerance", FormatNumber(p.ToleranceMs, "0.#") + " m/s");
                    if (showDetailGTolerance)
                        DrawPair("G tolerance", FormatNumber(p.ToleranceG, "0.#") + " g");
                    EndDetailPanel();
                }

                DrawStockPartDetails(p);
                DrawScienceModules(p);

                if (p.ScanSat != null && p.ScanSat.HasScannerModules)
                {
                    if (visibleDetailSections.Contains(InformationSection.ScanSat))
                    {
                        BeginDetailPanel("SCANsat");
                        DrawPair("Electric charge", FormatNumber(p.ScanSat.EcPerSec, "0.0#") + " EC/s");
                        DrawPair("Field of view", FormatNumber(p.ScanSat.Fov, "0.##") + "°");
                        DrawPair("Requires daylight", p.ScanSat.RequiresDaylight ? "Yes" : "No");
                        DrawPair("Science", FormatNumber(p.ScanSat.Science, "0.##"));
                        EndDetailPanel();
                    }

                    if (visibleDetailSections.Contains(InformationSection.ScanTypes))
                    {
                        BeginDetailPanel("Scan Types");
                        DrawPair("Biome", p.ScanSat.Biome);
                        DrawPair("Altimetry", p.ScanSat.Altimetry);
                        DrawPair("Visual", p.ScanSat.Visual);
                        DrawPair("Resource", p.ScanSat.Resource);
                        DrawPair("Anomaly", p.ScanSat.Anomaly);
                        EndDetailPanel();
                    }

                    if (visibleDetailSections.Contains(InformationSection.AltitudeRange))
                    {
                        BeginDetailPanel("Altitude Range");
                        DrawPair("Minimum", p.ScanSat.MinAltitudeText + " km");
                        DrawPair("Optimal", p.ScanSat.OptimalAltitudeText + " km");
                        DrawPair("Maximum", p.ScanSat.MaxAltitudeText + " km");
                        EndDetailPanel();
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void BeginDetailPanel(string title)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(title))
                GUILayout.Label(title, sectionStyle);
        }

        private void EndDetailPanel()
        {
            GUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        private void DrawStockPartDetails(PartRecord part)
        {
            if (part == null || part.StockData == null || !part.StockData.HasData)
                return;

            foreach (StockDetailSection section in part.StockData.Sections)
            {
                if (section == null || section.Values.Count == 0)
                    continue;

                InformationSection stockSectionKind;
                if (TryGetStockSectionKind(section, out stockSectionKind) &&
                    !visibleDetailSections.Contains(stockSectionKind))
                    continue;

                BeginDetailPanel(section.Title);
                foreach (StockDetailValue value in section.Values)
                {
                    if (value == null)
                        continue;
                    DrawPair(value.Label, value.Value);
                }
                EndDetailPanel();
            }

            if (visibleDetailSections.Contains(InformationSection.Resources) && part.StockData.Resources.Count > 0)
            {
                BeginDetailPanel("Resources");
                foreach (StockResourceInfo resource in part.StockData.Resources)
                {
                    if (resource == null)
                        continue;
                    DrawPair(resource.Name, string.IsNullOrEmpty(resource.DisplayText) ? "Present" : resource.DisplayText);
                }
                EndDetailPanel();
            }
        }

        private void DrawScienceModules(PartRecord part)
        {
            if (!visibleDetailSections.Contains(InformationSection.ScienceModules) ||
                part == null || part.ScienceModules == null || part.ScienceModules.Count == 0)
                return;

            BeginDetailPanel("Science Modules");

            for (int i = 0; i < part.ScienceModules.Count; i++)
            {
                ScienceModuleInfo module = part.ScienceModules[i];
                if (module == null)
                    continue;

                if (i > 0)
                    GUILayout.Space(6f);

                GUILayout.Label(string.IsNullOrEmpty(module.ModuleName) ? "Science module" : module.ModuleName, sectionStyle);

                foreach (ScienceFieldValue field in module.Fields)
                {
                    if (field == null)
                        continue;
                    DrawPair(field.Title, field.Value);
                }

                foreach (ScienceResourceValue resource in module.InputResources)
                {
                    if (resource == null)
                        continue;
                    DrawPair("Input resource", resource.DisplayText);
                }

                foreach (ScienceResourceValue resource in module.OutputResources)
                {
                    if (resource == null)
                        continue;
                    DrawPair("Output resource", resource.DisplayText);
                }
            }

            EndDetailPanel();
        }

        private void RebuildAvailablePartLookup()
        {
            availablePartByPath.Clear();
            availablePartById.Clear();
            availablePartByTitle.Clear();

            if (PartLoader.LoadedPartsList == null)
                return;

            foreach (AvailablePart availablePart in PartLoader.LoadedPartsList)
            {
                if (availablePart == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(availablePart.partUrl) &&
                    !availablePartByPath.ContainsKey(availablePart.partUrl))
                    availablePartByPath.Add(availablePart.partUrl, availablePart);

                if (!string.IsNullOrWhiteSpace(availablePart.name) &&
                    !availablePartById.ContainsKey(availablePart.name))
                    availablePartById.Add(availablePart.name, availablePart);

                string title = availablePart.title;
                try { title = KSP.Localization.Localizer.Format(title); }
                catch { }

                if (!string.IsNullOrWhiteSpace(title) &&
                    !availablePartByTitle.ContainsKey(title))
                    availablePartByTitle.Add(title, availablePart);
            }
        }

        private AvailablePart FindAvailablePart(PartRecord record)
        {
            if (record == null)
                return null;

            AvailablePart availablePart;

            if (!string.IsNullOrWhiteSpace(record.Path) &&
                availablePartByPath.TryGetValue(record.Path, out availablePart))
                return availablePart;

            if (!string.IsNullOrWhiteSpace(record.Id) &&
                availablePartById.TryGetValue(record.Id, out availablePart))
                return availablePart;

            if (!string.IsNullOrWhiteSpace(record.Part) &&
                availablePartByTitle.TryGetValue(record.Part, out availablePart))
                return availablePart;

            return null;
        }

        private void DrawPair(string name, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name + ":", GUILayout.Width(145f));
            GUILayout.Label(string.IsNullOrEmpty(value) ? "—" : value, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }


        private void EnsurePartDataCache()
        {
            object loadedPartsList = PartLoader.LoadedPartsList;
            int loadedPartsCount = PartLoader.LoadedPartsList == null
                ? -1
                : PartLoader.LoadedPartsList.Count;

            if (partDataCacheValid &&
                ReferenceEquals(cachedLoadedPartsListReference, loadedPartsList) &&
                cachedLoadedPartsCount == loadedPartsCount)
                return;

            RefreshData();
        }

        private void CapturePartDataCacheState()
        {
            cachedLoadedPartsListReference = PartLoader.LoadedPartsList;
            cachedLoadedPartsCount = PartLoader.LoadedPartsList == null
                ? -1
                : PartLoader.LoadedPartsList.Count;
            partDataCacheValid = true;
        }

        private void RefreshData()
        {
            DestroyRotatingPreview();
            DestroyLivePartTextures();
            InvalidateEditorFilterCache();
            RebuildAvailablePartLookup();
            ScienceModuleData.LoadDefinitions();
            try
            {
                List<PartRecord> live = LivePartDatabase.TryLoad();
                if (live != null && live.Count > 0)
                {
                    activeParts = live;
                    usingLiveData = true;
                    dataSourceText = "Live data from installed KSP parts";
                }
                else
                {
                    activeParts = new List<PartRecord>(SCANsatPartData.EmbeddedReferenceParts);
                    usingLiveData = false;
                    dataSourceText = "Embedded SCANsat reference data (loaded parts unavailable)";
                }

                PruneComparisonSelection();
                EnsureSelectedMod(GetAvailableMods());
                if (!string.IsNullOrEmpty(selectedCategory) && !GetAvailableCategories().Contains(selectedCategory))
                    selectedCategory = string.Empty;
                selectedIndex = 0;
                listScroll = Vector2.zero;
                detailScroll = Vector2.zero;
            }
            catch (Exception ex)
            {
                Debug.LogError("[PartExplorer] Failed to read loaded part data; using embedded fallback. " + ex);
                activeParts = new List<PartRecord>(SCANsatPartData.EmbeddedReferenceParts);
                usingLiveData = false;
                dataSourceText = "Embedded SCANsat reference data (live read failed)";
                PruneComparisonSelection();
            }
            finally
            {
                CapturePartDataCacheState();
            }
        }

        private static string FormatNumber(double value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }

    internal sealed class PartRecord
    {
        public string Part, Id, Path, ModName, Category, Description;
        public double Cost, Mass, MaxTemp, ToleranceMs, ToleranceG;
        public SCANsatPartData ScanSat;
        public List<ScienceModuleInfo> ScienceModules;
        public StockPartData StockData;

        public string ScanSummary
        {
            get { return ScanSat != null ? ScanSat.ScanSummary : string.Empty; }
        }

        public string ScienceSummary
        {
            get
            {
                if (ScienceModules == null || ScienceModules.Count == 0)
                    return string.Empty;

                return string.Join(" ", ScienceModules.Select(module =>
                {
                    if (module == null) return string.Empty;
                    IEnumerable<string> fields = module.Fields.Select(f => f == null ? string.Empty : (f.Title + " " + f.Value));
                    IEnumerable<string> inputs = module.InputResources.Select(r => r == null ? string.Empty : ("Input " + r.DisplayText));
                    IEnumerable<string> outputs = module.OutputResources.Select(r => r == null ? string.Empty : ("Output " + r.DisplayText));
                    return (module.ModuleName + " " + string.Join(" ", fields.Concat(inputs).Concat(outputs))).Trim();
                }));
            }
        }

        public string StockSummary
        {
            get { return StockData != null ? StockData.SearchSummary : string.Empty; }
        }

        public PartRecord(string part, string id, double cost, double mass, double maxTemp, double toleranceMs,
            double toleranceG, string path = "", string modName = "", string category = "", SCANsatPartData scanSat = null,
            List<ScienceModuleInfo> scienceModules = null, string description = "", StockPartData stockData = null)
        {
            Part = part;
            Id = id;
            Cost = cost;
            Mass = mass;
            MaxTemp = maxTemp;
            ToleranceMs = toleranceMs;
            ToleranceG = toleranceG;
            Path = path ?? string.Empty;
            ModName = modName ?? string.Empty;
            Category = category ?? string.Empty;
            ScanSat = scanSat;
            ScienceModules = scienceModules ?? new List<ScienceModuleInfo>();
            Description = description ?? string.Empty;
            StockData = stockData ?? new StockPartData();
        }
    }

    internal static class LivePartDatabase
    {
        public static List<PartRecord> TryLoad()
        {
            if (PartLoader.LoadedPartsList == null)
                return null;

            var result = new List<PartRecord>();
            foreach (AvailablePart ap in PartLoader.LoadedPartsList)
            {
                if (ap == null || ap.partPrefab == null || ap.partConfig == null)
                    continue;

                if (ap.category == PartCategories.none)
                    continue;

                ConfigNode[] modules = ap.partConfig.GetNodes("MODULE");
                Part prefab = ap.partPrefab;

                result.Add(new PartRecord(
                    Localize(ap.title),
                    ap.name ?? "—",
                    ap.cost,
                    prefab.mass,
                    prefab.maxTemp,
                    prefab.crashTolerance,
                    prefab.breakingForce,
                    ap.partUrl ?? string.Empty,
                    GetModName(ap),
                    ap.category.ToString(),
                    SCANsatPartData.FromLoadedPart(ap, modules),
                    ScienceModuleData.FromLoadedPart(ap, modules),
                    Localize(ap.description),
                    StockPartData.FromLoadedPart(ap, modules)));
            }

            return result.OrderBy(p => p.Part).ToList();
        }

        private static string GetModName(AvailablePart ap)
        {
            if (ap == null) return string.Empty;
            string path = ap.partUrl ?? string.Empty;
            if (!string.IsNullOrEmpty(path))
            {
                string normalized = path.Replace('\\', '/');
                int slash = normalized.IndexOf('/');
                if (slash > 0)
                    return normalized.Substring(0, slash);
                if (slash < 0)
                    return normalized;
            }
            return "Unknown";
        }

        private static string Localize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "(unnamed part)";
            try { return KSP.Localization.Localizer.Format(value); }
            catch { return value; }
        }
    }
}
