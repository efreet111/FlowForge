<!--
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-040
title: "Paridad VS Code — slash commands /flow-* (prompts pack)"
status: done
category: hotfix
flowforge_slug: "hu-040-vscode-slash-commands"
priority: P1
related_adrs: [ADR-008, ADR-011]
related_hu: [HU-038]
---

# HU-040 — Paridad VS Code — slash commands `/flow-*`

> **Tipo:** hotfix de paridad IDE · **Severidad:** P1 · **Bloquea:** onboarding en VS Code (Flatpak o nativo).

## User Story

**As a** usuario de FlowForge que trabaja en **Visual Studio Code** con la extensión GitHub Copilot Chat,
**I want** que los slash commands `/flow-start`, `/flow-plan`, `/flow-dev`, `/flow-verify`, `/flow-rework`, `/flow-status`, `/flow-close` aparezcan en el chat picker, exactamente igual que ya aparecen en Cursor y Antigravity,
**so that** pueda ejecutar el ciclo completo del SDLC de FlowForge sin tener que memorizar disparadores en lenguaje natural ni clonar el repo de FlowForge a mano.

---

## Background

`HU-038-opencode-commands-missing.md` corrigió el mismo defecto para OpenCode. La auditoría post-fix detectó que **VS Code tiene la misma patología** pero su pack no fue creado al momento de la paridad v0.4 (commit `cdd5c79`):

- `ide/cursor/commands/` ✅ existe (7 archivos)
- `ide/antigravity/workflows/` ✅ existe (6 archivos)
- `ide/opencode/commands/` ✅ (fix HU-038)
- `ide/vscode/prompts/` ❌ **nunca fue creado**

El bloque `# --- VS Code ---` de `ide/install.sh` (L334–L364) solo copia `agents/` + `copilot-instructions.md` y nunca copia prompts a las rutas que VS Code lee:

- **User-scope**: `~/.config/Code/User/prompts/` (o `~/.var/app/com.visualstudio.code/config/Code/User/prompts/` en Flatpak)
- **Workspace-scope**: `<project>/.github/prompts/`

Resultado: el orquestador responde (porque las instructions sí cargan), pero los `/flow-*` son invisibles en el chat picker.

---

## Acceptance Criteria (business-level)

- [x] **AC-1**: `ide/vscode/prompts/` contiene exactamente **7** archivos `flow-*.prompt.md` con frontmatter YAML válido (`description` obligatorio, `agent` cuando aplique).
- [x] **AC-2**: `bash ide/install.sh` copia los 7 prompts a `~/.config/Code/User/prompts/` (o al layout Flatpak si `$HOME` corresponde a un Flatpak sandbox).
- [x] **AC-3**: `bash ide/install.sh /ruta/proyecto` copia los 7 prompts a `<proyecto>/.github/prompts/`.
- [x] **AC-4**: En VS Code con GitHub Copilot Chat instalado, al escribir `/` en el chat picker aparecen listados los 7 comandos `/flow-*`.
- [x] **AC-5**: Cada prompt referencia al agente apropiado (e.g. `/flow-start` invoca `forge-orchestrator`; `/flow-verify` invoca `forge-verify`).
- [x] **AC-6**: `flowforge doctor` reporta el pack VS Code como 🟢 (o 🟡 explicativo) — nunca 🔴 silencioso.
- [x] **AC-7**: ADR-011 queda en `docs/decisions/` y modifica la matriz de ADR-008 agregando la fila de `prompts/` para GitHub Copilot.

---

## Scenarios (SDD Spec)

### Happy Path

- [ ] **[Instalación global — Linux nativo]**
  **GIVEN** un usuario con VS Code nativo en Linux (`~/.vscode/extensions/github.copilot*` presente)
  **WHEN** ejecuta `bash ide/install.sh` sin argumentos
  **THEN** los 7 archivos `flow-*.prompt.md` aparecen en `~/.config/Code/User/prompts/` y el log muestra `OK GitHub Copilot → ~/.config/Code/User/prompts/`
  **🧪 Ref**: `ide/install.sh#L334-L364`, ADR-011

