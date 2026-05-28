using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("HP Bars")]
    [SerializeField] private Slider          playerHPBar;
    [SerializeField] private Slider          enemyHPBar;
    [SerializeField] private TextMeshProUGUI playerHPLabel;
    [SerializeField] private TextMeshProUGUI enemyHPLabel;

    [Header("Result Panel")]
    [SerializeField] private GameObject      resultPanel;
    [SerializeField] private TextMeshProUGUI resultLabel;
    [SerializeField] private Button          retryButton;
    [SerializeField] private Button          buildButton;

    private void Start()
    {
        resultPanel.SetActive(false);
        retryButton.onClick.AddListener(() => BattleManager.Instance.RetryBattle());
        buildButton.onClick.AddListener( () => BattleManager.Instance.ReturnToBuild());
    }

    public void UpdatePlayerHP(float current, float max)
    {
        playerHPBar.value  = max > 0 ? current / max : 0f;
        playerHPLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void UpdateEnemyHP(float current, float max)
    {
        enemyHPBar.value  = max > 0 ? current / max : 0f;
        enemyHPLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void ShowResult(bool playerWon)
    {
        resultPanel.SetActive(true);
        resultLabel.text  = playerWon ? "YOU WIN!" : "YOU LOSE...";
        resultLabel.color = playerWon ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
    }
}
