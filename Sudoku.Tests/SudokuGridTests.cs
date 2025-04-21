using Bunit;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetrisWebAssembly.Pages;
using TetrisWebAssembly.GameLogic;
using AngleSharp.Diffing.Extensions;
using AngleSharp.Css.Values;

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
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("eval", _ => true);

        var cut = ctx.RenderComponent<SudokuGrid>();
        // Act
        var firstCell = cut.Find("input");
        firstCell.Input("5");

        // Assert
        Assert.Equal("5", firstCell.GetAttribute("value"));
    }
    [Fact]
    public void PrefilledCellsAreDisabled()
    {
        // Arrange
        using var ctx = new TestContext();
        // Setup JSInterop to handle eval call
        ctx.JSInterop.SetupVoid("eval", "document.getElementById('bg').play()");
        var cut = ctx.RenderComponent<SudokuGrid>();
        cut.Find("select").Change(Difficulty.Easy);

        // Act
        // Find all input elements that are disabled (prefilled cells)
        // Note: The actual implementation of the SudokuGrid component should have a way to mark prefilled cells as disabled.

        var prefilledCells = cut.FindAll("input[disabled]");
        // Assert
        Assert.NotEmpty(prefilledCells);
    }
    [Fact]
    public void PrefilledInputs_ShouldBeDisabled()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("eval", _ => true);

        var cut = ctx.RenderComponent<SudokuGrid>();
        cut.Find("select").Change(Difficulty.Easy);

        var allInputs = cut.FindAll("input");

        foreach (var input in allInputs)
        {
            var hasValue = input.TryGetAttrValue("value", out string value) && !string.IsNullOrWhiteSpace(value);

            if (hasValue)
            {
                Assert.True(input.HasAttribute("disabled"), $"Prefilled input with value '{value}' should be disabled.");
            }
            else
            {
                Assert.False(input.HasAttribute("disabled"), "Empty cell should be editable.");
            }
        }
    }

    [Fact]
    public void UserCanModifyEmptyCells()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("eval", _ => true);

        var cut = ctx.RenderComponent<SudokuGrid>(); var emptyCell = cut.Find("input:not([disabled])");
        // Act
        emptyCell.Input("5");
        // Assert
        Assert.Equal("5", emptyCell.GetAttribute("value"));
    }
    [Fact]
    public void CellValueIsUpdatedOnInput()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("eval", _ => true);

        var cut = ctx.RenderComponent<SudokuGrid>(); var emptyCell = cut.Find("input:not([disabled])");
        // Act
        emptyCell.Input("5");
        // Assert
        Assert.Equal("5", emptyCell.GetAttribute("value"));
    }
    [Fact]
    public void CellValueIsClearedOnEmptyInput()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("eval", _ => true);

        var cut = ctx.RenderComponent<SudokuGrid>(); var emptyCell = cut.Find("input:not([disabled])");
        // Act
        emptyCell.Input("");
        // Assert
        Assert.Equal("", emptyCell.GetAttribute("value"));
    }


    [Fact]
    public void CellValueIsNotUpdatedOnNonNumericInput()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.JSInterop.SetupVoid("eval", _ => true);

        var cut = ctx.RenderComponent<SudokuGrid>(); var emptyCell = cut.Find("input:not([disabled])");
        // Act
        emptyCell.Input("a");
        // Assert
        Assert.NotEqual("a", emptyCell.GetAttribute("value"));
    }
    [Fact]
    public void FindNextEmptyField_ReturnsCorrectCell()
    {
        // Arrange
        var cut = RenderComponent<SudokuGrid>();
        var grid = new List<List<string>>()
        {
            new List<string> { "5", "3", "", "6", "", "", "", "", "" },
            new List<string> { "6", "", "", "1", "9", "5", "", "", "" },
            new List<string> { "", "9", "8", "", "", "", "", "6", "" },
            new List<string> { "8", "", "", "", "6", "", "", "", "3" },
            new List<string> { "4", "", "", "8", "", "3", "", "", "1" },
            new List<string> { "7", "", "", "", "2", "", "", "", "6" },
            new List<string> { "", "6", "", "", "", "", "2", "8", "" },
            new List<string> { "", "", "", "4", "1", "9", "", "", "5" },
            new List<string> { "", "", "", "", "8", "", "", "7", "9" }
        };
        var expectedCell = (0, 2); // The first empty cell
        // Act
        var result = cut.Instance.FindNextEmptyField(grid);
        // Assert
        Assert.Equal(expectedCell, result);
    }


}
