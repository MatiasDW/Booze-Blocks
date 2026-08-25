using System;
using System.Collections.Generic;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class NetworkGameplayCoordinator : MonoBehaviour
    {
        private const string StartMessage = "BoozeBlocks.Start.v1";
        private const string InputMessage = "BoozeBlocks.Input.v1";
        private const string ActionMessage = "BoozeBlocks.Action.v1";
        private const string SnapshotMessage = "BoozeBlocks.Snapshot.v1";
        private const float InputInterval = 1f / 20f;
        private const float SnapshotInterval = 1f / 10f;
        private const int MaximumPlayers = 8;
        private const int MaximumKids = 96;

        private readonly Dictionary<ulong, PlayerVitals> networkPlayers = new Dictionary<ulong, PlayerVitals>(8);
        private readonly HashSet<ulong> receivedPlayerIds = new HashSet<ulong>();
        private readonly List<HordePoseSnapshot> hordePoses = new List<HordePoseSnapshot>(96);

        private NetworkManager networkManager;
        private PrototypeBootstrap playerFactory;
        private PlayerVitals localPlayer;
        private PlayerInputReader localInput;
        private HordeDirector horde;
        private PrototypeRound round;
        private RunSessionDirector runSession;
        private PrototypeStartMenu startMenu;
        private OnlineSessionController onlineSession;
        private DrinkPreparationStation drinkStation;
        private GrillTaskStation grill;
        private AttractionSource[] attractions = Array.Empty<AttractionSource>();
        private HordeEntrance[] entrances = Array.Empty<HordeEntrance>();
        private bool handlersRegistered;
        private float nextInputAt;
        private float nextSnapshotAt;

        public bool IsMatchStarted { get; private set; }
        public bool IsNetworkReady => handlersRegistered && networkManager != null && networkManager.IsListening;
        public bool CanHostStart => IsNetworkReady && networkManager.IsHost && !IsMatchStarted;

        public void Configure(NetworkManager manager, PrototypeBootstrap factory, PlayerVitals player,
            HordeDirector hordeDirector, PrototypeRound prototypeRound, RunSessionDirector session,
            PrototypeStartMenu menu, DrinkPreparationStation boozeStation, GrillTaskStation grillStation,
            AttractionSource[] attractionSources, HordeEntrance[] hordeEntrances,
            OnlineSessionController onlineController)
        {
            networkManager = manager;
            playerFactory = factory;
            localPlayer = player;
            localInput = player != null ? player.GetComponent<PlayerInputReader>() : null;
            horde = hordeDirector;
            round = prototypeRound;
            runSession = session;
            startMenu = menu;
            if (onlineSession != null) onlineSession.UnexpectedSessionEnded -= HandleUnexpectedSessionEnded;
            onlineSession = onlineController;
            if (onlineSession != null) onlineSession.UnexpectedSessionEnded += HandleUnexpectedSessionEnded;
            drinkStation = boozeStation;
            grill = grillStation;
            attractions = attractionSources ?? Array.Empty<AttractionSource>();
            entrances = hordeEntrances ?? Array.Empty<HordeEntrance>();
            Array.Sort(attractions, CompareComponentsByName);
            Array.Sort(entrances, CompareComponentsByName);
        }

        public void StartOnlineMatch()
        {
            if (!CanHostStart || startMenu == null) return;
            IsMatchStarted = true;
            startMenu.StartGame();
            SendStartMessageToAll();
        }

        private void Update()
        {
            if (!handlersRegistered)
            {
                if (networkManager != null && networkManager.IsListening) RegisterNetworkHandlers();
                return;
            }

            if (networkManager == null || !networkManager.IsListening)
            {
                UnregisterNetworkHandlers(true);
                return;
            }
            if (!IsMatchStarted) return;

            if (networkManager.IsServer)
            {
                if (Time.unscaledTime >= nextSnapshotAt)
                {
                    nextSnapshotAt = Time.unscaledTime + SnapshotInterval;
                    SendWorldSnapshot();
                }
            }
            else
            {
                SendLocalActions();
                if (Time.unscaledTime >= nextInputAt)
                {
                    nextInputAt = Time.unscaledTime + InputInterval;
                    SendLocalInput();
                }
            }
        }

        private void RegisterNetworkHandlers()
        {
            if (handlersRegistered || networkManager.CustomMessagingManager == null) return;
            handlersRegistered = true;
            CustomMessagingManager messages = networkManager.CustomMessagingManager;
            messages.RegisterNamedMessageHandler(StartMessage, HandleStartMessage);
            messages.RegisterNamedMessageHandler(InputMessage, HandleInputMessage);
            messages.RegisterNamedMessageHandler(ActionMessage, HandleActionMessage);
            messages.RegisterNamedMessageHandler(SnapshotMessage, HandleSnapshotMessage);
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            networkPlayers[networkManager.LocalClientId] = localPlayer;
            ApplyAuthorityRole(networkManager.IsServer);

            if (networkManager.IsServer)
            {
                IReadOnlyList<ulong> connectedClients = networkManager.ConnectedClientsIds;
                for (int i = 0; i < connectedClients.Count; i++) EnsureNetworkPlayer(connectedClients[i]);
            }
        }

        private void UnregisterNetworkHandlers(bool restoreOfflineAuthority)
        {
            if (!handlersRegistered) return;
            handlersRegistered = false;
            if (networkManager != null)
            {
                CustomMessagingManager messages = networkManager.CustomMessagingManager;
                if (messages != null)
                {
                    messages.UnregisterNamedMessageHandler(StartMessage);
                    messages.UnregisterNamedMessageHandler(InputMessage);
                    messages.UnregisterNamedMessageHandler(ActionMessage);
                    messages.UnregisterNamedMessageHandler(SnapshotMessage);
                }
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            DestroyRemotePlayers();
            IsMatchStarted = false;
            if (restoreOfflineAuthority) ApplyAuthorityRole(true);
        }

        private void ApplyAuthorityRole(bool isAuthoritative)
        {
            horde?.SetSimulationAuthority(isAuthoritative);
            round?.SetSimulationAuthority(isAuthoritative);
            runSession?.SetSimulationAuthority(isAuthoritative);
            drinkStation?.SetSimulationAuthority(isAuthoritative);
            grill?.SetSimulationAuthority(isAuthoritative);
            for (int i = 0; i < attractions.Length; i++) attractions[i]?.SetSimulationAuthority(isAuthoritative);
            for (int i = 0; i < entrances.Length; i++) entrances[i]?.SetSimulationAuthority(isAuthoritative);

            if (localPlayer == null) return;
            localPlayer.SetSimulationAuthority(isAuthoritative);
            localPlayer.GetComponent<PlayerStateMachine>()?.SetSimulationAuthority(isAuthoritative);
            localPlayer.GetComponent<PlayerInventory>()?.SetExecutionAuthority(isAuthoritative);
            localPlayer.GetComponent<PlayerDefenseController>()?.SetExecutionAuthority(isAuthoritative);
            localPlayer.GetComponent<PlayerInteraction>()?.SetExecutionAuthority(isAuthoritative);
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;
            EnsureNetworkPlayer(clientId);
            if (IsMatchStarted) SendStartMessage(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (networkPlayers.TryGetValue(clientId, out PlayerVitals player) && player != localPlayer)
            {
                networkPlayers.Remove(clientId);
                if (player != null) Destroy(player.gameObject);
            }
            if (clientId == networkManager.LocalClientId)
            {
                bool returnToMenu = IsMatchStarted;
                UnregisterNetworkHandlers(true);
                if (returnToMenu) ReturnToMenuScene();
            }
        }

        private PlayerVitals EnsureNetworkPlayer(ulong clientId)
        {
            if (networkPlayers.TryGetValue(clientId, out PlayerVitals existing) && existing != null) return existing;
            if (clientId == networkManager.LocalClientId)
            {
                networkPlayers[clientId] = localPlayer;
                return localPlayer;
            }
            if (playerFactory == null) return null;
            PlayerVitals created = playerFactory.CreateNetworkPlayer(clientId, networkManager.IsServer);
            networkPlayers[clientId] = created;
            return created;
        }

        private void SendStartMessageToAll()
        {
            using FastBufferWriter writer = CreateWriter(8);
            writer.WriteValueSafe(runSession != null ? runSession.RunSeed : 1);
            networkManager.CustomMessagingManager.SendNamedMessageToAll(StartMessage, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void SendStartMessage(ulong clientId)
        {
            using FastBufferWriter writer = CreateWriter(8);
            writer.WriteValueSafe(runSession != null ? runSession.RunSeed : 1);
            networkManager.CustomMessagingManager.SendNamedMessage(StartMessage, clientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void HandleStartMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (networkManager.IsServer || senderClientId != NetworkManager.ServerClientId) return;
            if (!TryReadStart(ref reader, out int seed)) return;
            runSession?.ApplyAuthoritativeSeed(seed);
            IsMatchStarted = true;
            startMenu?.StartGame();
        }

        private void SendLocalInput()
        {
            if (localInput == null) return;
            PlayerMotor motor = localPlayer != null ? localPlayer.GetComponent<PlayerMotor>() : null;
            Vector2 move = motor != null && motor.ControlEnabled
                ? CalculateWorldMove(localInput.Move)
                : Vector2.zero;
            sbyte x = (sbyte)Mathf.Clamp(Mathf.RoundToInt(move.x * 127f), -127, 127);
            sbyte y = (sbyte)Mathf.Clamp(Mathf.RoundToInt(move.y * 127f), -127, 127);
            FastBufferWriter writer = CreateWriter(16);
            try
            {
                writer.WriteValueSafe(x);
                writer.WriteValueSafe(y);
                WriteAppearance(ref writer, localPlayer != null ? localPlayer.GetComponent<PlayerAppearance>() : null);
                networkManager.CustomMessagingManager.SendNamedMessage(InputMessage, NetworkManager.ServerClientId,
                    writer, NetworkDelivery.UnreliableSequenced);
            }
            finally
            {
                writer.Dispose();
            }
        }

        private void SendLocalActions()
        {
            if (localInput == null) return;
            PlayerMotor motor = localPlayer != null ? localPlayer.GetComponent<PlayerMotor>() : null;
            if (motor != null && !motor.ControlEnabled) return;
            byte actions = 0;
            if (localInput.InteractPressed) actions |= 1;
            if (localInput.UseItemPressed) actions |= 2;
            if (localInput.DrinkPressed) actions |= 4;
            if (localInput.JumpPressed) actions |= 8;
            if (actions == 0) return;
            using FastBufferWriter writer = CreateWriter(2);
            writer.WriteValueSafe(actions);
            networkManager.CustomMessagingManager.SendNamedMessage(ActionMessage, NetworkManager.ServerClientId,
                writer, NetworkDelivery.ReliableSequenced);
        }

        private void HandleInputMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (!networkManager.IsServer || senderClientId == networkManager.LocalClientId) return;
            PlayerVitals player = EnsureNetworkPlayer(senderClientId);
            if (player == null) return;
            try
            {
                reader.ReadValueSafe(out sbyte x);
                reader.ReadValueSafe(out sbyte y);
                ReadAppearance(ref reader, player.GetComponent<PlayerAppearance>());
                player.GetComponent<PlayerInputReader>()?.SetRemoteWorldInput(new Vector2(x / 127f, y / 127f));
            }
            catch (OverflowException)
            {
                Debug.LogWarning($"Input online incompleto descartado para cliente {senderClientId}.");
            }
        }

        private void HandleActionMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (!networkManager.IsServer || senderClientId == networkManager.LocalClientId) return;
            PlayerVitals player = EnsureNetworkPlayer(senderClientId);
            if (player == null) return;
            try
            {
                reader.ReadValueSafe(out byte actions);
                player.GetComponent<PlayerInputReader>()?.PulseRemoteActions((byte)(actions & 15));
            }
            catch (OverflowException)
            {
                Debug.LogWarning($"Accion online incompleta descartada para cliente {senderClientId}.");
            }
        }

        private void SendWorldSnapshot()
        {
            FastBufferWriter writer = CreateWriter(1200);
            try
            {
                WriteSharedState(ref writer);
                networkManager.CustomMessagingManager.SendNamedMessageToAll(SnapshotMessage, writer,
                    NetworkDelivery.UnreliableSequenced);
            }
            finally
            {
                writer.Dispose();
            }
        }

        private void WriteSharedState(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeTime(round?.RemainingTime ?? 0f));
            writer.WriteValueSafe((byte)(round?.State ?? PrototypeRoundState.Playing));
            writer.WriteValueSafe((ushort)Mathf.Clamp(horde?.CurrentWave ?? 1, 1, ushort.MaxValue));
            writer.WriteValueSafe((byte)Mathf.Clamp(horde?.DifficultyStep ?? 0, 0, byte.MaxValue));
            writer.WriteValueSafe(horde == null || horde.IsWaveActive);
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeTime(horde?.WaveRemainingTime ?? 0f));
            writer.WriteValueSafe(runSession != null && runSession.IsUpgradeChoiceOpen);
            WriteScore(ref writer);
            WritePlayers(ref writer);
            WriteHorde(ref writer);
            WriteWorldTasks(ref writer);
        }

        private void WriteScore(ref FastBufferWriter writer)
        {
            PartyScoreModel score = runSession?.Score;
            writer.WriteValueSafe(score?.Score ?? 0);
            writer.WriteValueSafe(score?.Servings ?? 0);
            writer.WriteValueSafe(score?.Barricades ?? 0);
            writer.WriteValueSafe(score?.Distractions ?? 0);
            writer.WriteValueSafe(score?.BurnedMeals ?? 0);
            writer.WriteValueSafe(score?.Rescues ?? 0);
        }

        private void WritePlayers(ref FastBufferWriter writer)
        {
            int count = 0;
            foreach (KeyValuePair<ulong, PlayerVitals> pair in networkPlayers)
            {
                if (pair.Value != null && count < MaximumPlayers) count++;
            }
            writer.WriteValueSafe((byte)count);
            int written = 0;
            foreach (KeyValuePair<ulong, PlayerVitals> pair in networkPlayers)
            {
                if (written >= count || pair.Value == null) continue;
                WritePlayer(ref writer, pair.Key, pair.Value);
                written++;
            }
        }

        private static void WritePlayer(ref FastBufferWriter writer, ulong clientId, PlayerVitals player)
        {
            Vector3 position = player.transform.position;
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodePosition(position.x));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodePosition(position.y));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodePosition(position.z));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeYaw(player.transform.eulerAngles.y));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeRatio(player.HealthRatio));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeRatio(player.BuzzRatio));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeRatio(player.BalanceRatio));
            writer.WriteValueSafe((byte)player.GetComponent<PlayerStateMachine>().State);
            PlayerInventoryModel inventory = player.GetComponent<PlayerInventory>().Model;
            writer.WriteValueSafe((byte)Mathf.Clamp(inventory.DrinkServings, 0, byte.MaxValue));
            writer.WriteValueSafe((byte)Mathf.Clamp(inventory.DrinkCapacity, 1, byte.MaxValue));
            writer.WriteValueSafe((byte)inventory.DefenseItem);
            writer.WriteValueSafe((byte)Mathf.Clamp(inventory.DefenseUses, 0, byte.MaxValue));
            writer.WriteValueSafe((byte)Mathf.Clamp(inventory.DefensePowerLevel, 0, byte.MaxValue));
            WriteAppearance(ref writer, player.GetComponent<PlayerAppearance>());
        }

        private void WriteHorde(ref FastBufferWriter writer)
        {
            horde?.FillPoseSnapshots(hordePoses);
            int count = Mathf.Min(MaximumKids, hordePoses.Count);
            writer.WriteValueSafe((byte)count);
            for (int i = 0; i < count; i++)
            {
                HordePoseSnapshot pose = hordePoses[i];
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodePosition(pose.Position.x));
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodePosition(pose.Position.z));
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeYaw(pose.Yaw));
            }
        }

        private void WriteWorldTasks(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe((byte)(drinkStation?.State ?? DrinkStationState.Idle));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeTime(drinkStation?.RemainingPreparation ?? 0f));
            writer.WriteValueSafe((byte)Mathf.Clamp(drinkStation?.ServingsReady ?? 0, 0, byte.MaxValue));
            writer.WriteValueSafe((byte)(grill?.State ?? GrillTaskState.NeedsFuel));
            writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeTime(grill?.Remaining ?? 0f));

            writer.WriteValueSafe((byte)Mathf.Min(byte.MaxValue, attractions.Length));
            for (int i = 0; i < attractions.Length; i++)
            {
                AttractionSource source = attractions[i];
                writer.WriteValueSafe(source != null && source.IsInteractionEnabled);
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeTime(source?.ActiveRemaining ?? 0f));
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeTime(source?.CooldownRemaining ?? 0f));
            }
            writer.WriteValueSafe((byte)Mathf.Min(byte.MaxValue, entrances.Length));
            for (int i = 0; i < entrances.Length; i++)
            {
                HordeEntrance entrance = entrances[i];
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeRatio(entrance?.IntegrityRatio ?? 0f));
                writer.WriteValueSafe(NetworkSnapshotQuantization.EncodeRatio(entrance?.BuildProgressRatio ?? 0f));
            }
        }

        private void HandleSnapshotMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (networkManager.IsServer || senderClientId != NetworkManager.ServerClientId || !IsMatchStarted) return;
            try
            {
                ReadSharedState(ref reader);
            }
            catch (OverflowException exception)
            {
                Debug.LogWarning($"Snapshot online incompleto descartado: {exception.Message}");
            }
        }

        private void ReadSharedState(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort roundTime);
            reader.ReadValueSafe(out byte roundState);
            reader.ReadValueSafe(out ushort waveNumber);
            reader.ReadValueSafe(out byte difficultyStep);
            reader.ReadValueSafe(out bool waveActive);
            reader.ReadValueSafe(out ushort waveTime);
            reader.ReadValueSafe(out bool upgradePaused);
            ReadScore(ref reader);
            ReadPlayers(ref reader);
            ReadHorde(ref reader, waveNumber, difficultyStep, waveActive, waveTime);
            ReadWorldTasks(ref reader);
            round?.ApplyRemoteState(NetworkSnapshotQuantization.DecodeTime(roundTime),
                (PrototypeRoundState)Mathf.Clamp(roundState, 0, 2));
            runSession?.ApplyRemoteUpgradePause(upgradePaused);
        }

        private void ReadScore(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out int score);
            reader.ReadValueSafe(out int servings);
            reader.ReadValueSafe(out int barricades);
            reader.ReadValueSafe(out int distractions);
            reader.ReadValueSafe(out int burnedMeals);
            reader.ReadValueSafe(out int rescues);
            runSession?.Score.ApplySnapshot(score, servings, barricades, distractions, burnedMeals, rescues);
        }

        private void ReadPlayers(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte playerCount);
            if (playerCount > MaximumPlayers) throw new OverflowException("Cantidad de jugadores fuera de rango.");
            receivedPlayerIds.Clear();
            for (int i = 0; i < playerCount; i++) ReadPlayer(ref reader);

            List<ulong> staleIds = null;
            foreach (KeyValuePair<ulong, PlayerVitals> pair in networkPlayers)
            {
                if (pair.Key == networkManager.LocalClientId || receivedPlayerIds.Contains(pair.Key)) continue;
                staleIds ??= new List<ulong>();
                staleIds.Add(pair.Key);
            }
            if (staleIds == null) return;
            for (int i = 0; i < staleIds.Count; i++)
            {
                ulong id = staleIds[i];
                if (networkPlayers.TryGetValue(id, out PlayerVitals player) && player != null) Destroy(player.gameObject);
                networkPlayers.Remove(id);
            }
        }

        private void ReadPlayer(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong clientId);
            reader.ReadValueSafe(out short x);
            reader.ReadValueSafe(out short y);
            reader.ReadValueSafe(out short z);
            reader.ReadValueSafe(out ushort yaw);
            reader.ReadValueSafe(out byte health);
            reader.ReadValueSafe(out byte buzz);
            reader.ReadValueSafe(out byte balance);
            reader.ReadValueSafe(out byte state);
            reader.ReadValueSafe(out byte drinks);
            reader.ReadValueSafe(out byte drinkCapacity);
            reader.ReadValueSafe(out byte defenseItem);
            reader.ReadValueSafe(out byte defenseUses);
            reader.ReadValueSafe(out byte defensePower);
            PlayerVitals player = EnsureNetworkPlayer(clientId);
            PlayerAppearance appearance = player != null ? player.GetComponent<PlayerAppearance>() : null;
            ReadAppearance(ref reader, appearance);
            receivedPlayerIds.Add(clientId);
            if (player == null) return;

            Vector3 position = new Vector3(NetworkSnapshotQuantization.DecodePosition(x),
                NetworkSnapshotQuantization.DecodePosition(y), NetworkSnapshotQuantization.DecodePosition(z));
            ApplyPlayerPose(player, clientId == networkManager.LocalClientId, position,
                NetworkSnapshotQuantization.DecodeYaw(yaw));
            player.ApplyRemoteSnapshot(NetworkSnapshotQuantization.DecodeRatio(health),
                NetworkSnapshotQuantization.DecodeRatio(buzz), NetworkSnapshotQuantization.DecodeRatio(balance));
            player.GetComponent<PlayerStateMachine>()?.ApplyRemoteState((PlayerState)Mathf.Clamp(state, 0, 3));
            player.GetComponent<PlayerInventory>()?.Model.ApplySnapshot(drinks, drinkCapacity,
                (DefenseItemType)Mathf.Clamp(defenseItem, 0, 2), defenseUses, defensePower);
        }

        private static void WriteAppearance(ref FastBufferWriter writer, PlayerAppearance appearance)
        {
            writer.WriteValueSafe((byte)(appearance?.ShirtIndex ?? 0));
            writer.WriteValueSafe((byte)(appearance?.SkinIndex ?? 0));
            writer.WriteValueSafe((byte)(appearance?.PantsIndex ?? 0));
            writer.WriteValueSafe((byte)(appearance?.HairIndex ?? 0));
            writer.WriteValueSafe((byte)(appearance?.AccessoryIndex ?? 0));
            writer.WriteValueSafe((byte)(appearance?.FacialHairIndex ?? 0));
            writer.WriteValueSafe(appearance != null && appearance.HasGlasses);
        }

        private static void ReadAppearance(ref FastBufferReader reader, PlayerAppearance appearance)
        {
            reader.ReadValueSafe(out byte shirt);
            reader.ReadValueSafe(out byte skin);
            reader.ReadValueSafe(out byte pants);
            reader.ReadValueSafe(out byte hair);
            reader.ReadValueSafe(out byte accessory);
            reader.ReadValueSafe(out byte facialHair);
            reader.ReadValueSafe(out bool glasses);
            appearance?.ApplySnapshot(shirt, skin, pants, hair, accessory, facialHair, glasses);
        }

        private static void ApplyPlayerPose(PlayerVitals player, bool locallyPredicted, Vector3 position, float yaw)
        {
            float distance = Vector3.Distance(player.transform.position, position);
            float blend = locallyPredicted && distance < 4f ? 0.22f : 1f;
            Vector3 corrected = Vector3.Lerp(player.transform.position, position, blend);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = corrected;
                body.rotation = Quaternion.Slerp(body.rotation, rotation, blend);
            }
            else
            {
                player.transform.SetPositionAndRotation(corrected,
                    Quaternion.Slerp(player.transform.rotation, rotation, blend));
            }
        }

        private void ReadHorde(ref FastBufferReader reader, int waveNumber, int difficultyStep,
            bool waveActive, ushort waveTime)
        {
            reader.ReadValueSafe(out byte kidCount);
            if (kidCount > MaximumKids) throw new OverflowException("Cantidad de horda fuera de rango.");
            hordePoses.Clear();
            for (int i = 0; i < kidCount; i++)
            {
                reader.ReadValueSafe(out short x);
                reader.ReadValueSafe(out short z);
                reader.ReadValueSafe(out ushort yaw);
                hordePoses.Add(new HordePoseSnapshot(
                    new Vector3(NetworkSnapshotQuantization.DecodePosition(x), 1f,
                        NetworkSnapshotQuantization.DecodePosition(z)),
                    NetworkSnapshotQuantization.DecodeYaw(yaw)));
            }
            horde?.ApplyRemoteSnapshot(hordePoses, waveNumber, difficultyStep, waveActive,
                NetworkSnapshotQuantization.DecodeTime(waveTime));
        }

        private void ReadWorldTasks(ref FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte drinkState);
            reader.ReadValueSafe(out ushort drinkRemaining);
            reader.ReadValueSafe(out byte servings);
            drinkStation?.ApplyRemoteState((DrinkStationState)Mathf.Clamp(drinkState, 0, 2),
                NetworkSnapshotQuantization.DecodeTime(drinkRemaining), servings);
            reader.ReadValueSafe(out byte grillState);
            reader.ReadValueSafe(out ushort grillRemaining);
            grill?.ApplyRemoteState((GrillTaskState)Mathf.Clamp(grillState, 0, 5),
                NetworkSnapshotQuantization.DecodeTime(grillRemaining));

            reader.ReadValueSafe(out byte attractionCount);
            for (int i = 0; i < attractionCount; i++)
            {
                reader.ReadValueSafe(out bool enabled);
                reader.ReadValueSafe(out ushort activeRemaining);
                reader.ReadValueSafe(out ushort cooldownRemaining);
                if (i < attractions.Length)
                    attractions[i]?.ApplyRemoteState(enabled,
                        NetworkSnapshotQuantization.DecodeTime(activeRemaining),
                        NetworkSnapshotQuantization.DecodeTime(cooldownRemaining));
            }
            reader.ReadValueSafe(out byte entranceCount);
            for (int i = 0; i < entranceCount; i++)
            {
                reader.ReadValueSafe(out byte integrity);
                reader.ReadValueSafe(out byte buildProgress);
                if (i < entrances.Length)
                    entrances[i]?.ApplyRemoteState(NetworkSnapshotQuantization.DecodeRatio(integrity),
                        NetworkSnapshotQuantization.DecodeRatio(buildProgress));
            }
        }

        private void DestroyRemotePlayers()
        {
            foreach (KeyValuePair<ulong, PlayerVitals> pair in networkPlayers)
            {
                if (pair.Value != null && pair.Value != localPlayer) Destroy(pair.Value.gameObject);
            }
            networkPlayers.Clear();
        }

        private static FastBufferWriter CreateWriter(int capacity)
        {
            return new FastBufferWriter(capacity, Allocator.Temp);
        }

        private static bool TryReadStart(ref FastBufferReader reader, out int seed)
        {
            try
            {
                reader.ReadValueSafe(out seed);
                return true;
            }
            catch (OverflowException)
            {
                seed = 0;
                Debug.LogWarning("Mensaje de inicio online incompleto descartado.");
                return false;
            }
        }

        private static Vector2 CalculateWorldMove(Vector2 input)
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = camera != null ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 world = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
            return new Vector2(world.x, world.z);
        }

        private static int CompareComponentsByName(Component left, Component right)
        {
            return string.Compare(left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty, StringComparison.Ordinal);
        }

        private void OnDestroy()
        {
            if (onlineSession != null) onlineSession.UnexpectedSessionEnded -= HandleUnexpectedSessionEnded;
            UnregisterNetworkHandlers(false);
        }

        private void HandleUnexpectedSessionEnded()
        {
            if (!IsMatchStarted) return;
            ReturnToMenuScene();
        }

        private static void ReturnToMenuScene()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrEmpty(scene.name)) SceneManager.LoadScene(scene.name);
        }
    }
}
