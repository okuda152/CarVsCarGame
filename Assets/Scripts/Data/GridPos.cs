using System;
using UnityEngine;

/// <summary>3D グリッド座標。Dictionary のキーとして使えるよう IEquatable を実装。</summary>
[Serializable]
public struct GridPos : IEquatable<GridPos>
{
    public int x, y, z;

    public GridPos(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }

    // ── 演算子 ───────────────────────────────────────────────────────
    public static GridPos operator +(GridPos a, GridPos b) =>
        new GridPos(a.x + b.x, a.y + b.y, a.z + b.z);

    public static GridPos operator -(GridPos a, GridPos b) =>
        new GridPos(a.x - b.x, a.y - b.y, a.z - b.z);

    public static bool operator ==(GridPos a, GridPos b) =>
        a.x == b.x && a.y == b.y && a.z == b.z;

    public static bool operator !=(GridPos a, GridPos b) => !(a == b);

    // ── IEquatable ───────────────────────────────────────────────────
    public bool Equals(GridPos other) => this == other;

    public override bool Equals(object obj) => obj is GridPos g && this == g;

    public override int GetHashCode() => HashCode.Combine(x, y, z);

    public override string ToString() => $"({x},{y},{z})";

    // ── 6 方向オフセット定数 ─────────────────────────────────────────
    public static readonly GridPos Up    = new GridPos( 0,  1,  0);
    public static readonly GridPos Down  = new GridPos( 0, -1,  0);
    public static readonly GridPos Right = new GridPos( 1,  0,  0);
    public static readonly GridPos Left  = new GridPos(-1,  0,  0);
    public static readonly GridPos Fwd   = new GridPos( 0,  0,  1);
    public static readonly GridPos Back  = new GridPos( 0,  0, -1);
}
