# PalabrasProhibidas

**Cuidado con lo que dices. El juego está escuchando.**

Cada nivel tiene unas **palabras prohibidas**. Si dices una por el micrófono, aparece a tu lado un enemigo real del nivel, persigue al grupo durante un minuto y desaparece. A todos les sale un mensaje rojo enorme con quién dijo qué.

La lista crece con cada nivel superado: cuanto más lejos llega la partida, más difícil es hablar.

Es la primera versión pública. Funciona en multijugador, pero puede tener fallos. Cualquier comentario es bienvenido.

> **Do you speak English?** This mod listens for Spanish. The English version is [ForbiddenWords](https://thunderstore.io/c/repo/p/Tommy777/ForbiddenWords/).

## Cómo se juega

- **Al empezar el nivel** salen las palabras prohibidas en el centro de la pantalla durante unos segundos. Hay que memorizarlas.
- **Si alguien dice una por el chat de voz**, aparece un enemigo elegido al azar entre los de ese nivel, a entre 4 y 12 metros de quien la dijo.
- **Sin espera y sin tope por defecto.** Si se dice tres veces, salen tres enemigos.
- **Los enemigos invocados se van a los 60 segundos** y no sueltan núcleo al morir. Son un castigo, no un botín.
- **2 palabras en el primer nivel y una más por cada nivel superado**, hasta 8. Se sortean de nuevo en cada nivel.
- **Lista de fábrica** de unas 80 palabras de las que se dicen jugando (*cuidado*, *carro*, *ayuda*...), o **una lista propia**.
- **Este paquete escucha español.** El inglés es un mod aparte, [ForbiddenWords](https://thunderstore.io/c/repo/p/Tommy777/ForbiddenWords/).

## Requisitos

- **El anfitrión necesita el mod.** Es quien sortea las palabras y crea los enemigos.
- **Cada jugador cuya voz deba contar necesita el mod**, en la misma versión. Quien no lo tenga ve los enemigos, pero su voz no cuenta y no le salen los mensajes.
- **Solo salas multijugador.** El mod escucha el chat de voz del propio juego, que no existe en un jugador. Para jugar solo, crea una sala.
- **Un micrófono silenciado no cuenta.** El mod oye exactamente lo que el juego envía a los demás, nada más.
- **Instala solo uno de los dos.** PalabrasProhibidas y ForbiddenWords son el mismo mod en dos idiomas; si están los dos, el segundo se apaga solo. La sala debe ponerse de acuerdo en uno: el anfitrión elige las palabras y cada modelo de voz solo entiende su idioma.

## Privacidad

El reconocimiento de voz se hace **sin conexión, en tu propio PC** ([Vosk](https://alphacephei.com/vosk/)). No se envía audio ni texto a ningún sitio. Lo único que llega al anfitrión es "este jugador dijo esta palabra prohibida". Nada de lo que dices se escribe en el log, salvo que actives `VerboseLog`.

## Configuración

`BepInEx/config/tommy777.palabrasprohibidas.cfg`, o el menú de mods del juego si tienes REPOConfig. Las opciones marcadas con *anfitrión* solo importan en el anfitrión.

| Opción | Por defecto | Qué hace |
|---|---|---|
| `Words.CustomList` | vacío | *Anfitrión.* Tus propias palabras, separadas por comas. Vacío = lista de fábrica. |
| `Words.StartingWords` | `2` | *Anfitrión.* Palabras en el primer nivel. |
| `Words.WordsPerLevel` | `1` | *Anfitrión.* Palabras de más por cada nivel superado. |
| `Words.MaxWords` | `8` | *Anfitrión.* Máximo de palabras a la vez. |
| `Words.ListSeconds` | `8` | Segundos que se muestra la lista al empezar el nivel. |
| `Recognition.Mode` | `Strict` | `Strict`: casi nunca salta por error, se le escapa alguna. `Sensitive`: casi no se le escapa ninguna, pero saltan palabras parecidas o a medio decir. |
| `Recognition.MinConfidence` | `0.5` | Súbela si saltan palabras que nadie dijo; bájala si se le escapan las de verdad. |
| `Enemy.Seconds` | `60` | *Anfitrión.* Cuánto se queda un enemigo invocado. `0` = para siempre. |
| `Enemy.Cooldown` | `0` | *Anfitrión.* Segundos entre invocaciones. `0` = sin espera. |
| `Enemy.MaxAtOnce` | `0` | *Anfitrión.* Tope de enemigos invocados vivos. `0` = sin límite. |
| `Enemy.AllowGroups` | `false` | *Anfitrión.* Invocar también enemigos de grupo. Puede fallar. |
| `Debug.VerboseLog` | `false` | Escribe en el log lo que el reconocedor oye de tu micrófono. Solo para buscar fallos. |

**Las palabras propias** tienen que ser palabras sueltas, en minúsculas y con sus tildes, y existir en el vocabulario del modelo de voz. Las desconocidas se saltan (el log del anfitrión dice cuántas). Las palabras largas y distintivas se reconocen mejor.

## Límites conocidos

- El reconocimiento no es perfecto. En modo `Strict`, una palabra dicha muy rápido en mitad de una frase puede escaparse; una palabra dicha sola o al final de la frase tarda cerca de un segundo en contar.
- Los enemigos que vienen en grupo están excluidos por defecto.
- Sin espera y sin tope, una sala que no pare de gritar las palabras puede llenar el nivel de enemigos. Si el rendimiento baja, pon un valor en `Enemy.MaxAtOnce`.
- La descarga pesa unos 70 MB porque incluye el modelo de voz. Se descomprime una sola vez en `BepInEx/cache/PalabrasProhibidas`.
- Probado en la versión v0.5.1 del juego, en Windows y en Linux con Proton.

## Avisar de un problema

Adjunta el `BepInEx/LogOutput.log` del anfitrión y del jugador afectado, y di qué palabras estaban activas.

## Créditos

- Hecho por **Tommy777**. Buena parte del código se escribió con asistencia de IA.
- Reconocimiento de voz: [Vosk](https://alphacephei.com/vosk/), de Alpha Cephei, con el modelo `vosk-model-small-es-0.42` (Apache 2.0). Ver `THIRD_PARTY_NOTICES.md`.
- Usa los enemigos del propio juego y no incluye código ni recursos del juego.
