using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoidRogues
{
    public class MeleeHitTrackerComponent : MonoBehaviour
    {
        // TODO: Port IHitTarget from LichLord
        // [SerializeField]
        // private HashSet<IHitTarget> _hitsPerSwing = new HashSet<IHitTarget>();
        // public HashSet<IHitTarget> HitsPerSwing => _hitsPerSwing;

        // public void AddHitTarget(IHitTarget hitTarget)
        // {
        //     _hitsPerSwing.Add(hitTarget);
        // }

        public void DrawHitShape(Vector3 position, float radius)
        {
#if UNITY_EDITOR
            StartCoroutine(DrawShapeRoutine(position, radius, 2f));
#endif
        }

#if UNITY_EDITOR
        private IEnumerator DrawShapeRoutine(Vector3 center, float radius, float duration)
        {
            float endTime = Time.realtimeSinceStartup + duration;
            while (Time.realtimeSinceStartup < endTime)
            {
                float life = endTime - Time.realtimeSinceStartup;
                float alpha = Mathf.Clamp01(life / 0.3f);
                Color color = new Color(1f, 0.5f, 0f, alpha * 0.7f);

                DrawWireSphere(center, radius, color);
                yield return null;
            }
        }

        private void DrawWireSphere(Vector3 center, float radius, Color color)
        {
            const int SEGMENTS = 24;
            DrawCircle(center, Vector3.up, radius, color, SEGMENTS);
            DrawCircle(center, Vector3.right, radius, color, SEGMENTS);
            DrawCircle(center, Vector3.forward, radius, color, SEGMENTS);
        }

        private void DrawCircle(Vector3 center, Vector3 axis, float radius, Color color, int segments)
        {
            float step = 360f / segments;
            Vector3 prev = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                Vector3 offset = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis) * (Vector3.right * radius);
                Vector3 point = center + offset;

                if (i > 0)
                    Debug.DrawLine(prev, point, color);

                prev = point;
            }
        }
#endif
    }
}
