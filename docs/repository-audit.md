# Auditoria de repositorios de referencia

Revision realizada el 19 de julio de 2026. El objetivo es aprender patrones concretos sin mezclar proyectos completos, dependencias antiguas ni contenido con licencia incierta.

## Resultado

| Repositorio | Lo que nos sirve | Decision | Licencia observada |
| --- | --- | --- | --- |
| [sunidhi04/Fall-guys-clone](https://github.com/sunidhi04/Fall-guys-clone) | Movimiento con Rigidbody, cambio de material fisico en paredes y paquetes modernos de Unity 6 | Solo referencia. Es pequeno y el README promete mas multiplayer/IA de lo que aparece en sus scripts propios | Sin licencia raiz |
| [inklord/Rush-Dush-tfg](https://github.com/inklord/Rush-Dush-tfg) | Catalogo de obstaculos, lobby, bots, camara y flujo entre pruebas | Solo referencia. Usa Photon PUN2 y su IA consulta destinos/vecinos con demasiada frecuencia para una horda | Sin licencia raiz; uso educativo declarado |
| [dillon-taylor710/Fall-Guys-Prototype](https://github.com/dillon-taylor710/Fall-Guys-Prototype) | Estados de caida/recuperacion, plataformas y obstaculos controlados por autoridad | Referencia reutilizable con atribucion, pero no adoptar su controlador ni Mirror: usa Input legacy y movimiento cliente-autoritativo | MIT |
| [HyagoOliveira/KitchenChaos](https://github.com/HyagoOliveira/KitchenChaos) | Separacion de motor, input, items, holders, eventos, configuracion y flujo de partida | Mejor referencia de arquitectura modular. Evitar sus paquetes externos ActionCode para no atar el proyecto a un registro privado | MIT |
| [lazZ33/unity_overcooked](https://github.com/lazZ33/unity_overcooked) | Separacion cliente/servidor, interfaces de interaccion, ScriptableObjects y generacion procedural | Aprender la division autoridad/presentacion. No necesitamos mapas procedurales en el MVP ni su Netcode for GameObjects 1.5.2 | Sin licencia raiz |
| [Jvdputten-gamedev/overcooked-clone](https://github.com/Jvdputten-gamedev/overcooked-clone) | ScriptableObjects para recetas, eventos de progreso y UI desacoplada | Solo referencia secundaria; es un clon/tutorial antiguo y no agrega una arquitectura de red | Sin licencia raiz |
| [Unity-Technologies/com.unity.services.samples.parties](https://github.com/Unity-Technologies/com.unity.services.samples.parties) | Flujo crear/unirse, codigo de lobby, ready-up, cambio de host y eventos | Consultar al construir lobby. El proyecto usa Unity 2020 y APIs pre-release; no debe ser la base del gameplay | Terminos de Unity Gaming Services |
| [jadeharlev/PartyTricks](https://github.com/jadeharlev/PartyTricks) | Unity 6.3, cuatro mandos, pooling, estados de minijuego, servicios y tests | Mejor referencia moderna para flujo y calidad, pero solo conceptual porque no publica licencia | Sin licencia raiz |
| [AlexRak2/Coop-Party-Template-Unity-Mirror-Steam](https://github.com/AlexRak2/Coop-Party-Template-Unity-Mirror-Steam) | Ready-up, invitaciones de Steam, lista de amigos y pruebas con varias instancias | Guardar para una fase Steam posterior. Usa Mirror/FizzySteamworks y no coincide con la futura capa de red | MIT declarado en README; sin archivo de licencia raiz |
| [Unity-Technologies/com.unity.multiplayer.samples.coop](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) | Sesiones, autenticacion, Relay, autoridad, RPC y ocultamiento de latencia | Referencia oficial para implementar la siguiente capa online con APIs actuales, sin importar el proyecto completo | Unity Companion License |
| [MirrorNetworking/Mirror](https://github.com/MirrorNetworking/Mirror) | Snapshot interpolation, interest management, simulacion de latencia y transporte Steam | Alternativa valida y MIT, pero no mezclarla con NGO. Revisar solo sus patrones de profiling e interpolacion | MIT |

## Patrones adoptados

- Motor, input, estado y presentacion viven en componentes separados.
- La caida es un estado autoritativo con tiempo de recuperacion; el ragdoll sera presentacion local.
- La horda usa NPC clasicos: seguimiento directo, separacion local y presion agregada a intervalos. No usa sistemas de IA, NavMesh ni pathfinding individual.
- Items y distracciones implementan una interaccion contextual comun.
- Configuracion y balance quedaran fuera de la logica mediante ScriptableObjects.
- Los enemigos se crean bajo demanda hasta alcanzar el maximo necesario y luego se reciclan con pooling.
- Networking se conectara a una simulacion ya separada de camara, UI, audio y efectos.

## Patrones rechazados

- Copiar proyectos, assets o plugins enteros.
- Photon PUN2, Mirror y Netcode for GameObjects antiguo en el mismo proyecto.
- Active ragdoll como locomocion principal.
- `FindObjectOfType`, `OverlapSphere` o recalculo de rutas para cada enemigo en cada frame.
- Sincronizar huesos, animaciones o cada contacto fisico de la horda.
- Generacion procedural antes de validar una arena disenada a mano.

## Auditoria de escalabilidad aplicada

La primera implementacion propia repetia algunos problemas observados en los repositorios de referencia: objetivo unico, conteo fijo de enemigos y trabajo individual por agente. La segunda revision corrigio esos puntos:

- A diferencia de los bots de `Rush-Dush-tfg`, cada unidad no busca jugadores ni vecinos globalmente en cada frame. El host reparte objetivos y la separacion consulta un grid espacial.
- A diferencia del controlador Mirror de `Fall-Guys-Prototype`, la seleccion de objetivos y la presion no dependen del cliente local; quedaron centralizadas en `HordeDirector` para conectarlas luego a autoridad de host.
- Se adopto de `KitchenChaos` la separacion conceptual entre configuracion, motor y estado, sin depender de sus paquetes externos.
- Se adopto de `PartyTricks` la idea de estados y pooling medible, pero el pool de horda puede reducir y volver a crecer con jugadores que entran o salen.
- La ronda ya no contiene arrays de cuatro posiciones ni pierde al caer el primer jugador. Consulta el registro dinamico y termina solo cuando no queda ningun integrante activo.
- Distracciones y bebidas aumentan su capacidad con el equipo; escalar solo enemigos habria debilitado las herramientas cooperativas al subir de cuatro a ocho jugadores.

La simulacion de horda no fija el cupo de la sesion. La primera configuracion online apunta a ocho jugadores, mientras que el costo queda protegido por un maximo independiente de 96 unidades y crece por oleadas.

## Aceleracion visual y online

- Los personajes del prototipo son mallas cartoon propias construidas con formas low-poly y vertex colors. No dependen de modelos comerciales, rigs ni paquetes externos.
- No se usan modelos extraidos de juegos comerciales ni repositorios sin licencia. Se replica la direccion visual de humanos caricaturescos mediante assets reutilizables con procedencia documentada.
- Para Unity 6, la capa online se construira con Multiplayer Services Sessions, Relay y Netcode for GameObjects. El sample oficial `Boss Room` sera referencia de flujo y autoridad, no una base copiada.
- Mirror queda como alternativa si Steam P2P pasa a ser un requisito prioritario. Cambiar de stack despues de implementar gameplay de red seria costoso, por eso no se instalaran ambos.

## Atribucion futura

Si se incorpora codigo de un repositorio MIT, hay que conservar su aviso de copyright y licencia en `ThirdPartyNotices.md`. La implementacion inicial de Booze & Blocks es original y no copia codigo externo.
