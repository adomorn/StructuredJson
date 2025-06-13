# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial implementation of StructuredJson library
- Path-based API for JSON manipulation
- Support for .NET Standard 2.0, .NET 8, and .NET 9
- Comprehensive test suite
- XML documentation
- GitHub Actions workflow for automated releases
- Cross-platform compatibility improvements

### Changed
- None

### Deprecated
- None

### Removed
- None

### Fixed
- None

### Security
- None

## [1.0.0] - 2024-03-19

### Added
- Initial release of StructuredJson library
- Path-based JSON manipulation API
- Support for .NET Standard 2.0
- Intelligent type conversion system
- Comprehensive path validation
- Sparse array support
- Robust error handling
- Full XML documentation
- Comprehensive unit test coverage

### Features
- Path-based API for intuitive JSON navigation
- Smart type conversion between strings and numbers
- Array manipulation with automatic null-filling
- Locale-aware number formatting
- Comprehensive error handling
- Memory-efficient sparse array support

### Technical Details
- Built on .NET Standard 2.0
- Uses System.Text.Json for serialization
- Dictionary-based O(1) key lookups
- Optimized path parsing with regex
- Lazy evaluation for type conversions 