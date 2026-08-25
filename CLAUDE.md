# Booze & Blocks — Guía para agentes

Este archivo es el punto de entrada para cualquier agente (Claude Code u otro) que arranca sin contexto previo. Léelo primero. La verdad de gameplay/roadmap vive en el `README.md` y en `docs/`; este archivo enseña **cómo trabajar** aquí.

## Qué es esto

Prototipo Unity **6000.5.4f1** (Built-in RP, **no URP todavía**) de party survival cooperativo: adultos torpes defendiendo un patio de parrillada de una horda caricaturesca de niños. Host-autoritativo, meta 8 jugadores online, código original sin assets externos copiados.

- Fantasía y estado actual → `README.md`
- Roadmap por fases → `docs/product-roadmap.md`
- Presupuesto CPU/GPU/red → `docs/performance-budget.md`
- Arquitectura online → `docs/online-architecture.md`
- Variedad de partidas → `docs/replayability.md`
- Investigación de referencias → `docs/repository-audit.md`, `docs/comparative-gap-analysis.md`
- Protocolo de playtest → `docs/playtest-session.md`

## Cómo abrir y correr

1. Unity Hub → abrir esta carpeta como proyecto con la versión `6000.5.4f1` (Apple Silicon si estás en Mac M).
2. Menú `Booze & Blocks > Create Prototype Scene` (regenera `Assets/_Game/Scenes/Prototype.unity` desde código).
3. Play. `PrototypeBootstrap` construye todo el mundo, jugador, sistemas, HUD y networking desde código.
4. Para testear escalado sin red, seleccioná el `PrototypeBootstrap` en la escena y cambiá `Simulated Player Count` (1–8) antes de Play.
5. Build de mano en mano: `Booze & Blocks > Build macOS App` deja el binario en `Builds/macOS/BoozeAndBlocks.app`. Args de línea: `-boozePlayers N -boozeAutoStart`.

Controles resumidos en `README.md`. Multiplayer online requiere vincular UGS en `Edit > Project Settings > Services`.

## Cómo correr tests

**EditMode desde CLI** (batch mode, ~2–5 min):

```bash
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics \
  -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode \
  -testResults "$(pwd)/.context/editmode-results.xml" \
  -logFile "$(pwd)/.context/editmode.log"
```

**NO usar `-quit`** con `-runTests` — hace que salga antes de ejecutar los tests. Los resultados quedan en el XML; buscar `passed="N" failed="M"`.

Batería actual: **52 tests EditMode** sobre modelos puros. PlayMode tiene un smoke test `PrototypeRuntimeTests` y un `PlayerInputReaderTests`.

Cualquier cambio de gameplay debe mantener EditMode verde antes de commitear.

## Estructura de código

- `Assets/_Game/Scripts/Runtime/` — asmdef `BoozeBlocks.Runtime`
  - `Camera/` — cámara third-person
  - `Distractions/` — atracciones (globos, camión, música, etc.)
  - `Horde/` — director, escalado, oleadas, unidades, entradas con barricadas, grid espacial
  - `Interaction/` — parrilla, mezcla, defensa, revivir, interfaz `IInteractable`
  - `Player/` — motor físico, input, vitales, inventario, state machine, feedback
  - `Prototype/` — capa "todo lo demás": bootstrap, HUD, menús, red, VFX, audio, apariencia, sesión, escenario
- `Assets/_Game/Scripts/Editor/` — asmdef `BoozeBlocks.Editor`, builder de escena + build macOS
- `Assets/_Game/Shaders/` — shaders originales (`StylizedCharacter`, `BlobShadow`)
- `Assets/_Game/Tests/EditMode` y `PlayMode` — asmdefs `BoozeBlocks.EditModeTests` / `PlayModeTests`

Archivos grandes conocidos:

- `PrototypeBootstrap.cs` ~1300 líneas — construye materiales, iluminación, arena, props, jugador, sistemas, red, HUD, menús. La Fase 6 del roadmap pide dividirlo; hasta entonces, agregá métodos nuevos siguiendo el patrón `BuildXxx(Transform parent)` y usá los helpers `CreatePart(...)` / `CreateBlock(...)`.
- `NetworkGameplayCoordinator.cs` ~690 líneas — snapshot host-autoritativo a 10 Hz.

