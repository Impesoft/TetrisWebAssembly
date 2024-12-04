namespace TetrisWebAssembly.Helpers;

public static class SudokuUtils
{
    public static bool IsSolved(List<List<string>> puzzle)
    {
        for (int i = 0; i < 9; i++)
        {
            // Check each row and column
            if (!IsValidSet(GetRow(puzzle, i)) || !IsValidSet(GetColumn(puzzle, i)))
                return false;
        }

        // Check each 3x3 sub-grid
        for (int row = 0; row < 9; row += 3)
        {
            for (int col = 0; col < 9; col += 3)
            {
                if (!IsValidSet(GetSubGrid(puzzle, row, col)))
                    return false;
            }
        }

        return true;
    }

    private static bool IsValidSet(IEnumerable<string> values)
    {
        var set = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToHashSet();
        return set.Count == 9 && set.All(v => int.TryParse(v , out _));
    }

    private static IEnumerable<string> GetRow(List<List<string>> puzzle, int row)
    {
        for (int col = 0; col < 9; col++)
            yield return puzzle[row][col];
    }

    private static IEnumerable<string> GetColumn(List<List<string>> puzzle, int col)
    {
        for (int row = 0; row < 9; row++)
            yield return puzzle[row][col];
    }

    private static IEnumerable<string> GetSubGrid(List<List<string>> puzzle, int startRow, int startCol)
    {
        for (int row = startRow; row < startRow + 3; row++)
        {
            for (int col = startCol; col < startCol + 3; col++)
                yield return puzzle[row][col];
        }
    }
}
