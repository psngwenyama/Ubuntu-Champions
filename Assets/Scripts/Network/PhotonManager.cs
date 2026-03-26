using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance { get; private set; }

    private Dictionary<int, PlayerNetwork> players = new Dictionary<int, PlayerNetwork>();

    // Make these public with public setters
    public string playerRole { get; set; }
    public string playerName { get; set; }
    public int playerId { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("PhotonManager Instance created");
        }
        else
        {
            Debug.Log("Destroying duplicate PhotonManager");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon...");
        }
        else
        {
            Debug.Log("Already connected to Photon");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby");
    }

    public void CreateRoom(string roomName, byte maxPlayers)
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };

        // Set host properties
        Hashtable hostProps = new Hashtable
        {
            ["hostId"] = playerId,
            ["hostName"] = playerName
        };
        roomOptions.CustomRoomProperties = hostProps;

        // Make these properties visible in the lobby
        string[] roomPropsInLobby = { "hostName", "hostId" };
        roomOptions.CustomRoomPropertiesForLobby = roomPropsInLobby;

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully");
        SceneManager.LoadScene("GameScene");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");

        // Set player role in room properties
        Hashtable playerProps = new Hashtable
        {
            ["playerName"] = playerName,
            ["role"] = playerRole,
            ["userId"] = playerId
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        Debug.Log($"Set local player properties - Name: {playerName}, Role: {playerRole}");

        SceneManager.LoadScene("GameScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player entered: {newPlayer.NickName} with properties: {string.Join(", ", newPlayer.CustomProperties)}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName}");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room");
        SceneManager.LoadScene("LobbyScene");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void ClearPlayerData()
    {
        playerName = "";
        playerRole = "";
        playerId = 0;
    }
}