using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class OnlineSessionController : MonoBehaviour
    {
        private const int MinPlayers = 2;
        private const int MaxPlayers = 8;

        private ISession activeSession;
        private Action leaveCompleted;
        private bool isLeavingSession;

        public event Action UnexpectedSessionEnded;

        public bool IsBusy { get; private set; }
        public bool IsProjectLinked => !string.IsNullOrWhiteSpace(Application.cloudProjectId);
        public bool HasActiveSession => activeSession != null && activeSession.IsMember;
        public bool IsHost => activeSession != null && activeSession.IsHost;
        public string JoinCode => activeSession?.Code ?? string.Empty;
        public int PlayerCount => activeSession?.PlayerCount ?? 0;
        public int Capacity => activeSession?.MaxPlayers ?? 0;
        public string StatusText { get; private set; } =
            "Vincula el proyecto a Unity Cloud para habilitar las salas online.";

        public void CreateSession(int requestedCapacity, bool isPrivate)
        {
            if (IsBusy || HasActiveSession) return;
            _ = CreateSessionAsync(Mathf.Clamp(requestedCapacity, MinPlayers, MaxPlayers), isPrivate);
        }

        public void JoinSession(string joinCode)
        {
            if (IsBusy || HasActiveSession) return;
            string normalizedCode = NormalizeJoinCode(joinCode);
            if (string.IsNullOrEmpty(normalizedCode))
            {
                StatusText = "Escribe un codigo de sala valido.";
                return;
            }

            _ = JoinSessionAsync(normalizedCode);
        }

        public void LeaveSession()
        {
            LeaveSession(null);
        }

        public void LeaveSession(Action onCompleted)
        {
            if (onCompleted != null) leaveCompleted += onCompleted;
            if (IsBusy) return;
            if (activeSession == null)
            {
                InvokeLeaveCompleted();
                return;
            }
            _ = LeaveSessionAsync();
        }

        public static string NormalizeJoinCode(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace(" ", string.Empty).ToUpperInvariant();
        }

        private async Task CreateSessionAsync(int capacity, bool isPrivate)
        {
            if (!BeginOnlineOperation("Creando sala y reservando Relay...")) return;

            try
            {
                await EnsureServicesReadyAsync();
                SessionOptions options = new SessionOptions
                {
                    MaxPlayers = capacity,
                    IsPrivate = isPrivate,
                    Name = $"Booze & Blocks {UnityEngine.Random.Range(100, 1000)}"
                }.WithRelayNetwork();

                AttachSession(await MultiplayerService.Instance.CreateSessionAsync(options));
                StatusText = $"Sala lista. Comparte el codigo {JoinCode}.";
            }
            catch (Exception exception)
            {
                HandleFailure("No se pudo crear la sala", exception);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task JoinSessionAsync(string joinCode)
        {
            if (!BeginOnlineOperation($"Uniendose a {joinCode}...")) return;

            try
            {
                await EnsureServicesReadyAsync();
                AttachSession(await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode));
                StatusText = $"Conectado a {JoinCode}. Esperando al anfitrion.";
            }
            catch (Exception exception)
            {
                HandleFailure("No se pudo unir a la sala", exception);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LeaveSessionAsync()
        {
            IsBusy = true;
            isLeavingSession = true;
            StatusText = "Saliendo de la sala...";
            ISession sessionToLeave = activeSession;
            DetachSession();

            try
            {
                await sessionToLeave.LeaveAsync();
                ShutdownTransport();
                StatusText = "Saliste de la sala.";
            }
            catch (Exception exception)
            {
                HandleFailure("No se pudo cerrar la sesion limpiamente", exception);
            }
            finally
            {
                IsBusy = false;
                isLeavingSession = false;
                InvokeLeaveCompleted();
            }
        }

        private bool BeginOnlineOperation(string status)
        {
            if (!IsProjectLinked)
            {
                StatusText = "Falta vincular el proyecto: Edit > Project Settings > Services.";
                return false;
            }

            if (NetworkManager.Singleton == null)
            {
                StatusText = "La configuracion de red no esta disponible.";
                return false;
            }

            IsBusy = true;
            StatusText = status;
            return true;
        }

        private static async Task EnsureServicesReadyAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private void AttachSession(ISession session)
        {
            DetachSession();
            activeSession = session;
            activeSession.Changed += RefreshStatus;
            activeSession.PlayerJoined += HandlePlayerChanged;
            activeSession.PlayerHasLeft += HandlePlayerChanged;
            activeSession.RemovedFromSession += HandleRemovedFromSession;
        }

        private void DetachSession()
        {
            if (activeSession == null) return;
            activeSession.Changed -= RefreshStatus;
            activeSession.PlayerJoined -= HandlePlayerChanged;
            activeSession.PlayerHasLeft -= HandlePlayerChanged;
            activeSession.RemovedFromSession -= HandleRemovedFromSession;
            activeSession = null;
        }

        private void HandlePlayerChanged(string playerId)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (activeSession == null) return;
            StatusText = IsHost
                ? $"Sala {JoinCode}: {PlayerCount}/{Capacity} jugadores."
                : $"Conectado a {JoinCode}: {PlayerCount}/{Capacity} jugadores.";
        }

        private void HandleRemovedFromSession()
        {
            bool shouldNotify = !isLeavingSession;
            DetachSession();
            ShutdownTransport();
            StatusText = "La sala fue cerrada por el anfitrion.";
            if (shouldNotify) UnexpectedSessionEnded?.Invoke();
        }

        private void HandleFailure(string context, Exception exception)
        {
            bool shouldNotify = activeSession != null && !isLeavingSession;
            ShutdownTransport();

            DetachSession();
            string detail = string.IsNullOrWhiteSpace(exception.Message) ? exception.GetType().Name : exception.Message;
            StatusText = $"{context}: {detail}";
            Debug.LogWarning($"{context}. {exception}");
            if (shouldNotify) UnexpectedSessionEnded?.Invoke();
        }

        private void OnDestroy()
        {
            ISession sessionToLeave = activeSession;
            DetachSession();
            if (sessionToLeave != null) _ = LeaveDuringShutdownAsync(sessionToLeave, NetworkManager.Singleton);
            else ShutdownTransport();
        }

        private static async Task LeaveDuringShutdownAsync(ISession session, NetworkManager networkManager)
        {
            try
            {
                await session.LeaveAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo abandonar la sala durante el cierre. {exception.Message}");
            }
            finally
            {
                if (networkManager != null && networkManager.IsListening) networkManager.Shutdown();
            }
        }

        private static void ShutdownTransport()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }

        private void InvokeLeaveCompleted()
        {
            Action callback = leaveCompleted;
            leaveCompleted = null;
            callback?.Invoke();
        }
    }
}
