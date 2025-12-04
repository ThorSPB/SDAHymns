# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SDAHymns is a modern, cross-platform desktop application for displaying hymns in church services, built for the Romanian Seventh Day Adventist Church. This is a **complete rewrite from scratch** of a legacy application, focusing on improved UX, streaming optimization, and automation capabilities.

**Important:** The `Imnuri Azs/` folder contains the old legacy application and is **only kept as a reference for hymn resources** (XML indexes and PowerPoint files). Do not modify or rely on the legacy app code.

## Spec-Driven Development

This project follows a **specification-driven approach** to manage complexity across multiple development sessions:

- **Spec files** are located in `.claude/spec/`
- Each spec defines a feature, component, or system in detail
- Specs include implementation status, acceptance criteria, and technical details
- **`.claude/spec/_context.md`** tracks all specs and their current status
- **Always check `_context.md` first** to understand what's planned, in-progress, or completed
- Update spec status as work progresses
- Reference related specs for cross-cutting concerns

### Spec Workflow

1. **Planning:** Create new spec file in `.claude/spec/` with detailed requirements
2. **Implementation:** Follow spec, update status to "in-progress"
3. **Completion:** Mark as "implemented", add notes about deviations
4. **Testing:** Mark as "tested" once verified
5. **Update `_context.md`:** Keep context file synchronized with spec status

## Issue Tracking & Workflow

This project uses **GitHub Issues** to track all features, bugs, and tasks. Each spec should have a corresponding GitHub issue for visibility and tracking.

### Issue-Driven Development Flow

```
Spec File → GitHub Issue → Branch → PR → Merge (closes issue)
```

**Step-by-Step:**

1. **Create Issue** (via gh CLI or web):
   ```bash
   gh issue create --title "[Spec 010] Auto-Updates with Velopack" \
     --body "See .claude/spec/010-auto-updates.md for details" \
     --label "enhancement"
   ```

2. **Create Branch** (named after issue):
   ```bash
   git checkout -b feature/auto-updates-#6
   # or
   git checkout -b fix/verse-display-#15
   ```

3. **Work on Implementation**:
   - Follow the spec
   - Commit regularly with clear messages
   - Reference issue in commits: `git commit -m "feat: add update check service (#6)"`

4. **Create Pull Request** (closes issue automatically):
   ```bash
   gh pr create --title "feat: implement auto-updates with Velopack" \
     --body "Implements auto-update system with Velopack.

   **Changes:**
   - Added UpdateService with GitHub Releases integration
   - Created update notification UI in MainWindow
   - Modified CI/CD to create Velopack packages

   **Testing:**
   - Tested update check on startup
   - Verified delta updates work
   - Tested on Windows 10 and 11

   Closes #6"
   ```

5. **Merge PR** → Issue automatically closes

### Closing Multiple Issues in One PR

You can close multiple related issues in a single PR:

```markdown
## Summary
Refactored CLI command handlers to use consistent DI pattern.

## Changes
- Updated ImportCommand to use injected services
- Updated TestPptCommand to use injected parser
- Removed duplicate GetSolutionRoot() methods

Closes #10, #11, #12
```

**Supported Keywords:**
- `Closes #123` or `Fixes #123` (case-insensitive)
- Can use on separate lines or comma-separated
- Works in PR description or commit messages

### Issue Labels (Recommended)

**Standard Labels:**
- `enhancement` - New features
- `bug` - Bug fixes
- `documentation` - Docs updates
- `spec:XXX` - Links to specific spec (e.g., `spec:006`)
- `priority: high` / `priority: medium` / `priority: low`
- `phase:1`, `phase:2`, etc. - Development phase

**Create Labels:**
```bash
# Create spec labels
gh label create "spec:006" --color "0075ca" --description "Related to Spec 006"

# Create priority labels
gh label create "priority: high" --color "d73a4a" --description "High priority"
gh label create "priority: medium" --color "fbca04" --description "Medium priority"
gh label create "priority: low" --color "0e8a16" --description "Low priority"
```

### Useful gh CLI Commands

