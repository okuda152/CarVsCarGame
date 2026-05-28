using UnityEngine;

/// <summary>
/// プレイヤー操作の車コントローラー。
/// CarBuilder が Configure() でタイヤ配置に基づく物理パラメータを設定する。
/// タイヤ 0 個 → 移動不可（その場で急減速して止まる）。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    private Rigidbody   _rb;
    private WeaponMount _mount;

    // ── タイヤ物理パラメータ（CarBuilder.Configure で設定）────────
    int   _tireCount;
    float _maxSpeed       = 9f;
    float _turnSpeed      = 80f;
    /// <summary>
    /// 横方向速度の残存率（0=完全グリップ, 1=完全スリップ）。
    /// 毎 FixedUpdate に横速度を掛けて摩擦を再現する。
    /// </summary>
    float _lateralDamping = 0.05f;

    /// <summary>
    /// CarBuilder がアセンブル後に呼ぶ。
    /// タイヤ数・最高速度・旋回速度・横グリップをまとめて設定する。
    /// </summary>
    public void Configure(int tireCount, float maxSpeed, float turnSpeed, float lateralDamping)
    {
        _tireCount      = tireCount;
        _maxSpeed       = maxSpeed;
        _turnSpeed      = turnSpeed;
        _lateralDamping = lateralDamping;
    }

    void Awake()
    {
        _rb    = GetComponent<Rigidbody>();
        _mount = GetComponent<WeaponMount>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
            _mount?.FireAll();
    }

    void FixedUpdate()
    {
        ApplyDrive(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
    }

    // ─────────────────────────────────────────────────────────────
    void ApplyDrive(float throttle, float steering)
    {
        if (_tireCount == 0)
        {
            // タイヤなし：摩擦だけで止まる（武器は使える）
            var vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(vel.x * 0.80f, vel.y, vel.z * 0.80f);
            return;
        }

        // ① 横グリップ ── タイヤが横滑りを打ち消す
        ApplyLateralGrip();

        // ② 前後駆動 ── 目標速度との差分を毎フレームインパルスで埋める
        float fwdSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        float diff     = throttle * _maxSpeed - fwdSpeed;
        float impulse  = Mathf.Clamp(diff * 12f, -22f, 22f) * Time.fixedDeltaTime;
        _rb.AddForce(transform.forward * impulse, ForceMode.VelocityChange);

        // ③ ステアリング ── 前進中のみ有効（後退時は逆向き）
        if (Mathf.Abs(fwdSpeed) > 0.3f)
        {
            float yaw = steering * _turnSpeed * Time.fixedDeltaTime * Mathf.Sign(fwdSpeed);
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }
    }

    void ApplyLateralGrip()
    {
        var local = transform.InverseTransformDirection(_rb.linearVelocity);
        local.x  *= _lateralDamping;   // 横速度を減衰させる
        _rb.linearVelocity = transform.TransformDirection(local);
    }
}