## Convenciones que no negociamos

Están explícitamente en el roadmap y presupuesto:

- **NO URP** todavía. Built-in RP hasta que haya medición que justifique migrar.
- **NO Animator, NO rig, NO NavMesh, NO NavMeshAgent.** El adulto y los niños usan mallas cartoon originales y animación procedural. Locomoción de horda es steering + grid espacial + presión agregada.
- **NO `NetworkObject` por niño.** La horda se sincroniza como snapshot agrupado. Ver `HordeDirector.SetSimulationAuthority(bool)`.
- **NO active ragdoll como locomoción.** Los derribos usan una cápsula estable con un blend visual, no un ragdoll por huesos.
- **NO Photon, NO Mirror.** El stack es Unity Multiplayer Services + NGO.
- **NO Addressables, DOTS, Burst, Jobs** hasta que un Profiler real los justifique.
- **NO mejoras permanentes de poder** entre partidas; sólo desbloqueos cosméticos y variedad de crisis/tareas.
- **GC objetivo = 0 B/frame** durante gameplay estable.
- Los NPC actualizan dirección **4 Hz**, no cada frame.
- Un solo `ParticleSystem` compartido (máx 180 partículas) para todo el VFX del gameplay.

## Presupuesto de rendimiento

Objetivo 60 FPS (16.67 ms/frame):

| Área | Presupuesto |
|---|---:|
| Horda host | ≤ 3 ms |
| Jugadores/física | ≤ 2 ms |
| Scripts restantes | ≤ 2 ms |
| Render | ≤ 8 ms |
| GC estable | 0 B/frame |
| Snapshot red host | ≤ 1.2 KB @ 10 Hz |

Escalado horda: `min(14 + (jugadores-1)*6 + escalón*6, 96)`. Ver `docs/performance-budget.md` para matriz de profiling completa.

## Materiales y estilo visual

- Todos los materiales runtime se crean en `PrototypeBootstrap.CreateMaterials()` con `Shader.Find("Standard")` y `enableInstancing = true`.
- Piezas cúbicas visibles usan `StylizedGeometry.ChamferedCube` (una malla biselada compartida de 96 vértices).
- El adulto usa el shader **`BoozeBlocks/StylizedCharacter`** — vertex-color mask sobre 4 zonas + rim light emissive. Cambios cosméticos se hacen con `MaterialPropertyBlock`.
- Sombra procedural bajo el adulto: componente `BlobShadow` + shader `BoozeBlocks/BlobShadow` (mask circular en fragment, sin textura).
- Los niños comparten una única malla, `MaterialPropertyBlock` por instancia, sombras y probes off.

## Patrones útiles

- **Modelos puros** (structs/clases sin `MonoBehaviour`) para reglas testables: `HordeScalingModel`, `PlayerVitalsModel`, `PlayerInventoryModel`, `BarricadeIntegrityModel`, `GrillTaskModel`, `NetworkSnapshotModels`, `TeamRules`, etc. Cada modelo nuevo debería tener test EditMode.
- **Registros estáticos** con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` para limpiar estado entre entradas a Play Mode sin recarga de dominio.
- **Autoridad de simulación**: sistemas del host verifican `hasSimulationAuthority` para dejar de simular en clientes cuando NGO conecta.
- **Cuantización**: posiciones en cm enteros de 16 bits, ratios y tiempos también cuantizados para el snapshot.

## Flujo de trabajo con Git

- Branch principal: `main` (base de PRs).
- Los commits son escasos y grandes: es normal ver iteraciones amplias en un solo commit.
- Antes de commitear: EditMode verde (52/52) y sin errores de compilación (los warnings de `FindObjectsSortMode` en `PrototypeRuntimeTests` son preexistentes y aceptables).
- Nunca commitear `.context/` (gitignored), `Library/`, `Temp/`, `Builds/`.

## Cosas que ya intentamos y descartamos

Documentado en `docs/repository-audit.md` y `docs/comparative-gap-analysis.md`. Antes de proponer "usemos X repo/paquete", chequeá esa auditoría; probablemente ya fue evaluado.

## Idioma

El proyecto y sus discusiones son en **español**. Los comentarios de código y nombres de identificadores pueden estar en inglés, pero comunicaciones con el mantenedor son en español.
