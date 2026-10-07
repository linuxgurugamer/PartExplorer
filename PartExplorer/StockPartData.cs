using System;
using System.Collections.Generic;
using System.Linq;

namespace PartExplorer
{
    /// <summary>
    /// Extracts stock KSP part/module information used by the Details pane.
    /// Kept separate from SCANsat- and science-specific parsing.
    /// </summary>
    internal sealed class StockPartData
    {
        public readonly List<StockDetailSection> Sections = new List<StockDetailSection>();
        public readonly List<StockResourceInfo> Resources = new List<StockResourceInfo>();

        public bool HasData
        {
            get { return Sections.Count > 0 || Resources.Count > 0; }
        }

        public string SearchSummary
        {
            get
            {
                IEnumerable<string> sectionText = Sections.SelectMany(section =>
                    new[] { section.Title }.Concat(section.Values.Select(value => value.Label + " " + value.Value)));
                IEnumerable<string> resourceText = Resources.Select(resource =>
                    resource.Name + " " + resource.Amount + " " + resource.MaxAmount);
                return string.Join(" ", sectionText.Concat(resourceText));
            }
        }

        public static StockPartData FromLoadedPart(AvailablePart availablePart, ConfigNode[] moduleNodes)
        {
            var data = new StockPartData();
            if (availablePart == null || availablePart.partConfig == null)
                return data;

            ConfigNode[] modules = moduleNodes ?? availablePart.partConfig.GetNodes("MODULE") ?? new ConfigNode[0];

            AddCargoSections(data, modules);
            AddCommandSections(data, modules);
            AddDataTransmitterSections(data, modules);
            AddProbeControlPointSections(data, modules);
            AddReactionWheelSections(data, modules);
            AddSasSections(data, modules);
            AddStoredResources(data, availablePart.partConfig);

            return data;
        }

        private static void AddCargoSections(StockPartData data, ConfigNode[] modules)
        {
            foreach (ConfigNode module in FindModules(modules, "ModuleCargoPart"))
            {
                StockDetailSection section = NewSection(data, "Cargo Part Info");
                AddValue(section, module, "packedVolume", "Packed volume");
                AddValue(section, module, "stackableQuantity", "Stackable quantity");
                AddValue(section, module, "packedVolumeLimit", "Packed volume limit");
                RemoveEmptySection(data, section);
            }
        }

        private static void AddCommandSections(StockPartData data, ConfigNode[] modules)
        {
            foreach (ConfigNode module in FindModules(modules, "ModuleCommand"))
            {
                StockDetailSection section = NewSection(data, "Command");
                AddValue(section, module, "minimumCrew", "Minimum crew");
                AddValue(section, module, "hasHibernation", "Hibernation");
                AddValue(section, module, "hibernationMultiplier", "Hibernation multiplier");
                AddModuleResources(section, module, "Resource consumption");
                RemoveEmptySection(data, section);
            }
        }

        private static void AddDataTransmitterSections(StockPartData data, ConfigNode[] modules)
        {
            foreach (ConfigNode module in FindModules(modules, "ModuleDataTransmitter"))
            {
                StockDetailSection section = NewSection(data, "Data Transmitter");
                AddValue(section, module, "antennaType", "Antenna type");
                AddValue(section, module, "packetInterval", "Packet interval");
                AddValue(section, module, "packetSize", "Packet size");
                AddValue(section, module, "packetResourceCost", "Packet resource cost");
                AddValue(section, module, "requiredResource", "Required resource");
                AddValue(section, module, "antennaPower", "Antenna power");
                AddValue(section, module, "optimumRange", "Optimum range");
                AddValue(section, module, "antennaCombinable", "Combinable");
                AddValue(section, module, "antennaCombinableExponent", "Combinable exponent");
                RemoveEmptySection(data, section);
            }
        }

        private static void AddProbeControlPointSections(StockPartData data, ConfigNode[] modules)
        {
            foreach (ConfigNode module in FindModules(modules, "ModuleProbeControlPoint"))
            {
                StockDetailSection section = NewSection(data, "Probe Control Point");
                AddValue(section, module, "minimumCrew", "Minimum crew");
                AddValue(section, module, "multiHop", "Multi-hop");
                RemoveEmptySection(data, section);
            }
        }

