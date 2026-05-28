/// <summary>
/// ビルドグリッドに配置できるタイルの種類。
/// </summary>
public enum TileType
{
    Empty,       // 何も置いていない
    Block,       // 車体ブロック（HP に加算）
    Tire,        // タイヤ（2 個以上でバトル可能）
    Turret,      // 砲台武器
    MachineGun   // 機関銃武器
}
