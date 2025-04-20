# TetrisWebAssembly

TetrisWebAssembly is a Blazor WebAssembly project that implements a Tetris game and additional features like a Sudoku solver. This project is built using .NET 9 and leverages modern web technologies to deliver a rich, interactive experience directly in the browser.

## Features
- **Tetris Game**: A fully functional Tetris game with responsive controls and dynamic gameplay.
- **Sudoku Solver**: A Sudoku grid with solving capabilities, including solving individual cells or the entire puzzle.
- **Offline Support**: Service worker integration for offline functionality.
- **Responsive Design**: Optimized for both desktop and mobile devices.

---

## Prerequisites
Before setting up the project, ensure you have the following installed:
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A modern web browser (e.g., Chrome, Edge, or Firefox)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **ASP.NET and web development** workload installed.

---

## Setup Instructions

1. **Clone the Repository**
   ```bash
   git clone https://github.com/your-repo/TetrisWebAssembly.git
   cd TetrisWebAssembly
   ```

2. **Restore Dependencies**
   Run the following command to restore all required NuGet packages:
   ```bash
   dotnet restore
   ```

3. **Build the Project**
   Compile the project using:
   ```bash
   dotnet build
   ```

4. **Run the Application**
   Start the Blazor WebAssembly application:
   ```bash
   dotnet run --project TetrisWebAssembly
   ```
   This will launch the application and host it locally. Open your browser and navigate to `http://localhost:5000`.

---

## Project Structure
- **`TetrisWebAssembly`**: The main Blazor WebAssembly project containing the Tetris game, Sudoku solver, and UI components.
- **`Sudoku.Tests`**: A test project for validating the Sudoku solver and other logic using xUnit and bUnit.

### Key Files
- `TetrisWebAssembly/Pages/Tetris.razor`: The Tetris game page.
- `TetrisWebAssembly/Pages/SudokuGrid.razor`: The Sudoku solver page.
- `TetrisWebAssembly/GameLogic`: Contains the core game logic for Tetris and Sudoku.
- `TetrisWebAssembly/wwwroot`: Static assets like CSS, JavaScript, and service worker files.

---

## Testing
The project includes unit tests for the Sudoku solver and other components. To run the tests, use:
dotnet test

---

## Deployment
To publish the application for deployment:
1. Run the following command:
   ```bash
   dotnet publish -c Release -o publish
   ```
2. The output will be located in the `publish` directory. You can host the application on any static web server or deploy it to platforms like Azure Static Web Apps or GitHub Pages.

---

## Contributing
Contributions are welcome! Feel free to open issues or submit pull requests to improve the project.

---

## License
This project is licensed under the [MIT License](./LICENSE).

---

## Acknowledgments
- Built with [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor).
- Inspired by classic Tetris, Breakout and Sudoku games.