```bash
# List all open issues
gh issue list

# View specific issue
gh issue view 6

# Assign issue to yourself
gh issue edit 6 --add-assignee @me

# Add labels
gh issue edit 6 --add-label "priority: high,spec:010"

# Close issue manually
gh issue close 6 --comment "Completed in PR #42"

# Search issues
gh issue list --label "enhancement"
gh issue list --state "closed"
gh issue list --assignee "@me"

# Create issue from template (if you create .github/ISSUE_TEMPLATE/)
gh issue create --template feature_request.md
```

### Best Practices

1. **One Issue Per Spec** - Each spec should have exactly one tracking issue
2. **Reference Issues in Commits** - Use `#123` in commit messages for linkage
3. **Close Issues via PRs** - Always use "Closes #X" in PR descriptions
4. **Update Spec Status** - When closing issue, also update spec status in `_context.md`
5. **Keep Issues Focused** - Large features should be broken into multiple issues
6. **Use Draft PRs** - Mark PRs as draft while work is in progress

### Example: Full Feature Implementation

```bash
# 1. Check spec and issue
cat .claude/spec/010-auto-updates.md
gh issue view 6

# 2. Create branch
git checkout -b feature/auto-updates-#6

# 3. Implement (multiple commits)
git commit -m "feat: add UpdateService with Velopack (#6)"
git commit -m "feat: add update notification UI (#6)"
git commit -m "ci: update workflow for Velopack packages (#6)"
git commit -m "test: add UpdateService tests (#6)"

# 4. Push and create PR
git push -u origin feature/auto-updates-#6
gh pr create --title "feat: implement auto-updates with Velopack" \
  --body "Full implementation of Spec 010.

Closes #6"

# 5. After merge, update spec
# Edit .claude/spec/_context.md: Change status to ✅ Implemented
```

## Technology Stack

- **Language:** C# (.NET 10 LTS)
- **UI Framework:** Avalonia UI (cross-platform XAML-based)
- **Database:** SQLite with Entity Framework Core 10
- **Audio:** NAudio (piano recording playback)
- **CLI:** CommandLineParser
- **Updates:** Velopack (or custom GitHub Releases integration)
- **License:** AGPL-3.0

## Target Platforms

- Windows 10/11 (primary)
- Windows 7/8 (if feasible without extra effort)
- macOS ARM (M1/M2/M3)
- macOS Intel (support comes automatically with Avalonia)

## Core Features

### 1. Hymn Display & Projection
- Dual-window system: Control window + full-screen display window
- Display hymns on projector/second screen
- Navigate through verses/slides
- Fully customizable display styling (fonts, colors, opacity, backgrounds)

### 2. CLI Support (Critical)
- Full command-line interface for automation and scripting
- Enable AI integration and programmatic control
- Control all app functions via CLI
- Uses IPC to communicate with running GUI instance

### 3. Auto-Updates
- Seamless update mechanism for users
- Check for updates from GitHub Releases
- One-click update installation
- No manual downloads required

### 4. Piano Recordings
- Audio playback of piano accompaniment for hymns
- Recorded from church services (CQ-16 mixer)
- Bundled with application (compressed MP3/Opus)
- Playback controls (play, pause, speed adjustment)
- Community contributions possible

### 5. OBS/Streaming Optimization
- Dedicated output mode for streaming
- Customizable text rendering (not just PowerPoint)
- Background opacity, color, font customization
- Transparent background support
- Multiple display profiles (Projector vs OBS Stream vs Practice)

### 6. Keyboard-Driven Workflow
- Full hotkey support (productivity-focused)
- Navigate without touching mouse
- Configurable keyboard shortcuts
- Quick hymn lookup and switching

### 7. Additional Features
- **Service Planner:** Pre-plan hymn order for services
- **Verse Selection:** Choose specific verses to display
- **Display Profiles:** Save/load different display configurations
- **Statistics:** Track hymn usage frequency
- **Remote Control:** Web/mobile interface for remote operation
- **Export:** Generate PDFs/images for bulletins
- **Offline-First:** Fully functional without internet

## Architecture

