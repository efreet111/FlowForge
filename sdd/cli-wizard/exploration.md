# Exploration: FlowForge CLI Wizard & Rules Compiler

## Current State
- Currently, installing FlowForge skills is done by running `install-skills.sh`, which copies physical `SKILL.md` files into local/global skill directories.
- Integrating rules with IDEs (like Cursor or Cline) is entirely manual: the user must manually copy and paste documentation rules from `09-open-source-integration.md` into their `.cursorrules` or `.clinerules` files.
- There is no automated process to configure:
  1. API keys and providers.
  2. Model routing for the 7 skills (e.g. choosing Haiku for Discovery, Sonnet for Arch).
  3. The agent's custom "Persona" or tone.
  4. Backend database endpoints (local SQLite, cloud SQLite, or PostgreSQL) which are managed by `engram-dotnet`.
- This manual process leaves room for errors, inconsistent setups, and increases friction for new developers adopting FlowForge.

## Affected Areas
- `install-skills.sh` — Can be augmented or called by the new wizard to perform the physical skills placement alongside rules compilation.
- `skills/` — All 7 directories (`forge-orchestrator`, `forge-discovery`, etc.) will be read by the compiler to merge their prompts into a single `.cursorrules` or `.clinerules` file.
- `[NEW] flowforge-init.js` — The new zero-dependency Node.js CLI Wizard.
- `[NEW] .flowforge.json` — The resulting configuration file created by the wizard.

## Approaches

### 1. Pure Bash Script (`flowforge-init.sh`)
An interactive Shell script using `read` commands, terminal escape codes for color, and appending text files via redirection.
- **Pros**:
  - Zero dependencies.
  - Extremely fast.
  - Native to Unix-like terminals (Linux/macOS).
- **Cons**:
  - Parsing, validating, and writing JSON (`.flowforge.json`) in pure Bash is fragile, ugly, and hard to maintain without `jq` (which might not be installed on the user's system).
  - Advanced validation (e.g., checking database connections) is tedious in Bash.
- **Effort**: Medium.

### 2. Zero-Dependency Node.js CLI Script (`flowforge-init.js`)
An interactive JavaScript CLI using the built-in `readline` and `fs` modules, styled with basic ANSI escape characters to make a premium console visual experience.
- **Pros**:
  - Zero external npm packages required (fully portable via `node flowforge-init.js` or `npx`).
  - Native and robust JSON handling (`JSON.stringify` and `JSON.parse` are bulletproof).
  - Easy file system manipulation for reading the `SKILL.md` markdown files, stripping frontmatter, and compiling them into `.cursorrules` or `.clinerules`.
  - Accessible to 99% of web/software developers who already have Node.js installed.
- **Cons**:
  - Requires Node.js runtime (not an issue for software development teams).
- **Effort**: Low.

### 3. C# Native AOT Tool (`forge` Binary)
Crear una aplicación de consola en C# (.NET 10) configurada para compilación **Native AOT (Ahead-of-Time)** (`<PublishAot>true</PublishAot>`). Se compila para cada sistema operativo (Linux, macOS, Windows) y se sube como binario pre-compilado en los GitHub Releases.
- **Pros**:
  - **Binario Autónomo y Zero-Dependencias**: El resultado es un único archivo ejecutable (ej. `forge` de ~10MB). **NO requiere que el desarrollador tenga .NET o Node.js instalado** en su máquina.
  - **Velocidad Absoluta (AOT)**: Tiempos de arranque instantáneos (sub-milisegundo) y consumo de RAM ínfimo, exactamente igual que los binarios escritos en Go.
  - **Alineación con el Equipo**: Dado que el backend `engram-dotnet` ya está escrito en C#, el equipo puede programar, mantener y extender la lógica de compilación de reglas y configuración usando C# (su lenguaje nativo), sin tener que aprender Go ni JavaScript.
  - **Experiencia Premium**: Permite una instalación con un simple curl:
    `curl -sSL https://flowforge.sh/install.sh | bash` (el cual descarga el binario nativo AOT según la arquitectura y lo mueve a `/usr/local/bin/forge`).
- **Cons**:
  - Requiere configurar GitHub Actions para compilar el binario para múltiples arquitecturas (Linux x64, macOS Apple Silicon, Windows x64) en la etapa de release.
- **Effort**: Medio.

---

## Recommendation

**Approach 3 (C# Native AOT Tool)** es la recomendación definitiva para producción y la que le dará a FlowForge ese toque de software "Enterprise / Premium" que buscamos.

Cumple con todos tus requisitos:
1.  **Es como Go**: Se compila a código de máquina nativo. Es un único ejecutable sin dependencias externas.
2.  **Facilidad de Instalación**: El usuario final puede instalarlo con una sola línea de comando y usar `sudo install` o moverlo al PATH para poder correr `forge init` o `forge compile` en cualquier carpeta.
3.  **Alineación del Equipo**: El código fuente es C# puro, permitiendo que tu equipo de desarrollo .NET le dé mantenimiento sin fricciones de lenguaje.

### Lógica de Funcionamiento del CLI AOT (`forge`):
Al correr `forge init` o `forge compile`:
1.  **Lector de Skills**: El binario leerá recursivamente las skills de la carpeta local `./skills/` (o una carpeta global en `~/.flowforge/skills/`).
2.  **Limpiador de Frontmatter**: Parser básico en C# que remueve las cabeceras YAML (`---`) de los archivos `SKILL.md`.
3.  **Generador del JSON**: Crea y escribe el archivo `.flowforge.json` con las API Keys, Modelos ruteados y la Persona.
4.  **Ensamblador de Reglas**: Escribe el archivo final `.cursorrules` o `.clinerules` inyectando la configuración de la Persona y la base de datos al inicio de forma dinámica.

---

## Risks
- **Compilación multiplataforma**: Para empaquetar en macOS (M1/M2/M3) y Linux, necesitamos configurar workflows de CI/CD (GitHub Actions) apropiados, ya que Native AOT requiere compilar en la plataforma de destino.
- **Tamaño del binario**: El binario AOT de C# ronda los 8-15MB. Es extremadamente pequeño para los estándares modernos pero más grande que un script en Bash o JS.

---

## Ready for Proposal
**Sí**. La opción de C# Native AOT resuelve de manera elegante la portabilidad extrema de Go manteniendo la pila tecnológica del equipo (.NET). El orquestador propondrá el diseño de esta herramienta CLI AOT en la propuesta de la fase 1.
