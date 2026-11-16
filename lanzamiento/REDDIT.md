# Mensaje para Reddit

Un solo mensaje para los dos mods. Antes de publicar, lee las normas del subreddit: muchos limitan la autopromoción o piden una etiqueta (flair) concreta para mods. Sustituye `[ENLACE INGLÉS]` y `[ENLACE ESPAÑOL]` por las páginas de Thunderstore de ForbiddenWords y PalabrasProhibidas. Un clip corto de alguien diciendo la palabra y el mensaje rojo saltando vale más que todo el texto.

El subreddit es de habla inglesa, así que la versión principal va en inglés. Debajo está la misma en español por si la quieres para otro sitio.

---

## Versión en inglés

**Título:**

> [Mod] Say a forbidden word in voice chat and a monster spawns next to you — ForbiddenWords 1.0 (English and Spanish), feedback wanted

**Texto:**

Hi! I made a mod that turns R.E.P.O.'s voice chat against you, and the first public version is up.

**The idea:** every level has a few forbidden words, shown on screen when the level starts. Say one into your mic and a real enemy from that level spawns a few metres from you, hunts for a minute, then vanishes. Everyone gets a big red "TOMMY SAID "CART"" with a screen flash, so there is no hiding who did it.

**How it works in a run:**

- 2 words on the first level, one more for every level you clear, up to 8. New words every level.
- The built-in list is everyday things you say while playing: *careful*, *cart*, *help*, *behind*... About 80 words.
- No cooldown and no cap by default. Say it three times, get three enemies. Both can be limited in the config.
- Summoned enemies drop no orb. They are a punishment, not a farm.
- You can write your own word list. Inside jokes work great.

**Two versions, pick one:**

- **English — ForbiddenWords:** [ENLACE INGLÉS]
- **Español — PalabrasProhibidas:** [ENLACE ESPAÑOL]

They are the same mod; each one carries the voice model and word list for its language. A lobby should agree on one, and you should not install both (the second one just switches itself off).

**About the microphone:** recognition is fully offline, on your own PC (it uses Vosk). No audio or text goes anywhere. The mod listens to the game's own voice chat stream, so it only hears what your friends already hear, and a muted mic does not count. The only thing sent to the host is "this player said this word".

**What you need:** the host needs the mod, and so does every player whose voice should count (same version). Multiplayer lobbies only, since singleplayer has no voice chat. Each download is 70 to 80 MB because of the voice model.

**What I need:** people to break it. It has only been tested with two players, on v0.5.1. Recognition is not perfect: the default mode almost never triggers by mistake but sometimes misses a word said fast; there is a more sensitive mode if you prefer chaos. Tell me which words are too easy or too cruel, and if something breaks, the host's `LogOutput.log` helps a lot.

Honest note: a lot of the code was written with AI assistance, and it's tagged that way on Thunderstore. It uses the game's own enemies and includes no game code or assets.

Thanks for reading, and good luck staying quiet.

---

## Versión en español

**Título:**

> [Mod] Di una palabra prohibida por el micro y te aparece un monstruo al lado — PalabrasProhibidas 1.0 (español e inglés), busco opiniones

**Texto:**

¡Hola! Hice un mod que pone el chat de voz de R.E.P.O. en contra de los jugadores, y ya está la primera versión pública.

**La idea:** cada nivel tiene unas palabras prohibidas, que salen en pantalla al empezar. Si dices una por el micro, aparece a pocos metros un enemigo real de ese nivel, persigue al grupo un minuto y desaparece. A todos les sale en rojo "TOMMY DIJO "CARRO"" con un destello en la pantalla, así que no hay forma de esconder quién fue.

**Cómo funciona en una partida:**

- 2 palabras en el primer nivel y una más por cada nivel superado, hasta 8. Cambian en cada nivel.
- La lista de fábrica son cosas que se dicen jugando: *cuidado*, *carro*, *ayuda*, *detrás*... Unas 80 palabras.
- Sin espera y sin tope por defecto: si se dice tres veces, salen tres enemigos. Las dos cosas se pueden limitar en la configuración.
- Los enemigos invocados no sueltan núcleo. Son un castigo, no una granja.
- Se puede escribir una lista propia. Las bromas internas funcionan muy bien.

**Dos versiones, se elige una:**

- **Español — PalabrasProhibidas:** [ENLACE ESPAÑOL]
- **English — ForbiddenWords:** [ENLACE INGLÉS]

Son el mismo mod; cada una lleva el modelo de voz y la lista de su idioma. La sala debe ponerse de acuerdo en una, y no hay que instalar las dos (la segunda se apaga sola).

**Sobre el micrófono:** el reconocimiento es totalmente sin conexión, en tu propio PC (usa Vosk). No se envía audio ni texto a ningún sitio. El mod escucha el chat de voz del propio juego, así que solo oye lo que ya oyen tus amigos, y un micro silenciado no cuenta. Lo único que llega al anfitrión es "este jugador dijo esta palabra".

**Qué hace falta:** el anfitrión necesita el mod, y también cada jugador cuya voz deba contar (misma versión). Solo salas multijugador, porque en un jugador no hay chat de voz. Cada descarga pesa entre 70 y 80 MB por el modelo de voz.

**Qué necesito:** gente que lo rompa. Solo está probado con dos jugadores, en la v0.5.1. El reconocimiento no es perfecto: el modo por defecto casi nunca salta por error, pero a veces se le escapa una palabra dicha rápido; hay un modo más sensible para quien prefiera el caos. Cuéntenme qué palabras son demasiado fáciles o demasiado crueles, y si algo falla, el `LogOutput.log` del anfitrión ayuda mucho.

Nota honesta: buena parte del código está escrito con ayuda de IA, y así está etiquetado en Thunderstore. Usa los enemigos del propio juego y no incluye código ni recursos del juego.

Gracias por leer, y suerte quedándose callados.
