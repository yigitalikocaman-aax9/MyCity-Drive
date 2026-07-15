using UnityEngine;
using TMPro;

public class Speedometer : MonoBehaviour
{
    [Header("Bağlantılar")]
    [Tooltip("Boş bırakabilirsiniz; kod sahnede aktif olan arabayı otomatik bulacaktır.")]
    public Rigidbody carRigidbody; // Arabanın Rigidbody bileşeni
    public TMP_Text speedText;     // UI_SpeedometerText objemiz
    public AudioSource engineAudio; // Arabaya eklediğimiz Audio Source

    [Header("Hız Ayarları")]
    public float speedMultiplier = 3.6f; // m/s -> KM/H çevirici
    public float speedLimit = 100f;      // Bu hızı geçince yazı kırmızı olacak

    [Header("Renk Ayarları")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.8f); 
    public Color warningColor = new Color(1f, 0.1f, 0.1f, 1f); 

    [Header("Ses ve Devir Ayarları")]
    public float minPitch = 0.8f;   // Vitesin en başındaki en düşük ses tonu
    public float maxPitch = 1.8f;   // Vitesin sonundaki en yüksek ses tonu

    // Senin belirttiğin vites geçiş hız sınırları
    private readonly float[] gearLimits = { 20f, 50f, 80f, 100f, 120f, 130f };

    private float nextSearchTime = 0f; // Performans dostu arama için zamanlayıcı

    void Start()
    {
        if (speedText != null)
        {
            speedText.color = normalColor;
        }

        FindActiveCar();
    }

    void Update()
    {
        // Eğer sahnede araba yoksa veya değiştiyse, performans kaybı yaratmadan her 1 saniyede bir yeni arabayı tara
        if (carRigidbody == null || !carRigidbody.gameObject.activeInHierarchy)
        {
            if (Time.time > nextSearchTime)
            {
                FindActiveCar();
                nextSearchTime = Time.time + 1f; // Saniyede bir kez çalışır
            }
        }

        if (carRigidbody != null)
        {
            // Arabanın anlık hızını KM/H olarak hesapla
            float currentSpeed = carRigidbody.linearVelocity.magnitude * speedMultiplier;
            int displaySpeed = Mathf.RoundToInt(currentSpeed);

            // 1. Hız Göstergesi Yazısı ve Renk Kontrolü
            if (speedText != null)
            {
                speedText.text = displaySpeed.ToString("000") + " KM/H"; // Formatı "000" yaparak senin görseldeki gibi 000 KM/H duruşunu koruduk
                speedText.color = (currentSpeed >= speedLimit) ? warningColor : normalColor;
            }

            // 2. Vites ve Dinamik Motor Sesi Kontrolü
            if (engineAudio != null)
            {
                int currentGear = CalculateGear(currentSpeed);
                float pitchValue = CalculatePitchForGear(currentSpeed, currentGear);
                
                // Hesaplanan ses perdesini motora aktar
                engineAudio.pitch = pitchValue;
            }
        }
        else
        {
            // Sahnede aktif araba yoksa göstergeyi sıfırla
            if (speedText != null)
            {
                speedText.text = "000 KM/H";
                speedText.color = normalColor;
            }
        }
    }

    // Sahnede o an aktif (görünür/kullanılan) olan arabayı otomatik bulur
    void FindActiveCar()
    {
        CarController[] cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        
        foreach (CarController car in cars)
        {
            // Sadece hiyerarşide aktif/açık olan arabayı seç
            if (car.gameObject.activeInHierarchy)
            {
                carRigidbody = car.GetComponent<Rigidbody>();
                engineAudio = car.GetComponent<AudioSource>();
                break; 
            }
        }
    }

    // Arabanın hızına göre şu an hangi viteste olduğunu bulur
    int CalculateGear(float speed)
    {
        for (int i = 0; i < gearLimits.Length; i++)
        {
            if (speed < gearLimits[i])
            {
                return i;
            }
        }
        return gearLimits.Length;
    }

    // Bulunan vitesin içindeki hıza göre motor devrini (pitch) hesaplar
    float CalculatePitchForGear(float speed, int gear)
    {
        float minSpeedForThisGear = 0f;
        float maxSpeedForThisGear = gearLimits[0];

        if (gear > 0)
        {
            minSpeedForThisGear = gearLimits[gear - 1];
            if (gear < gearLimits.Length)
            {
                maxSpeedForThisGear = gearLimits[gear];
            }
            else
            {
                maxSpeedForThisGear = 200f;
            }
        }

        float gearSpeedRange = maxSpeedForThisGear - minSpeedForThisGear;
        float currentSpeedInGear = speed - minSpeedForThisGear;
        float gearProgress = Mathf.Clamp01(currentSpeedInGear / gearSpeedRange);

        return Mathf.Lerp(minPitch, maxPitch, gearProgress);
    }
}