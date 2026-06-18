---
title: "MCP engram: usar binario fijo, no dotnet run --no-build"
type: config
topic_key: config/mcp-engram-binary-path
date: "2026-05-30"
scope: team
project: team/flowforge
significance: high
---

## What
El comando MCP en ~/.cursor/mcp.json debe usar el binario compilado, no dotnet run.

Configuración correcta:
  "command": "E:\\Proyectos\\engram-dotnet\\dist\\win-x64-fixed\\engram.exe"
  "args": ["mcp"]

## Why
dotnet run --no-build puede colgarse si el binario está desactualizado o el proceso
de build fue interrumpido. Causa que el MCP quede inestable, lo que impide que
mem_save, mem_search y mem_session_summary funcionen durante la sesión. Cuando MCP
cuelga, el agente no puede persistir nada aunque quiera hacerlo.

## Where
- ~/.cursor/mcp.json (config de usuario, fuera del repo)
- ia-work/context-project.md (documentación del path correcto)

## Learned
Sin ENGRAM_URL → modo local-first correcto (escribe a SQLite local antes de sync).
ENGRAM_SYNC_ENABLED=true + ENGRAM_SERVER_URL → push al servidor cuando esté sano.
"Cerrar Cursor" no dispara ningún save en Engram — requiere que el agente llame
mem_session_summary explícitamente antes de despedirse.
17 mutaciones en cola local de integration-test esperando push — prueba de que el
flujo offline-first funciona; lo que falló fue que el agente no llamó las tools.
