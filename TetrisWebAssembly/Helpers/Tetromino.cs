﻿using TetrisWebAssembly.Models;

namespace TetrisWebAssembly.Helpers;
public class Tetromino
{
    private static readonly Random Random = new();

    public Tetromino(List<TetrisBlock> blocks, string color)
    {
        Blocks = blocks;
        Color = color;
    }
    public List<TetrisBlock> Blocks { get; private set; } = new();
    public string Color { get; private set; } = "#FFFFFF"; // Default color
    private static readonly Dictionary<string, (List<(double x, double y)> shape, string color)> TetrominoDefinitions = new()
    {
        { "I", (new List<(double x, double y)> { (0, 0), (1, 0), (2, 0), (3, 0) }, "#00FFFF") }, // Cyan
        { "O", (new List<(double x, double y)> { (0, 0), (1, 0), (0, 1), (1, 1) }, "#FFFF00") }, // Yellow
        { "T", (new List<(double x, double y)> { (0, 0), (1, 0), (2, 0), (1, 1) }, "#800080") }, // Purple
        { "S", (new List<(double x, double y)> { (1, 0), (2, 0), (0, 1), (1, 1) }, "#00FF00") }, // Green
        { "Z", (new List<(double x, double y)> { (0, 0), (1, 0), (1, 1), (2, 1) }, "#FF0000") }, // Red
        { "J", (new List<(double x, double y)> { (0, 0), (0, 1), (1, 1), (2, 1) }, "#0000FF") }, // Blue
        { "L", (new List<(double x, double y)> { (2, 0), (0, 1), (1, 1), (2, 1) }, "#FFA500") }  // Orange
    };
    public static Tetromino GenerateRandom(double blockSize)
    {
        var selected = TetrominoDefinitions.ElementAt(Random.Next(TetrominoDefinitions.Count));
        var shapeOffsets = selected.Value.shape;
        var color = selected.Value.color;

        var blocks = shapeOffsets.Select(offset =>
            new TetrisBlock(offset.x * blockSize, offset.y * blockSize, color)
        ).ToList();

        return new Tetromino(blocks, color);
    }
    public bool CanMove(List<TetrisBlock> existingBlocks, double width, double height, double dx, double dy, double blockSize)
    {
        return Blocks.All(block =>
            block.X + dx * blockSize >= 0 &&
            block.X + dx * blockSize < width * blockSize &&
            block.Y + dy * blockSize < height * blockSize &&
            !existingBlocks.Any(b => b.X == block.X + dx * blockSize && b.Y == block.Y + dy * blockSize));
    }

    public void Move(double dx, double dy, double blockSize)
    {
        Blocks.ForEach(b =>
        {
            b.X += dx * blockSize;
            b.Y += dy * blockSize;
        });
    }
    public void RotateClockwise(List<TetrisBlock> existingBlocks, double width, double height, double blockSize)
    {
        var pivot = Blocks[0];

        var rotatedBlocks = Blocks.Select(block =>
        {
            double relativeX = block.X - pivot.X;
            double relativeY = block.Y - pivot.Y;

            // Apply rotation formula
            double rotatedX = -relativeY + pivot.X;
            double rotatedY = relativeX + pivot.Y;

            return new TetrisBlock(rotatedX, rotatedY, block.Color);
        }).ToList();

        if (rotatedBlocks.All(b =>
            b.X >= 0 && b.X < width * blockSize &&
            b.Y >= 0 && b.Y < height * blockSize &&
            !existingBlocks.Any(existing => existing.X == b.X && existing.Y == b.Y)))
        {
            Blocks = rotatedBlocks;
        }
    }
}
