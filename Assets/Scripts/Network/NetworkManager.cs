using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static NetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("NetworkManager Instance created");
        }
        else
            Destroy(gameObject);
    }
    #endregion

    #region Public Variables
    [Header("Player Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string playerPrefabName = "PlayerPrefab";

    [Header("Network Settings")]
    [SerializeField] private string gameVersion = "1.0";

    // Player data (set after login)
    public string PlayerName { get; set; }
    public string PlayerRole { get; set; }
    public int PlayerId { get; set; }

    // Track all players in the current room
    public Dictionary<int, GameObject> ActivePlayers { get; private set; } = new Dictionary<int, GameObject>();
    #endregion

    #region Private Variables
    private bool isConnecting = false;
    private string roomNameToJoin = "";
    #endregion

    #region Unity Methods
    private void Start()
    {
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    #endregion

    #region Connection Methods
    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected to Photon");
            return;
        }

        isConnecting = true;
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void DisconnectFromPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        if (isConnecting)
            PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby");
        isConnecting = false;

        // If we have a room to join, join it now
        if (!string.IsNullOrEmpty(roomNameToJoin))
        {
            Debug.Log($"Joining pending room: {roomNameToJoin}");
            JoinRoom(roomNameToJoin);
            roomNameToJoin = "";
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected from Photon: {cause}");
        isConnecting = false;
        roomNameToJoin = "";
    }
    #endregion

    #region Room Methods
    public void CreateRoom(string roomName, byte maxPlayers = 10)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Cannot create room: Not connected to Photon");
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };

        // Set host properties
        Hashtable roomProps = new Hashtable
        {
            ["hostId"] = PlayerId,
            ["hostName"] = PlayerName
        };
        roomOptions.CustomRoomProperties = roomProps;

        // CRITICAL: These properties will be visible in the lobby
        string[] roomPropsInLobby = { "hostName", "hostId" };
        roomOptions.CustomRoomPropertiesForLobby = roomPropsInLobby;

        Debug.Log($"Creating room: {roomName} with max players: {maxPlayers}");
        Debug.Log($"Room properties: hostName={PlayerName}, hostId={PlayerId}");

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected)
        {
            roomNameToJoin = roomName;
            ConnectToPhoton();
            return;
        }

        if (!PhotonNetwork.InLobby)
        {
            roomNameToJoin = roomName;
            PhotonNetwork.JoinLobby();
            return;
        }

        Debug.Log($"Attempting to join room: {roomName}");
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Room created successfully: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Room is visible: {PhotonNetwork.CurrentRoom.IsVisible}");
        Debug.Log($"Room is open: {PhotonNetwork.CurrentRoom.IsOpen}");

        SetLocalPlayerProperties();

        // Load the game scene
        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message} (Code: {returnCode})");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("=== JOINED ROOM ===");
        Debug.Log($"Room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
        Debug.Log($"Is Master Client: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"Client State: {PhotonNetwork.NetworkClientState}");
        Debug.Log($"Is Connected: {PhotonNetwork.IsConnected}");

        SetLocalPlayerProperties();

        // Log all existing players
        Debug.Log("=== PLAYERS IN ROOM ===");
        foreach (var player in PhotonNetwork.CurrentRoom.Players)
        {
            Debug.Log($"Player {player.Key}: {player.Value.NickName}");
        }

        // Load the game scene
        Debug.Log("Loading GameScene...");
        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message} (Code: {returnCode})");
        roomNameToJoin = "";
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room");
        ActivePlayers.Clear();
        SceneManager.LoadScene("LobbyScene");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName} (Actor: {newPlayer.ActorNumber})");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName} (Actor: {otherPlayer.ActorNumber})");
    }
    #endregion

    #region Player Methods
    private void SetLocalPlayerProperties()
    {
        Hashtable playerProps = new Hashtable
        {
            ["playerName"] = PlayerName,
            ["role"] = PlayerRole,
            ["userId"] = PlayerId
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
        PhotonNetwork.LocalPlayer.NickName = PlayerName;

        Debug.Log($"Set local player properties - Name: {PlayerName}, Role: {PlayerRole}, ID: {PlayerId}");
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab not assigned!");
            return;
        }

        if (playerPrefab.GetComponent<PhotonView>() == null)
        {
            Debug.LogError("Player Prefab missing PhotonView!");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition(PlayerRole);
        Debug.Log($"Spawning player at: {spawnPosition}");

        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity, 0);

        if (playerObj == null)
        {
            Debug.LogError("Failed to spawn player! Make sure prefab is in Resources folder.");
            return;
        }

        PhotonView photonView = playerObj.GetComponent<PhotonView>();
        if (photonView != null)
            ActivePlayers[photonView.ViewID] = playerObj;

        PlayerNetwork playerNetwork = playerObj.GetComponent<PlayerNetwork>();
        if (playerNetwork != null)
            playerNetwork.Initialize(PlayerName, PlayerRole, PlayerId);
    }

    private Vector3 GetSpawnPosition(string role)
    {
        switch (role.ToLower())
        {
            case "host": return new Vector3(0, 1, 5);
            case "audience": return new Vector3(0, 1, -5);
            default: return new Vector3(Random.Range(-3, 3), 1, 0);
        }
    }

    public void ClearPlayerData()
    {
        PlayerName = "";
        PlayerRole = "";
        PlayerId = 0;
        ActivePlayers.Clear();
    }
    #endregion
}