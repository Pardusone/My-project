# Circuit Puzzle Development Rules

Read PROJECT_SPEC.md completely before making architectural or gameplay decisions.

## Project
Engine: Unity 6
Language: C#
Target Platforms: Android and iOS
Orientation: Portrait
Game Type: Educational electronics puzzle game

## Core Rules
- Gameplay functionality has priority over visual polish.
- Keep circuit simulation independent from presentation.
- Do not hard-code puzzle solutions.
- Evaluate puzzles using the actual electrical circuit built by the player.
- Keep systems modular and testable.
- Avoid giant MonoBehaviour classes.
- Prefer data-driven level and component definitions.
- Prefer ScriptableObjects where appropriate.
- Avoid unnecessary third-party dependencies.
- Placeholder graphics are acceptable.
- Fix compile errors before continuing to another major feature.
- Do not modify unrelated working systems unnecessarily.

## Development Workflow
Before implementing a feature:
1. Inspect the existing project.
2. Read the relevant section of PROJECT_SPEC.md.
3. Reuse existing architecture where appropriate.
4. Implement the feature directly in the project.
5. Check for compilation problems.
6. Keep the project playable.

## Architecture
Keep these systems separated:
- Circuit Editor
- Circuit Graph / Data Model
- Electrical Simulation
- Component Presentation
- Level Objectives
- Diagnostics
- UI
- Save / Progression

Simulation should output data such as:
- node voltages
- branch currents
- component states
- diagnostics

Presentation and UI should consume those results.

## Simulation Scope
Do not build a full SPICE simulator.

Build a simplified but educationally correct high-school-level DC circuit simulation system that can later be expanded.