using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; 

public class RegisterManager : MonoBehaviour
{
    [Header("Register UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_Dropdown roleDropdown; 
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backToLoginButton;
    [SerializeField] private TextMeshProUGUI feedbackText; 

    [Header("Settings")]
    [SerializeField] private string loginSceneName = "LoginScene";

    private void Start()
    {
        // Add null checks to prevent errors
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterClick);
        
        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(OnBackToLoginClick);
        
        // Setup role dropdown
        if (roleDropdown != null)
        {
            roleDropdown.ClearOptions();
            roleDropdown.AddOptions(new System.Collections.Generic.List<string> { "Player", "Host", "Audience" });
        }
    }

    private void OnRegisterClick()
    {
        // Validate inputs with null checks
        if (usernameInput == null || emailInput == null || passwordInput == null || confirmPasswordInput == null)
        {
            ShowFeedback("UI elements not properly configured");
            return;
        }

        if (string.IsNullOrEmpty(usernameInput.text) ||
            string.IsNullOrEmpty(emailInput.text) ||
            string.IsNullOrEmpty(passwordInput.text))
        {
            ShowFeedback("Please fill in all fields");
            return;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            ShowFeedback("Passwords do not match");
            return;
        }

        if (passwordInput.text.Length < 6)
        {
            ShowFeedback("Password must be at least 6 characters");
            return;
        }

        if (roleDropdown == null)
        {
            ShowFeedback("Role dropdown not configured");
            return;
        }

        string selectedRole = roleDropdown.options[roleDropdown.value].text.ToLower();

        StartCoroutine(AttemptRegistration(selectedRole));
    }

    private IEnumerator AttemptRegistration(string role)
    {
        if (registerButton != null)
            registerButton.interactable = false;
        
        ShowFeedback("Registering...");

        yield return PHPConnector.Instance.Register(
            usernameInput.text,
            emailInput.text,
            passwordInput.text,
            role,
            (success, message) =>
            {
                if (success)
                {
                    ShowFeedback("Registration successful! Redirecting to login...");
                    StartCoroutine(RedirectToLogin());
                }
                else
                {
                    ShowFeedback(message);
                    if (registerButton != null)
                        registerButton.interactable = true;
                }
            }
        );
    }

    private IEnumerator RedirectToLogin()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(loginSceneName);
    }

    private void OnBackToLoginClick()
    {
        SceneManager.LoadScene(loginSceneName);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        Debug.Log(message);
    }

    // Optional: Add this method to validate setup in editor
    private void OnValidate()
    {
        if (usernameInput == null)
            Debug.LogWarning("Username Input is not assigned in RegisterManager", this);
        if (emailInput == null)
            Debug.LogWarning("Email Input is not assigned in RegisterManager", this);
        if (passwordInput == null)
            Debug.LogWarning("Password Input is not assigned in RegisterManager", this);
        if (confirmPasswordInput == null)
            Debug.LogWarning("Confirm Password Input is not assigned in RegisterManager", this);
        if (roleDropdown == null)
            Debug.LogWarning("Role Dropdown is not assigned in RegisterManager", this);
        if (registerButton == null)
            Debug.LogWarning("Register Button is not assigned in RegisterManager", this);
        if (backToLoginButton == null)
            Debug.LogWarning("Back To Login Button is not assigned in RegisterManager", this);
        if (feedbackText == null)
            Debug.LogWarning("Feedback Text is not assigned in RegisterManager", this);
    }
}