### Project Structure
```
SDAHymns/
├── SDAHymns.Desktop/          # Main Avalonia GUI application
│   ├── Views/                 # XAML view files
│   │   ├── ControlWindow.axaml       # Main control interface
│   │   ├── DisplayWindow.axaml       # Full-screen hymn display
│   │   ├── SettingsWindow.axaml      # Configuration
│   │   └── ServicePlannerView.axaml  # Service planning
│   ├── ViewModels/            # MVVM view models
│   ├── Models/                # UI-specific models
│   ├── Services/              # UI services
│   └── Assets/                # Images, icons, resources
├── SDAHymns.CLI/              # Command-line interface
│   ├── Commands/              # CLI command handlers
│   └── Program.cs             # CLI entry point
├── SDAHymns.Core/             # Shared business logic
│   ├── Data/                  # Database context and models
│   │   ├── HymnsContext.cs           # EF Core DbContext
│   │   ├── Models/
│   │   │   ├── Hymn.cs               # Hymn entity
│   │   │   ├── Verse.cs              # Verse/slide entity
│   │   │   ├── HymnCategory.cs       # Category (companioni, crestine, etc.)
│   │   │   ├── AudioRecording.cs     # Piano recording metadata
│   │   │   ├── DisplayProfile.cs     # Display configuration
│   │   │   └── ServicePlan.cs        # Service planning
│   │   └── Migrations/
│   ├── Services/              # Core business logic
│   │   ├── HymnService.cs            # Hymn data operations
│   │   ├── AudioService.cs           # Audio playback management
│   │   ├── DisplayService.cs         # Display rendering logic
│   │   ├── ConfigService.cs          # App configuration
│   │   ├── UpdateService.cs          # Auto-update logic
│   │   ├── ExportService.cs          # PDF/image export
│   │   └── StatisticsService.cs      # Usage tracking
│   └── Utilities/             # Helper classes
├── SDAHymns.Tests/            # Unit and integration tests
├── Resources/                 # Application resources
│   ├── hymns.db                      # SQLite database (generated)
│   ├── audio/                        # Piano recordings
│   │   ├── companioni/
│   │   │   ├── 001.mp3
│   │   │   └── 001.metadata.json    # Tempo, date, etc.
│   │   ├── crestine/
│   │   ├── exploratori/
│   │   ├── licurici/
│   │   └── tineret/
│   ├── config/
│   │   ├── display-profiles.json    # Saved display configs
│   │   └── user-settings.json       # User preferences
│   └── legacy/                       # Reference only
│       └── Imnuri Azs/              # Old app (DO NOT USE)
└── docs/                      # Documentation
```

### Dual-Window System

**Control Window (Primary):**
- Search and browse hymns
- Select verses to display
- Control audio playback
- Manage service planner
- Configure display profiles
- Access settings

**Display Window (Secondary):**
- Full-screen on projector/second monitor
- Renders hymn verses with custom styling
- Receives commands from control window
- Independent of control window visibility
- Can run in OBS-optimized mode

### Data Flow

1. **Startup:** Parse legacy XML files → Populate SQLite database (first run only)
2. **Search:** User searches hymn → Query SQLite → Display results
3. **Select:** User selects hymn → Load verses from DB → Send to display window
4. **Display:** Display window renders verses with active profile styling
5. **Navigate:** Hotkeys/buttons → Update current verse → Refresh display
6. **Audio:** Play audio → Load MP3 from resources → Stream to audio output

### CLI Architecture

CLI communicates with GUI via:
- **IPC (Inter-Process Communication):** Named pipes or HTTP localhost server
- **Shared Core Logic:** Both GUI and CLI use `SDAHymns.Core` services
- **Commands:** `show`, `search`, `next`, `prev`, `play-audio`, `config`, `setlist`

## Development Commands

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run desktop app
dotnet run --project SDAHymns.Desktop

# Run CLI
dotnet run --project SDAHymns.CLI -- show 123

# Run tests
dotnet test
```

### Database Management
```bash
# Create migration
dotnet ef migrations add MigrationName --project SDAHymns.Core

# Update database
dotnet ef database update --project SDAHymns.Core

# Generate hymns.db from legacy XML (custom command)
dotnet run --project SDAHymns.Desktop -- --import-legacy
```

### Publishing
```bash
# Publish self-contained Windows
dotnet publish -c Release -r win-x64 --self-contained

