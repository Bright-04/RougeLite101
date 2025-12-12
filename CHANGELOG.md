# Changelog

All notable changes to RougeLite101 will be documented in this file.

## [Unreleased]

### Added

- **MIT License** for the project with comprehensive license documentation
- Comprehensive documentation structure
- Manual tilemap room design system
- Room Creation Helper Unity editor tool
- Room Template Validator Unity editor tool
- Automatic error detection and recovery for missing RoomTemplate components
- System architecture documentation
- Quick reference guides for level designers

### Changed

- Reorganized documentation into proper folder structure
- Improved DungeonManager error handling for missing components
- Enhanced Flash component to work with multiple health component types
- Cleaned up debug statements from production code

### Fixed

- Critical logic error in SlimeAI behavior flow
- Potential NullReferenceException in SlimePathFinding
- Hardcoded component dependencies in Flash system
- Empty implementation in ActiveWeapon component
- "RoomTemplate missing on room" error with automatic recovery
- Unity meta file cleanup - removed orphaned Interfaces.meta file causing empty folder recreation

### Technical Improvements

- Added null checks for better stability
- Implemented automatic component configuration
- Created validation systems for room prefabs
- Improved code quality and maintainability

## Documentation Structure

### Added Folders

- `Documentation/` - Main documentation folder
- `Documentation/Level Design/` - Room creation and design guides
- `Documentation/Quick References/` - Quick reference materials
- `Documentation/System Architecture/` - Technical documentation

### Added Files

- `README.md` - Main project documentation
- `Documentation/README.md` - Documentation index
- `Documentation/Level Design/Manual_Tilemap_Room_Guide.md` - Complete room creation guide
- `Documentation/Quick References/Tilemap_Quick_Reference.md` - Quick reference for experienced users
- `Documentation/System Architecture/System_Overview.md` - Technical architecture documentation
- `CHANGELOG.md` - This changelog file

### Moved Files

- Moved `Manual_Tilemap_Room_Guide.md` from root to `Documentation/Level Design/`
- Moved `Tilemap_Quick_Reference.md` from root to `Documentation/Quick References/`