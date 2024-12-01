namespace TetrisWebAssembly.Models;

public class TetrisBlock
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; } // Store the color of the block

    public TetrisBlock(int x, int y, string color)
    {
        X = x;
        Y = y;
        Color = color;
    }
}