- [ ] **[Instalación global — Flatpak]**
  **GIVEN** un usuario con VS Code instalado vía Flatpak (`~/.var/app/com.visualstudio.code/...` presente)
  **WHEN** ejecuta `bash ide/install.sh`
  **THEN** los 7 archivos se copian a `~/.var/app/com.visualstudio.code/config/Code/User/prompts/` (NO a `~/.config/Code/User/prompts/`)
  **🧪 Ref**: `ide/install.sh#L334-L364`, ADR-011 (detección de sandbox)

- [ ] **[Instalación por proyecto]**
  **GIVEN** un proyecto vacío en `/tmp/example`
  **WHEN** ejecuta `bash ide/install.sh /tmp/example`
  **THEN** `/tmp/example/.github/prompts/flow-*.prompt.md` contiene los 7 archivos
  **🧪 Ref**: `ide/install.sh` PROJECT_PATH branch

- [ ] **[Slash command visible en el chat picker]**
  **GIVEN** VS Code con GitHub Copilot Chat abierto
  **WHEN** el usuario escribe `/` en el campo de chat
  **THEN** el picker lista 7 entradas con prefijo `flow-` (`flow-start`, `flow-plan`, `flow-dev`, `flow-verify`, `flow-rework`, `flow-status`, `flow-close`)
  **🧪 Ref**: PM-VSC-3 (ver § Manual Tests)

- [ ] **[Delegación correcta al agent picker]**
  **GIVEN** una workspace donde el usuario invoca `/flow-verify`
  **WHEN** el comando se ejecuta
  **THEN** VS Code muestra en el log del chat que el agente activo es `forge-verify` (validable inspeccionando el frontmatter `agent: forge-verify` del prompt correspondiente)
  **🧪 Ref**: `ide/vscode/prompts/flow-verify.prompt.md`

### Edge Cases

- [ ] **[VS Code sin GitHub Copilot ni Kilo Code]**
  **GIVEN** un VS Code sin extensiones de chat instaladas
  **WHEN** corre el instalador
  **THEN** se imprime el aviso amarillo actual ("best-effort mode") y se instala en formato paralelo a Kilo, sin fallar.
  **🧪 Ref**: `ide/install.sh` `BEST_EFFORT=1`

- [ ] **[Kilo Code activo, no GitHub Copilot]**
  **GIVEN** VS Code con sólo Kilo Code
  **WHEN** corre el instalador
  **THEN** Kilo Code recibe `~/.config/kilo/agents/*.md` (no se rompen prompts — Kilo los lee desde otra ruta que no es responsabilidad de este HU).
  **🧪 Ref**: `ide/install.sh` rama Kilo

- [ ] **[Carpeta prompts ya existe con archivos viejos]**
  **GIVEN** una instalación previa que dejó versiones obsoletas
  **WHEN** se reinstala
  **THEN** los prompts se sobreescriben sin error y queda un backup con timestamp en `~/.flowforge-backups/<TS>/`.
  **🧪 Ref**: `ide/install.sh` `BACKUP_DIR`

- [ ] **[Ejecución de PowerShell (Windows/macOS Intel) si aplica]**
  **GIVEN** el instalador `.ps1`
  **WHEN** se ejecuta con VS Code detectado
  **THEN** replica las mismas rutas que la rama bash (paridad total).
  **🧪 Ref**: `ide/install.ps1`

### Error Cases

- [ ] **[Directorio de destino no escribible]**
  **GIVEN** un `$HOME` sin permisos de escritura en `.config/Code/User`
  **WHEN** corre el instalador
  **THEN** aborta con código != 0, mensaje claro y NO se sobreescribe `copilot-instructions.md` antes del fallo.
  **🧪 Ref**: `set -euo pipefail` ya en `ide/install.sh`

