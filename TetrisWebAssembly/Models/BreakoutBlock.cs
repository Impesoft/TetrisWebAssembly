namespace TetrisWebAssembly.Models;

public class BreakoutBlock
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Color { get; set; } = "white";
    public int HitsRemaining { get; set; }
}

