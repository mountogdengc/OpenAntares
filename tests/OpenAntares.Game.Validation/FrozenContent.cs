using System;
using System.IO;
using System.Text.Json;
using Godot;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Saves;
namespace OpenAntares.GameValidation;
internal static class FrozenContent
{
    internal static ContentSet Rules5(){var result=ContentLoader.Load(new[]{new ContentSource("frozen-rules5",File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/rules5-content.json")))},requireSpecies:true,requireRivals:true,requireDevelopment:true);return result.Content??throw new InvalidOperationException(string.Join("; ",result.Errors));}
    internal static ContentSet Rules4(){var result=ContentLoader.Load(new[]{new ContentSource("frozen-rules4",File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/rules4-content.json")))},requireSpecies:true,requireRivals:true);return result.Content??throw new InvalidOperationException(string.Join("; ",result.Errors));}
    internal static ContentSet Rules3()=>SaveGame.ReadFile(ProjectSettings.GlobalizePath("res://tests/OpenAntares.Simulation.Tests/Fixtures/legacy-rules3-v4-save.json")).Game!.Content;
}
