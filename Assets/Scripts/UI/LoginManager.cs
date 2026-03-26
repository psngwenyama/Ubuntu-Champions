using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("Login UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Settings")]
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string registerSceneName = "RegisterScene";

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClick);
        registerButton.onClick.AddListener(OnRegisterClick);

        // Check if NetworkManager exists
        if (NetworkManager.Instance == null)
            Debug.LogError("NetworkManager not found in scene!");
    }

    private void OnLoginClick()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            ShowFeedback("Please fill in all fields");
            return;
        }

        StartCoroutine(AttemptLogin());
    }

    private IEnumerator AttemptLogin()
    {
        loginButton.interactable = false;
        ShowFeedback("Logging in...");

        yield return PHPConnector.Instance.Login(
            usernameInput.text,
            passwordInput.text,
            (success, message, userData) =>
            {
                if (success && userData != null)
                {
                    ShowFeedback("Login successful!");

                    NetworkManager.Instance.PlayerName = userData["username"].ToString();
                    NetworkManager.Instance.PlayerRole = userData["role"].ToString();
                    NetworkManager.Instance.PlayerId = int.Parse(userData["id"].ToString());

                    NetworkManager.Instance.ConnectToPhoton();

                    SceneManager.LoadScene(lobbySceneName);
                }
                else
                {
                    ShowFeedback(message);
                    loginButton.interactable = true;
                }
            }
        );
    }

    private void OnRegisterClick()
    {
        SceneManager.LoadScene(registerSceneName);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
        Debug.Log(message);
    }
}