using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    private string roomName;
    private LobbyManager lobbyManager;

    public void Setup(string name, string playerInfo, LobbyManager manager)
    {
        roomName = name;
        lobbyManager = manager;

        if (roomNameText != null)
            roomNameText.text = name;

        if (playerCountText != null)
            playerCountText.text = playerInfo;
    }

    private void Start()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClick);
    }

    private void OnJoinClick()
    {
        if (lobbyManager != null && !string.IsNullOrEmpty(roomName))
            lobbyManager.JoinRoom(roomName);
    }

    private void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinClick);
    }
}