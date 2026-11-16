# Historial de cambios

## 1.0.0 — primera versión pública

- Palabras prohibidas por nivel, mostradas en pantalla al empezar.
- Reconocimiento de voz sin conexión (Vosk) sobre el micrófono del juego de cada jugador; al anfitrión solo le llega la detección.
- Decir una palabra invoca un enemigo al azar del nivel junto a quien la dijo. Se va a los 60 segundos y no suelta núcleo.
- Sin espera y sin tope por defecto; las dos cosas son opciones.
- La lista crece con los niveles superados: 2 palabras, una más por nivel, hasta 8.
- Lista de fábrica en español, o una lista propia del anfitrión. El inglés es un mod aparte, ForbiddenWords.
- Mensajes en el centro de la pantalla con la fuente del juego, destello, distorsión y sacudida de cámara.
- Modos de reconocimiento `Strict` y `Sensitive`, con umbral de confianza ajustable.
