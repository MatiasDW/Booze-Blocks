# Director de partidas y rejugabilidad

La variacion recomendada no depende de generar mapas completos al azar. Cada partida combina una semilla, una crisis principal, tareas distribuidas y elecciones de mejora. El patio conserva su identidad y el equipo puede aprenderlo, pero la ruta optima cambia.

## Composicion de una partida

1. Una crisis de parrillada modifica una regla durante toda la sesion.
2. Dos de cuatro estaciones de tarea quedan activas en posiciones validas.
3. Cada dos oleadas el equipo elige una de tres mejoras compartidas.
4. Las entradas de la horda cambian entre patrones anunciados antes de cada oleada.

La semilla debe mostrarse en el lobby para repetir una combinacion o compartirla con amigos. El host genera y valida la configuracion; los clientes reciben solamente la semilla y las decisiones confirmadas.

## Crisis principales

| Crisis | Cambio de reglas | Contrajuego |
| --- | --- | --- |
| Sin hielo | La botella pierde una carga maxima y el cooler debe repararse | Completar dos viajes de repuestos recupera capacidad y acelera mezclas |
| Corte de luz | Camion, barra y sonido comienzan apagados | Transportar combustible al generador habilita una zona por vez |
| Parrilla atrasada | Hay que mantener carne cocinandose sin que se queme | Un jugador cocina mientras los demas defienden y buscan bebida |
| Vecinos furiosos | La musica distrae mejor, pero llena un medidor de ruido | Alternar musica y silencio evita perder temporalmente la barra |
| Cumpleanos doble | Aparecen mas ninos y dos tortas funcionan como objetivos secundarios | Mover las tortas cambia la ruta de la horda y compra tiempo |

## Tareas sorteables

- Preparar mezcla, esperar y repartir porciones.
- Encender la parrilla y cocinar una tanda sin abandonarla demasiado tiempo.
- Llevar hielo desde el camion al cooler usando una mano del inventario.
- Reparar el generador con piezas del cobertizo.
- Montar el equipo de musica para crear una distraccion de gran radio.
- Reponer globos o juguetes en un punto distinto del patio.

Las tareas deben obligar a cruzar la arena y dividir responsabilidades. Ninguna debe resolverse manteniendo `E` sin tomar decisiones.

## Mejoras entre oleadas

La eleccion es compartida y presenta tres opciones de categorias distintas:

- Supervivencia: mas capacidad de botella, menor drenaje o recuperacion mas rapida.
- Defensa: mas alcance, durabilidad o empuje de herramientas.
- Logistica: preparacion mas rapida, transporte de dos recursos o cooldown menor.
- Distraccion: mayor radio, duracion o capacidad por jugador.

Una partida de 6 oleadas ofrece dos o tres elecciones. Eso crea builds de equipo sin convertir el juego en un sistema de estadisticas permanente.

## Patrones de horda

- Embudo: dos entradas opuestas con mucha densidad.
- Rodeo: cuatro entradas con grupos pequenos.
- Falsa alarma: una entrada anunciada cambia antes del spawn, con senal visual suficiente.
- Escolta: una parte de la horda prioriza una tarea o recurso del escenario.

Los patrones cambian rutas y posicionamiento, pero no agregan IA individual. El host asigna un objetivo grupal y cada `KidUnit` conserva el steering liviano actual.

## Progresion persistente

La progresion entre partidas debe desbloquear variedad, no poder bruto acumulativo. Ejemplos: nuevas crisis, skins, animaciones, herramientas laterales y recetas con ventajas y desventajas. Evitar mejoras permanentes de dano o vida que vuelvan injustas las sesiones con jugadores nuevos.
