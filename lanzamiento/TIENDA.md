# Publicar ForbiddenWords y PalabrasProhibidas

Son el mismo mod en dos paquetes: **ForbiddenWords** (inglés) y **PalabrasProhibidas** (español). Cada uno tiene su repositorio y su página en Thunderstore, y hay un único mensaje de Reddit para los dos ([REDDIT.md](REDDIT.md)). Este documento es igual en los dos repositorios.

| | Inglés | Español |
|---|---|---|
| Paquete | `ForbiddenWords` | `PalabrasProhibidas` |
| Zip (`scripts/package.sh`) | `dist/ForbiddenWords-1.0.0.zip`, 80 MB | `dist/PalabrasProhibidas-1.0.0.zip`, 70 MB |
| Repositorio | `Tomasg-dev/ForbiddenWords` | `Tomasg-dev/PalabrasProhibidas` |
| Configuración | `tommy777.forbiddenwords.cfg` | `tommy777.palabrasprohibidas.cfg` |

## Antes de subir

1. **Prueba una partida completa con cada paquete, mejor con 3 o más jugadores.** Hasta ahora solo se ha probado con dos, y la 1.0.0 cambia cosas respecto a la última versión probada (sin espera, sin tope, sin núcleo, idioma fijo por paquete). Sobre todo:
   - la lista sale en el centro al empezar cada nivel, también a los clientes;
   - cuando un cliente dice una palabra, el enemigo aparece junto a él y el mensaje rojo sale a todos;
   - el enemigo se va al minuto y, si lo matan, no suelta núcleo;
   - decir la palabra varias veces seguidas saca varios enemigos sin que el juego se resienta;
   - en el segundo nivel hay una palabra más y son distintas;
   - con los dos paquetes instalados a la vez, el log avisa y solo trabaja uno.
2. **Los nombres no se pueden cambiar en Thunderstore después de subirlos.** Si prefieres otros, hay que cambiarlos antes en `thunderstore/manifest.json`, en `Plugin.cs` y en el nombre de la carpeta y del `.csproj`.
3. **Los enlaces entre las dos páginas suponen que el equipo de Thunderstore se llama `Tommy777`** (`thunderstore.io/c/repo/p/Tommy777/...`). Si el equipo tiene otro nombre, corrige el enlace en los dos `thunderstore/README.md` y vuelve a empaquetar.
4. **Créditos:** figura solo Tommy777. Si debe aparecer alguien más, hay que añadirlo en `LICENSE`, en los README y en el `.csproj` de los dos repositorios.
5. **Etiqueta "AI Generated".** Buena parte del código se escribió con asistencia de IA, así que corresponde marcarla en los dos.

## Qué hay en cada zip

| Archivo | Para qué |
|---|---|
| `manifest.json` | Nombre, versión, descripción corta y dependencias |
| `icon.png` | Icono de 256 x 256, con la marca EN o ES en la esquina. Está generado por código; se puede cambiar por otro PNG de 256 x 256 |
| `README.md` | La página del mod, en el idioma del paquete |
| `CHANGELOG.md` | Historial de versiones |
| `THIRD_PARTY_NOTICES.md` | Licencias de Vosk, el modelo y las librerías que lo acompañan |
| `ForbiddenWords.dll` o `PalabrasProhibidas.dll` | El mod |
| `Vosk.dll`, `libvosk.dll`, `libstdc++-6.dll`, `libgcc_s_seh-1.dll`, `libwinpthread-1.dll` | El reconocedor de voz Vosk |
| `vosk-model-small-en-us-0.15.zip` o `vosk-model-small-es-0.42.zip` | El modelo de voz. Va como zip dentro del zip a propósito: el gestor de mods aplanaría sus carpetas |

## Datos de cada ficha

**ForbiddenWords**

- Versión `1.0.0`, dependencia `BepInEx-BepInExPack-5.4.2305`
- Descripción: *Each level has forbidden words. Say one in voice chat and a real enemy spawns next to you for a minute. The list grows every level. Offline speech recognition, custom word lists. English version (Spanish: PalabrasProhibidas).*
- Web: `https://github.com/Tomasg-dev/ForbiddenWords`

**PalabrasProhibidas**

- Versión `1.0.0`, dependencia `BepInEx-BepInExPack-5.4.2305`
- Descripción: *Cada nivel tiene palabras prohibidas. Si dices una por el micro, aparece un enemigo a tu lado durante un minuto. Voz reconocida sin conexión, lista propia opcional. Versión en español (English: ForbiddenWords).*
- Web: `https://github.com/Tomasg-dev/PalabrasProhibidas`

**Categorías para los dos:** `Mods`, `Monsters`, `Server-side`, `Client-side`, `AI Generated`.

Las webs suponen que los repositorios se llamarán así. Si usas otros nombres o no los subes, cambia `website_url` en cada `thunderstore/manifest.json` y vuelve a ejecutar `scripts/package.sh`.

## Pasos para subirlos

Hazlo dos veces, una por paquete. Sube primero uno, comprueba que su página existe, y después el otro, para que los enlaces cruzados funcionen cuanto antes.

1. Entra en thunderstore.io e inicia sesión.
2. Usa el mismo equipo con el que publicaste PlayerMonster (el nombre del equipo es el "autor" que aparece junto al mod).
3. Ve a **Upload**, elige la comunidad **R.E.P.O.**, el equipo y el zip.
4. Marca las categorías y sube.
5. Abre la página del mod y comprueba el README, el icono y el enlace a la otra versión.
6. Instálalo desde el gestor de mods en un perfil limpio y arranca el juego una vez.

Cuando estén los dos, pon sus direcciones en `REDDIT.md` y publica el mensaje.

## Subir el código a GitHub

Cada carpeta ya es un repositorio local con su commit. Crea en GitHub dos repositorios vacíos, `ForbiddenWords` y `PalabrasProhibidas` (sin README ni licencia, que ya los traen), y en cada carpeta:

```bash
git remote add origin git@github.com:Tomasg-dev/ForbiddenWords.git
```

```bash
git push -u origin main
```

En la carpeta de PalabrasProhibidas, lo mismo con `git@github.com:Tomasg-dev/PalabrasProhibidas.git`.

`terceros/` y `dist/` no se suben: pesan más de lo que GitHub admite por archivo y se regeneran con los scripts. Si quieres ofrecer el zip en GitHub, súbelo como archivo adjunto de una *Release*.

## Para una versión nueva

Los dos repositorios comparten todos los `.cs`. Lo que cambia entre ellos está en `docs/ARQUITECTURA.md`, apartado "Un código, dos mods".

1. Haz el cambio en uno y copia los `.cs` de `src/` al otro.
2. Cambia la versión en los dos: `thunderstore/manifest.json`, `Plugin.cs` y el `.csproj` (`package.sh` avisa si no coinciden las dos primeras). Conviene que los dos paquetes lleven siempre el mismo número.
3. Añade la entrada en cada `thunderstore/CHANGELOG.md`.
4. Ejecuta `scripts/package.sh` en cada uno y sube los dos zips.
