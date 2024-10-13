using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class Extensions {
    public static Vector3 ToVector3(this Vector2 vec) => new (vec.x, vec.y, 0f);
}
