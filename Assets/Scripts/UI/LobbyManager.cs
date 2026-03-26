using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private Button logoutButton;

    [Header("Room Creation")]
    [SerializeField] private GameObject roomCreationPanel;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomListItemPrefab;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 10;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private bool isRefreshing = false;

    private void Start()
    {
        Debug.Log("LobbyManager Start called");

        // Check if we're in the lobby
        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Not in lobby, joining...");
            PhotonNetwork.JoinLobby();
        }

        // Display user info
        if (NetworkManager.Instance != null)
        {
            welcomeText.text = $"Welcome, {NetworkManager.Instance.PlayerName}!";
            roleText.text = $"Role: {NetworkManager.Instance.PlayerRole.ToUpper()}";

            // Show/hide room creation panel based on role
            bool isHost = NetworkManager.Instance.PlayerRole.ToLower() == "host";
            roomCreationPanel.SetActive(isHost);
            Debug.Log($"Is Host: {isHost}, Room Creation Panel Active: {isHost}");
        }
        else
        {
            Debug.LogError("NetworkManager.Instance is null!");
        }

        // Setup buttons
        createRoomButton.onClick.AddListener(OnCreateRoomClick);
        logoutButton.onClick.AddListener(OnLogoutClick);

        // Setup max players dropdown
        SetupMaxPlayersDropdown();

        // Initial refresh
        StartCoroutine(RefreshRoomListAfterDelay());
    }

    private IEnumerator RefreshRoomListAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        RefreshRoomList();
    }

    private void SetupMaxPlayersDropdown()
    {
        maxPlayersDropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = 2; i <= maxPlayersPerRoom; i += 2)
            options.Add($"{i} Players");
        maxPlayersDropdown.AddOptions(options);
        maxPlayersDropdown.value = options.Count - 1; // Select highest by default
    }

    private void OnCreateRoomClick()
    {
        if (NetworkManager.Instance.PlayerRole.ToLower() != "host")
        {
            ShowFeedback("Only hosts can create rooms");
            return;
        }

        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            ShowFeedback("Please enter a room name");
            return;
        }

        if (roomNameInput.text.Length < 3)
        {
            ShowFeedback("Room name must be at least 3 characters");
            return;
        }

        byte maxPlayers = maxPlayersPerRoom;
        if (maxPlayersDropdown != null)
        {
            string selected = maxPlayersDropdown.options[maxPlayersDropdown.value].text;
            maxPlayers = byte.Parse(selected.Split(' ')[0]);
        }

        Debug.Log($"Creating room: {roomNameInput.text} with max players: {maxPlayers}");
        NetworkManager.Instance.CreateRoom(roomNameInput.text, maxPlayers);
        ShowFeedback("Creating room...");
    }

    public void JoinRoom(string roomName)
    {
        Debug.Log($"JoinRoom called for: {roomName}");
        ShowFeedback($"Joining {roomName}...");
        NetworkManager.Instance.JoinRoom(roomName);
    }

    public void RefreshRoomList()
    {
        if (isRefreshing) return;
        StartCoroutine(RefreshRoomListCoroutine());
    }

    private IEnumerator RefreshRoomListCoroutine()
    {
        isRefreshing = true;
        Debug.Log("Refreshing room list...");

        cachedRoomList.Clear();
        UpdateRoomListUI();

        // Force lobby refresh
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            yield return new WaitForSeconds(0.5f);
            PhotonNetwork.JoinLobby();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
        }

        yield return new WaitForSeconds(1f);
        isRefreshing = false;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Room list updated. Rooms received: {roomList.Count}");

        foreach (var room in roomList)
        {
            Debug.Log($"Room: {room.Name}, Players: {room.PlayerCount}/{room.MaxPlayers}, Visible: {room.IsVisible}, Removed: {room.RemovedFromList}");
        }

        UpdateCachedRoomList(roomList);
        UpdateRoomListUI();
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList || !info.IsVisible || !info.IsOpen)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    Debug.Log($"Removing room from cache: {info.Name}");
                    cachedRoomList.Remove(info.Name);
                }
                continue;
            }

            Debug.Log($"Adding/Updating room in cache: {info.Name} ({info.PlayerCount}/{info.MaxPlayers})");

            if (cachedRoomList.ContainsKey(info.Name))
                cachedRoomList[info.Name] = info;
            else
                cachedRoomList.Add(info.Name, info);
        }
    }

    private void UpdateRoomListUI()
    {
        Debug.Log($"Updating room list UI. Cached rooms: {cachedRoomList.Count}");

        // Clear existing list
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        // Check if there are any rooms
        if (cachedRoomList.Count == 0)
        {
            ShowFeedback("No rooms available. Create one as a host!");
            return;
        }

        // Create new list items
        int roomsDisplayed = 0;
        foreach (var room in cachedRoomList.Values)
        {
            if (room.PlayerCount < room.MaxPlayers)
            {
                CreateRoomListItem(room);
                roomsDisplayed++;
            }
        }

        if (roomsDisplayed == 0)
        {
            ShowFeedback("All rooms are full. Check back later!");
        }
        else
        {
            Debug.Log($"Displayed {roomsDisplayed} rooms");
            if (feedbackText != null && feedbackText.text.Contains("No rooms"))
                feedbackText.text = "";
        }
    }

    private void CreateRoomListItem(RoomInfo roomInfo)
    {
        GameObject listItem = Instantiate(roomListItemPrefab, roomListContent);

        TextMeshProUGUI roomNameText = listItem.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI playerCountText = listItem.transform.Find("PlayerCountText")?.GetComponent<TextMeshProUGUI>();
        Button joinButton = listItem.transform.Find("JoinButton")?.GetComponent<Button>();

        if (roomNameText != null)
            roomNameText.text = roomInfo.Name;

        if (playerCountText != null)
        {
            playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

            // Color code based on player count
            if (roomInfo.PlayerCount >= roomInfo.MaxPlayers)
                playerCountText.color = Color.red;
            else if (roomInfo.PlayerCount >= roomInfo.MaxPlayers / 2)
                playerCountText.color = Color.yellow;
            else
                playerCountText.color = Color.green;
        }

        if (joinButton != null)
        {
            // Clear any existing listeners
            joinButton.onClick.RemoveAllListeners();

            // Add new listener
            joinButton.onClick.AddListener(() => JoinRoom(roomInfo.Name));

            // Disable button if room is full
            if (roomInfo.PlayerCount >= roomInfo.MaxPlayers)
            {
                joinButton.interactable = false;

                // Change button text
                TextMeshProUGUI buttonText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "FULL";
            }
            else
            {
                joinButton.interactable = true;
                TextMeshProUGUI buttonText = joinButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "JOIN";
            }
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Successfully joined Photon lobby");
        ShowFeedback("Connected to server");
        RefreshRoomList();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created, waiting to load GameScene...");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create room failed: {message}");

        if (returnCode == 32766)
            ShowFeedback("Room name already exists. Please choose another.");
        else
            ShowFeedback($"Failed to create room: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");

        if (returnCode == 32765)
            ShowFeedback("Room is full. Try another room.");
        else if (returnCode == 32758)
            ShowFeedback("Room no longer exists. Refreshing list...");
        else
            ShowFeedback($"Failed to join room: {message}");

        RefreshRoomList();
    }

    private void OnLogoutClick()
    {
        Debug.Log("Logging out...");
        NetworkManager.Instance.ClearPlayerData();
        NetworkManager.Instance.DisconnectFromPhoton();
        SceneManager.LoadScene("LoginScene");
    }

    private void ShowFeedback(string message)
    {
        Debug.Log($"Lobby Feedback: {message}");
        if (feedbackText != null)
        {
            feedbackText.text = message;

            // Clear feedback after 3 seconds for non-error messages
            if (!message.Contains("Failed") && !message.Contains("Error") &&
                !message.Contains("No rooms") && !message.Contains("full"))
            {
                CancelInvoke(nameof(ClearFeedback));
                Invoke(nameof(ClearFeedback), 3f);
            }
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
        {
            if (cachedRoomList.Count == 0)
                feedbackText.text = "No rooms available. Create one as a host!";
            else
                feedbackText.text = "";
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}