- [ ] **[Frontmatter inválido en algún prompt]**
  **GIVEN** un archivo `.prompt.md` sin `description:` o con YAML malformado
  **WHEN** el agente de forge-verify audita
  **THEN** emite verdict REWORK y se rechaza la entrega.
  **🧪 Ref**: forge-verify, NPM-1, NPM-2

---

## Functional Requirements (FR-)

| ID | Descripción | Verificable por |
|----|-------------|-----------------|
| FR-1 | `ide/vscode/prompts/` debe contener 7 archivos `flow-*.prompt.md` con frontmatter mínimo `description`. | `ls ide/vscode/prompts/flow-*.prompt.md \| wc -l` debe devolver `7`. |
| FR-2 | Cada prompt debe declarar `agent: forge-<rol>` cuando delegue; los prompts que arrancan el flujo declaran `agent: forge-orchestrator`. | `grep -E '^agent:' ide/vscode/prompts/flow-*.prompt.md` |
| FR-3 | El instalador debe detectar si está corriendo dentro de un Flatpak sandbox y ajustar la ruta de prompts. | `ide/install.sh` -> nueva función `detect_vscode_prompts_path()`. |
| FR-4 | El instalador debe copiar prompts a `<PROJECT_PATH>/.github/prompts/` cuando se le pase ruta de proyecto. | ejecutar `bash ide/install.sh /tmp/foo` y verificar 7 archivos en `/tmp/foo/.github/prompts/`. |
| FR-5 | `flowforge doctor` debe distinguir entre `agents` instalados y `prompts` instalados (dos checks independientes). | `sdd/cli-wizard/doctor-diagnostic/`. |

## Non-Functional Requirements (NFR-)

| ID | Tipo | Descripción | Métrica |
|----|------|-------------|---------|
| NFR-1 | Compatibilidad | Scripts deben seguir corriendo en bash 3.2+ (macOS) sin cambios. | `bash --version` test en CI `test-installer.yml`. |
| NFR-2 | Seguridad | Si la ruta de destino no es escribible, abortar sin escribir nada parcial (atomicidad). | `trap` cleanup |
| NFR-3 | Observabilidad | Cada bloque de instalación debe imprimir un `OK …` o `! …` por línea visible al humano. | inspección visual del log |
| NFR-4 | Reversibilidad | Re-ejecución del instalador no debe duplicar ni corromper prompts existentes. | backup + rsync/cp idempotente |
| NFR-5 | Documentación | `docs/16-ide-integration-plan.md` y `README.md` deben actualizarse mencionando el fix. | grep por "/flow-" |

## Security & STRIDE (resumen)

| Amenaza | Mitigación |
|---------|------------|
| **Tampering**: usuario edita `~/.config/Code/User/prompts/flow-dev.md` para inyectar instrucciones maliciosas | Advertencia en README: el installer es local-scope; revisar diff antes de re-ejecutar. |
| **Repudiation**: "¿quién agregó este prompt?" | El instalador deja log timestamp + ruta; mensajes `OK …` visibles. |
| **Elevation**: prompt que invoca un agent distinto del declarado en `.flowforge.json` | Cada prompt declara explícitamente `agent:` en frontmatter; el chat picker de VS Code respeta la declaración. |
| **Info disclosure**: prompts podrían contener paths absolutos sensibles | Los prompts NO contienen paths absolutos — sólo descripciones y `$ARGUMENTS`. |

---

## Manual Tests (PM-*)

> forge-verify NO califica los PM-*. forge-memory exige `[x]` antes del close.

