# Analisis comparativo de Booze & Blocks

Este documento registra patrones de diseno y arquitectura estudiados. La implementacion de Booze & Blocks es original: no se copiaron scripts, escenas, modelos ni assets de terceros.

## Comparacion

| Referencia | Licencia observada | Lo valioso | Decision aplicada |
| --- | --- | --- | --- |
| [HyagoOliveira/KitchenChaos](https://github.com/HyagoOliveira/KitchenChaos) | MIT | Cadena de tareas, recetas configurables y estados de preparacion | Parrilla con estados de combustible, calentado, coccion, servicio y quemado |
| [Jvdputten-gamedev/overcooked-clone](https://github.com/Jvdputten-gamedev/overcooked-clone) | Sin licencia de repo | Estados de partida y riesgo de quemar comida | Ventana breve para servir y penalizacion original por ignorar el asado |
| [lazZ33/unity_overcooked](https://github.com/lazZ33/unity_overcooked) | Sin licencia de repo | Separacion cliente/servidor e interacciones genericas | Crisis, mejoras y puntuacion usan estados pequenos, discretos y serializables |
| [dillon-taylor710/Fall-Guys-Prototype](https://github.com/dillon-taylor710/Fall-Guys-Prototype) | MIT | Obstaculos, checkpoints, movimiento fisico y Mirror | Se conserva el controlador Rigidbody y se priorizan tareas antes de sumar mas fisica sincronizada |
| [sunidhi04/Fall-guys-clone](https://github.com/sunidhi04/Fall-guys-clone) | Sin licencia de repo | Prototipo directo con managers sencillos | La ampliacion sigue siendo procedural, sin dependencias ni escenas adicionales |
| [inklord/Rush-Dush-tfg](https://github.com/inklord/Rush-Dush-tfg) | Sin licencia de repo | Agentes y multijugador Photon en un proyecto academico | Se rechaza IA compleja; la horda mantiene steering clasico y pooling |
| [jadeharlev/PartyTricks](https://github.com/jadeharlev/PartyTricks) | Sin licencia de repo | Flujo de rondas, tienda y modificadores | Cada dos oleadas aparecen tres mejoras aleatorias para todo el equipo |
| [AlexRak2/Coop-Party-Template-Unity-Mirror-Steam](https://github.com/AlexRak2/Coop-Party-Template-Unity-Mirror-Steam) | Sin licencia de repo | Lobby, ready state y previsualizacion de personajes | Se mantiene personalizacion previa; lobby y ready se reservan para el hito online |
| [Unity Game Lobby sample](https://github.com/Unity-Technologies/com.unity.services.samples.game-lobby) | Licencia Unity | Separacion de Lobby, Relay y estado de juego | Se mantiene el plan host-authoritative; no se agrega red antes de estabilizar el loop |

## Ampliacion implementada

- Parrilla cooperativa con seis estados y feedback visual.
- Cuatro crisis de partida: carbon mojado, mezcla aguada, cumpleanos doble y cercos sueltos.
- Puntuacion compartida por asados, barricadas, distracciones y oleadas; la comida quemada resta puntos.
- Eleccion de tres mejoras cada dos oleadas: botella, defensa, mezcla, parrilla o distracciones.
- Semilla de partida y elecciones deterministas, aptas para autoridad del host.
- Cero paquetes, texturas o meshes nuevos; solo C# y primitivas ya incluidas.

## Patrones rechazados por ahora

- Active ragdoll completo: eleva el costo de sincronizacion y la inestabilidad fisica online.
- NavMesh por enemigo: innecesario para la horda ligera basada en steering y grilla espacial.
- Photon, Mirror o Netcode antes del vertical slice: obligaria a depurar gameplay y red simultaneamente.
- Inventarios profundos y recetas con muchos ingredientes: agregan UI y micromanejo antes de validar la parrilla basica.
- Tienda persistente con mejoras de poder: puede volver injustas las partidas; la progresion futura desbloqueara variedad, no ventaja permanente.

## Siguiente corte recomendado

1. Probar si parrilla, booze, entradas y distracciones generan decisiones interesantes con 2 a 4 personas.
2. Ajustar duraciones y puntuacion con datos de partidas, no por intuicion.
3. Agregar un segundo objetivo corto, como hielo o generador, solo si la parrilla ya funciona.
4. Implementar lobby y Relay con autoridad del host; sincronizar eventos discretos y no cada objeto decorativo.
