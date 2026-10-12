using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PartExplorerAPI;
using UnityEngine;

[assembly: AssemblyTitle("PartExplorer API Test")]
[assembly: AssemblyDescription("Small test addon for the PartExplorer public API")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("PartExplorer API Test")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]

namespace PartExplorerApiTest
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class PartExplorerApiTester : MonoBehaviour
    {
        private const int WindowId = 731977;
        private const int TestPartCount = 12;

        private Rect windowRect = new Rect(300f, 120f, 430f, 520f);
        private bool windowVisible = true;
        private string status = "Ready. F8 shows/hides this test window.";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                windowVisible = !windowVisible;
        }

        private void OnGUI()
        {
            if (!windowVisible)
                return;

            windowRect = GUILayout.Window(
                WindowId,
                windowRect,
                DrawWindow,
                "PartExplorer API Test",
                GUILayout.Width(430f));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label("Tests PartExplorer through the source-only reflection wrapper.");
            GUILayout.Label(
                "Installed: " + PartExplorerWrapper.IsInstalled +
                "   Available: " + PartExplorerWrapper.IsAvailable +
                "   API: " + PartExplorerWrapper.ApiVersion +
                "   Version: " + PartExplorerWrapper.PartExplorerVersion);
            GUILayout.Label(
                "Open: " + PartExplorerWrapper.IsOpen +
                "   Restricted: " + PartExplorerWrapper.IsRestricted +
                "   Count: " + PartExplorerWrapper.RestrictedPartCount +
                "   Picker: " + PartExplorerWrapper.IsPickerActive);
            GUILayout.Space(6f);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open PartExplorer"))
                    SetResult("Open", PartExplorerWrapper.Open());
                if (GUILayout.Button("Close PartExplorer"))
                    SetResult("Close", PartExplorerWrapper.Close());
            }

            if (GUILayout.Button("Show first 12 loaded parts + source title"))
                ShowAvailablePartsTest();

            if (GUILayout.Button("Show first 8 by internal part name"))
                ShowNamedPartsTest();

            if (GUILayout.Button("Show one part"))
                ShowOnePartTest();

            if (GUILayout.Button("Compare first 6 loaded parts"))
                ComparePartsTest();

            if (GUILayout.Button("Pick one from first 12 loaded parts"))
                PickPartTest();

            GUI.enabled = PartExplorerWrapper.IsPickerActive;
            if (GUILayout.Button("Cancel active picker"))
                SetResult("CancelPartPicker", PartExplorerWrapper.CancelPartPicker());
            GUI.enabled = true;

            GUI.enabled = CanUseLivePartsTest();
            if (GUILayout.Button("Show parts on current craft"))
                ShowLivePartsTest();
            GUI.enabled = true;

            if (GUILayout.Button("Clear PartExplorer restriction"))
                SetResult("ClearPartListRestriction", PartExplorerWrapper.ClearPartListRestriction());

            GUILayout.Space(8f);
            GUILayout.Label(status, GUILayout.MinHeight(54f));
            GUILayout.Space(4f);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close tester", GUILayout.Width(100f)))
                    windowVisible = false;
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private static List<AvailablePart> GetLoadedTestParts(int count)
        {
            return PartLoader.LoadedPartsList
                .Where(part => part != null)
                .OrderBy(part => part.title ?? part.name)
                .Take(count)
                .ToList();
        }

        private void ShowAvailablePartsTest()
        {
            List<AvailablePart> parts = GetLoadedTestParts(TestPartCount);
            bool ok = PartExplorerWrapper.ShowParts(parts, "API Test - AvailablePart list");
            SetResult("ShowParts(AvailablePart, sourceTitle) with " + parts.Count + " parts", ok);
        }

        private void ShowNamedPartsTest()
        {
            List<string> names = GetLoadedTestParts(8)
                .Select(part => part.name)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            bool ok = PartExplorerWrapper.ShowParts(names, "API Test - internal part names");
            SetResult("ShowParts(string, sourceTitle) with " + names.Count + " names", ok);
        }

        private void ShowOnePartTest()
        {
            AvailablePart part = GetLoadedTestParts(1).FirstOrDefault();
            bool ok = PartExplorerWrapper.ShowPart(part);
            SetResult("ShowPart(AvailablePart)" + (part == null ? string.Empty : " - " + part.name), ok);
        }

        private void ComparePartsTest()
        {
            List<AvailablePart> parts = GetLoadedTestParts(6);
            bool ok = PartExplorerWrapper.CompareParts(parts, "API Test - comparison");
            SetResult("CompareParts(AvailablePart, sourceTitle) with " + parts.Count + " parts", ok);
        }

        private void PickPartTest()
        {
            List<AvailablePart> parts = GetLoadedTestParts(TestPartCount);
            bool ok = PartExplorerWrapper.PickPart(
                parts,
                "API Test - choose one part",
                part =>
                {
                    string chosen = part == null ? "<null>" : (part.title ?? part.name ?? "<unnamed>");
                    status = "Picker selected: " + chosen;
                },
                () =>
                {
                    status = "Picker cancelled.";
                });
            SetResult("PickPart with " + parts.Count + " candidates", ok);
        }

        private void ShowLivePartsTest()
        {
            List<Part> parts = GetCurrentCraftParts();
            if (parts.Count == 0)
            {
                status = "No current editor craft or active vessel parts found.";
                return;
            }

            bool ok = PartExplorerWrapper.ShowParts(parts, "API Test - current craft");
            SetResult("ShowParts(Part, sourceTitle) with " + parts.Count + " craft parts", ok);
        }

        private void SetResult(string action, bool success)
        {
            status = success
                ? action + " succeeded."
                : action + " failed: " + PartExplorerWrapper.LastError;
        }

        private static bool CanUseLivePartsTest()
        {
            if (HighLogic.LoadedSceneIsFlight)
                return FlightGlobals.ActiveVessel != null &&
                       FlightGlobals.ActiveVessel.parts != null &&
                       FlightGlobals.ActiveVessel.parts.Count > 0;

            if (HighLogic.LoadedSceneIsEditor)
                return EditorLogic.fetch != null &&
                       EditorLogic.fetch.ship != null &&
                       EditorLogic.fetch.ship.parts != null &&
                       EditorLogic.fetch.ship.parts.Count > 0;

            return false;
        }

        private static List<Part> GetCurrentCraftParts()
        {
            if (HighLogic.LoadedSceneIsFlight &&
                FlightGlobals.ActiveVessel != null &&
                FlightGlobals.ActiveVessel.parts != null)
            {
                return FlightGlobals.ActiveVessel.parts
                    .Where(part => part != null)
                    .ToList();
            }

            if (HighLogic.LoadedSceneIsEditor &&
                EditorLogic.fetch != null &&
                EditorLogic.fetch.ship != null &&
                EditorLogic.fetch.ship.parts != null)
            {
                return EditorLogic.fetch.ship.parts
                    .Where(part => part != null)
                    .ToList();
            }

            return new List<Part>();
        }
    }
}
