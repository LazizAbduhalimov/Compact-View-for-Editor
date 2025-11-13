# Unity Editor Window Controls

A comprehensive Unity Editor extension that provides advanced window management capabilities including title bar hiding, custom menu bar, window controls, and drag functionality.

## Features

### 🎛️ Window Controls
- **Minimize/Maximize/Close buttons** - macOS-style colored circle buttons in the toolbar
- **Safety confirmation dialog** - Prevents accidental editor closure
- **Visual feedback** - Hover and click effects on buttons

### 📋 Custom Menu Bar
- **Dropdown menu button** - Access all Unity menu items from a single button  
- **Alphabetical sorting** - Menu items organized for easy navigation
- **Safety filtering** - Dangerous commands are filtered out for protection
- **Reflection-based** - Automatically discovers all available menu items

### 🖱️ Window Dragging
- **Smart drag areas** - Click and drag empty toolbar space to move the window
- **Intelligent detection** - Avoids conflicts with interactive UI elements
- **Native Windows API** - Smooth dragging experience

### 🎨 UI Customization
- **Hide title bar** - Maximize screen real estate
- **Hide menu bar** - Clean, minimal interface
- **Hide status bar** - Remove bottom status information
- **Custom toolbar elements** - Add professional window management controls

## Installation

### Via Unity Package Manager (Git URL)
1. Open Unity Package Manager
2. Click the `+` button and select "Add package from git URL"
3. Enter: `https://github.com/LazizAbduhalimov/Backtrace.git?path=/Packages/com.editorutils.windowcontrols`

### Manual Installation
1. Download the package files
2. Place them in your project's `Packages/com.editorutils.windowcontrols/` folder
3. Unity will automatically import the package

## Usage

### Basic Setup
The package automatically initializes when Unity starts. No additional setup required!

### Settings Configuration
1. Go to `Window → Editor UI Settings` or find the `EditorUISettings` asset
2. Configure the available options:
   - **Hide Title Bar** - Removes the window title bar
   - **Hide Menu Bar** - Hides the main Unity menu
   - **Show Window Controls** - Displays minimize/maximize/close buttons
   - **Show MenuBar Button** - Shows the dropdown menu button
   - **Hide Status Bar** - Removes the bottom status bar
   - **Enable Window Drag** - Allows dragging the window from toolbar

### Window Controls
- **Yellow/Orange circle** - Minimize window
- **Green circle** - Maximize/restore window  
- **Red circle** - Close editor (with confirmation)

### Menu Access
- Click the **≡** button in the toolbar to access all Unity menu commands
- All menu items are organized alphabetically by category

## Requirements

- **Unity 2021.3** or later
- **Windows** operating system (uses Windows API)
- **Editor only** (not for runtime use)

## Architecture

The package follows a modular architecture:

- `WindowControlsCoordinator` - Main orchestrator
- `WindowButtonsManager` - Handles window control buttons
- `MenuBarManager` - Manages custom menu dropdown
- `WindowDragManager` - Handles window dragging
- `EditorUISettings` - Configuration and persistence

## Technical Details

### Windows API Integration
The package uses P/Invoke to call Windows API functions for:
- Window manipulation (minimize, maximize, close)
- Window dragging simulation
- Title bar and menu bar hiding

### UI Framework
- Built with **UIElements** for modern, responsive UI
- Programmatic texture generation for smooth circle buttons
- Anti-aliased graphics for professional appearance

### Safety Features
- Confirmation dialogs for destructive actions
- Automatic error handling and logging
- Graceful fallbacks for edge cases

## Compatibility

- ✅ Unity 2021.3+
- ✅ Unity 2022.x
- ✅ Unity 6000.x
- ✅ Windows 10/11
- ❌ macOS (Windows API dependency)
- ❌ Linux (Windows API dependency)

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and updates.