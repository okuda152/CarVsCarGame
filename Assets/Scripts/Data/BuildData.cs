using System.Collections.Generic;

/// <summary>
/// シーン遷移をまたいで保持するビルドデータ（静的コンテナ）。
/// 3D グリッド対応: Dictionary&lt;GridPos, TileType&gt;。
/// </summary>
public static class BuildData
{
    /// <summary>セルの一辺サイズ（ワールド単位）。</summary>
    public const float CellSize = 1.0f;

    /// <summary>配置済みタイルのマップ。キーは 3D グリッド座標。</summary>
    public static Dictionary<GridPos, TileType> Grid = new();

    public static void Reset() => Grid.Clear();
}
