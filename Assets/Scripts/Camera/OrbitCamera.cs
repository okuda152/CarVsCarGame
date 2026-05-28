using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ビルドシーン用オービットカメラ。
/// ・左ドラッグ（一定距離以上動かしたとき）→ 回転
/// ・中クリックドラッグ → 回転
/// ・スクロールホイール → ズーム
/// 短い左クリック（ドラッグなし）はカメラ操作を行わない → BuildManager がタイル配置に使う。
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Header("ピボット")]
    [SerializeField] Vector3 pivotPoint = new Vector3(0f, 1.5f, 0f);

    [Header("初期状態")]
    [SerializeField] float yaw      = 45f;
    [SerializeField] float pitch    = 38f;
    [SerializeField] float distance = 12f;

    [Header("操作感度")]
    [SerializeField] float sensitivity = 720f;
    [SerializeField] float zoomSpeed   = 5f;

    [Header("制限")]
    [SerializeField] float minDist  =  4f;
    [SerializeField] float maxDist  = 24f;
    [SerializeField] float minPitch =  8f;
    [SerializeField] float maxPitch = 82f;

    // ドラッグ判定
    Vector2 _pressPos;
    bool    _dragging;
    const float DragThreshold = 5f;   // px

    void LateUpdate()
    {
        // UI ボタン上では操作しない
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        bool leftDown = Input.GetMouseButtonDown(0);
        bool leftHeld = Input.GetMouseButton(0);
        bool midHeld  = Input.GetMouseButton(2);

        // 左ボタン押し始めにドラッグ開始位置を記録
        if (leftDown) { _pressPos = Input.mousePosition; _dragging = false; }

        // ドラッグ閾値を超えたらフラグを立てる
        if (leftHeld && !_dragging &&
            Vector2.Distance(Input.mousePosition, _pressPos) > DragThreshold)
            _dragging = true;

        // 離したらフラグをリセット
        if (!leftHeld) _dragging = false;

        // 回転：左ドラッグ（閾値超え）または中ボタンドラッグ
        bool orbit = midHeld || (leftHeld && _dragging);
        if (orbit)
        {
            yaw   += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // ズーム
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        distance  = Mathf.Clamp(distance, minDist, maxDist);

        // カメラ配置
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(
            pivotPoint + rot * new Vector3(0f, 0f, -distance),
            rot);
    }
}
