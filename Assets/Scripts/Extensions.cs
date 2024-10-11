using DelaunatorSharp;
using System.Collections.Generic;

namespace UnityEngine {
    public static class Extensions {
        public static Vector3 ToVector3(this IPoint point, float z = 0.5f) => new ((float)point.X, (float)point.Y, z);

        public static Point ToPoint(this Vector3 vec) => new (vec.x, vec.y);

        public static IEnumerable<Point> ToPointEnumeration(this Vector3[] vecs) {
            foreach (Vector3 vec in vecs) {
                yield return new Point(vec.x, vec.y);
            }
        }
    }
}
