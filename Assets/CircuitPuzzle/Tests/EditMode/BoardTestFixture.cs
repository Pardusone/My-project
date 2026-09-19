using System.Collections.Generic;
using CircuitPuzzle.Components;
using NUnit.Framework;
using UnityEngine;

namespace CircuitPuzzle.Tests
{
    public abstract class BoardTestFixture
    {
        private readonly List<ComponentDefinition> definitions = new List<ComponentDefinition>();

        protected ComponentDefinition CreateDefinition(int width = 2, int height = 1, int[] rotations = null)
        {
            ComponentDefinition definition = ScriptableObject.CreateInstance<ComponentDefinition>();
            definitions.Add(definition);
            definition.Configure("test.part", "Test Part", ComponentType.Resistor, "", "", null,
                Color.white, new Vector2Int(width, height), rotations ?? new[] { 0, 1, 2, 3 },
                new[]
                {
                    new TerminalDefinition("a", "Terminal A", "A", new Vector2(0, height * 0.5f)),
                    new TerminalDefinition("b", "Terminal B", "B", new Vector2(width, height * 0.5f))
                });
            return definition;
        }

        [TearDown]
        public void TearDownDefinitions()
        {
            foreach (ComponentDefinition definition in definitions)
                Object.DestroyImmediate(definition);
            definitions.Clear();
        }
    }
}
