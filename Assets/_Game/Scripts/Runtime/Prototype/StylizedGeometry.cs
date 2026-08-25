using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    public static class StylizedGeometry
    {
        private const float Half = 0.5f;
        private const float Bevel = 0.075f;
        private static Mesh chamferedCube;

        public static Mesh ChamferedCube => chamferedCube != null
            ? chamferedCube
            : chamferedCube = BuildChamferedCube();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            chamferedCube = null;
        }

        private static Mesh BuildChamferedCube()
        {
            float inset = Half - Bevel;
            List<Vector3> vertices = new List<Vector3>(144);
            List<int> triangles = new List<int>(216);

            for (int axis = 0; axis < 3; axis++)
            {
                int first = (axis + 1) % 3;
                int second = (axis + 2) % 3;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    AddFace(vertices, triangles,
                        Point(axis, sign * Half, first, -inset, second, -inset),
                        Point(axis, sign * Half, first, -inset, second, inset),
                        Point(axis, sign * Half, first, inset, second, inset),
                        Point(axis, sign * Half, first, inset, second, -inset));
                }
            }

            for (int first = 0; first < 3; first++)
            {
                for (int second = first + 1; second < 3; second++)
                {
                    int lengthAxis = 3 - first - second;
                    for (int firstSign = -1; firstSign <= 1; firstSign += 2)
                    {
                        for (int secondSign = -1; secondSign <= 1; secondSign += 2)
                        {
                            AddFace(vertices, triangles,
                                Point(first, firstSign * Half, second, secondSign * inset,
                                    lengthAxis, -inset),
                                Point(first, firstSign * Half, second, secondSign * inset,
                                    lengthAxis, inset),
                                Point(first, firstSign * inset, second, secondSign * Half,
                                    lengthAxis, inset),
                                Point(first, firstSign * inset, second, secondSign * Half,
                                    lengthAxis, -inset));
                        }
                    }
                }
            }

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        AddFace(vertices, triangles,
                            new Vector3(x * Half, y * inset, z * inset),
                            new Vector3(x * inset, y * Half, z * inset),
                            new Vector3(x * inset, y * inset, z * Half));
                    }
                }
            }

            Mesh mesh = new Mesh
            {
                name = "Shared Chamfered Cube",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Vector3 Point(int firstAxis, float firstValue, int secondAxis,
            float secondValue, int thirdAxis, float thirdValue)
        {
            Vector3 point = Vector3.zero;
            point[firstAxis] = firstValue;
            point[secondAxis] = secondValue;
            point[thirdAxis] = thirdValue;
            return point;
        }

        private static void AddFace(List<Vector3> vertices, List<int> triangles,
            params Vector3[] points)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Length; i++) center += points[i];
            center /= points.Length;
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            if (Vector3.Dot(normal, center) < 0f) Array.Reverse(points);

            int start = vertices.Count;
            vertices.AddRange(points);
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }
    }
}
