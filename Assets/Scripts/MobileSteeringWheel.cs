using UnityEngine;
using UnityEngine.EventSystems;

public class MobileSteeringWheel : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Araba Bağlantıları")]
    [Tooltip("Eğer burayı boş bırakırsan, kod sahnedeki aktif arabayı otomatik bulur.")]
    public CarController activeCar; 

    [Tooltip("Oyundaki tüm arabaları buraya sürükleyip liste halinde tutabilirsin (Opsiyonel)")]
    public CarController[] allCars;

    public RectTransform wheelImage; 
    
    [Header("Direksiyon Ayarları")]
    public float maxSteerAngle = 200f; 
    public float releaseSpeed = 400f;  

    private float wheelAngle = 0f;
    private float lastWheelAngle = 0f;
    private bool isDragging = false;
    private Vector2 centerPoint;

    void Start()
    {
        if (wheelImage == null)
            wheelImage = GetComponent<RectTransform>();

        // Eğer Inspector'da aktif bir araba atanmadıysa, sahnedeki arabayı otomatik bulalım
        if (activeCar == null)
        {
            FindActiveCarInScene();
        }
    }

    void Update()
    {
        // Eğer bir şekilde araba bağlantısı koparsa veya yeni araba gelirse otomatik bulmaya çalış
        if (activeCar == null || !activeCar.gameObject.activeInHierarchy)
        {
            FindActiveCarInScene();
        }

        if (!isDragging && wheelAngle != 0f)
        {
            wheelAngle = Mathf.MoveTowards(wheelAngle, 0f, releaseSpeed * Time.deltaTime);
            ApplyRotation();
        }
    }

    /// <summary>
    /// Sahne üzerinde o anda aktif (görünür/çalışan) olan CarController'ı bulur.
    /// </summary>
    public void FindActiveCarInScene()
    {
        // Önce allCars listesinden aktif olanı arayalım
        if (allCars != null && allCars.Length > 0)
        {
            foreach (var car in allCars)
            {
                if (car != null && car.gameObject.activeInHierarchy)
                {
                    activeCar = car;
                    return;
                }
            }
        }

        // Eğer listede yoksa veya liste boşsa, sahnedeki tüm aktif objeleri tarayalım
        CarController foundCar = GameObject.FindAnyObjectByType<CarController>();
        if (foundCar != null)
        {
            activeCar = foundCar;
        }
    }

    /// <summary>
    /// Listeden veya butonla belirli bir arabayı aktif etmek istediğinde çağırabileceğin fonksiyon.
    /// </summary>
    public void SetActiveCar(int carIndex)
    {
        if (allCars != null && carIndex >= 0 && carIndex < allCars.Length)
        {
            // Tüm arabaları kapat
            foreach (var car in allCars)
            {
                if (car != null) car.gameObject.SetActive(false);
            }

            // Sadece seçilen arabayı aç
            if (allCars[carIndex] != null)
            {
                allCars[carIndex].gameObject.SetActive(true);
                activeCar = allCars[carIndex];
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        centerPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, wheelImage.position);
        lastWheelAngle = Vector2.SignedAngle(Vector2.up, eventData.position - centerPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPoint = eventData.position;
        float currentAngle = Vector2.SignedAngle(Vector2.up, currentPoint - centerPoint);
        
        float angleDifference = Mathf.DeltaAngle(lastWheelAngle, currentAngle);
        
        wheelAngle -= angleDifference; 
        
        wheelAngle = Mathf.Clamp(wheelAngle, -maxSteerAngle, maxSteerAngle);
        lastWheelAngle = currentAngle;

        ApplyRotation();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    void ApplyRotation()
    {
        if (wheelImage != null)
        {
            wheelImage.localRotation = Quaternion.Euler(0, 0, -wheelAngle);
        }

        if (activeCar != null)
        {
            float steerInput = wheelAngle / maxSteerAngle; 
            activeCar.SetSteerInput(steerInput);
        }
    }
}