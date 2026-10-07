using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PartExplorer
{
    internal sealed class ScienceFieldValue
    {
        public string Title;
        public string Value;
    }

    internal sealed class ScienceResourceValue
    {
        public string Name;
        public string Detail;

        public string DisplayText
        {
            get
            {
                if (string.IsNullOrEmpty(Detail))
                    return Name ?? string.Empty;
                return (Name ?? string.Empty) + " (" + Detail + ")";
            }
        }
    }

    internal sealed class ScienceModuleInfo
    {
        public string ModuleName;
        public readonly List<ScienceFieldValue> Fields = new List<ScienceFieldValue>();
        public readonly List<ScienceResourceValue> InputResources = new List<ScienceResourceValue>();
        public readonly List<ScienceResourceValue> OutputResources = new List<ScienceResourceValue>();
    }

    internal static class ScienceModuleData
    {
        private sealed class FieldDefinition
        {
            public int Number;
            public string FieldName;
            public string Title;
            public string TitleField;
        }

        private sealed class ModuleDefinition
        {
            public string ModuleName;
            public readonly List<FieldDefinition> Fields = new List<FieldDefinition>();
        }

        private static readonly Dictionary<string, ModuleDefinition> Definitions =
            new Dictionary<string, ModuleDefinition>(StringComparer.OrdinalIgnoreCase);

        public static int DefinitionCount { get { return Definitions.Count; } }

        public static string ConfigDirectory
        {
            get
            {
                return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "PartExplorer", "PluginData", "ScienceMods");
            }
        }

        public static void LoadDefinitions()
        {
            Definitions.Clear();

            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                    Debug.Log("[PartExplorer] Created ScienceMods directory: " + ConfigDirectory);
                    return;
                }

                foreach (string file in Directory.GetFiles(ConfigDirectory, "*.cfg", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        foreach (Dictionary<string, string> values in ParseBlocks(file))
                        {
                            ModuleDefinition definition = BuildDefinition(values);
                            if (definition == null)
                                continue;

                            if (Definitions.ContainsKey(definition.ModuleName))
                                Debug.LogWarning("[PartExplorer] Duplicate ScienceMods definition for " +
                                    definition.ModuleName + "; replacing the earlier definition with " + Path.GetFileName(file));

                            Definitions[definition.ModuleName] = definition;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[PartExplorer] Unable to load science module config '" + file + "': " + ex.Message);
                    }
                }

                Debug.Log("[PartExplorer] Loaded " + DefinitionCount.ToString(CultureInfo.InvariantCulture) +
                    " science module definition(s) from PluginData/ScienceMods");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PartExplorer] Unable to load ScienceMods definitions: " + ex.Message);
            }
        }

        public static List<ScienceModuleInfo> FromLoadedPart(AvailablePart ap, ConfigNode[] modules)
        {
            var result = new List<ScienceModuleInfo>();
            if (ap == null || modules == null || Definitions.Count == 0)
                return result;

            foreach (ConfigNode module in modules)
            {
                if (module == null)
                    continue;

                string moduleName = module.GetValue("name") ?? string.Empty;
                ModuleDefinition definition;
                if (string.IsNullOrEmpty(moduleName) || !Definitions.TryGetValue(moduleName, out definition))
                    continue;

                var info = new ScienceModuleInfo { ModuleName = moduleName };

                foreach (FieldDefinition field in definition.Fields.OrderBy(f => f.Number))
                {
                    string value = GetModuleValue(ap, module, moduleName, field.FieldName);
                    if (string.IsNullOrEmpty(value))
                        continue;

                    string title = field.Title;
                    if (!string.IsNullOrEmpty(field.TitleField))
                    {
                        string dynamicTitle = GetModuleValue(ap, module, moduleName, field.TitleField);
                        if (!string.IsNullOrEmpty(dynamicTitle))
                            title = dynamicTitle;
                    }

                    if (string.IsNullOrEmpty(title))
                        title = field.FieldName;

                    info.Fields.Add(new ScienceFieldValue
                    {
                        Title = Localize(title),
                        Value = Localize(value)
                    });
                }

                ExtractResources(ap, module, moduleName, info);
                result.Add(info);
            }

            return result;
        }

        private static IEnumerable<Dictionary<string, string>> ParseBlocks(string file)
        {
            var blocks = new List<Dictionary<string, string>>();
            Dictionary<string, string> current = null;

            foreach (string rawLine in File.ReadAllLines(file))
            {
                string line = StripComment(rawLine).Trim();
                if (line.Length == 0)
                    continue;

                int open = line.IndexOf('{');
                if (open >= 0)
                {
                    if (current == null)
                        current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    line = line.Substring(open + 1).Trim();
                    if (line.Length == 0)
                        continue;
                }

                int close = line.IndexOf('}');
                if (close >= 0)
                {
                    string beforeClose = line.Substring(0, close).Trim();
                    if (beforeClose.Length > 0)
                        ParseKeyValue(beforeClose, ref current);
                    FinishBlock(ref current, blocks);
                    continue;
                }

                ParseKeyValue(line, ref current);
            }

            FinishBlock(ref current, blocks);
            return blocks;
        }

        private static void ParseKeyValue(string line, ref Dictionary<string, string> current)
        {
            int equals = line.IndexOf('=');
            if (equals <= 0)
                return;

            if (current == null)
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string key = line.Substring(0, equals).Trim();
            string value = line.Substring(equals + 1).Trim();
            if (key.Length > 0)
                current[key] = value;
        }

        private static void FinishBlock(ref Dictionary<string, string> current, List<Dictionary<string, string>> blocks)
        {
            if (current != null && current.Count > 0)
                blocks.Add(current);
            current = null;
        }

        private static string StripComment(string line)
        {
            if (line == null) return string.Empty;
            int comment = line.IndexOf("//", StringComparison.Ordinal);
            return comment >= 0 ? line.Substring(0, comment) : line;
        }

        private static ModuleDefinition BuildDefinition(Dictionary<string, string> values)
        {
            if (values == null)
                return null;

            string moduleName;
            if (!values.TryGetValue("ModuleName", out moduleName) || string.IsNullOrWhiteSpace(moduleName))
                return null;

            var definition = new ModuleDefinition { ModuleName = moduleName.Trim() };

            foreach (KeyValuePair<string, string> pair in values)
            {
                const string prefix = "expField_";
                if (!pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string suffix = pair.Key.Substring(prefix.Length);
                int number;
                if (!int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    continue;

                if (string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                string title;
                string titleField;
                values.TryGetValue(prefix + number.ToString(CultureInfo.InvariantCulture) + "_Title", out title);
                values.TryGetValue(prefix + number.ToString(CultureInfo.InvariantCulture) + "_TitleField", out titleField);

                definition.Fields.Add(new FieldDefinition
                {
                    Number = number,
                    FieldName = pair.Value.Trim(),
                    Title = (title ?? string.Empty).Trim(),
                    TitleField = (titleField ?? string.Empty).Trim()
                });
            }

            return definition;
        }

        private static string GetModuleValue(AvailablePart ap, ConfigNode node, string moduleName, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return string.Empty;

            string value = node.GetValue(fieldName);
            if (!string.IsNullOrEmpty(value))
                return value;

            // Some KSPFields only exist as initialized values on the prefab PartModule.
            // Use reflection as a fallback so ScienceMods configs can refer to those too.
            try
            {
                if (ap != null && ap.partPrefab != null && ap.partPrefab.Modules != null)
                {
                    foreach (PartModule partModule in ap.partPrefab.Modules)
                    {
                        if (partModule == null || !string.Equals(partModule.moduleName, moduleName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        Type type = partModule.GetType();
                        var field = type.GetField(fieldName,
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (field != null)
                        {
                            object raw = field.GetValue(partModule);
                            if (raw != null)
                                return Convert.ToString(raw, CultureInfo.InvariantCulture);
                        }

                        var property = type.GetProperty(fieldName,
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (property != null && property.CanRead)
                        {
                            object raw = property.GetValue(partModule, null);
                            if (raw != null)
                                return Convert.ToString(raw, CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        private static void ExtractResources(AvailablePart ap, ConfigNode module, string moduleName, ScienceModuleInfo info)
        {
            if (module == null || info == null)
                return;

            AddResourceNodes(module.GetNodes("INPUT_RESOURCE"), true, info);
            AddResourceNodes(module.GetNodes("INPUTRESOURCE"), true, info);
            AddResourceNodes(module.GetNodes("OUTPUT_RESOURCE"), false, info);
            AddResourceNodes(module.GetNodes("OUTPUTRESOURCE"), false, info);

            // Stock ModuleScienceLab uses RESOURCE_PROCESS for resources consumed
            // while operating the lab.
            AddResourceNodes(module.GetNodes("RESOURCE_PROCESS"), true, info);

            foreach (ConfigNode child in module.GetNodes("RESOURCE"))
            {
                if (child == null)
                    continue;

                string direction = FirstValue(child, "type", "direction", "Direction");
                if (string.Equals(direction, "output", StringComparison.OrdinalIgnoreCase))
                    AddResourceNode(child, false, info);
                else
                {
                    // Several science mods (notably DMagic Orbital Science) use
                    // an unlabeled RESOURCE node inside a PartModule for power
                    // consumption.  Treat an unlabeled RESOURCE as an input.
                    AddResourceNode(child, true, info);
                }
            }

            // Common field-based resource declarations used by science mods.
            // These do not use child RESOURCE nodes, so recognize them here.
            AddFieldResource(module, "resourceToUse", "resourceCost", true, info);
            AddFieldResource(module, "experimentResource", "resourceCost", true, info);
            AddFieldResource(module, "reactant", "reactantPerProduct", true, info);
            AddFieldResource(module, "product", "productPerHour", false, info);

            // Common ElectricCharge fields used by science modules that consume
            // power directly in code rather than through a child resource node.
            AddRuntimeResource(ap, module, moduleName, "ElectricCharge", "powerRequirement", "rate", true, info);
            AddRuntimeResource(ap, module, moduleName, "ElectricCharge", "powerUsage", "rate", true, info);
            AddRuntimeResource(ap, module, moduleName, "ElectricCharge", "EC_COST", "cost", true, info);
            AddRuntimeResource(ap, module, moduleName, "ElectricCharge", "EC_PER_SAMPLE", "per sample", true, info);
        }

        private static void AddRuntimeResource(AvailablePart ap, ConfigNode module, string moduleName,
            string resourceName, string valueField, string detailLabel, bool input, ScienceModuleInfo info)
        {
            if (module == null || info == null || string.IsNullOrEmpty(valueField))
                return;

            string value = GetModuleValue(ap, module, moduleName, valueField);
            if (string.IsNullOrEmpty(value))
                return;

            var resource = new ScienceResourceValue
            {
                Name = Localize(resourceName),
                Detail = (string.IsNullOrEmpty(detailLabel) ? valueField : detailLabel) + " " + value
            };

            List<ScienceResourceValue> target = input ? info.InputResources : info.OutputResources;
            if (!target.Any(r => string.Equals(r.Name, resource.Name, StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(r.Detail, resource.Detail, StringComparison.OrdinalIgnoreCase)))
                target.Add(resource);
        }

        private static void AddFieldResource(ConfigNode module, string resourceField, string detailField,
            bool input, ScienceModuleInfo info)
        {
            if (module == null || info == null)
                return;

            string resourceName = module.GetValue(resourceField);
            if (string.IsNullOrEmpty(resourceName))
                return;

            string detailValue = string.IsNullOrEmpty(detailField) ? string.Empty : module.GetValue(detailField);
            var resource = new ScienceResourceValue
            {
                Name = Localize(resourceName),
                Detail = string.IsNullOrEmpty(detailValue) ? string.Empty : detailField + " " + detailValue
            };

            List<ScienceResourceValue> target = input ? info.InputResources : info.OutputResources;
            if (!target.Any(r => string.Equals(r.Name, resource.Name, StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(r.Detail, resource.Detail, StringComparison.OrdinalIgnoreCase)))
                target.Add(resource);
        }

        private static void AddResourceNodes(ConfigNode[] nodes, bool input, ScienceModuleInfo info)
        {
            if (nodes == null)
                return;
            foreach (ConfigNode node in nodes)
                AddResourceNode(node, input, info);
        }

        private static void AddResourceNode(ConfigNode node, bool input, ScienceModuleInfo info)
        {
            if (node == null || info == null)
                return;

            string resourceName = FirstValue(node, "ResourceName", "resourceName", "name", "resource");
            if (string.IsNullOrEmpty(resourceName))
                resourceName = "(unnamed resource)";

            var resource = new ScienceResourceValue
            {
                Name = Localize(resourceName),
                Detail = ResourceDetail(node)
            };

            List<ScienceResourceValue> target = input ? info.InputResources : info.OutputResources;
            if (!target.Any(r => string.Equals(r.Name, resource.Name, StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(r.Detail, resource.Detail, StringComparison.OrdinalIgnoreCase)))
                target.Add(resource);
        }

        private static string ResourceDetail(ConfigNode node)
        {
            string[] names = { "Ratio", "ratio", "rate", "Rate", "amount", "Amount", "ResourceRatio", "resourceAmount" };
            foreach (string name in names)
            {
                string value = node.GetValue(name);
                if (!string.IsNullOrEmpty(value))
                    return name + " " + value;
            }
            return string.Empty;
        }

        private static string FirstValue(ConfigNode node, params string[] names)
        {
            foreach (string name in names)
            {
                string value = node.GetValue(name);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

        private static string Localize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (!value.StartsWith("#", StringComparison.Ordinal)) return value;
            try { return KSP.Localization.Localizer.Format(value); }
            catch { return value; }
        }
    }
}
