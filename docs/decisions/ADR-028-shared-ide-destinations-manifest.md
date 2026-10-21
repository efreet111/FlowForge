# ADR-028: Manifest único de destinos IDE compartido por shell installer y actualizador C#

**Estado:** Proposed
**Fecha:** 2026-10-20
**Autores:** FlowForge installer crew (hallazgos de validación en máquina real de HU-040)
**Relacionado con:** [ADR-008](ADR-008-ide-installer-path-matrix.md), [ADR-027](ADR-027-vscode-prompts-pack.md), [HU-041](../tasks/HU-001-HU-099/HU-041-installer-updater-parity.md), [HU-040](../tasks/HU-001-HU-099/HU-040-vscode-slash-commands-missing.md)

## Contexto

FlowForge instala packs IDE por dos caminos **completamente independientes**:

| Pathway | Lenguaje | Dispara |
|---------|-----------|---------|
| `ide/install.sh`, `ide/install.ps1` | bash / PowerShell | Instalación inicial, bootstrap remoto, setups manuales |
| `flowforge update --component flowforge-skills` | C# (.NET 10 AOT) | Actualización de packs ya instalados |

Ambos escriben en los **mismos directorios de destino**, pero **ninguno conoce la lógica del otro**. La matriz de rutas canónica existe únicamente como documento ([ADR-008](ADR-008-ide-installer-path-matrix.md)); ningún código la consume.

La validación en máquina real (2026-10-20) produjo **4 defectos independientes** que son instancias del mismo patrón:

| # | Defecto | Pathway | Consecuencia observada |
|---|---------|---------|----------------------|
| **D1** | `sed` de `agent-models.json` atascado tras la migración de schema (commit `2637562`) | shell | El installer aborta antes del bloque VS Code → prompts nunca se instalan |
| **D2** | `GetAgentDirsForIde()` no declara `prompts/` para ningún IDE | C# | `flowforge update` nunca sincroniza los slash commands de HU-040 |
| **D3** | `CacheRefresher` usa `git clone` en vez del working tree | C# | Un dev pierde features locales (Decision Extraction HU-030, Drift Health Check HU-031) |
| **D4** | `FLOWFORGE_PROVIDER` se lee en shell pero no en C#; el default degrada subscripciones | ambos | 8/8 agentes OpenCode cambiaron de `opencode-go/*` a `opencode-zen/*`; `flowforge.md` quedó con `__FLOWFORGE_MODEL__` |

Cada defecto se 발견ó por prueba directa, no por análisis. El patrón underlying es que **la tabla de destinos está duplicada en código, en dos lenguajes, con dos mecanismos de resolución de paths (Flatpak vs nativo), y no hay fuente única**.

## Decisión

Adoptamos un **manifest declarativo único** como fuente de verdad para la matriz destino × IDE × asset, consumido por ambos pathways.

### 1. El manifest

Ubicación: `ide/ide-destinations.yaml` (raíz de `ide/`, junto a los packs).

Estructura:

```yaml
version: 1
defaults:
  backup: true
  overwrite_modified: ask        # ask | backup-and-overwrite | never

ides:
  cursor:
    detect:
      - { type: dir, path: "~/.cursor" }
    assets:
      - id: rules
        source: ide/cursor/rules
        dest: "~/.cursor/rules"
        pattern: "*.mdc"
      - id: agents
        source: ide/cursor/agents
        dest: "~/.cursor/agents"
        pattern: "forge-*.md"
        managed_by: update       # sincronizado por flowforge update
      - id: commands
        source: ide/cursor/commands
        dest: "~/.cursor/commands"
        pattern: "*.md"

  vscode:
    detect:
      - { type: extension, path: "~/.vscode/extensions", prefix: "github.copilot" }
      - { type: extension, path: "~/.vscode/extensions", prefix: "kilocode" }
    assets:
      - id: agents
        source: ide/vscode/agents
        dest: "~/.copilot/agents"
        pattern: "*.agent.md"
        managed_by: update
      - id: instructions
        source: ide/vscode/copilot-instructions.md
        dest: "~/.copilot/instructions/flowforge.instructions.md"
        generated_frontmatter: { applyTo: "'**'" }
      - id: prompts                     # ← HU-040 / ADR-027
        source: ide/vscode/prompts
        dest_resolvers:
          - when: { dir_exists: "~/.var/app/com.visualstudio.code" }
            path: "~/.var/app/com.visualstudio.code/config/Code/User/prompts"
          - when: { platform: linux }
            path: "~/.config/Code/User/prompts"
          - when: { platform: windows }
            path: "%APPDATA%/Code/User/prompts"
        pattern: "flow-*.prompt.md"
        managed_by: update               # ← cierra D2

  opencode:
    assets:
      - id: agents
        source: ide/opencode/templates/agents
        dest: "~/.config/opencode/agents"
        pattern: "*.md.tpl"
        transforms: [ strip_suffix, patch_model ]   # ← cierra D1 + D4
        model_source: ide/opencode/config/agent-models.json
        provider_env: FLOWFORGE_PROVIDER
        provider_inference: true                  # ← cierra D4
      - id: commands
        source: ide/opencode/commands
        dest: "~/.config/opencode/commands"
        pattern: "*.md"
        managed_by: update
```

### 2. Consumidores

