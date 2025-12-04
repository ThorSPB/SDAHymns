# Spec Context

This file tracks all specifications and their implementation status.

**Always check this file first** when working on the project to understand current state.

## Status Definitions

- **📋 Planned** - Spec written, not started
- **🚧 In Progress** - Currently being implemented
- **✅ Implemented** - Code complete, not tested
- **✓ Tested** - Implemented and verified working
- **⏸️ Blocked** - Cannot proceed (dependencies or decisions needed)
- **❌ Deprecated** - No longer relevant

## Specifications

### Infrastructure

| ID | Spec | Status | Notes |
|----|------|--------|-------|
| 001 | [Project Structure](001-project-structure.md) | ✅ Implemented | Solution with 4 projects, all dependencies configured |

### Core Features

| ID | Spec | Status | Notes |
|----|------|--------|-------|
| 002 | [Data Layer & EF Core](002-data-layer.md) | ✅ Implemented | All 9 entities, DbContext, migration, 156KB database with seed data |
| 003 | [Legacy XML Import](003-legacy-xml-import.md) | ✅ Implemented | 1,254 hymns total (1,070 from XML + 184 from orphan PPT files) |
| 004 | [PowerPoint Verse Extraction](004-powerpoint-verse-extraction.md) | ✅ Implemented | 1,249/1,254 hymns (99.6%) - 4,629 verses imported successfully |
| 005 | [Basic Hymn Display](005-basic-hymn-display.md) | ✅ Implemented | Dual-window system (control + display), verse navigation, dark theme, auto-scaling |
| 006 | [Enhanced Control Window](006-enhanced-control-window.md) | 📋 Planned | Full-text search, browse, recent/favorites, keyboard navigation |
| 007 | [Display Profiles](007-display-profiles.md) | 📋 Planned | Customizable fonts, colors, backgrounds, effects - preset + custom profiles |
| 008 | [Keyboard Shortcuts](008-keyboard-shortcuts.md) | 📋 Planned | Global hotkeys, customizable keybindings, shortcut hints |
| 009 | [Service Planner](009-service-planner.md) | 📋 Planned | Pre-plan services, live mode, templates, PDF export |
| _TBD_ | Audio Playback | 📋 Planned | Piano recording playback |
| _TBD_ | CLI Interface | 📋 Planned | Command-line tool with IPC |
| _TBD_ | Auto-Update System | 📋 Planned | Update mechanism |
| _TBD_ | Export Functionality | 📋 Planned | PDF/image export (individual hymns) |
| _TBD_ | Statistics Tracking | 📋 Planned | Usage analytics |

### Advanced Features

| ID | Spec | Status | Notes |
|----|------|--------|-------|
| _TBD_ | Remote Control API | 📋 Planned | HTTP API for remote control |
| _TBD_ | OBS Integration | 📋 Planned | WebSocket/browser source |

## Implementation Order

### Phase 1: Foundation ✅ COMPLETED
1. ✅ Project structure setup
2. ✅ Data layer and models
3. ✅ Legacy XML import functionality (1,254 hymns)
   - ✅ XML parsing (1,070 hymns)
   - ✅ Orphan PPT import with title extraction (184 hymns)
4. ✅ PowerPoint verse extraction (4,629 verses from 1,249 hymns)
   - ✅ Verse extraction logic with automatic sequential numbering
   - ✅ Database import service with resumable imports
   - ✅ Full import complete: 99.6% success rate
5. ✅ Basic hymn display with dual-window system
   - ✅ Control window (search by number, navigate, preview)
   - ✅ Display window (clean projection, auto-scaling)
   - ✅ Dark theme, left-aligned text, aspect ratio toggle
   - ✅ Shared ViewModel, instant sync between windows

### Phase 2: Core Features (READY FOR IMPLEMENTATION)
1. **006: Enhanced Control Window** - Full-text search, browse, recent/favorites
2. **007: Display Profiles** - Customizable appearance (fonts, colors, backgrounds)
3. **008: Keyboard Shortcuts** - Global hotkeys, customizable keybindings
4. **009: Service Planner** - Pre-plan services, live mode, templates

### Phase 3: Enhanced Features
1. Audio playback system
2. Service planner
3. Export functionality
4. Statistics tracking

### Phase 4: Advanced Features
1. CLI interface with IPC
2. Auto-update system
3. Remote control API
4. OBS integration

## Current Session Focus

