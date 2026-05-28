using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildUI : MonoBehaviour
{
    [Header("パレットボタン")]
    [SerializeField] Button blockButton;
    [SerializeField] Button tireButton;
    [SerializeField] Button turretButton;
    [SerializeField] Button machineGunButton;

    [Header("ステータス・開始")]
    [SerializeField] TextMeshProUGUI statusLabel;
    [SerializeField] Button          battleStartButton;

    static readonly Color ColOn  = new Color(0.35f, 0.90f, 0.35f);
    static readonly Color ColOff = new Color(0.22f, 0.22f, 0.32f);

    void Start()
    {
        blockButton.onClick.AddListener(     () => BuildManager.Instance.SelectTile(TileType.Block));
        tireButton.onClick.AddListener(      () => BuildManager.Instance.SelectTile(TileType.Tire));
        turretButton.onClick.AddListener(    () => BuildManager.Instance.SelectTile(TileType.Turret));
        machineGunButton.onClick.AddListener(() => BuildManager.Instance.SelectTile(TileType.MachineGun));
        battleStartButton.onClick.AddListener(() => BuildManager.Instance.StartBattle());
    }

    public void RefreshPalette(TileType selected)
    {
        Tint(blockButton,      selected == TileType.Block);
        Tint(tireButton,       selected == TileType.Tire);
        Tint(turretButton,     selected == TileType.Turret);
        Tint(machineGunButton, selected == TileType.MachineGun);
    }

    public void RefreshStatus(int blocks, int tires, int weapons, string hint, bool canStart)
    {
        string counts = $"ブロック: {blocks}   タイヤ: {tires}   武器: {weapons}/1";
        string req    = canStart
            ? "<color=#77ff77>バトル開始できます！</color>"
            : $"<color=#ff7777>{hint}</color>";
        statusLabel.text = counts + "\n" + req;

        battleStartButton.interactable = canStart;
    }

    static void Tint(Button b, bool on) => b.image.color = on ? ColOn : ColOff;
}
