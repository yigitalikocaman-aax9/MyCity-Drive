using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders (fizik - gorunmez)")]
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;

    [Header("Wheel Pivots (Hiyerarşideki PivotFL, PivotFR vb.)")]
    public Transform wheelMeshFL;
    public Transform wheelMeshFR;
    public Transform wheelMeshRL;
    public Transform wheelMeshRR;

    [Header("Araba Gövdesi Ayarı (Arabayı Kaldırmak İçin)")]
    public Transform[] carPartsToRaise;
    public float carBodyHeightOffset = 0f;

    [Header("Settings")]
    public float motorForce = 1500f;
    [Header("Vites Ayarları")]
    [Tooltip("Eğer araba D konumunda geri, R konumunda ileri gidiyorsa bu kutucuğu işaretle!")]
    public bool reverseMotorDirection = false;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;

    [Header("Steering Feel (donusu iyilestirir)")]
    public float steerSpeed = 5f;
    public bool reduceSteerAtSpeed = true;
    public float speedForMinSteer = 100f;
    [Range(0.1f, 1f)]
    public float minSteerMultiplier = 0.4f;

    [Header("Tire Grip")]
    public float forwardFrictionStiffness = 1.5f;
    public float sidewaysFrictionStiffness = 1.5f;

    [Header("Brake Light Status (read-only)")]
    public bool isBraking;

    public enum Gear { Drive, Reverse }
    [Header("Transmission")]
    public Gear currentGear = Gear.Drive;

    [Header("Model Açı Düzeltmesi")]
    public Vector3 meshRotationOffset = new Vector3(0, 90, 0);
    [Header("Model Yükseklik Düzeltmesi")]
    public Vector3 meshPositionOffset = new Vector3(0, 0, 0);
    [Header("Collider Pozisyon Düzeltmesi")]
    public Vector3 colliderPositionOffset = new Vector3(0, 0, 0);

    private float throttleInput; 
    private float steerInput;    
    private bool isHoldingBrake; 

    private Rigidbody rb;
    private float currentSteerAngle;
    private Vector3[] originalBodyLocalPositions;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        ApplyFrictionSettings(wheelFL);
        ApplyFrictionSettings(wheelFR);
        ApplyFrictionSettings(wheelRL);
        ApplyFrictionSettings(wheelRR);

        // Gövde pozisyonlarını kaydet
        if (carPartsToRaise != null && carPartsToRaise.Length > 0)
        {
            originalBodyLocalPositions = new Vector3[carPartsToRaise.Length];
            for (int i = 0; i < carPartsToRaise.Length; i++)
            {
                if (carPartsToRaise[i] != null)
                    originalBodyLocalPositions[i] = carPartsToRaise[i].localPosition;
            }
        }
    }

    void ApplyFrictionSettings(WheelCollider wc)
    {
        if (wc == null) return;
        WheelFrictionCurve forward = wc.forwardFriction;
        forward.stiffness = forwardFrictionStiffness;
        wc.forwardFriction = forward;

        WheelFrictionCurve sideways = wc.sidewaysFriction;
        sideways.stiffness = sidewaysFrictionStiffness;
        wc.sidewaysFriction = sideways;
    }

    public void SetThrottleInput(float value) { throttleInput = value; }
    public void SetSteerInput(float value) { steerInput = value; }
    public void SetBrakeInput(bool state) { isHoldingBrake = state; }

    public void ToggleGear()
    {
        float speed = rb.linearVelocity.magnitude * 3.6f;
        if (speed < 5f) 
        {
            currentGear = currentGear == Gear.Drive ? Gear.Reverse : Gear.Drive;
            Debug.Log("Yeni Vites: " + currentGear);
        }
    }

    void FixedUpdate()
    {
        ApplySteering();
        ApplyMotorTorque();
        ApplyBrakes();
        
        isBraking = isHoldingBrake; 

        SyncAllWheelMeshes();
        UpdateCarBodyHeight();
    }

    void ApplySteering()
    {
        float steerMultiplier = 1f;
        if (reduceSteerAtSpeed)
        {
            float speedKmh = rb.linearVelocity.magnitude * 3.6f;
            if (speedKmh > 0.1f) // Hız sıfırken bölme hatası olmasın diye güvenliğe aldık
            {
                float t = Mathf.Clamp01(speedKmh / speedForMinSteer);
                steerMultiplier = Mathf.Lerp(1f, minSteerMultiplier, t);
            }
        }

        float targetSteerAngle = steerInput * maxSteerAngle * steerMultiplier;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSpeed * Time.fixedDeltaTime);

        if(wheelFL != null) wheelFL.steerAngle = currentSteerAngle;
        if(wheelFR != null) wheelFR.steerAngle = currentSteerAngle;
    }

    void ApplyMotorTorque()
    {
        float gearMultiplier = (currentGear == Gear.Drive) ? 1f : -1f; 

        if (reverseMotorDirection)
        {
            gearMultiplier = -gearMultiplier;
        }

        float appliedMotorTorque = throttleInput * motorForce * gearMultiplier;
        if (isHoldingBrake) appliedMotorTorque = 0f;

        if(wheelFL != null) wheelFL.motorTorque = appliedMotorTorque;
        if(wheelFR != null) wheelFR.motorTorque = appliedMotorTorque;
        if(wheelRL != null) wheelRL.motorTorque = appliedMotorTorque;
        if(wheelRR != null) wheelRR.motorTorque = appliedMotorTorque;
    }

    void ApplyBrakes()
    {
        float appliedBrakeForce = isHoldingBrake ? brakeForce : 0f;

        if(wheelFL != null) wheelFL.brakeTorque = appliedBrakeForce;
        if(wheelFR != null) wheelFR.brakeTorque = appliedBrakeForce;
        if(wheelRL != null) wheelRL.brakeTorque = appliedBrakeForce;
        if(wheelRR != null) wheelRR.brakeTorque = appliedBrakeForce;
    }

    void SyncAllWheelMeshes()
    {
        SyncWheelMesh(wheelFL, wheelMeshFL, true);
        SyncWheelMesh(wheelFR, wheelMeshFR, false);
        SyncWheelMesh(wheelRL, wheelMeshRL, true);
        SyncWheelMesh(wheelRR, wheelMeshRR, false);
    }

    void SyncWheelMesh(WheelCollider collider, Transform mesh, bool isLeftWheel)
    {
        if (collider == null || mesh == null) return;

        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        
        Vector3 directionOffset = colliderPositionOffset;
        if (!isLeftWheel) directionOffset.x = -directionOffset.x;

        // DÜZELTME: Eğer arabanın Scale'i 1,1,1 değilse, pozisyonu yerel matrise göre dönüştürerek çarpıklığı önlüyoruz
        Vector3 finalPosition = pos + (rot * meshPositionOffset) + (transform.TransformDirection(directionOffset));
        Quaternion finalRotation = rot * Quaternion.Euler(meshRotationOffset);

        mesh.position = finalPosition;
        mesh.rotation = finalRotation;
    }

    void UpdateCarBodyHeight()
    {
        // DÜZELTME: Eğer yükseklik sıfırsa ve parça yoksa hiç çalıştırma, pozisyonları ezme!
        if (carPartsToRaise == null || originalBodyLocalPositions == null || carBodyHeightOffset == 0f) return;

        for (int i = 0; i < carPartsToRaise.Length; i++)
        {
            if (carPartsToRaise[i] != null && i < originalBodyLocalPositions.Length)
            {
                Vector3 targetLocalPos = originalBodyLocalPositions[i];
                targetLocalPos.y += carBodyHeightOffset;
                carPartsToRaise[i].localPosition = targetLocalPos;
            }
        }
    }
}