using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Sahneler arası geçiş için şart

public class SplashToMainMenu : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Kaç saniye sonra ana menüye geçilsin?")]
    public float beklemeSuresi = 10f;

    [Tooltip("Geçiş yapılacak ana menü sahnesinin tam adı")]
    public string anaMenuSahneAdi = "MainMenu";

    void Start()
    {
        // Oyuna girildiği an geri sayım sürecini başlatıyoruz
        StartCoroutine(GecisSayaci());
    }

    IEnumerator GecisSayaci()
    {
        // Belirttiğimiz süre kadar (10 saniye) bekler
        yield return new WaitForSeconds(beklemeSuresi);

        // Süre dolunca ana menü sahnesini yükler
        SceneManager.LoadScene(anaMenuSahneAdi);
    }
}