using Bunit;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetrisWebAssembly.Pages;

namespace Sudoku.Tests;
public class SudokuGridTests : TestContext
{
    [Fact]
    public void SudokuGrid_RendersCorrectNumberOfCells()
    {
        // Arrange & Act: Render the SudokuGrid component.
        var cut = RenderComponent<SudokuGrid>();

        // Assert: Verify that 81 input elements (for 9x9 grid) are rendered.
        Assert.Equal(81, cut.FindAll("input").Count);
    }

    [Fact]
    public void UserCanInputNumbersInCells()
    {
        // Arrange
        var cut = RenderComponent<SudokuGrid>();

        // Act
        var firstCell = cut.Find("input");
        firstCell.Input("5");

        // Assert
        Assert.Equal("5", firstCell.GetAttribute("value"));
    }
}
