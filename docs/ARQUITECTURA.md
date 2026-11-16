# Arquitectura de ForbiddenWords

Notas internas, iguales en los dos repositorios (ForbiddenWords y PalabrasProhibidas): cómo funciona el mod por dentro y qué se aprendió del juego al hacerlo. Comprobado en la versión v0.5.1 del juego.

## Recorrido de una palabra

```
micrófono ─► Photon Voice (juego) ─► Oido ─► cola ─► Reconocedor (hilo) ─► Vosk
                                                            │ palabra prohibida
                                                            ▼
                             Plugin.Update ─► Red.DeteccionLocal
                                                  │ cliente: evento 147 al host
                                                  ▼
                                  host: Aparicion.Pedir ─► enemigo junto al jugador
                                                  │ evento 148 a todos
                                                  ▼
                                          Pantalla.MostrarDicha
```

## Un código, dos mods

ForbiddenWords (inglés) y PalabrasProhibidas (español) comparten todos los `.cs`. Lo que cambia:

| | ForbiddenWords | PalabrasProhibidas |
|---|---|---|
| `.csproj` | define `INGLES` | no la define |
| `scripts/idioma.sh` | modelo `vosk-model-small-en-us-0.15` | modelo `vosk-model-small-es-0.42` |
| Carpeta del proyecto | `src/ForbiddenWords` | `src/PalabrasProhibidas` |
| `thunderstore/`, README | en inglés | en español |

- `Plugin.cs` fija con `#if INGLES` el GUID, el nombre y el idioma; `Listas.cs`, la lista de fábrica.
- Para llevar un cambio de un repositorio al otro basta copiar los `.cs` y cambiar la versión en los dos.
- Los dos usan la misma marca y los mismos eventos de red, así que en una sala mezclada todos ven la lista y los mensajes. Cada uno solo reconoce las palabras que existen en su modelo.
- Si los dos están instalados a la vez, el primero que carga deja una marca en el `AppDomain` y el segundo se apaga con un aviso en el log. Sin eso, cada palabra invocaría dos enemigos.

## Audio: engancharse a la voz del juego

- El juego crea la voz local en `PlayerVoiceChat.PhotonVoiceCreated` y le añade su propio procesador con `AddPostProcessor`. El mod hace lo mismo justo después, con un parche de Harmony (`Oido.cs`).
- El procesador recibe cada fotograma de audio (48 000 Hz, mono, 16 bits en las pruebas), lo copia y lo devuelve intacto. Photon reutiliza el búfer, por eso hay que copiar.
- `LocalVoiceFramed.PushData` no hace nada si `TransmitEnabled` es falso. Consecuencia: con el micrófono silenciado el mod no oye nada. Se dejó así a propósito: solo cuenta lo que oyen los demás.
- En un jugador no hay voz, así que el mod solo funciona en salas.

## Vosk dentro de BepInEx

- `libvosk.dll` es nativa. Windows no busca DLL nativas en la carpeta del plugin, así que `CargaNativa` la carga por ruta completa con `LoadLibraryEx` y la opción de buscar sus dependencias junto a ella. Después, el `DllImport("libvosk")` de `Vosk.dll` la encuentra por nombre. Funciona también con Proton.
- Los gestores de mods (Gale, r2modman) aplanan las carpetas del paquete, y un modelo de Vosk necesita su estructura de carpetas. Por eso cada modelo viaja como un único `.zip` y se descomprime la primera vez en `BepInEx/cache/ForbiddenWords`.
- Los modelos pequeños no traen la lista de palabras en un archivo; se consulta con `Model.FindWord`.

## Reconocimiento: por qué dictado libre

Se probaron dos formas con voz sintética y la palabra "monstruo":

| | Gramática (`Sensitive`) | Dictado libre (`Strict`) |
|---|---|---|
| "mostro", "mostrador", "mostró", "monstruoso", "monstr" | todas salen como "monstruo" con confianza 1.0 | ninguna |
| Frases con la palabra | las detecta todas | se le escapa alguna |

En gramática Vosk solo puede elegir entre las palabras prohibidas y "desconocido", así que encaja en ellas cualquier cosa parecida y la confianza no sirve para filtrar. En dictado libre la palabra compite con todo el idioma.

Reglas del modo `Strict`:

- La palabra cuenta si su confianza es al menos `MinConfidence`.
- A mitad de frase (resultado parcial) solo cuenta si ya hay otra palabra detrás, porque la última todavía puede cambiar ("monstruo" → "monstruoso").
- Tras una detección hay 2 segundos en los que no se emite otra, para que una misma frase no cuente dos veces.

## Enemigos: crear, colocar y retirar

- `LevelGenerator.Instance.EnemySpawn(setup, posición)` crea el enemigo en red. No devuelve lo creado: se comparan los `EnemyParent` de antes y de después.
- Crear el enemigo junto al jugador no basta. Al aparecer, cada enemigo llama a `SemiFunc.EnemySpawn`, que lo teletransporta a `EnemyParent.firstSpawnPoint`, y el juego elige como primer punto el más alejado del camión. El mod sustituye ese punto por un `LevelPoint` a 4-12 m del jugador. Si el punto está ocupado, el juego asigna otro y reintenta; el mod vuelve a poner el suyo hasta que aparece.
- Al crear enemigos con el nivel ya generado, el juego no les registra a los jugadores (`Enemy.PlayerAdded`); sin eso no los ven. El mod lo hace a mano.
- El juego nunca destruye enemigos: los desactiva y los vuelve a sacar con temporizadores. Para retirar uno se usa `SpawnedTimerSet(0)`, igual que cuando se le acaba su tiempo, y después se mantiene `DespawnedTimer` muy alto para que no vuelva. Desaparece de verdad al cambiar de nivel.
- `Enemy.Health.spawnValuable = false` evita que suelte el núcleo al morir.
- Los `EnemySetup` con varios objetos (grupos) se excluyen por defecto: suelen llevar un objeto "director" pensado para crearse al generar el nivel.

## Red y rondas

- Dos eventos de Photon con `RaiseEvent`: 147 (cliente → host, "he dicho esta palabra") y 148 (host → todos, "este jugador dijo esta palabra"). Cada mensaje lleva una marca de texto, por si otro mod usa los mismos números.
- Quién habló se toma del remitente que da Photon, no del contenido. El host solo acepta palabras de la ronda actual, y los clientes solo aceptan anuncios del host.
- Las palabras y un número de ronda se publican en propiedades de la sala. Los que entran tarde las reciben solos.
- Para saber que ha empezado un nivel nuevo se compara el identificador de `LevelGenerator.Instance`, que es un objeto distinto en cada nivel.
- La lista no se muestra al generarse el nivel (aún se ve la pantalla de carga), sino cuando `GameDirector.currentState` es `Main` y `LoadingUI.levelAnimationCompleted`.

## Pantalla

`BigMessageUI` del juego no dio el resultado esperado (no se vio en el centro), así que los mensajes van en un lienzo propio por encima de todo, con la fuente de `BigMessageUI.instance.Text`. Mismo enfoque que el aviso de PlayerMonster.

## Pendiente

- Probar con más de dos jugadores.
- Modo en el que solo el host reconoce la voz de todos (los demás no necesitarían el mod).
- Base para el siguiente mod: decir el nombre de un objeto para poder cargarlo.
