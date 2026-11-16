# PalabrasProhibidas

Un mod para [R.E.P.O.](https://store.steampowered.com/app/3241660/REPO/): cada nivel tiene unas palabras prohibidas. Si dices una por el micrófono, aparece a tu lado un enemigo real del nivel durante un minuto.

- **Jugadores:** qué hace el mod, cómo se instala y cómo se configura está en el [README de la página del mod](thunderstore/README.md).
- **Desarrolladores:** este archivo explica cómo está organizado el repositorio y cómo compilarlo.

Este repositorio es la versión en **español**. La versión en inglés, [ForbiddenWords](https://github.com/Tomasg-dev/ForbiddenWords), tiene el mismo código con el idioma cambiado al compilar (la constante `INGLES` del `.csproj`).

Creado por **Tommy777**. Buena parte del código se escribió con asistencia de IA.

## Organización

```
PalabrasProhibidas/
├── src/PalabrasProhibidas/   El mod (C#, plugin de BepInEx 5)
│   ├── Plugin.cs             Entrada: configuración y bucle de cada fotograma
│   ├── CargaNativa.cs        Carga la DLL nativa de Vosk desde la carpeta del plugin
│   ├── Oido.cs               Se engancha a la voz del juego (Photon Voice) y copia el audio del micro
│   ├── Reconocedor.cs        Hilo aparte: alimenta a Vosk y avisa de las palabras prohibidas
│   ├── Listas.cs             Lista de fábrica y textos de pantalla
│   ├── Red.cs                Rondas y red: sorteo, aviso del cliente al anfitrión, anuncio
│   ├── Aparicion.cs          Anfitrión: crea el enemigo junto a quien habló y lo retira
│   └── Pantalla.cs           Mensajes en el centro de la pantalla y su animación
├── thunderstore/             Lo que se publica: manifiesto, icono, README, historial
├── docs/                     Cómo funciona por dentro y qué se aprendió del juego
├── lanzamiento/              Guía de publicación y mensaje de Reddit (comunes a los dos mods)
├── scripts/                  obtener-terceros.sh (descarga Vosk y el modelo), package.sh (arma el zip), idioma.sh (qué modelo)
└── THIRD_PARTY_NOTICES.md    Licencias de lo que se redistribuye
```

`terceros/` (Vosk y el modelo de voz, unos 100 MB) y `dist/` (el zip) no están en el repositorio. Los scripts los vuelven a crear.

## Compilar

Hace falta el SDK de .NET, el juego instalado y un perfil de BepInEx (Gale, r2modman o manual).

1. Indica dónde están el juego y BepInEx. Los valores por defecto de `Directory.Build.props` son Steam en Linux y el perfil `Default` de Gale. Para cambiarlos, crea `Directory.Build.props.user` junto a él:

   ```xml
   <Project>
     <PropertyGroup>
       <GameDir>C:\Program Files (x86)\Steam\steamapps\common\REPO</GameDir>
       <BepInExDir>C:\Users\TU_USUARIO\AppData\Roaming\com.kesomannen.gale\repo\profiles\Default\BepInEx</BepInExDir>
     </PropertyGroup>
   </Project>
   ```

2. Descarga Vosk y el modelo (una sola vez):

   ```bash
   scripts/obtener-terceros.sh
   ```

3. Arma el paquete:

   ```bash
   scripts/package.sh
   ```

   El resultado es `dist/PalabrasProhibidas-<versión>.zip`, listo para importar como mod local o subir a Thunderstore.

El repositorio no contiene archivos del juego. El mod se compila contra las DLL de tu propia instalación.

## Cómo funciona, en breve

1. El juego de cada jugador ya captura el micrófono para el chat de voz (Photon Voice). El mod añade un procesador a ese flujo y copia el audio; nunca abre el micrófono por su cuenta.
2. Un hilo aparte pasa el audio a Vosk, un reconocedor de voz sin conexión, y busca las palabras prohibidas del nivel.
3. Si hay una, el cliente envía al anfitrión un evento mínimo de Photon. No viaja audio.
4. El anfitrión crea un enemigo con la función del propio juego, cambia su primer punto de aparición para que salga junto a quien habló, y lo retira al cabo de un rato.
5. El anfitrión sortea las palabras al empezar cada nivel y las publica en las propiedades de la sala, para que todos escuchen las mismas.

Más detalle en [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md).

## Licencia

MIT, ver [LICENSE](LICENSE). Los componentes de terceros conservan sus licencias: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