**Date:** 2025-12-04
**Goal:** ✅ PHASE 1 COMPLETE - Basic Hymn Display
**Session 1 (2025-12-03 - Completed):**
- ✅ Upgraded to .NET 10 LTS
- ✅ Spec 001: Project Structure
- ✅ Spec 002: Data Layer & EF Core
- ✅ Spec 003: Legacy XML Import (1,070 hymns from XML)
- ✅ Orphan PPT Import: LibreOffice integration + PowerPoint parser (184 hymns)
  - Built `PowerPointParserService` with PPT→PPTX conversion
  - Created `import-orphan-ppt` CLI command
  - All 1,254 hymns now in database with proper titles

**Session 2 (Completed - Spec 004):**
- ✅ Extended `PowerPointParserService` with enhanced verse extraction
  - `ExtractVersesAsync()` method with multi-segment slide parsing
  - Handles 4+ different verse/chorus patterns
  - Automatic sequential verse numbering for unnumbered verses
  - Y-position-based text extraction for correct reading order
  - 45-second timeout to prevent LibreOffice hangs
  - Enhanced chorus deduplication with content comparison
- ✅ Created `IVerseImportService` and `VerseImportService`
  - Per-hymn transaction isolation
  - Resumable imports with `--start-from` parameter
  - Automatic skip of already-imported hymns
- ✅ Created `import-verses` CLI command
  - Full support for category, hymn ID, limit, and start-from options
  - Progress reporting every 10 hymns
  - Statistics command
- ✅ Full import completed: **1,249/1,254 hymns (99.6%) - 4,629 verses**
  - Crestine: 919/919 (100%) - 3,468 verses
  - Exploratori: 120/120 (100%) - 390 verses
  - Licurici: 83/83 (100%) - 315 verses
  - Tineret: 65/69 (94.2%) - 250 verses
  - Companioni: 62/63 (98.4%) - 206 verses

**Session 3 (2025-12-04 - Completed - Spec 005):**
- ✅ Created `HymnDisplayService` for database queries
- ✅ Implemented `MainWindowViewModel` with MVVM pattern (CommunityToolkit.Mvvm)
- ✅ Built control window (MainWindow) with:
  - Hymn number input with Enter key support
  - Category dropdown selection
  - Previous/Next navigation with auto-enable/disable
  - 4:3/16:9 aspect ratio toggle
  - Verse preview with auto-scaling
  - Status bar and verse indicator
- ✅ Built display window (DisplayWindow) with:
  - Clean projection interface (no controls)
  - Black background, white text, left-aligned
  - Auto-scaling content (no scrollbars)
  - Instant sync with control window
- ✅ Fixed multiple issues:
  - Database path (copy to output directory)
  - ComboBox binding (x:String vs ComboBoxItem)
  - Command notifications (NotifyCanExecuteChangedFor)
  - Text alignment and scaling
- ✅ Added 7 integration tests (all passing)
- ✅ Dark theme throughout application
- ✅ Hymn title includes number prefix

**🎉 PHASE 1 FOUNDATION: COMPLETE**

All Phase 1 goals achieved:
- ✅ 1,254 hymns imported from legacy XML/PPT
- ✅ 4,629 verses extracted and stored
- ✅ Database layer functional (EF Core + SQLite)
- ✅ Basic display system validates data model end-to-end
- ✅ Dual-window system ready for projection
- ✅ Romanian text rendering correctly

**🎯 NEXT SESSION:** Begin Phase 2 Implementation

**All Phase 2 Specs Complete and Ready:**
- 📋 **Spec 006**: Enhanced Control Window (search, browse, recent/favorites)
- 📋 **Spec 007**: Display Profiles (customizable fonts/colors/backgrounds)
- 📋 **Spec 008**: Keyboard Shortcuts (global hotkeys, customization)
- 📋 **Spec 009**: Service Planner (pre-plan services, live mode)

**Recommended Implementation Order:**
1. Start with **Spec 006** (Enhanced Control Window) - foundation for all other features
2. Then **Spec 008** (Keyboard Shortcuts) - integrates with control window
3. Then **Spec 007** (Display Profiles) - visual customization
4. Finally **Spec 009** (Service Planner) - advanced workflow feature

**Estimated Effort:**
- Spec 006: ~3-4 hours (SearchService, UI refactor, recent/favorites)
- Spec 008: ~2-3 hours (HotKeyManager, shortcuts overlay, customization)
- Spec 007: ~4-5 hours (Profile model, editor UI, apply logic)
- Spec 009: ~3-4 hours (Service plan model, planner UI, live mode)

## Notes

- Prioritize offline-first functionality
- Ensure keyboard accessibility in all features
- Test multi-monitor scenarios regularly
- Romanian text encoding (UTF-8) must be verified
