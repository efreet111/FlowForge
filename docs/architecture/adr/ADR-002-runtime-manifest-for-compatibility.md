# ADR-002: Manifest remoto para gestión de compatibilidad entre versiones

**Estado:** Aceptado  
**Fecha:** 2026-06-23  
**Autores:** equipo engram / FlowForge  
**Contexto de trabajo:** ENG-301 — Stack Installer

---

## Contexto

El FlowForge installer instala componentes de repos independientes:

- `efreet111/engram-dotnet` — evoluciona con su propio ciclo de releases
- `efreet111/FlowForge` — incluye el installer y los skills

Cuando `engram-dotnet` lanza una nueva versión que introduce cambios que afectan al installer (por ejemplo, nuevo formato de MCP config, nuevas flags del CLI), es necesario:

1. Que el installer sepa que la nueva versión es compatible o incompatible.
2. Que el usuario sea advertido si su installer está desactualizado.
3. Que esta lógica se pueda **actualizar sin recompilar el installer** (el binario AOT es estático).

### Opciones consideradas

| Opción | Descripción | Problema |
|--------|-------------|---------|
| **Versión hardcodeada en el binario** | El binario AOT tiene el rango de compatibilidad embebido | No se puede actualizar sin recompilar y redistributar el binario |
| **Consultar GitHub Releases API directamente** | Siempre descarga "latest" sin validar compatibilidad | No hay forma de bloquear versiones incompatibles |
| **Manifest remoto (elegido)** | Archivo `manifest.yaml` en el repo FlowForge; se descarga en runtime | Permite actualizar reglas de compatibilidad publicando solo un cambio en `main` |

---

## Decisión

**El installer descarga `manifest.yaml` de la rama `main` del repo FlowForge en cada operación de install/update.**

URL: `https://raw.githubusercontent.com/efreet111/FlowForge/main/install/manifest.yaml`

El manifest define:

```yaml
requires:
  engram-dotnet: ">=0.3.0"       # Versiones de engram compatibles con el installer actual
  installer:     ">=0.1.0-alpha.1" # Versión mínima del installer para este manifest
```

### Comportamiento en runtime

```
flowforge install / flowforge update
    │
    ├── Intenta descargar manifest.yaml (timeout 5s)
    │
    ├── Si red no disponible → usa manifest por defecto (no bloquea la operación)
    │
    ├── Verifica requires.installer vs versión actual del binario
    │   └── Si desactualizado → avisa "actualizá con: flowforge update --self"
    │
    └── Verifica requires.engram-dotnet vs versión a instalar/actualizar
        └── Si incompatible → error con mensaje claro
```

### Degradación controlada

El manifest es **best-effort**: si GitHub no está disponible (sin red, timeout, 404), el installer continúa con valores por defecto. Esto garantiza que el installer funcione en entornos offline o con restricciones de red.

---

## Flujo de vida cuando hay un breaking change

**Escenario**: `engram-dotnet` v1.0.0 cambia el formato del MCP config y requiere un installer actualizado.

1. Se trabaja en `FlowForge` para adaptar el installer al nuevo formato → se lanza `v0.2.0`.
2. Se actualiza `manifest.yaml` en `main`:
   ```yaml
   requires:
     engram-dotnet: ">=1.0.0"
     installer:     ">=0.2.0"
   ```
3. Usuarios con installer `v0.1.x` que corran `flowforge update`:
   - Descargan el manifest remoto
   - Ven que su installer es `v0.1.x < v0.2.0`
   - Reciben el mensaje: *"Este installer está desactualizado. Actualizá con: `flowforge update --self`"*
   - No se instala ninguna versión incompatible de `engram-dotnet`

4. Usuarios que acepten la auto-actualización del installer: descargan `v0.2.0` y luego continúan con `engram-dotnet v1.0.0`.

---

## Consecuencias

**Positivas:**
- Las reglas de compatibilidad son actualizables sin tocar el binario AOT.
- Los usuarios con versiones viejas del installer reciben mensajes de error claros, no comportamientos silenciosos rotos.
- El manifest es un contrato explícito y versionado entre los dos repos.

**Restricciones asumidas:**
- El manifest debe mantenerse actualizado en `main` cuando cambie la compatibilidad.
- El parser YAML del manifest es minimalista (sin dependencia externa) para mantener AOT simple — soporta el subconjunto necesario del formato.
- Toda lógica de compatibilidad crítica debe estar en el manifest, no hardcodeada en el binario.

---

## Referencias

- `FlowForge/install/manifest.yaml`
- `FlowForge/src/FlowForge.Installer/Infrastructure/ManifestClient.cs`
- `FlowForge/src/FlowForge.Installer/Models/RemoteManifest.cs`
- ADR-001 — Stack tecnológico del installer
