# Booze & Blocks

Prototipo de party survival cooperativo en Unity: adultos torpes controlan una horda caricaturesca mediante movimiento, posicionamiento y distracciones.

## Estado actual

La primera base jugable incluye:

- Movimiento fisico controlado con aceleracion, inercia, salto, control aereo y recuperacion.
- Adulto cartoon low-poly con brazos y piernas procedurales, balanceo y derribo visual; no usa rig, Animator ni texturas.
- Estadisticas separadas de `Health`, `Buzz` y `Balance`.
- Horda de NPC clasicos con una malla humana estatica de bajo costo, seguimiento directo, separacion por grid espacial y presion agregada; no usa NavMesh, machine learning ni servicios de IA.
- Oleadas de 20 segundos con 5 segundos de descanso y aumento de dificultad cada 2 oleadas.
- Registro dinamico de jugadores sin un limite fijo en la simulacion; objetivo inicial de red: 8 jugadores.
- Distracciones con prioridad, radio, duracion y capacidad.
- Pooling adaptativo que conserva y reutiliza la capacidad maxima alcanzada por la sesion.
- Empuje direccional agregado de la horda y derribo hibrido: gameplay estable en capsula, presentacion fisica exagerada.
- Inventario por jugador con botella, escoba o sarten, usos limitados y mejoras durante la partida.
- Estacion compartida que prepara mezclas por tiempo antes de permitir rellenar la botella.
- `BOOZE LAB` reconocible por su letrero, baliza, barril, mezclador, botellas y luz de estado.
- Cuatro entradas fisicas de horda que pueden bloquearse temporalmente con pallets, mesas, coolers o basureros; si todas estan cerradas, los nuevos spawns esperan.
- Personalizacion liviana por presets: color de ropa y piel, bigote, gorro de fiesta, sombrero vaquero o jockey, sin texturas adicionales.
- Menu previo a la partida para personalizar al adulto antes de iniciar la parrillada; oleadas y cronometro permanecen pausados mientras esta abierto.
- Camion de helados desbloqueado en la oleada 2, con activacion temporal y cooldown reutilizable.
- Patio de parrillada ampliado con grill, barra, beer pong, lounge, sonido, mesas, carpa de comida, cooler, cobertizo, luces y senderos. Camion, globos y juguetes quedan como distracciones perifericas.
- Tests EditMode para modelos y reglas, mas un smoke test PlayMode del bucle completo.

La primera oleada usa 14 unidades para un jugador, 32 para cuatro y 56 para ocho. Cada escalon de dificultad suma 6 y el tope configurable es 96. Consulta [el presupuesto de rendimiento](docs/performance-budget.md) para la formula, los objetivos y la matriz de profiling.

El prototipo usa el pipeline Built-in y materiales simples para reducir dependencias. Los personajes son mallas originales del proyecto; URP se incorporara al comenzar el vertical slice visual, creando y versionando correctamente sus assets de pipeline y perfiles de calidad.

Consulta [la auditoria de repositorios](docs/repository-audit.md) para ver que aprendimos de cada proyecto y sus restricciones de licencia. La futura capa online se define en [arquitectura multijugador](docs/online-architecture.md).
El sistema propuesto para variar crisis, tareas, mejoras y entradas entre sesiones esta en [director de partidas](docs/replayability.md).

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
6. Personaliza al adulto en el menu inicial y pulsa `Iniciar parrillada` o `Enter`.

Acepta el reinicio del Editor si Unity lo solicita al activar el Input System nuevo.

Controles:

- Menu inicial: mouse; `A/D` cambia ropa, `W/S` piel, `Q/E` gorro, `M` bigote y `Enter` inicia.
- `WASD`, flechas o stick izquierdo: movimiento.
- Mover el mouse o el stick derecho: camara. `Esc` libera el cursor y un clic lo captura nuevamente.
- `E` o boton sur del gamepad: interactuar.
- `Espacio` o boton norte: saltar.
- `F`, click izquierdo o bumper derecho: usar objeto defensivo.
- `Q` o bumper izquierdo: beber una carga de la botella.

`E` muestra un aviso cuando hay algo al alcance. `F` requiere haber recogido una escoba o sarten y `Q` solo consume una carga si falta BUZZ; si una accion no puede ejecutarse, el HUD explica el motivo.

La meta del prototipo es sobrevivir 150 segundos. Hay que preparar bebida en el `BOOZE LAB`, recoger herramientas, bloquear entradas y coordinar distracciones; el camion se desbloquea en la oleada 2 y puede reutilizarse despues de su cooldown. Las oleadas 3, 4, 5 y 6 mejoran capacidad, fuerza y velocidad de preparacion durante la partida.

Para probar el escalado sin networking, selecciona `PrototypeBootstrap` antes de pulsar Play y cambia `Simulated Player Count` entre 1 y 8. Solo el primer adulto recibe input; los demas simulan integrantes conectados para probar reparto de objetivos y poblacion de horda.

## Alcance inmediato

El prototipo sigue siendo offline para validar el loop. La siguiente etapa es perfilar 1, 4 y 8 jugadores simulados y conectar una capa host-autoritativa con Unity Multiplayer Services, Relay y Netcode for GameObjects. Ocho jugadores online es el objetivo inicial; no se sincronizara cada nino como un `NetworkObject` independiente.
