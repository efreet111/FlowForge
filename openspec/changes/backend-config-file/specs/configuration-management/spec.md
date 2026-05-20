# Configuration Management Specification

## Purpose

The Configuration Management system provides a structured, hierarchical approach to configuring `engram-dotnet`. It allows projects to dictate their own memory rules (like custom TTLs and backend choices) via a project-local file, fulfilling the "Directives as Code" principle, while falling back to global defaults and environment variables.

## Requirements

### Requirement: Configuration Priority Resolution

The system MUST load configuration settings in a specific, deterministic priority order to resolve conflicts.

#### Scenario: All configuration sources present

- GIVEN the application starts
- AND an environment variable `ENGRAM_SERVER_URL` is set
- AND a local `.engram.json` exists in the working directory defining `server_url`
- AND a global `~/.config/engram/config.json` exists defining `server_url`
- WHEN the configuration is built
- THEN the environment variable MUST take precedence over the local file
- AND the local file MUST take precedence over the global file

#### Scenario: No configuration sources present

- GIVEN the application starts
- AND no environment variables are set
- AND no local or global JSON configuration files exist
- WHEN the configuration is built
- THEN the system MUST fall back to hardcoded default values
- AND the system MUST start correctly without throwing configuration errors

### Requirement: Project-Local Configuration

The system MUST support a project-local configuration file to define project-specific settings.

#### Scenario: Local configuration resolution

- GIVEN a user runs a command in a subdirectory of a Git repository
- WHEN the system attempts to load the local configuration
- THEN it SHOULD look for `.engram.json` in the current working directory
- AND if not found, it SHOULD traverse up the directory tree until it finds the file or reaches the `.git` root
- AND it MUST apply the settings defined in the discovered `.engram.json`

### Requirement: Global Developer Configuration

The system MUST support a global configuration file to define default settings across all projects for a specific user.

#### Scenario: Global configuration fallback

- GIVEN a local `.engram.json` exists but does not define `ttl_tool_use`
- AND a global `~/.config/engram/config.json` exists defining `ttl_tool_use` as "30d"
- WHEN the configuration is resolved
- THEN the system MUST apply the global `ttl_tool_use` value of "30d"

### Requirement: Configuration Schema Support

The configuration files MUST support defining backend store types, connection strings, and TTL rules.

#### Scenario: Configuring store type via local file

- GIVEN a local `.engram.json` defines the `store.type` as "postgres" and provides a valid `store.connection_string`
- WHEN the `engram-dotnet` system initializes the storage layer
- THEN it MUST connect to PostgreSQL using the provided connection string instead of the default SQLite store

### Requirement: Artifact Store Configuration

The configuration schema MUST support defining the preferred artifact store mode for the project.

#### Scenario: Configuring artifact store mode to OpenSpec

- GIVEN a local `.engram.json` defines `artifact_store.mode` as "openspec"
- WHEN the orchestrator initializes a new change
- THEN the system MUST read this configuration
- AND default to creating file-based specs in the `openspec/` directory

#### Scenario: Default artifact store mode

- GIVEN no configuration defines the `artifact_store.mode`
- WHEN the orchestrator checks the configuration
- THEN the system MUST default to an appropriate store mode based on the environment (e.g., "engram" if available, or "hybrid")
