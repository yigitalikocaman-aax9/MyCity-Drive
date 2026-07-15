using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileGearShifter : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Araba Bağlantıları")]
    [Tooltip("Eğer burayı boş bırakırsan, kod sahnedeki aktif arabayı otomatik bulur.")]
    public CarController activeCar; // Şu an kontrol edilen aktif araba

    [Tooltip("Oyundaki tüm arabaları buraya sürükleyip liste halinde tutabilirsin (Opsiyonel)")]
    public CarController[] allCars; // Diğer arabalar için kutucuklar

    public RectTransform handle; // Hareket edecek olan vites kolu görseli (UI_TransmissionHandle)
    
    [Header("Yazı Göstergeleri")]
    public TMP_Text dText;
    public TMP_Text rText;

    [Header("Tasarım Ayarları")]
    public float movementRange = 40f; // Vites kolunun yukarı/aşağı maksimum kayma mesafesi
    public float snapSpeed = 10f; // Vitesin yerine oturma (animasyon) hızı

    private float targetY = 0f;
    private bool isDragging = false;
    private Vector2 startPointerPos;
    private float startHandleY;

    public enum Gear { Drive, Reverse }
    private Gear currentGear = Gear.Drive;

    void Start()
    {
        // Başlangıçta vites D (Drive) konumunda başlasın (Yukarıda)
        targetY = movementRange;
        handle.anchoredPosition = new Vector2(0, targetY);
        UpdateVisuals();

        // Eğer Inspector'da aktif bir araba atanmadıysa, sahnedeki aktif olanı otomatik bul
        if (activeCar == null)
        {
            FindActiveCarInScene();
        }
    }

    void Update()
    {
        // Eğer araba hiyerarşide kapandıysa veya yoksa yenisini otomatik bulmaya çalış
        if (activeCar == null || !activeCar.gameObject.activeInHierarchy)
        {
            FindActiveCarInScene();
        }

        // Sürüklenmiyorken vitesi yerine yumuşakça kaydır
        if (!isDragging)
        {
            float currentY = Mathf.Lerp(handle.anchoredPosition.y, targetY, Time.deltaTime * snapSpeed);
            handle.anchoredPosition = new Vector2(0, currentY);
        }
    }

    /// <summary>
    /// Sahne üzerinde o anda aktif olan CarController'ı otomatik bulur.
    /// </summary>
    public void FindActiveCarInScene()
    {
        // Önce allCars listesinden hiyerarşide aktif olan bir araba var mı ona bakalım
        if (allCars != null && allCars.Length > 0)
        {
            foreach (var car in allCars)
            {
                if (car != null && car.gameObject.activeInHierarchy)
                {
                    activeCar = car;
                    SyncGearToActiveCar();
                    return;
                }
            }
        }

        // Eğer listede yoksa sahnedeki herhangi bir aktif CarController'ı bulalım
        CarController foundCar = GameObject.FindAnyObjectByType<CarController>();
        if (foundCar != null)
        {
            activeCar = foundCar;
            SyncGearToActiveCar();
        }
    }

    /// <summary>
    /// Yeni arabaya geçildiğinde arayüzdeki vites durumunu arabanın fiziksel yönüne eşitler.
    /// </summary>
    private void SyncGearToActiveCar()
    {
        if (activeCar != null)
        {
            // Araba Drive'da ise kolu yukarı (Drive konumuna), Reverse'te ise aşağıya (Reverse konumuna) çekiyoruz.
            if (activeCar.currentGear == CarController.Gear.Drive)
            {
                currentGear = Gear.Drive;
                targetY = movementRange;
            }
            else
            {
                currentGear = Gear.Reverse;
                targetY = -movementRange;
            }
            UpdateVisuals();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        startPointerPos = eventData.position;
        startHandleY = handle.anchoredPosition.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float diffY = eventData.position.y - startPointerPos.y;
        float newY = startHandleY + diffY;

        newY = Mathf.Clamp(newY, -movementRange, movementRange);
        handle.anchoredPosition = new Vector2(0, newY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // Kolun durduğu yere göre vitesi seç
        if (handle.anchoredPosition.y > 0)
        {
            targetY = movementRange;
            SetGear(Gear.Drive);
        }
        else
        {
            targetY = -movementRange;
            SetGear(Gear.Reverse);
        }
    }

    void SetGear(Gear newGear)
    {
        currentGear = newGear;
        UpdateVisuals();

        if (activeCar != null)
        {
            // Arabanın hızını kontrol ederek güvenli vites geçişi yapıyoruz
            float speed = activeCar.GetComponent<Rigidbody>().linearVelocity.magnitude * 3.6f;
            if (speed < 5f) 
            {
                // Kol D'ye çekildiyse arabaya CarController.Gear.Drive, R'ye çekildiyse CarController.Gear.Reverse gönderiyoruz.
                activeCar.currentGear = (newGear == Gear.Drive) ? CarController.Gear.Drive : CarController.Gear.Reverse;
                Debug.Log("Aktif Arabanın Vitesi Eşitlendi: " + activeCar.currentGear);
            }
            else
            {
                // Eğer araba hızlı gidiyorsa vitesi değiştirme, eski konumuna geri at
                Debug.LogWarning("Araba durmadan vites değiştiremezsin!");
                SyncGearToActiveCar();
            }
        }
    }

    void UpdateVisuals()
    {
        if (dText != null && rText != null)
        {
            // D seçiliyken D yeşil, R gri; R seçiliyken R kırmızı, D gri olsun
            if (currentGear == Gear.Drive)
            {
                dText.color = new Color(0.1f, 1f, 0.1f, 1f); // Parlak Yeşil
                rText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Soluk Gri
            }
            else
            {
                dText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Soluk Gri
                rText.color = new Color(1f, 0.1f, 0.1f, 1f); // Parlak Kırmızı
            }
        }
    }
}