using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using BlazorArcade.Helpers;
using BlazorArcade.Models;

namespace LevelDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            var levels = TangramLevelCatalog.GetLevels();
            string outputDir = @"q:\source\TetrisWebAssembly\BlazorArcade\wwwroot\tangram-levels";
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var manifestFiles = new List<string>();

            foreach (var level in levels)
            {
                // Generate a safe filename
                string safeName = level.Name.ToLower().Replace(" ", "_") + ".json";
                string fullPath = Path.Combine(outputDir, safeName);
                
                string json = JsonSerializer.Serialize(level, options);
                File.WriteAllText(fullPath, json);
                
                manifestFiles.Add(safeName);
                Console.WriteLine($"Saved {safeName}");
            }

            string manifestJson = JsonSerializer.Serialize(manifestFiles, options);
            File.WriteAllText(Path.Combine(outputDir, "manifest.json"), manifestJson);
            Console.WriteLine("Saved manifest.json");
        }
    }
}
