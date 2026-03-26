using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject playerPanel;
    [SerializeField] private GameObject audiencePanel;
    [SerializeField] private Button leaveButton;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isGameInitialized = false;

    private void Start()
    {
        Debug.Log("=== GAME MANAGER START ===");
        Debug.Log($"PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"Client State: {PhotonNetwork.NetworkClientState}");

        if (PhotonNetwork.NetworkClientState != ClientState.Joined)
        {
            Debug.Log($"Waiting for room connection. Current state: {PhotonNetwork.NetworkClientState}");

            if (statusText != null)
                statusText.text = "Connecting to room...";

            StartCoroutine(WaitForRoomConnection());
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Not in room yet, waiting for connection...");
            if (statusText != null)
                statusText.text = "Connecting to room...";

            StartCoroutine(WaitForRoomConnection());
            return;
        }

        InitializeGame();
    }

    private IEnumerator WaitForRoomConnection()
    {
        float timeout = 10f;
        float elapsed = 0f;
        int lastPlayerCount = 0;

        while (!PhotonNetwork.InRoom && elapsed < timeout)
        {
            if (PhotonNetwork.NetworkClientState == ClientState.Joined)
            {
                Debug.Log("Client state is Joined, room should be available soon...");
            }

            // Log state changes every 2 seconds
            if (elapsed % 2 < Time.deltaTime)
            {
                Debug.Log($"Waiting for room connection... State: {PhotonNetwork.NetworkClientState}, Elapsed: {elapsed:F1}s");

                if (statusText != null)
                    statusText.text = $"Connecting to room... ({elapsed:F0}s)";
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"Room connection established! Room: {PhotonNetwork.CurrentRoom.Name}");
            InitializeGame();
        }
        else
        {
            // Check if we're actually in a room but the flag hasn't updated
            if (PhotonNetwork.NetworkClientState == ClientState.Joined)
            {
                Debug.Log("Client state is Joined, forcing room check...");
                // Force a refresh of the room state
                if (PhotonNetwork.CurrentRoom != null)
                {
                    Debug.Log($"Found room: {PhotonNetwork.CurrentRoom.Name}");
                    InitializeGame();
                    yield break;
                }
            }

            Debug.LogError($"Failed to connect to room after {timeout} seconds! State: {PhotonNetwork.NetworkClientState}");

            if (statusText != null)
                statusText.text = "Failed to connect to room. Returning to lobby...";

            yield return new WaitForSeconds(2f);
            ReturnToLobby();
        }
    }

    private void InitializeGame()
    {
        if (isGameInitialized) return;
        isGameInitialized = true;

        Debug.Log("=== INITIALIZING GAME ===");
        Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom?.Name ?? "Unknown"}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
        Debug.Log($"Is Master Client: {PhotonNetwork.IsMasterClient}");

        // Log all players in room
        if (PhotonNetwork.CurrentRoom != null)
        {
            foreach (var player in PhotonNetwork.CurrentRoom.Players)
            {
                Debug.Log($"Player in room: {player.Value.NickName} (Actor: {player.Key})");
            }
        }

        // Check NetworkManager
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager.Instance is null!");
            return;
        }

        Debug.Log($"NetworkManager - Player: {NetworkManager.Instance.PlayerName}, Role: {NetworkManager.Instance.PlayerRole}");

        // Setup UI based on role
        SetupRoleUI();

        // Update room status
        UpdateRoomStatus();

        // Update player count
        UpdatePlayerCount();

        // Spawn the player after a short delay
        StartCoroutine(SpawnPlayerAfterDelay());

        // Setup leave button
        if (leaveButton != null)
            leaveButton.onClick.AddListener(LeaveGame);
    }

    private IEnumerator SpawnPlayerAfterDelay()
    {
        Debug.Log("Waiting 0.5 seconds before spawning player...");
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Spawning player now...");
            NetworkManager.Instance.SpawnPlayer();
        }
        else
        {
            Debug.LogError("Not in room when trying to spawn player!");
        }
    }

    private void SetupRoleUI()
    {
        if (NetworkManager.Instance == null) return;

        string role = NetworkManager.Instance.PlayerRole?.ToLower() ?? "player";
        Debug.Log($"Setting up UI for role: {role}");

        // Set role text
        if (roleText != null)
            roleText.text = $"Role: {role.ToUpper()}";

        // Show/hide role-specific panels
        if (hostPanel != null)
            hostPanel.SetActive(role == "host");

        if (playerPanel != null)
            playerPanel.SetActive(role == "player");

        if (audiencePanel != null)
            audiencePanel.SetActive(role == "audience");
    }

    private void UpdateRoomStatus()
    {
        if (statusText != null && PhotonNetwork.CurrentRoom != null)
        {
            statusText.text = $"Connected to: {PhotonNetwork.CurrentRoom.Name}";
            Debug.Log($"Status updated: {statusText.text}");
        }
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText != null && PhotonNetwork.CurrentRoom != null)
        {
            playerCountText.text = $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            Debug.Log($"Player count updated: {playerCountText.text}");
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName} (Actor: {newPlayer.ActorNumber})");

        UpdatePlayerCount();

        if (statusText != null)
            statusText.text = $"{newPlayer.NickName} joined!";
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName} (Actor: {otherPlayer.ActorNumber})");

        UpdatePlayerCount();

        if (statusText != null)
            statusText.text = $"{otherPlayer.NickName} left!";
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"New Master Client: {newMasterClient.NickName}");

        if (statusText != null)
            statusText.text = $"Host is now {newMasterClient.NickName}";
    }

    public void LeaveGame()
    {
        Debug.Log("Leaving game...");
        StartCoroutine(LeaveGameCoroutine());
    }

    private IEnumerator LeaveGameCoroutine()
    {
        if (statusText != null)
            statusText.text = "Leaving game...";

        NetworkManager.Instance.LeaveRoom();
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("LobbyScene");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room");
        SceneManager.LoadScene("LobbyScene");
    }

    private void ReturnToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    private void OnDestroy()
    {
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(LeaveGame);
    }
}