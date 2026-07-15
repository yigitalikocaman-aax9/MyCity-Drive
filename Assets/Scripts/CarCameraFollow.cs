using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Yeni Input System kütüphanesi aktifse kullanılıyor
#endif

public class CarCameraFollow : MonoBehaviour
{
    [Header("Araba Bağlantıları")]
    [Tooltip("Şu an takip edilen aktif araba. Boş bırakırsan kod sahnedekini otomatik bulur.")]
    public CarController activeCar;

    [Tooltip("Oyundaki tüm arabaları buraya sürükleyip liste halinde tutabilirsin (Opsiyonel)")]
    public CarController[] allCars;

    [Header("Otomatik Atanacak Noktalar (Dokunma)")]
    [Tooltip("Bu alanları boş bırakırsan, kod aktif arabanın altındaki hedefleri otomatik eşler.")]
    public Transform target;
    public Transform rearCameraPoint;
    public Transform frontCameraPoint;

    [Header("Yedek Konum Ayarları")]
    [Tooltip("Eğer arabanın özel kamera noktası (Rear/Front) yoksa kullanılacak yedek mesafe.")]
    public Vector3 offset = new Vector3(0f, 3.5f, -7f);
    public Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    [Header("Yön Ayarları")]
    public bool invertForward = false;

    [Header("Yumuşatma (Smooth) Ayarları")]
    public float positionSmoothTime = 0.15f;
    public float rotationSmoothSpeed = 6f;

    [Header("Serbest Bakış (Orbit) Ayarları")]
    [Tooltip("Yatayda dönme hızı.")]
    public float orbitXSpeed = 150f;
    [Tooltip("Dikeyde dönme hızı.")]
    public float orbitYSpeed = 120f;
    [Tooltip("Yere çok girmemesi için dikey alt limit.")]
    public float orbitYMinLimit = -5f;
    [Tooltip("Çok tepeden bakmaması için dikey üst limit.")]
    public float orbitYMaxLimit = 70f;
    [Tooltip("Arabadan ne kadar uzakta dönüleceği (Otomatik hesaplanır ama buradan sınırlandırabilirsin).")]
    public float orbitDistance = 6.5f;

    private Vector3 currentVelocity = Vector3.zero;

    // Orbit (Serbest Bakış) Takip Değişkenleri
    private bool isOrbiting = false;
    private float orbitX = 0.0f;
    private float orbitY = 0.0f;
    private int activeTouchId = -1;

    // Çift Tıklama Takip Değişkenleri
    private float lastTapTime = 0f;
    private const float doubleTapDelay = 0.3f; // Çift tıklama algılama süresi (Saniye)

    void Start()
    {
        // Başlangıçta aktif arabayı bul ve kamerayı ona bağla
        if (activeCar == null)
        {
            FindActiveCarInScene();
        }
        else
        {
            SetupCameraForCar(activeCar);
        }

        // Başlangıç dönüş değerlerini ata
        Vector3 angles = transform.eulerAngles;
        orbitX = angles.y;
        orbitY = angles.x;
    }

    void LateUpdate()
    {
        // Eğer araba hiyerarşide kapandıysa veya yoksa yenisini otomatik bulmaya çalış
        if (activeCar == null || !activeCar.gameObject.activeInHierarchy)
        {
            FindActiveCarInScene();
        }

        // Eğer hala takip edilecek bir hedef yoksa kamerayı çalıştırma
        if (target == null) return;

        // Yeni Input System ile dokunma ve çift tıklama kontrolleri
        HandleTouchInput();

        if (isOrbiting)
        {
            // --- SERBEST BAKIŞ (ORBIT) MODU ---
            Quaternion rotation = Quaternion.Euler(orbitY, orbitX, 0);
            
            Vector3 targetCenter = target.position + lookAtOffset;
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -orbitDistance);
            Vector3 desiredPosition = rotation * negDistance + targetCenter;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 10f);
        }
        else
        {
            // --- SİZİN ORİJİNAL TAKİP SİSTEMİNİZ ---
            Vector3 desiredPosition;

            if (activeCar != null && rearCameraPoint != null && frontCameraPoint != null)
            {
                if (activeCar.currentGear == CarController.Gear.Drive)
                {
                    desiredPosition = rearCameraPoint.position;
                }
                else
                {
                    desiredPosition = frontCameraPoint.position;
                }
            }
            else
            {
                Vector3 usedOffset = offset;
                if (invertForward)
                    usedOffset.z = -usedOffset.z;

                desiredPosition = target.position + target.TransformDirection(usedOffset);
            }

            // Yumuşak pozisyon takibi
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentVelocity,
                positionSmoothTime);

            // Yumuşak bakış açısı (Look At) takibi
            Vector3 lookTarget = target.position + lookAtOffset;
            if (lookTarget - transform.position != Vector3.zero)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationSmoothSpeed * Time.deltaTime);
            }

            orbitX = transform.eulerAngles.y;
            orbitY = transform.eulerAngles.x;
        }
    }

    private void HandleTouchInput()
    {
#if ENABLE_INPUT_SYSTEM
        // --- 1. YENİ INPUT SYSTEM: EDİTÖR / BİLGİSAYAR (MOUSE) KONTROLLERİ ---
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsPointerOverUIObjectNewSystem())
                {
                    float timeSinceLastClick = Time.time - lastTapTime;
                    if (timeSinceLastClick <= doubleTapDelay)
                    {
                        ResetCameraToNormal();
                        return;
                    }
                    lastTapTime = Time.time;
                    isOrbiting = true;
                }
            }

            if (mouse.leftButton.isPressed && isOrbiting)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                orbitX += mouseDelta.x * orbitXSpeed * 0.05f * Time.deltaTime;
                orbitY -= mouseDelta.y * orbitYSpeed * 0.05f * Time.deltaTime;
                orbitY = ClampAngle(orbitY, orbitYMinLimit, orbitYMaxLimit);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                isOrbiting = false;
            }
        }

        // --- 2. YENİ INPUT SYSTEM: MOBİL (DOKUNMATİK) KONTROLLER ---
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.touches.Count > 0)
        {
            foreach (var touch in touchscreen.touches)
            {
                int touchId = touch.touchId.ReadValue();

                if (touch.press.wasPressedThisFrame)
                {
                    if (!IsPointerOverUIObjectNewSystem(touchId))
                    {
                        // Çift dokunma algılama (Yeni sistem tapCount takibi)
                        if (touch.tapCount.ReadValue() == 2)
                        {
                            ResetCameraToNormal();
                            return;
                        }

                        activeTouchId = touchId;
                        isOrbiting = true;
                    }
                }

                if (touchId == activeTouchId)
                {
                    if (touch.isInProgress && isOrbiting)
                    {
                        Vector2 touchDelta = touch.delta.ReadValue();
                        orbitX += touchDelta.x * orbitXSpeed * 0.05f * Time.deltaTime;
                        orbitY -= touchDelta.y * orbitYSpeed * 0.05f * Time.deltaTime;
                        orbitY = ClampAngle(orbitY, orbitYMinLimit, orbitYMaxLimit);
                    }

                    if (touch.press.wasReleasedThisFrame)
                    {
                        isOrbiting = false;
                        activeTouchId = -1;
                    }
                }
            }
        }
