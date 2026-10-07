using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PartsReference
{
    /// <summary>
    /// Owns all SCANsat-specific part/module parsing and the embedded SCANsat fallback data.
    /// The rest of the part browser can remain mod-agnostic.
    /// </summary>
    internal sealed class SCANsatPartData
    {
        private const int AltLo = 1 << 0;
        private const int AltHi = 1 << 1;
        private const int VisualLo = 1 << 2;
        private const int BiomeFlag = 1 << 3;
        private const int AnomalyFlag = 1 << 4;
        private const int AnomalyDetail = 1 << 5;
        private const int VisualHi = 1 << 6;
        private const int ResourceLo = 1 << 7;
        private const int ResourceHi = 1 << 8;

        public bool HasScannerModules { get; private set; }
        public double EcPerSec { get; private set; }
        public double Fov { get; private set; }
        public double Science { get; private set; }
        public bool RequiresDaylight { get; private set; }
        public string Biome { get; private set; }
        public string Altimetry { get; private set; }
        public string Visual { get; private set; }
        public string Resource { get; private set; }
        public string Anomaly { get; private set; }
        public string MinAltitudeText { get; private set; }
        public string OptimalAltitudeText { get; private set; }
        public string MaxAltitudeText { get; private set; }

        public string ScanSummary
        {
            get
            {
                if (!HasScannerModules)
                    return string.Empty;
                return string.Join(" ", new[] { Biome, Altimetry, Visual, Resource, Anomaly }
                    .Where(s => !string.IsNullOrEmpty(s)));
            }
        }

        private SCANsatPartData()
        {
            Biome = string.Empty;
            Altimetry = string.Empty;
            Visual = string.Empty;
            Resource = string.Empty;
            Anomaly = string.Empty;
            MinAltitudeText = string.Empty;
            OptimalAltitudeText = string.Empty;
            MaxAltitudeText = string.Empty;
        }

        public static SCANsatPartData FromLoadedPart(AvailablePart ap, ConfigNode[] modules)
        {
            if (ap == null || modules == null)
                return null;

            List<ConfigNode> scanners = modules.Where(IsScannerModule).ToList();
            if (scanners.Count == 0)
                return null;

            int sensorType = 0;
            double ec = 0d;
            double fov = 0d;
            double minAlt = double.MaxValue;
            double bestAlt = double.MaxValue;
            double maxAlt = 0d;
            bool requireLight = false;

            foreach (ConfigNode scanner in scanners)
            {
                sensorType |= GetInt(scanner, "sensorType", 0);
                fov = Math.Max(fov, GetDouble(scanner, "fov", 0));
                minAlt = Math.Min(minAlt, GetDouble(scanner, "min_alt", double.MaxValue));
                bestAlt = Math.Min(bestAlt, GetDouble(scanner, "best_alt", double.MaxValue));
                maxAlt = Math.Max(maxAlt, GetDouble(scanner, "max_alt", 0));
                requireLight |= GetBool(scanner, "requireLight", false);

                foreach (ConfigNode resource in scanner.GetNodes("RESOURCE"))
                {
                    if (string.Equals(resource.GetValue("name"), "ElectricCharge", StringComparison.OrdinalIgnoreCase))
                        ec += GetDouble(resource, "rate", 0);
                }
            }

            return new SCANsatPartData
            {
                HasScannerModules = true,
                EcPerSec = ec,
                Fov = fov,
                Science = ReadScience(modules),
                RequiresDaylight = requireLight,
                Biome = Has(sensorType, BiomeFlag) ? "Yes" : string.Empty,
                Altimetry = Resolution(sensorType, AltLo, AltHi),
                Visual = Resolution(sensorType, VisualLo, VisualHi),
                Resource = Resolution(sensorType, ResourceLo, ResourceHi),
                Anomaly = Has(sensorType, AnomalyDetail) ? "Detail" : (Has(sensorType, AnomalyFlag) ? "Yes" : string.Empty),
                MinAltitudeText = AltitudeText(minAlt),
                OptimalAltitudeText = AltitudeText(bestAlt),
                MaxAltitudeText = AltitudeText(maxAlt)
            };
        }

        private static SCANsatPartData FromEmbedded(double ecPerSec, string biome, string altimetry, string visual,
            string resource, string anomaly, string minAlt, string optimalAlt, string maxAlt, double fov,
            bool daylight, double science)
        {
            return new SCANsatPartData
            {
                HasScannerModules = true,
                EcPerSec = ecPerSec,
                Biome = biome ?? string.Empty,
                Altimetry = altimetry ?? string.Empty,
                Visual = visual ?? string.Empty,
                Resource = resource ?? string.Empty,
                Anomaly = anomaly ?? string.Empty,
                MinAltitudeText = minAlt ?? string.Empty,
                OptimalAltitudeText = optimalAlt ?? string.Empty,
                MaxAltitudeText = maxAlt ?? string.Empty,
                Fov = fov,
                RequiresDaylight = daylight,
                Science = science
            };
        }

        private static PartRecord EmbeddedPart(string part, string id, double cost, double mass, double maxTemp,
            double toleranceMs, double toleranceG, double ecPerSec, string biome, string altimetry, string visual,
            string resource, string anomaly, string minAlt, string optimalAlt, string maxAlt, double fov,
            bool daylight, double science)
        {
            return new PartRecord(part, id, cost, mass, maxTemp, toleranceMs, toleranceG,
                string.Empty, "SCANsat", "Science",
                FromEmbedded(ecPerSec, biome, altimetry, visual, resource, anomaly, minAlt, optimalAlt, maxAlt, fov, daylight, science));
        }

        public static readonly PartRecord[] EmbeddedReferenceParts =
        {
            EmbeddedPart("MS-1 Multispectral Scanner", "SCAN-11", 3000, 0.075, 1200, 6, 50, 0.8, "Yes", "", "LoRes", "", "", "20", "70", "250", 3, true, 45),
            EmbeddedPart("MS-R Enhanced Multispectral Scanner", "SCAN-12", 10000, 0.125, 1200, 6, 50, 1.0, "Yes", "", "LoRes", "LoRes", "", "70", "300", "400", 1.5, true, 90),
            EmbeddedPart("R-3B Radar Altimeter", "SCAN-31", 3300, 0.075, 1200, 6, 50, 1.0, "", "LoRes", "", "", "", "5", "70", "250", 1.5, false, 45),
            EmbeddedPart("VS-1 High Resolution Imager", "SCAN-21", 7500, 0.05, 1200, 6, 50, 0.8, "", "", "HiRes", "", "", "20", "70", "250", 1.5, true, 160),
            EmbeddedPart("R-EO-1 Radar Antenna", "SCAN-32", 5500, 0.125, 1200, 6, 50, 1.0, "", "LoRes", "", "", "", "50", "100", "500", 3.5, false, 90),
            EmbeddedPart("SAR-X Antenna", "SCAN-41", 8000, 0.18, 1200, 6, 50, 2.0, "", "HiRes", "", "", "", "70", "250", "500", 1.5, false, 160),
            EmbeddedPart("SAR-C Antenna", "SCAN-42", 9800, 0.4, 1200, 6, 50, 2.5, "", "HiRes", "", "", "", "500", "700", "750", 3, false, 300),
            EmbeddedPart("SCAN Been There Done That®", "-", 13000, 0.02, 1200, 12, 50, 6.0, "", "", "", "", "Detail", "-", "-", "2", 1, false, 300),
            EmbeddedPart("VS-3 Advanced High Resolution Imager", "SCAN-22", 18400, 0.3, 1200, 6, 50, 1.5, "", "", "HiRes", "", "Yes", "70", "350", "500", 2.5, true, 300),
            EmbeddedPart("SCAN-R Resource Mapper", "SCAN-51", 15000, 0.1, 1200, 6, 50, 1.8, "", "", "", "HiRes", "", "20", "70", "250", 1, true, 160),
            EmbeddedPart("SCAN-R2 Advanced Resource Mapper", "SCAN-52", 15000, 0.275, 1200, 6, 50, 1.5, "", "", "", "HiRes", "", "70", "250", "500", 2.5, true, 300),
            EmbeddedPart("SCAN-RX Hyperspectral Resource Mapper", "SCAN-53", 17500, 0.5, 1200, 6, 50, 1.3, "", "", "", "HiRes", "", "100", "500", "750", 3, true, 1000),
            EmbeddedPart("MS-2A Advanced Multispectral Scanner", "SCAN-13", 15000, 0.25, 1200, 6, 50, 1.5, "Yes", "", "LoRes", "LoRes", "", "100", "500", "750", 4, true, 550),
            EmbeddedPart("VS-11 Classified Reconnaissance Imager", "SCAN-23", 25000, 1.75, 1200, 6, 50, 2.0, "", "", "HiRes", "", "Yes", "100", "200", "1000", 4, true, 1000),
            EmbeddedPart("SAR-L Antenna", "SCAN-43", 54000, 0.8, 1200, 6, 50, 4.5, "Yes", "HiRes", "", "", "", "250", "500", "1000", 4, false, 1000),
            EmbeddedPart("M700 Survey Scanner", "-", 1500, 0.2, 2000, 7, 50, 0.5, "", "", "", "LoRes", "", "15", "500", "7500", 3, false, 300),
            EmbeddedPart("M4435 Narrow-Band Scanner", "-", 1000, 0.1, 2000, 7, 50, 2.0, "", "", "", "HiRes", "", "10", "150", "500", 2, false, 1000)
        };

        public static bool HasResolution(string value, bool high)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            return value.IndexOf(high ? "HiRes" : "LoRes", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsScannerModule(ConfigNode node)
        {
            if (node == null) return false;
            string name = node.GetValue("name") ?? string.Empty;
            return string.Equals(name, "SCANsat", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "ModuleSCANresourceScanner", StringComparison.OrdinalIgnoreCase);
        }

        private static double ReadScience(ConfigNode[] modules)
        {
            double total = 0d;
            foreach (ConfigNode node in modules)
            {
                string name = node.GetValue("name") ?? string.Empty;
                if (!string.Equals(name, "SCANexperiment", StringComparison.OrdinalIgnoreCase))
                    continue;

                string experimentType = node.GetValue("experimentType");
                if (string.IsNullOrEmpty(experimentType))
                    continue;

                try
                {
                    ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentType);
                    if (experiment != null)
                        total += experiment.scienceCap;
                }
                catch { }
            }
            return total;
        }

        private static string Resolution(int flags, int low, int high)
        {
            bool lo = Has(flags, low), hi = Has(flags, high);
            if (lo && hi) return "LoRes + HiRes";
            if (hi) return "HiRes";
            if (lo) return "LoRes";
            return string.Empty;
        }

        private static bool Has(int flags, int bit)
        {
            return (flags & bit) != 0;
        }

        private static string AltitudeText(double meters)
        {
            if (meters == double.MaxValue || meters < 0) return "—";
            double km = meters / 1000.0;
            return km.ToString(km >= 100 ? "0" : km >= 10 ? "0.#" : "0.##", CultureInfo.InvariantCulture);
        }

        private static int GetInt(ConfigNode node, string name, int fallback)
        {
            int v;
            return int.TryParse(node.GetValue(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        private static double GetDouble(ConfigNode node, string name, double fallback)
        {
            double v;
            return double.TryParse(node.GetValue(name), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        private static bool GetBool(ConfigNode node, string name, bool fallback)
        {
            bool v;
            return bool.TryParse(node.GetValue(name), out v) ? v : fallback;
        }
    }
}
