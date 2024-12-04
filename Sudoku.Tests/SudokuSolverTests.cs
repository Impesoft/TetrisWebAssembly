using Bunit;
using TetrisWebAssembly.GameLogic;
using TetrisWebAssembly.Helpers;
using TetrisWebAssembly.Pages;

namespace Sudoku.Tests;

public class SudokuSolverTests
{
    [Fact]
    public void Solve_ValidPuzzle_ReturnsSolvedPuzzle()
    {
        var puzzle = new List<List<string>>()
        {
            new List<string> { "5", "3", "", "", "7", "", "", "", "" },
            new List<string> { "6", "", "", "1", "9", "5", "", "", "" },
            new List<string> { "", "9", "8", "", "", "", "", "6", "" },
            new List<string> { "8", "", "", "", "6", "", "", "", "3" },
            new List<string> { "4", "", "", "8", "", "3", "", "", "1" },
            new List<string> { "7", "", "", "", "2", "", "", "", "6" },
            new List<string> { "", "6", "", "", "", "", "2", "8", "" },
            new List<string> { "", "", "", "4", "1", "9", "", "", "5" },
            new List<string> { "", "", "", "", "8", "", "", "7", "9" }
        };

        var solver = new SudokuSolver();

        var result = solver.Solve(puzzle);

        Assert.True(SudokuUtils.IsSolved(result));
    }

    public class SudokuUtilsTests
    {
        [Fact]
        public void IsSolved_ValidSolvedPuzzle_ReturnsTrue()
        {
            var solvedPuzzle = new List<List<string>>()
                        {
                            new List<string> { "5", "3", "4", "6", "7", "8", "9", "1", "2" },
                            new List<string> { "6", "7", "2", "1", "9", "5", "3", "4", "8" },
                            new List<string> { "1", "9", "8", "3", "4", "2", "5", "6", "7" },
                            new List<string> { "8", "5", "9", "7", "6", "1", "4", "2", "3" },
                            new List<string> { "4", "2", "6", "8", "5", "3", "7", "9", "1" },
                            new List<string> { "7", "1", "3", "9", "2", "4", "8", "5", "6" },
                            new List<string> { "9", "6", "1", "5", "3", "7", "2", "8", "4" },
                            new List<string> { "2", "8", "7", "4", "1", "9", "6", "3", "5" },
                            new List<string> { "3", "4", "5", "2", "8", "6", "1", "7", "9" }
                        };

            Assert.True(SudokuUtils.IsSolved(solvedPuzzle));
        }

        [Fact]
        public void IsSolved_UnsolvedPuzzle_ReturnsFalse()
        {
            var unsolvedPuzzle = new List<List<string>>()
                        {
                            new List<string> { "5", "3", "4", "6", "7", "8", "9", "1", "2" },
                            new List<string> { "6", "7", "2", "1", "9", "5", "3", "4", "8" },
                            new List<string> { "1", "9", "8", "3", "4", "2", "5", "6", "7" },
                            new List<string> { "8", "5", "9", "7", "6", "1", "4", "2", "3" },
                            new List<string> { "4", "2", "6", "8", "5", "3", "7", "9", "1" },
                            new List<string> { "7", "1", "3", "9", "2", "4", "8", "5", "6" },
                            new List<string> { "9", "6", "1", "5", "3", "7", "2", "8", "4" },
                            new List<string> { "2", "8", "7", "4", "1", "9", "6", "3", "5" },
                            new List<string> { "", "", "", "", "", "", "", "", "" }
                        };

            Assert.False(SudokuUtils.IsSolved(unsolvedPuzzle));
        }

        [Fact]
        public void IsSolved_InvalidPuzzle_ReturnsFalse()
        {
            // Duplicate in the last row
            var invalidPuzzle = new List<List<string>>()
                        {
                            new List<string> { "5", "3", "4", "6", "7", "8", "9", "1", "2" },
                            new List<string> { "6", "7", "2", "1", "9", "5", "3", "4", "8" },
                            new List<string> { "1", "9", "8", "3", "4", "2", "5", "6", "7" },
                            new List<string> { "8", "5", "9", "7", "6", "1", "4", "2", "3" },
                            new List<string> { "4", "2", "6", "8", "5", "3", "7", "9", "1" },
                            new List<string> { "7", "1", "3", "9", "2", "4", "8", "5", "6" },
                            new List<string> { "9", "6", "1", "5", "3", "7", "2", "8", "4" },
                            new List<string> { "2", "8", "7", "4", "1", "9", "6", "3", "5" },
                            new List<string> { "3", "4", "5", "2", "8", "6", "1", "7", "7" }
                        };

            Assert.False(SudokuUtils.IsSolved(invalidPuzzle));
        }
    }

}
