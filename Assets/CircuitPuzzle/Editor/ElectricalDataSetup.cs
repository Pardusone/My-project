using System;
using CircuitPuzzle.Components;
using UnityEditor;
using UnityEngine;

namespace CircuitPuzzle.Editor
{
    /// <summary>Authors missing electrical metadata without replacing existing component assets.</summary>
    public static class ElectricalDataSetup
    {
        [MenuItem("Tools/Circuit Puzzle/Setup Phase 3 Electrical Data")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring component data.");

            foreach (string guid in AssetDatabase.FindAssets("t:ComponentDefinition",
                new[] { "Assets/CircuitPuzzle/Data/Components" }))
            {
                ComponentDefinition definition = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (definition.Electrical != null &&
                    (!string.IsNullOrEmpty(definition.Electrical.FromTerminalId) ||
                     !string.IsNullOrEmpty(definition.Electrical.ToTerminalId))) continue;
                ElectricalParameters parameters;
                switch (definition.ComponentType)
                {
                    case ComponentType.Battery:
                        parameters = new ElectricalParameters("positive", "negative", voltageVolts: 5);
                        break;
                    case ComponentType.Resistor:
                        parameters = new ElectricalParameters("a", "b", resistanceOhms: 330);
                        break;
                    case ComponentType.Led:
                        parameters = new ElectricalParameters("anode", "cathode");
                        break;
                    default:
                        parameters = new ElectricalParameters("a", "b");
                        break;
                }
                if (definition.FindTerminal(parameters.FromTerminalId) == null ||
                    definition.FindTerminal(parameters.ToTerminalId) == null)
                    throw new InvalidOperationException("Electrical terminal mapping needs authoring for " + definition.name);
                definition.ConfigureElectrical(parameters);
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
            }
            Debug.Log("Phase 3 electrical data ready. Battery: 5 V; resistor: 330 ohms. Existing authored values preserved.");
        }
    }
}
