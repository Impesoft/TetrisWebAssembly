# BlazorArcade

**BlazorArcade** is a comprehensive, feature-rich web application built with Blazor WebAssembly (.NET 9) that brings classic retro, puzzle, and arcade games directly to your browser with offline PWA support and modern, responsive design.

---

## 🎮 Featured Games & Tools

- **Tetris**: Classic tile-matching puzzle with responsive controls and ghost piece preview.
- **Breakout**: Arcade brick-breaker with paddle control, power-ups, and level progressions.
- **Sudoku**: Interactive grid generator with full solver capabilities (step solver & instant solve).
- **Pacman**: Classic maze runner featuring ghost AI, power pellets, and score tracking.
- **2048**: Sliding tile puzzle with smooth animations and high-score tracking.
- **Platformer**: Action platforming game engine with jumping physics and obstacle courses.
- **Donkey Kong**: Arcade platformer clone featuring jump mechanics, ladders, and barrels.
- **King's Valley**: Retro MSX puzzle-platformer clone with pyramid maze navigation and item collection.
- **Chess**: Classic 2-player chess board implementation with full piece movement and Staunton visuals.
- **Connect 4**: Drop-piece strategy game supporting 2-player local gameplay.
- **Bubble Shooter**: Match-3 bubble launcher with angle preview line and physics bouncing.
- **Eggerland / Lolo**: Grid-based puzzle game with enemy AI, egg shots, and map obstacles.
- **Tangram Master**: Geometric shape-fitting puzzle with SVG pixel-checking validation algorithms.
- **Block Dude**: TI-83 calculator classic puzzle-platformer where you pick up and stack blocks to escape.
- **Flappy Bird**: Arcade side-scroller with physics tap-jumping and pipe collision detection.
- **Block Blast!**: Authentic 8x8 block puzzle with pre-placed starter blocks, combo streak multipliers, and Web Audio sound synthesis.

---

## 🚀 Key Features

- **PWA & Offline Support**: Service worker integration allowing full offline playability.
- **Web Audio API Synthesis**: Retro sound effects generated directly via JavaScript Web Audio API.
- **Responsive Layouts**: Glassmorphic modern design optimized for desktop, tablet, and mobile touch screens.
- **Local Storage**: High-score persistence across sessions.

---

## 🛠️ Prerequisites

Ensure you have the following installed:
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A modern web browser (e.g., Chrome, Edge, Safari, or Firefox)

---

## 🏃 Setup & Run Instructions

1. **Clone the Repository**
   ```bash
   git clone https://github.com/your-repo/BlazorArcade.git
   cd BlazorArcade
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run the Application**
   ```bash
   dotnet run --project BlazorArcade
   ```
   Open your browser and navigate to `http://localhost:5000` (or `https://localhost:5001`).

---

## 🧪 Testing

Run the test suite using:
```bash
dotnet test
```

---

## 📦 Deployment

To publish the application for static hosting (e.g., GitHub Pages, Azure Static Web Apps, Cloudflare Pages):
```bash
dotnet publish -c Release -o publish
```

---

## 📄 License

This project is licensed under the [MIT License](./LICENSE).