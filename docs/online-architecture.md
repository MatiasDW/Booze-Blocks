# Arquitectura multijugador online

## Objetivo inicial

- Hasta 8 jugadores online por sesion.
- Modelo host-autoritativo para el MVP: un jugador actua como host y decide movimiento, oleadas, interacciones, presion y resultado de ronda.
- Unity Multiplayer Services SDK para sesiones, Lobby y Relay.
- Netcode for GameObjects para jugadores y objetos interactivos importantes.
- Relay con DTLS para evitar exponer la IP del host y simplificar NAT/firewalls.

Esta ruta reduce infraestructura inicial, pero no elimina el costo por uso de Unity Gaming Services. Para una beta competitiva o publica se debe reevaluar un servidor dedicado.

## Autoridad

El cliente solo envia input. El host valida y simula:

- Posicion y estado de jugadores.
- Inicio, descanso, numero y dificultad de oleada.
- Inventario, usos de herramientas, preparacion y reparto de bebida.
- Activacion y cooldown de globos y camion.
- Contactos agregados de la horda y knockdowns.
- Victoria, derrota, desconexion y reconexion.

`HordeDirector.SetSimulationAuthority` permite apagar la simulacion de gameplay en clientes. Cuando se integre NGO, solo host o servidor debe mantenerla activa.

## Sincronizacion de horda

No se creara un `NetworkObject` por nino. Con 96 unidades eso multiplicaria spawn messages, transforms, ownership y callbacks sin aportar gameplay.

El host mantendra la simulacion real y enviara snapshots compactos por lotes:

- 10 snapshots por segundo como punto de partida.
- ID de unidad, posicion 2D cuantizada, objetivo y flags de distraccion.
- Interpolacion visual en clientes entre los dos snapshots mas recientes.
- Eventos fiables solo para cambios de oleada, spawn, retiro y activacion de distracciones.
- Eventos fiables para recoger/usar herramientas, iniciar/finalizar mezclas y consumir porciones.
- La presion y los knockdowns se calculan exclusivamente en el host.

Este formato debe medirse antes de fijarlo. Si el ancho de banda supera el presupuesto, se reduce frecuencia, precision o relevancia por distancia; no se baja la autoridad.

## Implementacion actual

- `NetworkGameplayCoordinator` usa mensajes nombrados NGO; los personajes procedurales no dependen de prefabs de red.
- Clientes envian movimiento a 20 Hz y acciones importantes por un canal fiable.
- El host distribuye a 10 Hz un snapshot inferior a 1.2 KB con jugadores, apariencia, vitales, inventario, horda, ronda, puntuacion y tareas compartidas.
- Posiciones usan centimetros en enteros de 16 bits; rotacion, ratios y tiempos tambien se cuantizan.
- Solo el host ejecuta drenaje de BUZZ, presion, interacciones, parrilla, mezcla, distracciones y dano por secciones de barricadas. El snapshot envia su integridad cuantizada en un byte.
- La pausa online es local y envia movimiento cero; no congela la simulacion del host.
- Si cae el host, los clientes cierran el transporte y regresan al menu. Migracion de host sigue fuera del MVP.

## Frecuencias objetivo

| Sistema | Frecuencia inicial |
| --- | ---: |
| Simulacion del host | 30 Hz |
| Input de jugadores | 20 Hz |
| Snapshot agregado de mundo | 10 Hz |
| Snapshot agrupado de horda | 10 Hz |
| Actualizacion visual de NPC | Cada frame, interpolada |

## Riesgos del MVP

- Si el host abandona, la primera version terminara la partida; migracion de host queda fuera del primer prototipo online.
- Netcode for GameObjects no convierte automaticamente la fisica en prediccion robusta. El movimiento local necesitara anticipacion y correccion gradual.
- Relay resuelve conectividad, no anti-cheat ni autoridad.
- Ocho clientes deben probarse con latencia, jitter y perdida de paquetes simulados antes de agregar arte pesado.
- Sesiones, Relay y otros servicios generan consumo facturable; hay que configurar presupuestos y alertas antes de pruebas publicas.
