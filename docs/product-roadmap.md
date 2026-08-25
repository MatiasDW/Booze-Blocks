# Roadmap de producto: Booze & Blocks

## Diagnostico actual

El prototipo ya demuestra movimiento, hordas, tareas, crisis, mejoras y personalizacion. Todavia no es un vertical slice comercial porque carece de audio, onboarding, cierre de partida completo, contenido suficiente, multiplayer real y mediciones de rendimiento en build.

| Area | Estado | Brecha principal |
| --- | --- | --- |
| Identidad | Clara | Falta convertir el humor en momentos audiovisuales memorables |
| Movimiento | Funcional | Falta pulir anticipacion, impacto, recuperacion y camara |
| Cooperacion | Inicial | Las tareas aun pueden resolverse casi en paralelo y en silencio |
| Rejugabilidad | Inicial | Solo hay una arena, cuatro crisis y cinco mejoras |
| Progresion | Parcial | Ya hay resultados y rematch; faltan desafios y desbloqueos persistentes |
| Visuales | Prototipo estilizado | Geometria compartida y jerarquia mas clara; faltan assets, animacion y VFX de produccion |
| Audio | Prototipo procedural | Hay musica y senales basicas; falta audio de produccion y ambiente 3D |
| Online | Disenado, no conectado | Menu listo, pero sin Sessions, Relay ni Netcode |
| Rendimiento | Instrumentado | Overlay y marcadores listos; faltan mediciones de builds y percentiles sostenidos |
| Produccion | Fragil | `PrototypeBootstrap` concentra unas 926 lineas y dificulta crear contenido |

## Principios de engagement

La planificacion usa tres necesidades motivacionales como filtro:

- Competencia: el jugador entiende por que fallo, aprende rutas y siente que mejora.
- Autonomia: el equipo elige tareas, herramientas, mejoras y como responder a una crisis.
- Relacion social: las mejores jugadas requieren coordinarse, rescatarse y reirse juntos.

La investigacion de Ryan, Rigby y Przybylski relaciona autonomia, competencia y conexion con motivacion posterior para jugar. El deep dive de Overcooked muestra que simplemente agregar jugadores no crea cooperacion y que los roles se vuelven rutinarios si el nivel no altera el flujo. Fall Guys priorizo objetivos explicables en una frase, arte limpio y momentos improbables producidos por interacciones entre jugadores.

Fuentes:

- [Motivational Pull of Video Games](https://selfdeterminationtheory.org/SDT/documents/2006_RyanRigbyPrzybylski_MandE.pdf)
- [Building truly cooperative play in Overcooked](https://www.gamedeveloper.com/design/game-design-deep-dive-building-truly-cooperative-play-in-i-overcooked-i-)
- [How Mediatonic designed Fall Guys](https://www.pcgamesinsider.biz/feature/69833/how-mediatonic-made-the-takeshis-castle-of-video-games-in-fall-guys/)

## Bucle objetivo

Una sesion objetivo debe poder explicarse asi:

> Sobrevivan seis oleadas, mantengan viva la parrillada y resuelvan juntos los desastres antes de quedarse sin booze.

Arco de partida recomendado:

1. Preparacion de 20 a 30 segundos: ver crisis, elegir una herramienta y repartir tareas.
2. Oleada inicial legible: ensena entradas, parrilla y booze sin castigo severo.
3. Incidente dinamico: obliga a cambiar roles y cruzar el patio.
4. Descanso y mejora compartida: el equipo discute una eleccion.
5. Escalada: combinacion de horda, tareas y entorno, no solo mas enemigos.
6. Final corto y claro: ultimo evento, resultado, estadisticas, recompensas y rematch inmediato.

Duracion inicial a probar: 6 a 9 minutos. No se debe fijar definitivamente sin playtests.

## Fase 0: evidencia antes de contenido

Objetivo: saber si el prototipo actual es divertido y cuanto cuesta realmente.

Trabajo:

- Crear Development Builds Apple Silicon con escenarios de 1, 4 y 8 jugadores simulados.
- Capturar CPU, GPU, Rendering, Physics, memoria y GC durante 10 minutos.
- Registrar FPS promedio, percentil 1, frame mas lento, memoria maxima y temperatura sostenida.
- [Hecho] Agregar `ProfilerMarker` a actualizacion, poblacion, asignacion y presion de horda.
- [Hecho] Agregar overlay `F3` sin asignaciones por frame para FPS, peor frame, frames lentos y memoria C#.
- Ejecutar cinco sesiones individuales observadas para medir control y comprension; la cooperacion real se valida despues de conectar dos clientes en la Fase 2.
- Registrar primer momento de confusion, primera risa, primera estrategia util y causa de derrota.
- Preguntar al final: que entendieron, que fue injusto y si jugarian otra ronda.

Criterios internos de salida:

- Cero errores y cero GC estable en gameplay despues del warmup.
- 60 FPS sostenidos en el equipo objetivo, no solamente en el Mac M5.
- Al menos 4 de 5 jugadores entienden el objetivo principal sin explicacion verbal externa.
- Al menos 3 de 5 jugadores eligen jugar una segunda ronda.

Estos umbrales son objetivos del proyecto, no promedios de la industria.

## Fase 1: feel, legibilidad y cierre

Objetivo: hacer divertido un solo nivel antes de agregar mas niveles.

Gameplay:

- Afinar aceleracion, giro, salto, empuje, caida y recuperacion.
- Agregar rescate de companero derribado para reforzar cooperacion.
- Dar telegraph visual y sonoro antes de una oleada, entrada o crisis.
- Agregar pantalla de resultados con puntaje, oleada, asados, rescates y mejor momento.
- Implementar `Reintentar`, `Volver al lobby` y rematch con la misma semilla.

Estado aplicado en el prototipo:

- Pantalla de resultados con puntaje, oleada, tiempo, tareas, misma semilla y nueva partida.
- Objetivo contextual que prioriza comida por quemarse, booze bajo, refill, defensa y preparacion.
- Tutorial inicial de movimiento, interaccion, defensa y bebida.
- Musica y senales procedurales sin aumentar el peso con archivos de audio.
- Banner visual para oleadas, tareas, distracciones, barricadas y derribos.
- Rescate interactivo de companeros derribados con puntuacion y registro en resultados.
- Pausa con audio, sensibilidad, camera shake, avisos visuales, reinicio y vuelta al menu.
- Particulas compartidas para golpes, booze, parrilla, defensas, tareas y rescates.
- Barricadas con construccion de 2,75 segundos, progreso visual y cancelacion al alejarse.

Feedback audiovisual minimo:

- Pasos, salto, golpe, caida, bebida, herramienta, parrilla y alerta de oleada.
- Musica adaptativa de dos capas: fiesta estable y caos de oleada.
- Particulas simples para impacto, humo, booze, comida lista y comida quemada.
- Camera impulse leve y configurable, sin marear al jugador.
- Contorno o icono para objetivo interactuable y companero en peligro.

Onboarding:

- Tutorial contextual de menos de 60 segundos dentro del patio.
- Objetivo actual visible con verbo y distancia: `Enciende la parrilla`, no texto generico.
- Iconos consistentes para `E`, `F`, `Q` y mando.
- Opciones de tamano de UI, camera shake, subtitulos y modo daltonismo.

Criterio de salida: un jugador nuevo entiende movimiento, booze, parrilla, defensa y derrota dentro de su primera ronda.

## Fase 2: multiplayer tecnico temprano

Objetivo: validar el juego real entre dos Macs antes de producir mucho contenido.

Orden:

1. Instalar Multiplayer Services SDK, Netcode for GameObjects y Multiplayer Tools.
2. Autenticacion anonima y Sessions con crear/unirse por codigo.
3. Sincronizar dos jugadores, ready state y comienzo de partida.
4. Host autoritativo para inventario, tareas, oleadas, puntaje y resultado.
5. Prediccion local basica del jugador y correccion visual gradual.
6. Snapshot agrupado de horda, nunca un `NetworkObject` por nino.
7. Desconexion clara; el MVP puede terminar la partida si cae el host.
8. Escalar pruebas en orden: 2, 4 y finalmente 8 jugadores.

Pruebas obligatorias:

- 50, 100 y 180 ms de latencia.
- 1%, 3% y 5% de perdida de paquetes.
- Jitter, cliente lento, entrada tardia y salida voluntaria.
- Dos regiones distintas antes de prometer soporte internacional.

Unity recomienda el Multiplayer Services SDK para Sessions/Relay y ofrece Multiplayer Play Mode y Network Simulator para estas pruebas. Relay encaja con el modelo listen-server y evita exponer IPs, pero el host sigue cargando simulacion y ancho de banda.

Fuentes:

- [Unity Multiplayer overview](https://docs.unity.com/en-us/multiplayer)
- [Relay servers](https://docs.unity.com/en-us/mps-sdk/networking/relay-servers)
- [Multiplayer Play Mode](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.multiplayer.playmode.html)

## Fase 3: cooperacion que cambia durante la partida

Objetivo: evitar que cada jugador repita un rol fijo sin comunicarse. Esta fase se valida sobre el multiplayer de dos jugadores, no con NPC simulados.

Agregar solo dos sistemas primero:

1. Recurso transportable: hielo o combustible ocupa las manos y debe cruzar el mapa.
2. Incidente dinamico: generador, vecino furioso o fuga de cooler cambia rutas y prioridades.

Reglas:

- Cada incidente debe mover al menos a un jugador de su tarea habitual.
- Ninguna tarea debe resolverse manteniendo un boton sin decidir.
- Siempre debe existir una respuesta de emergencia imperfecta.
- El juego debe anunciar peligro antes de castigar.
- La dificultad escala por combinacion y tiempo, no solo por cantidad de ninos.

Sistemas sociales livianos:

- Ping contextual: ayuda, aqui, peligro y objetivo.
- Emotes visibles durante lobby y descansos.
- Rescates y asistencias registrados en resultados.
- Bonificaciones por acciones encadenadas del equipo, no por robar puntos individuales.

Contenido objetivo del primer vertical slice:

- 1 patio muy pulido con 2 configuraciones de obstaculos.
- 3 tareas completas: booze, parrilla y hielo/generador.
- 6 crisis con contrajuego, no solo modificadores numericos.
- 4 patrones de entrada claramente anunciados.
- 12 mejoras repartidas entre supervivencia, defensa, logistica y distraccion.

## Fase 4: rejugabilidad y progresion sana

Objetivo: dar razones para volver sin volver injusta la partida.

Metaprogresion:

- XP de cuenta por completar partidas, rescatar y cooperar.
- Desbloqueos cosmeticos: ropa, gorros, bigotes, animaciones y poses.
- Desafios cortos rotativos: servir sin quemar, bloquear entradas o ganar con una crisis.
- Album de incidentes absurdos y estadisticas personales.
- Semillas compartibles y tabla local de mejores puntajes.
- Nada de dano, vida o velocidad permanente comprable.

Variacion por partida:

- Crisis con contrajuego fisico.
- Posiciones de estaciones elegidas entre puntos validados.
- Obstaculos modulares que cambian rutas.
- Objetivo bonus opcional con riesgo/recompensa.
- Mutadores semanales solo despues de que el contenido base sea estable.

No priorizar aun:

- Battle pass, tienda real o moneda premium.
- Procedural generation completa.
- Decenas de recetas e inventario profundo.
- Ranked competitivo.

## Fase 5: direccion visual y audio de produccion

Objetivo visual: caricatura de parrillada adulta, legible en caos, no realismo.

Direccion propuesta:

- Siluetas humanas simples con cabeza, manos y accesorios grandes.
- Adultos altos y anchos; ninos claramente mas pequenos y agrupados.
- Paleta calida de fiesta contra colores frios de peligro y objetivos.
- Materiales planos con variacion de vertices, pocos shaders y texturas pequenas.
- Iluminacion de tarde, sombras solo en protagonistas y objetos grandes.
- VFX con formas graficas: estrellas de golpe, lineas de velocidad, humo y salpicaduras.
- Objetos importantes con animacion, luz o silueta; decoracion quieta y menos contrastada.

Antes de migrar a URP:

- Medir draw calls y tiempo de render en Built-in.
- Crear una escena A/B pequena con el look objetivo.
- Confirmar que URP mejora el pipeline de produccion, no asumir que mejora FPS.
- Si se migra, reemplazar el uso masivo de `MaterialPropertyBlock` por una estrategia compatible con SRP Batcher/GPU Resident Drawer.

Audio propuesto:

- Musica por capas en loops cortos y comprimidos.
- Variaciones de pitch para pasos, golpes y voces para evitar repeticion.
- Audio 3D solo para fuentes que orientan al jugador.
- Compresion Vorbis para musica/ambiente; PCM o ADPCM para SFX muy cortos segun medicion.
- Limite de voces simultaneas para que 96 ninos no creen 96 AudioSources.

Fall Guys mantuvo arte simple para preservar legibilidad con caos en pantalla. Booze & Blocks debe mejorar detalle y personalidad sin perder esa ventaja.

## Fase 6: arquitectura de contenido y rendimiento

Refactor previo a producir mas mapas:

- Dividir `PrototypeBootstrap` en factories de jugador, arena, estaciones y UI.
- Mover crisis, mejoras, oleadas y apariencia a ScriptableObjects.
- Crear prefabs modulares para que un disenador pueda montar variantes sin editar C#.
- Reemplazar IMGUI por UI Toolkit de produccion despues de estabilizar los flujos.
- Mantener modelos puros y eventos discretos para facilitar netcode y tests.

Optimizaciones candidatas, solo si el profiler las justifica:

- Combinar o static-batchear por zonas las 142 piezas decorativas inmoviles.
- Reducir Renderer y Transform de detalles pequenos repetidos.
- Mantener materiales compartidos y medir SetPass/draw calls con Frame Debugger.
- Actualizar movimiento visual de ninos en un manager central si 96 `Update` son costosos.
- Reducir steering por distancia y evitar SphereCast para unidades lejanas.
- Agregar culling y LOD solo cuando el arte final lo necesite.
- Cachear la camara en `WorldBillboard` y detener billboards fuera de pantalla.
- Eliminar strings y layout por frame al reemplazar IMGUI.
- No agregar Jobs, Burst, Addressables o DOTS hasta identificar un cuello real.

Presupuestos iniciales para 60 FPS:

| Area | Objetivo |
| --- | ---: |
| Frame total | 16.67 ms |
| Horda host | <= 3 ms |
| Jugadores/fisica | <= 2 ms |
| Scripts restantes | <= 2 ms |
| Render | <= 8 ms |
| GC estable | 0 B/frame |
| Memoria release | Definir despues del primer build medido |
| Red de horda | Definir despues del prototipo de snapshots |

Unity recomienda perfilar una Development Build en la plataforma objetivo, porque Play Mode no representa el costo real. Para draw calls se deben usar Rendering Stats, Profiler y Frame Debugger antes de elegir batching o una migracion de pipeline.

Fuentes:

- [Profiling your application](https://docs.unity3d.com/2022.2/Documentation/Manual/profiler-profiling-applications.html)
- [Choosing a draw-call optimization method](https://docs.unity3d.com/6000.0/Documentation/Manual/optimizing-draw-calls-choose-method.html)
- [UI Toolkit performance](https://docs.unity3d.com/6000.0/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html)

## Fase 7: demo y criterio de producto

La demo publica solo se prepara cuando:

- Dos a cuatro jugadores pueden crear, unir, jugar, terminar y repetir sin ayuda del desarrollador.
- Hay una ronda completa de 6 a 9 minutos con tres tareas y crisis dinamicas.
- La derrota se percibe justa y explica la causa.
- Los controles soportan teclado/mouse y mando.
- Opciones, accesibilidad basica y reconexion de lobby estan probadas.
- Build release mantiene el objetivo de rendimiento en hardware objetivo.
- Hay logs anonimos opt-in o planillas de playtest para medir abandono y rematch.

Metricas de producto a registrar:

- Tiempo hasta entender el objetivo.
- Tiempo hasta primera accion cooperativa.
- Porcentaje que termina una partida.
- Porcentaje que pulsa rematch.
- Causa de derrota y tarea ignorada.
- Uso de herramientas, crisis y mejoras.
- FPS percentil 1, memoria maxima y desconexiones.

## Orden inmediato

No comenzar por otro mapa ni por mas skins. El bloque de instrumentacion y cierre basico ya esta aplicado. Lo siguiente es:

1. Medir 1, 4 y 8 jugadores simulados en Development Build.
2. Ejecutar cinco sesiones con `docs/playtest-session.md`.
3. Corregir los problemas repetidos por al menos tres jugadores.
4. Conectar dos clientes con Sessions, Relay y Netcode.
5. Validar el rescate de companeros y una tarea realmente compartida online.
