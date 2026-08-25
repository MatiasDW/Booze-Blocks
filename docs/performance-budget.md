# Presupuesto de rendimiento

Este documento define limites iniciales para mantener Booze & Blocks liviano. Son objetivos de produccion, no resultados medidos en hardware objetivo: las pruebas automatizadas validan funcionalidad, pero falta perfilar Development Builds reales.

Durante una partida, `F3` abre el monitor liviano con FPS promedio, peor frame, porcentaje de frames sobre 33 ms y memoria C#. En Unity Profiler aparecen los marcadores `BoozeBlocks.Horde.Update`, `Population`, `Targeting` y `Pressure`.

## Escalado de horda

Formula predeterminada para una oleada activa:

```text
escalon = floor((oleada - 1) / 2)
unidades = min(14 + (jugadores - 1) * 6 + escalon * 6, 96)
```

| Jugadores | Oleada 1 | Oleada 3 | Oleada 5 |
| ---: | ---: | ---: | ---: |
| 1 | 14 | 20 | 26 |
| 2 | 20 | 26 | 32 |
| 4 | 32 | 38 | 44 |
| 6 | 44 | 50 | 56 |
| 8 | 56 | 62 | 68 |

El limite es de unidades, no de jugadores. La configuracion online inicial limita la sesion a ocho personas, mientras el tope independiente de 96 protege CPU, render y red. La partida abre con 25 segundos de preparacion; cada oleada dura 60 segundos, tiene 15 segundos de descanso y alcanza su poblacion objetivo linealmente durante los primeros 20 segundos.

El balance de recursos tambien escala: la estacion prepara entre 4 y 10 porciones por tanda segun el equipo, y los globos atraen 6 ninos mas 2 por cada jugador adicional. El camion absorbe toda la horda durante 18 segundos, se desbloquea en la oleada 2 y tiene 25 segundos de cooldown.

## Balance de contacto

- El adulto tiene 150 de vida y 100 de stamina.
- El primer nino en contacto aplica 7 de stamina y 0,65 de vida por segundo.
- Cada nino adicional aporta 55% de la presion de stamina y 50% del dano de vida base.
- Cuatro ninos juntos aplican 18,55 de stamina y 1,625 de vida por segundo; la reduccion marginal evita muertes instantaneas.
- Quedarse sin BUZZ drena 4 de vida por segundo. Servir carne bien preparada entrega 250 puntos y recupera 25 de vida.

## Presupuesto por frame

Objetivo de referencia: 60 FPS, equivalente a 16.67 ms por frame.

| Area | Presupuesto inicial |
| --- | ---: |
| Simulacion de horda | 3 ms o menos |
| Movimiento y fisica de jugadores | 2 ms o menos |
| Scripts restantes | 2 ms o menos |
| Renderizado | 8 ms o menos |
| Garbage Collector | 0 bytes por frame durante gameplay estable |

El HUD actual usa IMGUI solo para diagnostico y queda fuera de ese objetivo. La UI de produccion debe medirse por separado y reemplazarlo sin generar strings ni layouts por frame.

## Decisiones aplicadas

