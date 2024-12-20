using UnityEngine;

public static class Extensions {
    public static Vector3 ToVector3(this Vector2 vec) => new (vec.x, vec.y, 0f);
    public static Vector2 ToVector2(this Vector3 vec) => new (vec.x, vec.y);
}
