namespace TetrisWebAssembly.Models;

public class TetrisBlock
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Color { get; set; } // Store the color of the block

    public TetrisBlock(double x, double y, string color)
    {
        X = x;
        Y = y;
        Color = color;
    }
}
