# Security Policy

## Supported Versions

FlowForge está en evolución. Recomendamos usar siempre la última versión disponible.

## Reporting a Vulnerability

Si encontrás una vulnerabilidad de seguridad, por favor **no abras un issue público** con detalles explotables.

Incluí en tu reporte:
- Descripción del problema y el impacto.
- Pasos para reproducir.
- Archivos/rutas afectadas (por ejemplo `ide/install.ps1`, `ide/install.sh`, `skills/**/SKILL.md`).
- Tu entorno (OS/IDE).

## Qué consideramos “vulnerabilidad”

- Instaladores que puedan **sobrescribir archivos fuera de los directorios esperados** sin advertencia.
- Exposición accidental de secretos en docs/scripts.
- Instrucciones que incentiven prácticas inseguras (por ejemplo, deshabilitar protecciones sin explicación).