#else
        // ESKİ INPUT SİSTEMİ (Yedek olarak kodda kalması için, hata vermez)
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUIObject())
            {
                float timeSinceLastClick = Time.time - lastTapTime;
                if (timeSinceLastClick <= doubleTapDelay)
                {
                    ResetCameraToNormal();
                    return;
                }
                lastTapTime = Time.time;
                isOrbiting = true;
            }
        }

        if (Input.GetMouseButton(0) && isOrbiting)
        {
            orbitX += Input.GetAxis("Mouse X") * orbitXSpeed * 0.02f;
            orbitY -= Input.GetAxis("Mouse Y") * orbitYSpeed * 0.02f;
            orbitY = ClampAngle(orbitY, orbitYMinLimit, orbitYMaxLimit);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isOrbiting = false;
        }
#endif
    }

    private void ResetCameraToNormal()
    {
        isOrbiting = false;
        activeTouchId = -1;

        if (target != null)
        {
            orbitX = target.eulerAngles.y;
            orbitY = 15f;

            Vector3 desiredPosition;
            if (activeCar != null && rearCameraPoint != null && frontCameraPoint != null)
            {
                desiredPosition = (activeCar.currentGear == CarController.Gear.Drive) ? rearCameraPoint.position : frontCameraPoint.position;
            }
            else
            {
                Vector3 usedOffset = offset;
                if (invertForward) usedOffset.z = -usedOffset.z;
                desiredPosition = target.position + target.TransformDirection(usedOffset);
            }

            transform.position = desiredPosition;
            transform.LookAt(target.position + lookAtOffset);
        }
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    // Yeni Giriş Sistemi için UI kontrolü
    private bool IsPointerOverUIObjectNewSystem(int touchId = -1)
    {
        if (EventSystem.current == null) return false;

#if ENABLE_INPUT_SYSTEM
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        if (touchId != -1 && Touchscreen.current != null)
        {
            eventDataCurrentPosition.position = Touchscreen.current.touches[touchId].position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            eventDataCurrentPosition.position = Mouse.current.position.ReadValue();
        }
        
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
#else
        return IsPointerOverUIObject();
#endif
    }

    private bool IsPointerOverUIObject()
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void FindActiveCarInScene()
    {
        if (allCars != null && allCars.Length > 0)
        {
            foreach (var car in allCars)
            {
                if (car != null && car.gameObject.activeInHierarchy)
                {
                    SetupCameraForCar(car);
                    return;
                }
            }
        }

        CarController foundCar = GameObject.FindAnyObjectByType<CarController>();
        if (foundCar != null)
        {
            SetupCameraForCar(foundCar);
        }
    }

    private void SetupCameraForCar(CarController car)
    {
        activeCar = car;

        if (activeCar != null)
        {
            target = activeCar.transform;
            rearCameraPoint = FindChildByName(activeCar.transform, "RearCameraPoint");
            frontCameraPoint = FindChildByName(activeCar.transform, "FrontCameraPoint");

            if (rearCameraPoint == null) Debug.LogWarning(activeCar.gameObject.name + " üzerinde 'RearCameraPoint' isimli bir obje bulunamadı!");
            if (frontCameraPoint == null) Debug.LogWarning(activeCar.gameObject.name + " üzerinde 'FrontCameraPoint' isimli bir obje bulunamadı!");
        }
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }
}