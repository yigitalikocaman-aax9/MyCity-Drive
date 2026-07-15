using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Slider kullanmak için ekledik
using TMPro; // TextMeshPro kullanmak için ekledik
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneller")]
    [Tooltip("Girişteki PLAY, CARS, QUIT butonlarının olduğu ana panel")]
    public GameObject mainPanel; 
    
    [Tooltip("Geri butonunun ve araba seçme butonlarının olduğu panel")]
    public GameObject carsPanel;

    [Header("Yükleniyor Ekranı")]
    public GameObject loadingPanel; 
    
    [Header("Yüklenme Göstergeleri")]
    [Tooltip("Yüklenme yüzdesini yazacak TextMeshPro objesi")]
    public TextMeshProUGUI progressText;

    [Tooltip("Yüklenme durumunu gösterecek UI Slider (Bar)")]
    public Slider progressBar;

    /// <summary>
    /// Direkt PLAY butonuna basılırsa, en son seçilen (veya varsayılan 0) arabayla oyunu başlatır.
    /// </summary>
    public void StartGame()
    {
        StartCoroutine(LoadSceneAsyncCoroutine("MyCity"));
    }

    /// <summary>
    /// CARS butonuna basıldığında araba seçim panelini açar, ana paneli kapatır.
    /// </summary>
    public void OpenCarsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (carsPanel != null) carsPanel.SetActive(true);
    }

    /// <summary>
    /// Araba panelindeki "Geri (Back)" butonuna basıldığında ana menüye döner.
    /// </summary>
    public void BackToMainMenu()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (carsPanel != null) carsPanel.SetActive(false);
    }

    /// <summary>
    /// Seçim panelindeki herhangi bir araba butonuna tıklandığında çalışır.
    /// </summary>
    /// <param name="carIndex">0: Golf, 1: Passat, 2: İlerideki 3. Araba, 3: 4. Araba...</param>
    public void SelectCarAndPlay(int carIndex)
    {
        // Hangi arabayı seçtiğimizi hafızaya kaydediyoruz (İleride kaç araba eklersen ekle, burası değişmez!)
        PlayerPrefs.SetInt("SelectedCarIndex", carIndex);
        PlayerPrefs.Save();

        Debug.Log("Araba seçildi ve hafızaya kaydedildi. İndeks: " + carIndex);

        // Kaydettikten hemen sonra senin yazdığın asenkron yükleme coroutine'ini başlatıyoruz
        StartCoroutine(LoadSceneAsyncCoroutine("MyCity"));
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        // Eğer araba paneli açıksa yükleme ekranı gelmeden önce onu kapatıyoruz
        if (carsPanel != null) carsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            // Bu yüzden progress değerini 0.9f'e bölerek 0 ile 1 arasına eşitliyoruz.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Eğer ekranda bir Slider (Bar) varsa onun doluluk oranını ayarla
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Eğer ekranda yüzde yazısı varsa güncelle (Örn: %45)
            if (progressText != null)
            {
                progressText.text = "%" + Mathf.RoundToInt(progress * 100).ToString();
            }
            
            yield return null; 
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Oyundan çıkıldı!");
    }
}