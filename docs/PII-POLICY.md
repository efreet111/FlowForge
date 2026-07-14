# Política de PII — FlowForge

## Principios
1. **Plantillas libres de PII**: los archivos bajo `ide/opencode/templates/` usan placeholders `$HOME` y `$USER`. Nunca se incluyen rutas absolutas del desarrollador ni credenciales en el repo.
2. **Instalación local con datos del usuario**: durante `flowforge install --ide opencode --yes` o `bash ide/opencode/generate-config.sh`, `opencode.json` se genera con el `$HOME` y `$USER` reales del sistema, lo cual no se versiona.
3. **Cuidado con los free Zen**: los modelos de OpenCode Zen pueden usar tus prompts para entrenamiento. No incluyas código sensible en la configuración predeterminada.

## Patrones prohibidos (escaneados por `ide/opencode/lib/pii-scan.sh` y `PiiScanner.cs`)
- `/home/[a-z]+/`
- `@local.dev`
- `OPENCODIGO_API_KEY`
- `DEEPSEEK_API_KEY`
- `MINIMAX_API_KEY`

## Contribuciones y scrubbing
1. Antes de commitear, ejecutá `grep -rE '/home/[a-z]+/|@local\.dev|OPENCODIGO_API_KEY|DEEPSEEK_API_KEY|MINIMAX_API_KEY' ide/opencode/templates/ src/FlowForge.Installer/`.
2. Si aparecen coincidencias, reemplazá por `$HOME`, `$USER` o elimina la clave.
3. Cuando la PII ya está en el historial de Git, usamos `git filter-repo --path <archivo> --invert-paths` o reescribimos la historia según la guía de seguridad.

## Referencias
- README: sección OpenCode
- docs/opencode-installer.md (flujo, doctor, flags)
- `ide/opencode/lib/pii-scan.sh` y `src/FlowForge.Installer/Modules/OpenCode/PiiScanner.cs`
