# Booze & Blocks

Prototipo de party survival cooperativo en Unity: adultos torpes controlan una horda caricaturesca mediante movimiento, posicionamiento y distracciones.

## Estado actual

La primera base jugable incluye:

- Movimiento fisico controlado con aceleracion, inercia, salto, control aereo y recuperacion.
- Adulto cartoon low-poly con brazos y piernas procedurales, inercia exagerada y derribo fisico temporal; no usa rig, Animator ni texturas.
- Estadisticas separadas de `Health`, `Buzz` y `Balance`.
- Horda de NPC clasicos con salud individual, reaccion al golpe, malla humana estatica de bajo costo, seguimiento directo, separacion por grid espacial y presion agregada; no usa NavMesh, machine learning ni servicios de IA.
- Preparacion inicial de 25 segundos, oleadas de 60 segundos, descansos de 15 segundos y aumento de dificultad cada 2 oleadas.
- Registro dinamico de jugadores sin un limite fijo en la simulacion; objetivo inicial de red: 8 jugadores.
- Distracciones con prioridad, radio, duracion y capacidad.
- Pooling adaptativo que conserva y reutiliza la capacidad maxima alcanzada por la sesion.
- Empuje direccional agregado de la horda y derribo hibrido: gameplay estable en capsula, presentacion fisica exagerada.
- Inventario por jugador con botella, escoba de 24 usos o sarten de 16 usos y mejoras durante la partida.
- Animacion procedural de beber con brazo, botella visible y etiqueta `BOOZE`, sincronizada tambien al detectar consumo remoto.
- Estacion compartida que prepara mezclas por tiempo antes de permitir rellenar la botella.
- Parrilla cooperativa con combustible, calentado, coccion, servicio y riesgo de quemar la comida.
- Crisis aleatoria por sesion que altera parrilla, mezcla, distracciones o barricadas.
- Puntaje compartido por asados, entradas bloqueadas, distracciones y oleadas superadas.
- Eleccion de tres mejoras de equipo cada dos oleadas, en lugar de aumentos automaticos.
- `BOOZE LAB` reconocible por su letrero, baliza, barril, mezclador, botellas y luz de estado.
- Cuatro entradas fisicas con barricadas de 140 puntos de integridad. Pallets, coolers y basureros se rompen en tres secciones; las mesas en dos. Cada seccion perdida aumenta gradualmente el flujo de la horda hasta abrir la entrada completa.
- Personalizacion ampliada con diez colores de ropa, seis tonos de piel, pantalon y cabello independientes, seis gorros, cuatro estilos de vello facial y lentes.
- Menu principal con juego individual, configuracion multijugador, personalizacion, opciones y salida; oleadas y cronometro permanecen pausados mientras esta abierto.
- Multiplayer host-autoritativo con Unity Multiplayer Services, autenticacion anonima, sesiones por codigo y Relay. Sincroniza hasta 8 jugadores, personalizacion, inputs, acciones, horda agrupada, tareas, barricadas, ronda y puntuacion sin crear un `NetworkObject` por nino.
- Opciones persistentes de calidad grafica, resolucion, pantalla completa, VSync, limite de FPS, volumen maestro/musica/efectos y sensibilidad del mouse.
- Camion de helados desbloqueado en la oleada 2, con activacion temporal y cooldown reutilizable.
- Patio de parrillada ampliado con grill, barra, beer pong, lounge, sonido, mesas, carpa de comida, cooler, cobertizo, luces y senderos. Camion, globos y juguetes quedan como distracciones perifericas.
- Direccion visual low-poly suavizada con piezas biseladas compartidas, cercos redondeados, senderos organicos, vegetacion modular, cielo calido y carteles compactos; las colisiones conservan formas simples.
- Tests EditMode para modelos y reglas, mas un smoke test PlayMode del bucle completo.

La primera oleada llega progresivamente a 14 unidades para un jugador, 32 para cuatro y 56 para ocho durante sus primeros 20 segundos. Cada escalon de dificultad suma 6 y el tope configurable es 96. Consulta [el presupuesto de rendimiento](docs/performance-budget.md) para la formula, los objetivos y la matriz de profiling.

El prototipo usa el pipeline Built-in y materiales simples para reducir dependencias. Los personajes son mallas originales del proyecto; URP se incorporara al comenzar el vertical slice visual, creando y versionando correctamente sus assets de pipeline y perfiles de calidad.

Consulta [la auditoria comparativa](docs/comparative-gap-analysis.md) y [la auditoria de repositorios](docs/repository-audit.md) para ver que aprendimos de cada proyecto y sus restricciones de licencia. La capa online actual se describe en [arquitectura multijugador](docs/online-architecture.md).
El sistema propuesto para variar crisis, tareas, mejoras y entradas entre sesiones esta en [director de partidas](docs/replayability.md).
La priorizacion completa de engagement, multiplayer, arte, audio, rendimiento y playtests esta en [roadmap de producto](docs/product-roadmap.md).

## Requisitos

- Unity `6000.5.4f1`.
- En Mac con Apple Silicon, instala la variante Apple silicon del Editor y el modulo Mac Build Support.
- Modulo de compilacion para macOS o Windows segun la plataforma de prueba.
- Git LFS antes de agregar modelos, audio o texturas grandes.