        private static void AddReactionWheelSections(StockPartData data, ConfigNode[] modules)
        {
            foreach (ConfigNode module in FindModules(modules, "ModuleReactionWheel"))
            {
                StockDetailSection section = NewSection(data, "Reaction Wheel");
                AddValue(section, module, "PitchTorque", "Pitch torque");
                AddValue(section, module, "YawTorque", "Yaw torque");
                AddValue(section, module, "RollTorque", "Roll torque");
                AddValue(section, module, "authorityLimiter", "Authority limiter");
                AddModuleResources(section, module, "Resource consumption");
                RemoveEmptySection(data, section);
            }
        }

        private static void AddSasSections(StockPartData data, ConfigNode[] modules)
        {
            foreach (ConfigNode module in FindModules(modules, "ModuleSAS"))
            {
                StockDetailSection section = NewSection(data, "SAS");
                AddValue(section, module, "SASServiceLevel", "Service level");
                RemoveEmptySection(data, section);
            }
        }

        private static void AddStoredResources(StockPartData data, ConfigNode partConfig)
        {
            ConfigNode[] nodes = partConfig.GetNodes("RESOURCE") ?? new ConfigNode[0];
            foreach (ConfigNode node in nodes)
            {
                string name = GetValue(node, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                data.Resources.Add(new StockResourceInfo
                {
                    Name = name,
                    Amount = GetValue(node, "amount"),
                    MaxAmount = GetValue(node, "maxAmount")
                });
            }
        }

        private static IEnumerable<ConfigNode> FindModules(ConfigNode[] modules, string moduleName)
        {
            return (modules ?? new ConfigNode[0]).Where(module =>
                module != null && string.Equals(GetValue(module, "name"), moduleName, StringComparison.OrdinalIgnoreCase));
        }

        private static StockDetailSection NewSection(StockPartData data, string title)
        {
            int existing = data.Sections.Count(existingSection =>
                string.Equals(existingSection.BaseTitle, title, StringComparison.Ordinal));

            StockDetailSection newSection = new StockDetailSection
            {
                BaseTitle = title,
                Title = existing == 0 ? title : title + " " + (existing + 1)
            };
            data.Sections.Add(newSection);
            return newSection;
        }

        private static void RemoveEmptySection(StockPartData data, StockDetailSection section)
        {
            if (section != null && section.Values.Count == 0)
                section.Values.Add(new StockDetailValue { Label = "Available", Value = "Yes" });
        }

        private static void AddValue(StockDetailSection section, ConfigNode node, string fieldName, string label)
        {
            string value = GetValue(node, fieldName);
            if (string.IsNullOrWhiteSpace(value))
                return;

            section.Values.Add(new StockDetailValue { Label = label, Value = NormalizeValue(value) });
        }

        private static void AddModuleResources(StockDetailSection section, ConfigNode module, string label)
        {
            ConfigNode[] resources = module.GetNodes("RESOURCE") ?? new ConfigNode[0];
            foreach (ConfigNode resource in resources)
            {
                string name = GetValue(resource, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                string rate = GetValue(resource, "rate");
                string ratio = GetValue(resource, "ratio");
                string amount = GetValue(resource, "amount");
                string value = name;
                if (!string.IsNullOrEmpty(rate))
                    value += " — " + rate + "/s";
                else if (!string.IsNullOrEmpty(ratio))
                    value += " — ratio " + ratio;
                else if (!string.IsNullOrEmpty(amount))
                    value += " — " + amount;
                section.Values.Add(new StockDetailValue { Label = label, Value = value });
            }
        }

        private static string GetValue(ConfigNode node, string name)
        {
            if (node == null || string.IsNullOrEmpty(name))
                return string.Empty;
            try
            {
                string value = node.GetValue(name);
                return value == null ? string.Empty : value.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            return string.Empty;
        }

        private static string NormalizeValue(string value)
        {
            bool boolean;
            if (bool.TryParse(value, out boolean))
                return boolean ? "Yes" : "No";
            return value;
        }
    }

    internal sealed class StockDetailSection
    {
        public string BaseTitle = string.Empty;
        public string Title = string.Empty;
        public readonly List<StockDetailValue> Values = new List<StockDetailValue>();
    }

    internal sealed class StockDetailValue
    {
        public string Label = string.Empty;
        public string Value = string.Empty;
    }

    internal sealed class StockResourceInfo
    {
        public string Name = string.Empty;
        public string Amount = string.Empty;
        public string MaxAmount = string.Empty;

        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(Amount) && !string.IsNullOrEmpty(MaxAmount))
                    return Amount + " / " + MaxAmount;
                if (!string.IsNullOrEmpty(MaxAmount))
                    return "capacity " + MaxAmount;
                if (!string.IsNullOrEmpty(Amount))
                    return Amount;
                return string.Empty;
            }
        }
    }
}
