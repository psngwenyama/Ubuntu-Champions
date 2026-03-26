using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;  // Add this for TextMeshPro

public class MainMenu : MonoBehaviour
{
    public GameObject ButtonsPanel;
    public GameObject ConfirmationDialog;

    public void Start()
    {
        ButtonsPanel.SetActive(true);
        ConfirmationDialog.SetActive(false);
    }
    public void PlayGame()
    {
        SceneManager.LoadScene("Level 1");
    }

    public void instructionsMenu()
    {
        SceneManager.LoadScene("InstructionsMenu"); 
    }


    public void Quit()
    {
        ButtonsPanel.SetActive(false);
        ConfirmationDialog.SetActive(true);
    }

    public void ConfirmQuit()
    {
        Application.Quit();
    }

    public void CancelQuit()
    {
        ButtonsPanel.SetActive(true);
        ConfirmationDialog.SetActive(false);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}