using UnityEngine;

public class GameCarSpawners : MonoBehaviour
{
    [Header("Araba Listesi (Menüdeki Sırayla Aynı Olmalı)")]
    public GameObject[] playableCars;

    [Header("Gerekli Diğer Bileşenler")]
    public MonoBehaviour cameraFollow; // Özel script yerine genel MonoBehaviour kullanıyoruz
    public MonoBehaviour gearShifter;  // Özel script yerine genel MonoBehaviour kullanıyoruz

    private void Awake()
    {
        // 1. Menüden seçilen arabanın numarasını alıyoruz.
        int selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);

        // 2. Tüm arabaları tek tek kontrol ediyoruz.
        for (int i = 0; i < playableCars.Length; i++)
        {
            if (playableCars[i] != null)
            {
                if (i == selectedCarIndex)
                {
                    // Seçilen arabayı aktif et
                    playableCars[i].SetActive(true);

                    // Kamerayı yeni arabaya bağlama (Hata vermemesi için dinamik yöntem kullanıyoruz)
                    if (cameraFollow != null)
                    {
                        // cameraFollow scripti içindeki 'target' değişkenini bulup arabanın transformunu atar
                        var targetField = cameraFollow.GetType().GetField("target");
                        if (targetField != null)
                        {
                            targetField.SetValue(cameraFollow, playableCars[i].transform);
                        }
                    }

                    // Vites sistemine yeni arabayı tanıtma (Dinamik yöntem)
                    if (gearShifter != null)
                    {
                        var carControllerInstance = playableCars[i].GetComponent<CarController>();
                        if (carControllerInstance != null)
                        {
                            // gearShifter scripti içindeki araba kontrolcü değişkenini bulmaya çalışır
                            var carControllerField = gearShifter.GetType().GetField("carController") 
                                                    ?? gearShifter.GetType().GetField("car")
                                                    ?? gearShifter.GetType().GetField("controller");

                            if (carControllerField != null)
                            {
                                carControllerField.SetValue(gearShifter, carControllerInstance);
                            }
                        }
                    }
                }
                else
                {
                    // Seçilmeyen diğer tüm arabaları kapat
                    playableCars[i].SetActive(false);
                }
            }
        }
    }
}