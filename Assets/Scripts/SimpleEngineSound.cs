using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleEngineSound : MonoBehaviour
{
    private AudioSource engineAudioSource;

    [Header("Fizik Bileşeni")]
    [Tooltip("Arabanın üzerinde Rigidbody olan ana objesini buraya sürükleyin.")]
    public Rigidbody carRigidbody;

    [Header("Motor Ses Ayarları")]
    public float minPitch = 0.6f;     // Her vitesin başlangıcındaki en kalın devir sesi
    public float maxPitch = 1.8f;     // Her vitesin sonundaki en tiz devir sesi
    public float minVolume = 0.5f;    // Rölanti ses seviyesi
    public float maxVolume = 1.0f;    // Hızlandıkça çıkacağı maksimum ses seviyesi

    [Header("Vites Sistemi Ayarları")]
    [Tooltip("Vites geçiş sesini (Audio Clip) buraya sürükleyin.")]
    public AudioClip gearShiftClip; 
    [Range(0f, 1f)]
    public float gearShiftVolume = 0.8f; // Vites geçiş sesinin şiddeti

    [Tooltip("Arabanın km/h cinsinden yapabileceği maksimum hız.")]
    public float maxSpeed = 160f;
    
    // Toplam vites sayısı (5 Vites)
    private int totalGears = 5;
    private int currentGear = 1;

    void Start()
    {
        engineAudioSource = GetComponent<AudioSource>();
        
        if (carRigidbody == null)
        {
            carRigidbody = GetComponentInParent<Rigidbody>();
        }

        engineAudioSource.loop = true;
        engineAudioSource.spatialBlend = 0f; // Sesi net duymak için 2D
        if (!engineAudioSource.isPlaying)
        {
            engineAudioSource.Play();
        }
    }

    void Update()
    {
        if (carRigidbody == null) return;

        // Arabanın anlık hızını km/h olarak hesapla
        float currentSpeed = carRigidbody.linearVelocity.magnitude * 3.6f;
        
        // Hız oranını çıkar (0.0 - 1.0 arası)
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

        // --- SANAL VİTES SİSTEMİ ---
        // Toplam hızı vites sayısına göre bölüyoruz
        float gearRange = 1f / totalGears; 
        
        // Şu anki hıza göre hangi viteste olmamız gerektiğini hesaplıyoruz
        int targetGear = Mathf.FloorToInt(speedRatio / gearRange) + 1;
        targetGear = Mathf.Clamp(targetGear, 1, totalGears);

        // Eğer vites değiştiyse (Vites yükseldiyse veya düştüyse)
        if (targetGear != currentGear)
        {
            // Sadece vites yükselirken vites geçiş sesi çalalım
            if (targetGear > currentGear && gearShiftClip != null)
            {
                // PlayOneShot ana motor sesini kesmeden üzerine tek seferlik ses çalar
                engineAudioSource.PlayOneShot(gearShiftClip, gearShiftVolume);
            }
            currentGear = targetGear;
        }

        // Her vitesin kendi içindeki devir oranını (0.0 - 1.0) hesaplıyoruz
        float currentGearMinSpeed = (currentGear - 1) * gearRange;
        float currentGearMaxSpeed = currentGear * gearRange;
        float gearSpeedRatio = (speedRatio - currentGearMinSpeed) / (currentGearMaxSpeed - currentGearMinSpeed);
        gearSpeedRatio = Mathf.Clamp01(gearSpeedRatio);

        // Ses Tonu (Pitch) vites değiştikçe düşecek ve o vites içinde tekrar yükselecek!
        engineAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, gearSpeedRatio);
        
        // Genel ses seviyesi ise arabanın toplam hızına göre artsın
        engineAudioSource.volume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
    }
}