using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("カービルダー")]
    [SerializeField] CarBuilder carBuilder;

    [Header("スポーン地点")]
    [SerializeField] Transform playerSpawn;
    [SerializeField] Transform aiSpawn;

    [Header("UI / カメラ")]
    [SerializeField] BattleUI          battleUI;
    [SerializeField] ThirdPersonCamera thirdPersonCamera;

    bool _battleEnded;

    void Awake() => Instance = this;

    void Start()
    {
        SpawnPlayer();
        SpawnAI();
    }

    void SpawnPlayer()
    {
        var car = carBuilder.BuildPlayer();
        car.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
        car.tag = "Player";

        var mount = car.GetComponent<WeaponMount>();
        mount.SetOwnerTag("Player");

        var hp = car.GetComponent<HealthSystem>();
        hp.OnHealthChanged += battleUI.UpdatePlayerHP;
        hp.OnDied          += () => EndBattle(playerWon: false);
        battleUI.UpdatePlayerHP(hp.Current, hp.MaxHP);

        thirdPersonCamera.SetTarget(car.transform);
    }

    void SpawnAI()
    {
        var car = carBuilder.BuildAI();
        car.transform.SetPositionAndRotation(aiSpawn.position, aiSpawn.rotation);
        car.tag = "Enemy";

        var mount = car.GetComponent<WeaponMount>();
        mount.SetOwnerTag("Enemy");

        var hp = car.GetComponent<HealthSystem>();
        hp.OnHealthChanged += battleUI.UpdateEnemyHP;
        hp.OnDied          += () => EndBattle(playerWon: true);
        battleUI.UpdateEnemyHP(hp.Current, hp.MaxHP);
    }

    void EndBattle(bool playerWon)
    {
        if (_battleEnded) return;
        _battleEnded = true;

        var playerCar = GameObject.FindWithTag("Player")?.GetComponent<CarController>();
        if (playerCar != null) playerCar.enabled = false;
        var aiCar = Object.FindFirstObjectByType<AICarController>();
        if (aiCar != null) aiCar.enabled = false;

        battleUI.ShowResult(playerWon);
    }

    public void ReturnToBuild()
    {
        BuildData.Reset();
        SceneManager.LoadScene("BuildScene");
    }

    public void RetryBattle() => SceneManager.LoadScene("BattleScene");
}