- [x] **PM-VSC-1** *(instalación user-scope)*: Tras `bash ide/install.sh` en Linux nativo, existen `~/.config/Code/User/prompts/flow-{start,plan,dev,verify,rework,status,close}.prompt.md` (7 archivos) y su contenido tiene frontmatter YAML válido.
- [x] **PM-VSC-2** *(instalación Flatpak)*: Tras `bash ide/install.sh` en un sistema con VS Code Flatpak, existen 7 archivos en `~/.var/app/com.visualstudio.code/config/Code/User/prompts/` (NO en `~/.config/Code/User/prompts/`).
- [x] **PM-VSC-3** *(chat picker)*: Abriendo VS Code con GitHub Copilot Chat, escribir `/` muestra los 7 comandos listados. **Ejecutable por humano**.
- [x] **PM-VSC-4** *(delega al agent)*: Al ejecutar `/flow-status` con un proyecto que tiene `.ai-work/`, el log del chat reporta que el agente activo fue `forge-orchestrator` (validable por el humano abriendo el panel de output). **Ejecutable por humano**.
- [x] **PM-VSC-5** *(instalación por proyecto)*: `bash ide/install.sh /tmp/fresh && ls /tmp/fresh/.github/prompts/flow-*.prompt.md \| wc -l` debe devolver `7`.
- [x] **PM-VSC-6** *(reversibilidad)*: Re-ejecutar el instalador 3 veces seguidas no duplica archivos, deja backup en `~/.flowforge-backups/`, y el contenido final coincide con `ide/vscode/prompts/`.
- [x] **PM-VSC-7** *(doctor)*: `bash sdd/cli-wizard/.../doctor-diagnostic/` (o equivalente) reporta `VS Code agents: OK`, `VS Code prompts: OK`, `VS Code instructions: OK` por separado.

---

## ADR candidate

Esta HU **requiere** un nuevo ADR que modifique `ADR-008-ide-installer-path-matrix.md` para agregar la fila de `prompts/` faltante. El nuevo ADR será `ADR-011-vscode-prompts-pack.md`.

---

## Related Refs

- **ADR-008** (matriz IDE paths) — a modificar
- **HU-038** (`opencode-commands-missing`) — hermana gemela
- **`.ai-work/hotfix-vscode-slash-commands/`** — artefactos internos del fix
- **`ide/install.sh#334-364`** — bloque a editar
- **`docs/16-ide-integration-plan.md`** — doc a actualizar
- **`README.md`** (sección "Quick Start / VS Code") — agregar referencia

---

## Owner & Timeline

- **Owner**: TBD (sugerido: equipo installer + IDE parity)
- **Target milestone**: v0.4.2 (siguiente patch)
- **Effort**: ~1–2 días (7 archivos `.prompt.md` + 1 patch al installer + 1 ADR + 1 doc update + 1 plan + 1 verify)

## Definition of Done

- [ ] `ide/vscode/prompts/` contiene los 7 archivos con frontmatter válido
- [ ] `ide/install.sh` (y `.ps1`) copia prompts a las dos rutas (user + workspace)
- [ ] `ADR-011` queda en `docs/decisions/` y modifica ADR-008
- [ ] `docs/16-ide-integration-plan.md` refleja el nuevo pack
- [ ] `README.md` y/o `docs/QUICKSTART.md` mencionan los comandos VS Code
- [ ] forge-verify emite verdict PASS
- [ ] forge-memory cierra la HU con PM-VSC-* en `[x]`

## Memory Signal

Para `forge-memory` al cierre:
- **Tipo**: `pattern`
- **Título**: "IDE parity drift — slash command packs se olvidan al crear packs nuevos"
- **Regla**: "Cada nuevo `ide/<vendor>/` debe traer 3 cosas en paridad: `agents/` + `commands|prompts|workflows/` + bloque en `install.sh` que copie ambos a los paths correctos del IDE. ADR-008 es el contrato vivo."
- **Tags**: `ide-parity`, `installer`, `slash-commands`, `vscode`, `flatpak`
- **CWE/STRIDE link**: STRIDE-Tampering en README debe advertir sobre edición post-install
