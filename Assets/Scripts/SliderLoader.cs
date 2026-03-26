using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // Important: Add TextMeshPro namespace
using UnityEngine.SceneManagement;

public class SliderLoaderTMP : MonoBehaviour
{
    [Header("UI References")]
    public Slider loadingSlider;           // The slider component
    public TextMeshProUGUI percentageText; // TMP text for percentage
    public TextMeshProUGUI statusText;     // TMP text for status messages

    [Header("Loading Settings")]
    [Range(0.1f, 5f)]
    public float fillSpeed = 2f;           // Speed of slider fill animation
    public float minLoadTime = 2f;         // Minimum time to show loading screen
    public string menuSceneName = "Main Menu";

    [Header("Text Formatting")]
    public string percentageFormat = "{0}%";  // Format: "{0}%" or "Loading: {0}%"
    public bool showDecimal = false;          // Show decimal places?
    public int decimalPlaces = 0;             // Number of decimal places if showDecimal is true

    [Header("Status Messages")]
    public string[] loadingMessages = new string[]
    {
        "Starting game...",
        "Almost ready..."
    };

    [Header("Colors")]
    public Color loadingColor = Color.blue;
    public Color completeColor = Color.green;
    public Color textColor = Color.white;

    [Header("Animation")]
    public bool animateText = true;
    public float textPulseSpeed = 1f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private AsyncOperation asyncOperation;
    private Image fillImage;

    void Start()
    {
        // Get fill image reference
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
            fillImage = loadingSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = loadingColor;
            }
        }

        // Initialize TextMeshPro texts
        if (percentageText != null)
        {
            percentageText.color = textColor;
            UpdatePercentageText(0f);
        }

        if (statusText != null)
        {
            statusText.color = textColor;
            statusText.text = loadingMessages[0];
        }

        // Start loading process
        StartCoroutine(LoadMenuScene());
    }

    void Update()
    {
        // Smoothly fill the slider
        if (currentProgress < targetProgress)
        {
            currentProgress += Time.deltaTime * fillSpeed;
            currentProgress = Mathf.Min(currentProgress, targetProgress);

            if (loadingSlider != null)
            {
                loadingSlider.value = currentProgress;
            }

            // Update percentage text
            UpdatePercentageText(currentProgress);

            // Animate text if enabled
            if (animateText && percentageText != null)
            {
                AnimateText();
            }
        }

        // Check if loading is complete and slider is full
        if (currentProgress >= 0.99f && asyncOperation != null && asyncOperation.progress >= 0.9f)
        {
            if (fillImage != null)
            {
                fillImage.color = completeColor;
            }

            if (statusText != null)
            {
                statusText.text = "Complete!";
                statusText.color = completeColor;
            }
        }
    }

    IEnumerator LoadMenuScene()
    {
        // Start loading the scene asynchronously
        asyncOperation = SceneManager.LoadSceneAsync(menuSceneName);
        asyncOperation.allowSceneActivation = false;

        float startTime = Time.time;
        int lastMessageIndex = -1;

        // While the scene is loading (0 to 0.9 progress)
        while (asyncOperation.progress < 0.9f)
        {
            // Calculate target progress (0 to 0.9 maps to 0 to 0.9)
            targetProgress = asyncOperation.progress / 0.9f;

            // Update status message based on progress
            int messageIndex = Mathf.FloorToInt(targetProgress * loadingMessages.Length);
            messageIndex = Mathf.Clamp(messageIndex, 0, loadingMessages.Length - 1);

            if (messageIndex != lastMessageIndex && statusText != null)
            {
                statusText.text = loadingMessages[messageIndex];
                lastMessageIndex = messageIndex;

                // Optional: Play a sound or trigger animation when message changes
                StartCoroutine(StatusTextAnimation());
            }

            yield return null;
        }

        // Scene is 90% loaded, now fill the remaining 10%
        targetProgress = 1f;

        // Wait for minimum time if needed
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minLoadTime)
        {
            yield return new WaitForSeconds(minLoadTime - elapsedTime);
        }

        // Wait for slider to reach target
        while (currentProgress < targetProgress - 0.01f)
        {
            yield return null;
        }

        // Final status message
        if (statusText != null)
        {
            statusText.text = "Welcome!";
        }

        // Small delay for visual satisfaction
        yield return new WaitForSeconds(0.2f);

        // Activate the menu scene
        asyncOperation.allowSceneActivation = true;
    }

    void UpdatePercentageText(float progress)
    {
        if (percentageText == null) return;

        float percentage = progress * 100f;
        string percentageString;

        if (showDecimal)
        {
            string format = "{0:F" + decimalPlaces + "}";
            percentageString = string.Format(format, percentage);
        }
        else
        {
            percentageString = Mathf.RoundToInt(percentage).ToString();
        }

        percentageText.text = string.Format(percentageFormat, percentageString);
    }

    void AnimateText()
    {
        // Simple pulse animation for the percentage text
        float scale = 1f + Mathf.Sin(Time.time * textPulseSpeed * 10f) * 0.05f;
        percentageText.transform.localScale = new Vector3(scale, scale, 1f);
    }

    IEnumerator StatusTextAnimation()
    {
        if (statusText == null) yield break;

        // Simple fade in/out animation when status changes
        Color originalColor = statusText.color;
        statusText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
        yield return new WaitForSeconds(0.1f);

        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.5f, 1f, elapsed / 0.2f);
            statusText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        statusText.color = originalColor;
    }

    // Optional: Method to simulate loading for testing
    [ContextMenu("Simulate Loading")]
    public void SimulateLoading()
    {
        StartCoroutine(SimulateLoad());
    }

    IEnumerator SimulateLoad()
    {
        float simulatedProgress = 0f;
        while (simulatedProgress < 1f)
        {
            simulatedProgress += Time.deltaTime * 0.5f;
            targetProgress = simulatedProgress;
            yield return null;
        }
    }
}