| Consumidor | Qué lee | Qué NO hace |
|---|---|---|
| `ide/install.sh` | `ide-destinations.yaml` → resuelve detect + copia | No declara paths hardcoded |
| `ide/install.ps1` | ídem (mismo manifest) | ídem |
| `UpdateOrchestrator.cs` | ídem, vía un parser YAML embebido (source-gen, AOT-safe) | No tiene switch statement por IDE |
| `flowforge doctor` | ídem → verifica **cada** asset declarado | No hardcodea checks |

**Regla dura**: si un path no está en el manifest, **ningún código lo escribe**. Un path hardcoded en cualquiera de los tres es un bug por definición.

### 3. Reglas de seguridad que el manifest habilita

| Regla | Cierra |
|-------|--------|
| Todo asset con `managed_by: update` pasa por `UserModifiedAgentDetector` antes de sobrescribir | D3 (parcial) |
| Todo asset con `dest` existente crea backup preflight en `~/.flowforge-backups/<TS>/<ide>-<asset>/` | NFR-2 de HU-040 |
| Todo asset con `transforms` declara un post-condition check (`must_not_contain: ["__FLOWFORGE_"]`) | D4 |
| El resolver `dest_resolvers` se evalúa en orden; el primer `when` que matchea gana | Flatpak vs nativo (HU-040) |

### 4. Fuente de contenido: working tree vs clone

`CacheRefresher` cambia su política:

```
ANTES:  git clone --depth 1 → usa el clone como fuente
AHORA:  1. Detectar si el working tree (FLOWFORGE_REPO o ~/.flowforge/cache/FlowForge)
           tiene commits no pusheados que tocan ide/ o skills/
        2. Si hay → ABORTAR con: "cambios locales sin pushear detectados.
                Usá --force-local para actualizar desde el working tree,
                o hacé push/pull primero."
        3. Si no → continuar con el clone (comportamiento actual)
        4. --force-local → usa el working tree (nuevo flag explícito)
```

Esto elimina D3 en su raíz: **el downgrade era un bug de política de fuente, no de copia**.

## Alternativas consideradas

| Alternativa | Por qué se rechazó |
|-------------|---------------------|
| **A) Un installer único en C#; eliminar el shell** | El shell es requisito para bootstrap remoto (`curl | bash`) sin runtime .NET. Además Serena/Cursor/Antigravity priorizan shell. |
| **B) Un shell único; eliminar el C#** | `flowforge update` necesita AOT, verificación de assets, health-checks, rollback transaccional. Reimplementar en bash es una regresión. |
| **C) Un installer único en shell; el C# invoca al shell** | Acopla el binario AOT a bash del sistema; rompe Windows; hinders AOT trimming. |
| **D) Manifest como código generado** (C# `[GeneratedRegex]` + tabla bash sincronizada por un test) | Mantiene dos fuentes pero con un test que las ata. Más frágil que un dato compartido: el test falla *después* del daño. |
| **E) Sin manifest; sólo un test de paridad que compare ambos** | No previene el drift, sólo lo detecta tarde. Los 4 defectos pasando sin test es exactamente el problema. |

**Elegida: manifest declarativo compartido.** Es la única opción donde la fuente de verdad es un dato, no lógica duplicada.

## Consecuencias

### Positivas

- **D1, D2, D4 se cierran por construcción**: los tres son "este path/asset faltaba o estaba mal" — el manifest los hace explícitos y el test de paridad los falla en CI si divergen.
- **HU-040 y futuros packs** se añaden editando **una** línea de YAML, no dos puntos del código.
- **`flowforge doctor`** deriva sus checks del manifest → nunca más un check que valida algo que el installer no escribe.
- **Nuevo IDE** = bloque YAML + (si es nuevo) un detector. No requiere tocar ninguno de los tres consumidores.

### Negativas / Costes

- **Dependencia de un parser YAML** en el binario AOT. Mitigación: el manifest usa un subconjunto restringido de YAML (mapa, lista, escalares) y se parsea con un reader propio de ~300 líneas, source-generated, sin reflexión.
- **El shell necesita un parser YAML también.** Bash puro no tiene uno. Opciones evaluadas:
  - `yq` como dependencia externa → añade un requisito de instalación.
  - **Parser embebido en bash vía `python3`** → `install.sh` ya detecta `python3` para `patch_opencode_flowforge_json`; se reutiliza ese patrón.
  - **Convertir el manifest a JSON** → `jq` ya es dependencia de `install.sh` para `agent-models.json`. **Elegido: JSON** (`ide/ide-destinations.json`) porque los tres consumidores ya tienen un parser JSON disponible (`jq`, `System.Text.Json` source-gen, `ConvertFrom-Json` en PowerShell).

  > **Nota**: la decisión final es **JSON, no YAML**, para evitar añadir un parser YAML a cualquiera de los tres consumidores. Este ADR se revisó durante la redacción por esa razón.

- **Un cambio en el manifest afecta a los tres consumidores** → un error de formato rompe los tres. Mitigación: validación del schema del manifest al inicio de cada installer, con mensaje que nombre el asset y la clave offender.

### Trade-offs aceptados

- Sin YAML: se pierde la ergonomía de comentarios. Mitigación: un bloque `_comment` de campos pares por asset (`_why`, `_added_by`), que los tres consumidores ignoran.
- Un solo manifest para global + project scope: se differentiate con `scope: global | project` en cada asset.

## Cierre

Este ADR queda **Proposed** hasta que HU-041 implemente el manifest y `forge-verify` emita verdict `PASS` con los PM-041-* verificados. En ese momento, promoción a **Accepted** por `forge-memory`.