Un MacBook Pro M5 con 32 GB es compatible y suficiente para desarrollar este prototipo. Durante desarrollo usa builds `Apple Silicon`; una build universal `Intel 64-bit + Apple Silicon` amplia compatibilidad, pero aumenta el tamano del juego.

## Ejecutar el prototipo

1. Instala Unity `6000.5.4f1` desde Unity Hub.
2. Abre esta carpeta como proyecto.
3. Espera que Package Manager termine de instalar las dependencias.
4. Ejecuta `Booze & Blocks > Create Prototype Scene` en el menu del Editor.
5. Abre `Assets/_Game/Scenes/Prototype.unity` si no se abre automaticamente y pulsa Play.
6. Usa `Personalizar personaje` y `Opciones` si lo necesitas; luego pulsa `Jugar solo` o `Enter`.

Para habilitar `Crear sala online` y `Unirse con codigo`, vincula el proyecto desde `Edit > Project Settings > Services` a una organizacion y proyecto de Unity Cloud. Sin ese identificador los botones permanecen deshabilitados y el modo offline sigue funcionando. El host inicia la partida desde la sala; los clientes esperan esa senal y vuelven al menu si se pierde el host.

Para entregar el juego a otra persona, ejecuta `Booze & Blocks > Build macOS App`. La aplicacion queda en `Builds/macOS/BoozeAndBlocks.app`; quien la recibe no necesita instalar Unity. macOS puede pedir abrirla mediante click derecho > `Abrir` mientras la build no este firmada/notarizada.

Acepta el reinicio del Editor si Unity lo solicita al activar el Input System nuevo.

Controles:

- Menu inicial: mouse para navegar; `Enter` inicia juego individual y `Esc` vuelve al menu principal.
- `WASD`, flechas o stick izquierdo: movimiento.
- Mover el mouse o el stick derecho: camara. `Esc` libera el cursor y un clic lo captura nuevamente.
- `E` o boton sur del gamepad: interactuar.
- `Espacio` o boton norte: saltar.
- `F`, click izquierdo o bumper derecho: usar objeto defensivo.
- `Q` o bumper izquierdo: beber una carga de la botella.
- Mantener `Tab`: mostrar objetivos, tareas, poblacion, puntuacion y detalles de la partida.
- `F3`: mostrar u ocultar FPS, peor frame, frames lentos y memoria C#.
- `Esc`: pausar la partida y abrir opciones rapidas.

`E` muestra un aviso cuando hay algo al alcance. `F` requiere haber recogido una escoba o sarten y `Q` solo consume una carga si falta BUZZ; si una accion no puede ejecutarse, el HUD explica el motivo.

La meta del prototipo es sobrevivir 480 segundos y maximizar el puntaje de equipo. Hay que preparar bebida en el `BOOZE LAB`, cocinar y servir asado antes de que se queme, recoger herramientas, construir bloqueos y coordinar distracciones; un asado servido entrega 250 puntos y recupera 25 de vida. Los ninos comienzan con 3 de vida: la escoba hace 1 de dano y la sarten 2; su salud aumenta 0,5 por escalon de dificultad. BUZZ alto aumenta hasta 30% la velocidad y 55% la fuerza y dano defensivos, a cambio de deriva, frenado lento y giros imprecisos. El camion se desbloquea en la oleada 2 y puede reutilizarse despues de su cooldown. Construir una barricada tarda 2,75 segundos, exige permanecer cerca y no cierra la entrada hasta completarse. Durante una oleada, los intentos de entrada y la presion exterior reducen su integridad; al perder secciones deja pasar aproximadamente un tercio o dos tercios del flujo. Destruida la ultima pieza, la entrada queda abierta y comienza un cooldown de 9 segundos antes de poder reconstruir. Un companero derribado puede levantarse acercandose y pulsando `E`. Al terminar las oleadas 2, 4, 6 y siguientes, el equipo elige una de tres mejoras con mouse o las teclas `1`, `2` y `3`. El resultado permite repetir la misma semilla, iniciar una nueva o volver al menu.

Para probar el escalado sin networking, selecciona `PrototypeBootstrap` antes de pulsar Play y cambia `Simulated Player Count` entre 1 y 8. Solo el primer adulto recibe input; los demas simulan integrantes conectados para probar reparto de objetivos y poblacion de horda.

En una build se puede repetir la carga sin tocar el Inspector con `open Builds/macOS/BoozeAndBlocks.app --args -boozePlayers 8 -boozeAutoStart`. Sustituye `8` por `1` o `4` para completar la matriz de profiling.

## Alcance inmediato

El prototipo conserva el modo offline y ahora incluye una primera implementacion online host-autoritativa. Movimiento remoto, interacciones, inventario, tareas, presion, oleadas y resultado se resuelven en el host; cada cliente predice solo su movimiento y recibe snapshots compactos a 10 Hz. No se sincroniza cada nino como un `NetworkObject` independiente.

Pendiente antes de una beta publica: vincular UGS y ejecutar pruebas reales con dos a ocho builds, agregar simulacion de latencia/perdida, medir ancho de banda, mejorar la correccion del movimiento y decidir migracion de host. La primera version termina la sesion si el anfitrion se desconecta.

La pantalla multijugador define capacidad de 2 a 8 jugadores, sala publica o solo por codigo y codigo de union. Relay elige automaticamente la region de menor latencia. Crear y unirse solo se habilitan cuando el proyecto tiene un `cloudProjectId`, para no presentar una conexion falsa como funcional.
