# Assets de marca — FlowForge

Esta carpeta contiene la identidad visual del proyecto. **Solo está versionado lo que
tiene un consumidor real**; el resto vive fuera de git a propósito.

## Qué está versionado

| Archivo | Consumidor | Peso |
|---|---|---|
| [`readme/banner-1280x320.png`](readme/banner-1280x320.png) | Cabecera de `README.md` y `README.es.md` | 89 KB |
| [`social/social-preview-1280x640.png`](social/social-preview-1280x640.png) | GitHub → Settings → Social preview (subida manual) | 216 KB |
| `assets/README.md` | Este documento | — |

Total: **~305 KB**. El README renderiza exactamente **una** imagen; la social card no se
renderiza desde el repo, se sube a mano y el repo solo conserva la fuente.

## Qué NO está versionado

Excluido en [`.gitignore`](../.gitignore):

| Ruta | Contenido | Por qué |
|---|---|---|
| `brand/` | Masters 2048², SVG y banners de alta resolución (12,7 MB) | Sin consumidor. La historia de git es permanente: agregar después es trivial, quitar no. |
| `icon/` | 6 favicons (440 KB) | Congelados hasta que exista docs site. |

Ambas rutas existen solo en la máquina del maintainer. **Respaldarlas fuera del repo**
(disco externo, nube o repo privado de marca). Si se decide versionarlas, hacerlo con
Git LFS **antes** del primer push, no después:

```bash
git lfs install
git lfs track "assets/brand/*.png"
```

## Regla de oro

> Nunca referencies `assets/brand/` desde un README, una web o un binario.

`brand/` es fuente, no canal. Todo lo que se publica pasa por `readme/`, `social/` o `icon/`.

## Estado de los favicons (congelados)

Los favicons actuales **no están listos para publicarse**:

- `favicon-16.png` (761 bytes) es ilegible: el wordmark grabado en el arte se convierte en ruido a 16 px.
- Falta una **marca sin texto** (yunque + chispa, o la cabeza del enano) para 16/32 px.
- Falta `favicon.ico` multi-resolución (16/32/48) para consumidores legacy.
- Falta `apple-touch-icon-180.png` y un `icon-512` maskable para PWA.

No derivar los 16 px del logo completo: primero hay que diseñar la marca sin wordmark.

## Regenerar los derivados

Requiere los masters de `brand/` (no versionados):

```bash
magick assets/brand/flowforge-banner-master-3168x1344.png \
  -strip -colors 256 PNG8:assets/readme/banner-1280x320.png
magick assets/brand/flowforge-social-master-2752x1536.png \
  -strip -colors 256 PNG8:assets/social/social-preview-1280x640.png
optipng -o2 assets/readme/banner-1280x320.png assets/social/social-preview-1280x640.png
```

El banner tiene 75.804 colores reales (arte generado con degradados), así que cuantizar
a 256 no destruye el pixel art: la reducción es de 4,6× sin pérdida visible.

## Convención de nombres

`kebab-case`, con dimensiones cuando el tamaño importa: `banner-1280x320.png`.
Los masters llevan el sufijo `-master` o `-preview`. Nada de `_` ni de nombres
duplicados por carpeta histórica.

## Alt text

Toda imagen en un README lleva alt text descriptivo, en el idioma del documento.
El banner no es decorativo: describe el personaje y el concepto, no "logo".

## Licencia

El código de FlowForge es MIT. **Los assets de marca no están cubiertos por MIT**;
se incluyen para uso del propio proyecto (README, documentación, presentaciones).
Los términos de uso por terceros están pendientes de definir — no asumir reutilización
libre del logo o del personaje.

## Pendiente

- [ ] Marca sin wordmark para 16/32 px (decisión de diseño).
- [ ] `favicon.ico` + `apple-touch-180` + `icon-512` maskable, cuando exista docs site.
- [ ] Términos de licencia de marca.
- [ ] Respaldo externo de `brand/`.
- [ ] Variante del banner para tema claro (`#gh-light-mode-only`), si se quiere.
