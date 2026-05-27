# Contribuir a FlowForge

Gracias por querer mejorar FlowForge. Este repo contiene **metodología**, **skills**, y **archivos de integración** para IDEs (Cursor, OpenCode, Antigravity, VS Code).

## Cómo empezar

- Leé primero [`QUICKSTART.md`](QUICKSTART.md).
- Para entender el sistema completo (checkpoints, fases, casos de prueba), usá [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md).
- Para archivos de IDE, ver [`ide/README.md`](ide/README.md).

## Qué tipos de contribuciones aceptamos

- **Docs**: correcciones, clarificaciones, ejemplos, troubleshooting.
- **Instaladores**: mejoras en `ide/install.ps1` y `ide/install.sh` (robustez, detección, backups).
- **Integraciones IDE**: mejoras en `ide/cursor/`, `ide/opencode/`, `ide/antigravity/`, `ide/vscode/`.
- **Skills**: mejoras a `skills/**/SKILL.md` (manteniendo consistencia con checkpoints y semáforo).

## Issues

Al abrir un issue, por favor incluir:

- **SO**: Windows/macOS/Linux (y si es WSL).
- **IDE**: Cursor/OpenCode/Antigravity/VS Code.
- **Modo de instalación**: remoto (raw) vs local.
- **Logs**: output completo del instalador y pasos exactos para reproducir.

## Pull Requests (PRs)

### Principios
- **Cambios chicos y revisables**: una cosa por PR.
- **Sin contradicciones**: si tocás checkpoints/semáforo en un doc, revisá que `README.md`, `QUICKSTART.md` y `docs/14-*` no queden inconsistentes.
- **No rompas el onboarding**: lo primero que ve un usuario es `README.md`/`QUICKSTART.md`.

### Convenciones
- **Idioma**: el repo es principalmente en español. Si agregás docs en inglés, indicá explícitamente que son “tech notes” o traducí.
- **Rutas**: para Antigravity el estándar es `.agents/` (no `.agent/`).

## Reportar problemas de seguridad

Si encontrás un issue de seguridad (por ejemplo, un instalador que pueda sobrescribir algo crítico o filtrar datos), seguí la política en [`SECURITY.md`](SECURITY.md).

