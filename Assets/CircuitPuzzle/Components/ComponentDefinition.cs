using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircuitPuzzle.Components
{
    public enum ComponentType { Battery, Resistor, Led, Switch, LightBulb }

    /// <summary>Authored construction, display and electrical data. Runtime behavior lives separately.</summary>
    [CreateAssetMenu(menuName = "Circuit Puzzle/Component Definition")]
    public sealed class ComponentDefinition : ScriptableObject
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayName;
        [SerializeField] private ComponentType componentType;
        [SerializeField, TextArea] private string description;
        [SerializeField, TextArea] private string educationalDescription;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private Vector2Int footprint = new Vector2Int(2, 1);
        [SerializeField] private int[] allowedRotations = { 0, 1, 2, 3 };
        [SerializeField] private TerminalDefinition[] terminals = Array.Empty<TerminalDefinition>();
        [SerializeField] private ElectricalParameters electrical;

        private IReadOnlyList<int> rotationsView;
        private IReadOnlyList<TerminalDefinition> terminalsView;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public ComponentType ComponentType => componentType;
        public string Description => description;
        public string EducationalDescription => educationalDescription;
        public Sprite Sprite => sprite;
        public Color Tint => tint;
        public ElectricalParameters Electrical => electrical;
        public Vector2Int Footprint => footprint;
        public IReadOnlyList<int> AllowedRotations => rotationsView ??=
            Array.AsReadOnly(allowedRotations ?? Array.Empty<int>());
        public IReadOnlyList<TerminalDefinition> Terminals => terminalsView ??=
            Array.AsReadOnly(terminals ?? Array.Empty<TerminalDefinition>());

        public TerminalDefinition FindTerminal(string terminalId)
        {
            foreach (TerminalDefinition terminal in Terminals)
                if (terminal != null && terminal.Id == terminalId)
                    return terminal;
            return null;
        }

        public bool AllowsRotation(int quarterTurns)
        {
            int normalized = ((quarterTurns % 4) + 4) % 4;
            foreach (int rotation in AllowedRotations)
                if (rotation == normalized)
                    return true;
            return false;
        }

        /// <summary>For asset authoring. Never store per-instance measurements or switch state here.</summary>
        public void ConfigureElectrical(ElectricalParameters parameters)
        {
            electrical = parameters?.Copy();
        }

        /// <summary>For asset authoring/setup. Treat definitions as read-only during a session.</summary>
        public void Configure(string stableId, string displayName, ComponentType componentType,
            string description, string educationalDescription, Sprite sprite, Color tint,
            Vector2Int footprint, int[] allowedRotations, TerminalDefinition[] terminals)
        {
            this.stableId = stableId;
            this.displayName = displayName;
            this.componentType = componentType;
            this.description = description;
            this.educationalDescription = educationalDescription;
            this.sprite = sprite;
            this.tint = tint;
            this.footprint = footprint;
            this.allowedRotations = (int[])(allowedRotations ?? Array.Empty<int>()).Clone();
            this.terminals = (TerminalDefinition[])(terminals ?? Array.Empty<TerminalDefinition>()).Clone();
            OnValidate();
        }

        private void OnValidate()
        {
            rotationsView = null;
            terminalsView = null;
        }
    }

    [Serializable]
    public sealed class TerminalDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string shortLabel;
        [SerializeField] private Vector2 localPosition;
        [SerializeField] private string type;

        public string Id => id;
        public string DisplayName => displayName;
        public string ShortLabel => shortLabel;
        public Vector2 LocalPosition => localPosition;
        public string Type => type;

        public TerminalDefinition(string id, string displayName, string shortLabel,
            Vector2 localPosition, string type = "terminal")
        {
            this.id = id;
            this.displayName = displayName;
            this.shortLabel = shortLabel;
            this.localPosition = localPosition;
            this.type = type;
        }
    }
}