# Publish self-contained macOS ARM
dotnet publish -c Release -r osx-arm64 --self-contained

# Publish with single-file output
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Key Design Decisions

### Why Parse PowerPoint Files?

Instead of using PowerPoint COM interop (like old app):
- **Extract text** from .PPT files into SQLite verses
- **Eliminates Microsoft Office dependency**
- **Full styling control** for OBS/streaming optimization
- **Faster performance** (no PowerPoint overhead)
- **Keep originals** in `legacy/` for reference

**Implementation:** Use a library like `NetOffice` or `DocumentFormat.OpenXml` to parse .PPT files during initial import.

### Display Profile System

Each profile stores:
```csharp
public class DisplayProfile
{
    public string Name { get; set; }              // "Projector", "OBS Stream"
    public Color BackgroundColor { get; set; }
    public double BackgroundOpacity { get; set; } // 0.0 - 1.0
    public string FontFamily { get; set; }
    public int FontSize { get; set; }
    public Color TextColor { get; set; }
    public bool EnableShadow { get; set; }
    public bool TransparentBackground { get; set; }
    public TextAlignment Alignment { get; set; }
    // ... additional customization
}
```

### Audio File Organization

```
audio/
├── {category}/
│   ├── {number}.mp3          # Main audio file
│   └── {number}.metadata.json # Recording info
```

**Metadata format:**
```json
{
  "hymnNumber": 123,
  "category": "crestine",
  "recordingDate": "2024-11-15",
  "tempo": "moderato",
  "durationSeconds": 180,
  "recordedBy": "Church Name",
  "notes": "Recorded during Sunday service"
}
```

### Offline-First Design

- **All data bundled:** Hymn database, audio files, resources
- **No external dependencies:** No API calls, no internet required
- **Updates only:** Only feature that needs internet is auto-update checking
- **Self-contained:** Single executable with embedded resources (or app folder with resources/)

### Hotkey System (Productivity Focus)

**Default shortcuts:**
- `Ctrl+F`: Focus search
- `↑/↓`: Navigate hymn list
- `Enter`: Display selected hymn
- `←/→`: Previous/next verse
- `Space`: Play/pause audio
- `Ctrl+P`: Open service planner
- `Ctrl+,`: Open settings
- `F11`: Toggle fullscreen display
- `Esc`: Close display window
- `Ctrl+E`: Export current hymn

**All shortcuts configurable in settings.**

## Development Workflow

1. **First time setup:**
   - Clone repository
   - Run `dotnet restore`
   - Run app with `--import-legacy` to populate database from XML files

2. **Daily development:**
   - Make changes in appropriate project (Desktop/CLI/Core)
   - Run tests: `dotnet test`
   - Run app: `dotnet run --project SDAHymns.Desktop`

3. **Adding new hymns:**
   - Add audio file to `Resources/audio/{category}/{number}.mp3`
   - Add metadata JSON if available
   - Run import or manually add to database

4. **Creating releases:**
   - Update version in `.csproj` files
   - Build release: `dotnet publish`
   - Create GitHub Release
   - App will auto-detect new version and prompt users

## Important Notes

- **Romanian text:** Ensure UTF-8 encoding for proper diacritics (ă, â, î, ș, ț)
- **Multi-monitor:** Test display window on various screen configurations
- **Performance:** Keep display window rendering smooth (60 FPS target)
- **Accessibility:** Maintain keyboard navigation for all features
- **Testing:** Test with real projectors and OBS before releases

## Romanian Hymn Categories

1. **Imnuri crestine** - Main Christian hymnal (largest collection)
2. **Imnuri companioni** - Pathfinder/Companion hymns
3. **Imnuri exploratori** - Explorer hymns
4. **Imnuri licurici** - Firefly hymns (children's songs)
5. **Imnuri tineret** - Youth hymns

## Future Enhancements (Not Immediate Priority)

- Mobile remote control app
- Web interface for remote control
- Cloud sync for service plans (optional)
- Multiple language support beyond Romanian
- MIDI integration for electronic keyboards
- Lyrics video export for YouTube
- Integration with church management software
