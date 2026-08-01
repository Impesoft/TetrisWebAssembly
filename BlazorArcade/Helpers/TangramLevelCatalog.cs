using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BlazorArcade.Models;

namespace BlazorArcade.Helpers
{
    public static class TangramLevelCatalog
    {
        public static List<TangramPiece> CreateStandardTans()
        {
            return new List<TangramPiece>
            {
                new TangramPiece(
                    TangramPieceType.LargeTriangle1,
                    "lt1",
                    "#FF5722",
                    "Large Triangle A",
                    new Point2D[] { new Point2D(-200.0/3.0, -200.0/3.0), new Point2D(400.0/3.0, -200.0/3.0), new Point2D(-200.0/3.0, 400.0/3.0) }
                ),
                new TangramPiece(
                    TangramPieceType.LargeTriangle2,
                    "lt2",
                    "#3F51B5",
                    "Large Triangle B",
                    new Point2D[] { new Point2D(-200.0/3.0, -200.0/3.0), new Point2D(400.0/3.0, -200.0/3.0), new Point2D(-200.0/3.0, 400.0/3.0) }
                ),
                new TangramPiece(
                    TangramPieceType.MediumTriangle,
                    "mt",
                    "#9C27B0",
                    "Medium Triangle",
                    new Point2D[] { new Point2D(-141.4213562373095/3.0, -141.4213562373095/3.0), new Point2D(282.842712474619/3.0, -141.4213562373095/3.0), new Point2D(-141.4213562373095/3.0, 282.842712474619/3.0) }
                ),
                new TangramPiece(
                    TangramPieceType.SmallTriangle1,
                    "st1",
                    "#4CAF50",
                    "Small Triangle A",
                    new Point2D[] { new Point2D(-100.0/3.0, -100.0/3.0), new Point2D(200.0/3.0, -100.0/3.0), new Point2D(-100.0/3.0, 200.0/3.0) }
                ),
                new TangramPiece(
                    TangramPieceType.SmallTriangle2,
                    "st2",
                    "#FFEB3B",
                    "Small Triangle B",
                    new Point2D[] { new Point2D(-100.0/3.0, -100.0/3.0), new Point2D(200.0/3.0, -100.0/3.0), new Point2D(-100.0/3.0, 200.0/3.0) }
                ),
                new TangramPiece(
                    TangramPieceType.Square,
                    "sq",
                    "#00BCD4",
                    "Square",
                    new Point2D[] { new Point2D(-50, -50), new Point2D(50, -50), new Point2D(50, 50), new Point2D(-50, 50) }
                ),
                new TangramPiece(
                    TangramPieceType.Parallelogram,
                    "para",
                    "#E91E63",
                    "Parallelogram",
                    new Point2D[] { new Point2D(-100, -50), new Point2D(0, -50), new Point2D(100, 50), new Point2D(0, 50) }
                )
            };
        }

        public static async Task<List<TangramLevel>> LoadLevelsAsync(HttpClient http)
        {
            var levels = new List<TangramLevel>();
            try
            {
                var manifest = await http.GetFromJsonAsync<List<string>>("tangram-levels/manifest.json");
                if (manifest != null)
                {
                    foreach (var file in manifest)
                    {
                        try
                        {
                            var level = await http.GetFromJsonAsync<TangramLevel>($"tangram-levels/{file}");
                            if (level != null)
                            {
                                levels.Add(level);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load level {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load manifest: {ex.Message}");
            }
            return levels;
        }
    }
}
