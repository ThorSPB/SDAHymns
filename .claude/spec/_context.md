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
| _TBD_ | Control Window UI | 📋 Planned | Main application interface |
| _TBD_ | Display Window | 📋 Planned | Full-screen hymn projection |
| _TBD_ | Display Profiles | 📋 Planned | Customizable styling system |
| _TBD_ | Keyboard Shortcuts | 📋 Planned | Hotkey system implementation |
| _TBD_ | Audio Playback | 📋 Planned | Piano recording playback |
| _TBD_ | CLI Interface | 📋 Planned | Command-line tool with IPC |
| _TBD_ | Service Planner | 📋 Planned | Pre-plan hymn orders |
| _TBD_ | Auto-Update System | 📋 Planned | Update mechanism |
| _TBD_ | Export Functionality | 📋 Planned | PDF/image export |
| _TBD_ | Statistics Tracking | 📋 Planned | Usage analytics |

### Advanced Features

| ID | Spec | Status | Notes |
|----|------|--------|-------|
| _TBD_ | Remote Control API | 📋 Planned | HTTP API for remote control |
| _TBD_ | OBS Integration | 📋 Planned | WebSocket/browser source |

## Implementation Order

### Phase 1: Foundation (Current)
1. ✅ Project structure setup (COMPLETED)
2. ✅ Data layer and models (COMPLETED)
3. ✅ Legacy XML import functionality (COMPLETED - 1,254 hymns)
   - ✅ XML parsing (1,070 hymns)
   - ✅ Orphan PPT import with title extraction (184 hymns)
4. ✅ PowerPoint verse extraction (4,629 verses from 1,249 hymns) - COMPLETED
   - ✅ Verse extraction logic with automatic sequential numbering
   - ✅ Database import service with resumable imports
   - ✅ Full import complete: 99.6% success rate
5. Basic hymn display (minimal styling)

### Phase 2: Core Features
1. Control window UI (search, select, navigate)
2. Display window with customization
3. Display profiles system
4. Keyboard shortcuts

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

**Date:** 2025-12-03
**Goal:** Complete Phase 1 Foundation - Data Import
**Session 1 (Completed):**
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

**🎯 NEXT SESSION:** Begin Phase 1 final step - Basic hymn display UI

## Notes

- Prioritize offline-first functionality
- Ensure keyboard accessibility in all features
- Test multi-monitor scenarios regularly
- Romanian text encoding (UTF-8) must be verified
