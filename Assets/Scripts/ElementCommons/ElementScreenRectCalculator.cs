using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ElementCommons
{
    public struct ElementScreenRectCalculator
    {
        public Rect GetElementScreenRect(LocalTransform transform, PhysicsCollider collider, Camera camera)
        {
            RigidTransform rigidTransform = new RigidTransform(transform.Rotation, transform.Position);
            Aabb aabb = collider.Value.Value.CalculateAabb(rigidTransform);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? aabb.Min.x : aabb.Max.x,
                            y == 0 ? aabb.Min.y : aabb.Max.y,
                            z == 0 ? aabb.Min.z : aabb.Max.z
                        );

                        Vector3 screenPoint = camera.WorldToScreenPoint(corner);
                        minX = Mathf.Min(minX, screenPoint.x);
                        minY = Mathf.Min(minY, screenPoint.y);
                        maxX = Mathf.Max(maxX, screenPoint.x);
                        maxY = Mathf.Max(maxY, screenPoint.y);
                    }
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}

