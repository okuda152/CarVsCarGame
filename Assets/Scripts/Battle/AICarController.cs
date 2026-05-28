using UnityEngine;

/// <summary>
/// AI 車コントローラー。
/// プレイヤーを追跡して適切な距離を保ちながら攻撃する。
/// CarBuilder が Configure() でタイヤ物理パラメータを設定する。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AICarController : MonoBehaviour
{
    [SerializeField] float preferredDist  = 12f;
    [SerializeField] float fireAngleLimit = 25f;

    Rigidbody   _rb;
    WeaponMount _mount;
    Transform   _player;

    // ── タイヤ物理パラメータ（CarBuilder.Configure で設定）────────
    int   _tireCount;
    float _maxSpeed       = 7f;
    float _turnSpeed      = 75f;
    float _lateralDamping = 0.05f;

    public void Configure(int tireCount, float maxSpeed, float turnSpeed, float lateralDamping)
    {
        _tireCount      = tireCount;
        _maxSpeed       = maxSpeed * 0.85f;   // AI はやや遅め
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

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void FixedUpdate()
    {
        if (_player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // 前後スロットル
        float throttle = dist > preferredDist * 1.1f ?  1f
                       : dist < preferredDist * 0.7f ? -1f
                       : 0f;

        ApplyDrive(throttle);

        // プレイヤー方向へ回頭（_turnSpeed で制限）
        if (toPlayer.sqrMagnitude > 0.01f && _tireCount > 0)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation, targetRot, _turnSpeed * Time.fixedDeltaTime));
        }
    }

    void ApplyDrive(float throttle)
    {
        if (_tireCount == 0)
        {
            var vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(vel.x * 0.80f, vel.y, vel.z * 0.80f);
            return;
        }

        // 横グリップ
        var local = transform.InverseTransformDirection(_rb.linearVelocity);
        local.x  *= _lateralDamping;
        _rb.linearVelocity = transform.TransformDirection(local);

        // 駆動
        float fwdSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        float diff     = throttle * _maxSpeed - fwdSpeed;
        float impulse  = Mathf.Clamp(diff * 12f, -22f, 22f) * Time.fixedDeltaTime;
        _rb.AddForce(transform.forward * impulse, ForceMode.VelocityChange);
    }

    void Update()
    {
        if (_player == null) return;
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        if (Vector3.Angle(transform.forward, toPlayer) < fireAngleLimit)
            _mount?.FireAll();
    }
}
