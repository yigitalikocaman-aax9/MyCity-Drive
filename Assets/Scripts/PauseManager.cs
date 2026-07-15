using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Panelleri")]
    [Tooltip("Oyun durduğunda açılacak olan Pause Panelini buraya sürükleyin.")]
    public GameObject pausePanel;

    [Tooltip("Ekrandaki duraklatma butonunu (PauseButton) buraya sürükleyin (Pause menüsü açılınca gizlemek için).")]
    public GameObject pauseButton;

    private bool isPaused = false;

    void Start()
    {
        // Oyun başlarken panelin kapalı, butonun açık olduğundan emin olalım
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
        
        // Zaman akışının normal olduğundan emin olalım
        Time.timeScale = 1f; 
    }

    // Duraklatma butonuna basınca çalışacak fonksiyon
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Oyundaki tüm fizik ve zamanı durdurur

        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseButton != null) pauseButton.SetActive(false); // Arkadaki butonu gizle
    }

    // Resume (Devam Et) butonuna basınca çalışacak fonksiyon
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Zamanı normale döndürür

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
    }

    // Main Menu butonuna basınca çalışacak fonksiyon
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Sahne değişmeden önce zamanı mutlaka sıfırla, yoksa ana menü de donuk kalır!
        SceneManager.LoadScene("MainMenu"); // Ana menü sahnesinin adını buraya yazın
    }
}