- Los NPC actualizan su direccion cuatro veces por segundo, no en cada frame.
- Un grid espacial limita la separacion a celdas cercanas en vez de comparar toda la horda.
- La presion se agrega una vez por jugador y tick fisico.
- El pool crece solo hasta el maximo que la sesion haya necesitado y reutiliza unidades entre oleadas.
- Todas las unidades comparten material y se mueven por `Transform`; no tienen `Rigidbody` y su collider esta deshabilitado porque contacto y separacion se resuelven en la simulacion agregada, no en PhysX.
- La salud agrega dos `float` por unidad y se resuelve solo cuando un barrido defensivo recorre la horda; no crea barras, canvas, colliders ni `Update` adicionales.
- Las barricadas reciben dano agregado por intento de spawn y por tick de direccion; solo existen cuatro modelos de integridad y cuatro barras mundiales simples. No se simulan atacantes externos ni fisica por tablón.
- No hay Animator, ragdoll, NavMeshAgent, pathfinding ni NetworkObject por nino en el prototipo.
- El adulto y los ninos usan mallas cartoon propias sin huesos, clips ni texturas. Brazos, piernas y balanceo son procedurales; el adulto libera temporalmente las rotaciones de su unico Rigidbody durante el derribo, sin un ragdoll por huesos.
- Las piezas rectangulares visibles comparten una sola malla biselada de 96 vertices; cercos y plantas reutilizan primitivas. Los `BoxCollider` de limites y obstaculos no siguen ese detalle visual.
- Cada nino comparte una unica malla de 1.116 vertices y 1.296 poligonos (51 KB), un material de vertex colors y un `MaterialPropertyBlock` por instancia. Las sombras y probes de la horda estan desactivados.
- La evasion de obstaculos usa un `SphereCast` corto durante el tick de direccion a 4 Hz, no pathfinding por agente.
- URP, Cinemachine, AI Navigation y uGUI se posponen hasta que exista una necesidad medida; no forman parte del greybox.
- Los objetivos se reparten por distancia y saturacion para evitar concentrar toda la simulacion en un jugador.
- Los registros estaticos se reinician al cargar el subsistema, incluso si el Editor entra a Play Mode sin recargar el dominio.
- La interaccion consulta fisica a 10 Hz y fuerza una consulta inmediata al pulsar, en lugar de escanear cada frame.
- Los objetos defensivos recorren la lista de hasta 96 unidades solamente al usarse; no habilitan colliders ni rigidbodies en los ninos.
- Los empujes de horda reutilizan el mismo recorrido de contacto agregado; aplican como maximo un impulso por jugador cada 0.14 segundos.
- La preparacion, cooldowns e inventarios son estados pequenos sin `Update` costoso ni instanciacion continua.
- La musica y los efectos del prototipo se generan una sola vez al iniciar; no agregan archivos pesados ni crean una fuente por nino.
- El monitor de rendimiento reutiliza un buffer fijo de 600 muestras y no asigna memoria por frame.
- Todo el feedback grafico comparte un solo `ParticleSystem` limitado a 180 particulas; no existe un emisor por nino ni por objeto.

## Presupuesto de red

- Snapshot host: maximo `1.2 KB`, 10 veces por segundo.
- Salida teorica del host con 7 clientes: menos de `84 KB/s` mas overhead de transporte.
- Input por cliente: 20 mensajes pequenos por segundo; acciones via canal fiable solo cuando ocurren.
- La horda comparte un paquete cuantizado; nunca hay 96 `NetworkTransform` ni 96 `NetworkObject`.
- Antes de beta se debe medir Relay con 2, 4 y 8 clientes bajo 50/100/180 ms, jitter y 1-5% de perdida.

## Matriz de profiling

Antes de integrar networking se deben medir builds Development, no solo Play Mode:

| Escenario | Jugadores | Horda | Duracion minima |
| --- | ---: | ---: | ---: |
| Basico | 1 | 14-38 | 5 minutos |
| Coop medio | 4 | 32-62 | 10 minutos |
| Objetivo | 8 | 56-92 | 15 minutos |
| Stress | 12 | 96 | 15 minutos |

Registrar CPU Timeline, Physics, Rendering, memoria, GC allocations, FPS de percentil 1 y temperatura sostenida. El MacBook Pro M5 con 32 GB servira para desarrollo, pero la calidad se aprobara contra un equipo objetivo considerablemente mas lento.

## Riesgos pendientes

- La capa online existe, pero todavia falta validarla con Relay real, varios equipos, latencia, jitter y perdida.
- Ocho jugadores online es el objetivo; ocho jugadores locales requieren emparejar mandos, camaras y UI por jugador.
- El seguimiento directo actual no rodea obstaculos. Los mapas con bloqueos necesitaran un flow field o una red pequena de sectores calculada una vez para toda la horda, nunca pathfinding individual.
- El escenario todavia usa geometria procedural simple; no representa el costo final de arte, audio o VFX.
- Los presupuestos aun deben confirmarse con Unity Profiler en builds Development para Apple Silicon y un equipo objetivo mas lento.
