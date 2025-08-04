using System.Collections.Generic;
using UnityEngine;

public class DragonBuilder : MonoBehaviour
{
    public MeshFilter meshFilter;

    public float r = 1f; //Radius of circle
    public int points = 10;
    public int segments = 10;
    
    public float amplitude = .1f;
    public float frequency = .1f;

    public float segmentSpacing = 1f;

    public float spineWidth = .01f;
    
    private float _phase = 0f;

    private int _ringPoints;

    private Mesh _mesh;
    
    void Reset()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (_mesh == null)
            _mesh = new Mesh();

        if (meshFilter != null && meshFilter.sharedMesh != _mesh)
            meshFilter.sharedMesh = _mesh;
    }

    void Start()
    {
        if (_mesh == null)
            _mesh = new Mesh();

        if (meshFilter != null && meshFilter.mesh != _mesh)
            meshFilter.mesh = _mesh;
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;

        if (_mesh == null)
            return;

        GenerateMesh();
    }

    void GenerateMesh()
    {
        _mesh.Clear();
        
        List<float> tubeAngles = new List<float>();

        for (int i = 0; i < points; i++)
        {
            float angle = 2 * Mathf.PI * i / points;

            if (Mathf.Approximately(angle, 0f)) continue;

            tubeAngles.Add(angle);
        }
        
        // Creates a point either side of angle zero to adjust the width of the spine
        tubeAngles.Insert(0, -spineWidth);
        tubeAngles.Insert(1, 0f);
        tubeAngles.Insert(2, spineWidth);

        _ringPoints = tubeAngles.Count;
        
        Vector3[] vertices = new Vector3[_ringPoints * segments];
        
        Vector3[] normals = new Vector3[vertices.Length];
        
        int[] triangles = new int[segments  * _ringPoints * 6];
        
        Vector3 lastCenter = Vector3.zero;
        Vector3 lastTangent = Vector3.forward;
        
        float lastRadius = r;

        for (int j = 0; j < segments; j++)
        { 
            // Segment follows an animated sine wave
            
            // Center of the segment on the sine wave
            float x = j * segmentSpacing;
            float y = amplitude * Mathf.Sin(frequency * x + _phase);
            float z = 0;

            Vector3 center = new Vector3(x, y, z);


            // Tangent vector of the tsine wave at this segment
            float delta = 0.01f; // small step for derivative approx

            float y1 = amplitude * Mathf.Sin(frequency * (x + delta) + _phase);
            Vector3 pointAhead = new Vector3(x + delta, y1, z);

            Vector3 tangent = (pointAhead - center).normalized;
            
            if (j == segments - 1)
            {
                lastCenter = center;
                lastTangent = tangent;
            }


            // Normal and binormal vectors forming the circle's plane
            Vector3 normal = Vector3.up;
            Vector3 binormal = Vector3.Cross(normal, tangent).normalized;
            normal = Vector3.Cross(tangent, binormal).normalized;

            for (int i = 0; i < tubeAngles.Count; i++)
            {
                float radius = (i == 1) ? r * 2 : r;
                radius -= (radius - 0.5f) / (segments - 1) * j;

                if (i == tubeAngles.Count - 1)
                {
                    lastRadius = radius;
                }

                float angle = tubeAngles[i];
                // Position vertex on the circle, oriented perpendicular to tangent
                vertices[j * _ringPoints + i] = center
                                           + radius * Mathf.Cos(angle) * normal
                                           + radius * Mathf.Sin(angle) * binormal;
                
                // Calculate normals for each vertex
                normals[j * _ringPoints + i] = (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal).normalized;
            }
        }
        
        
        int t = 0; // index for triangle array

        for (int seg = 0; seg < segments; seg++) {
            if (seg == segments - 1) break;
            int nextSeg = seg + 1;

            for (int i = 0; i < _ringPoints; i++)
            {
                int a = seg * _ringPoints + i;
                int b = seg * _ringPoints + (i + 1) % _ringPoints;
                int aNext = nextSeg * _ringPoints + i;
                int bNext = nextSeg * _ringPoints + (i + 1) % _ringPoints;

                triangles[t++] = a;
                triangles[t++] = aNext;
                triangles[t++] = b;

                triangles[t++] = b;
                triangles[t++] = aNext;
                triangles[t++] = bNext;
            }
        }
        
        GenerateTail(ref vertices, ref normals, ref triangles, lastCenter, lastTangent, tubeAngles, lastRadius);

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.normals = normals;
    }

    void LateUpdate()
    {
        _phase += Time.deltaTime;
        GenerateMesh();
    }

    void GenerateTail(ref Vector3[] vertices, ref Vector3[] normals, ref int[] triangles, Vector3 center, Vector3 tangent, List<float> tubeAngles, float lastRadius)
    {
        Vector3[] tailVerts = new Vector3[_ringPoints * 5 + 1];
        Vector3[] tailNormals = new Vector3[tailVerts.Length];
        int[] tailTriangles = new int[tailVerts.Length * 6];
        
        float phi;
        
        for (int j = 0; j < 5; j++)
        {
            Vector3 normal = Vector3.up;
            Vector3 binormal = Vector3.Cross(normal, tangent).normalized;
            normal = Vector3.Cross(tangent, binormal).normalized;

             phi = (j / 5f) * Mathf.PI * 0.5f;

            float radius = lastRadius * Mathf.Cos(phi);
            Vector3 ringCenter = center + tangent * (lastRadius * Mathf.Sin(phi));
            
            for (int i = 0; i < tubeAngles.Count; i++)
            {
                float angle = tubeAngles[i];
                // Position vertex on the circle, oriented perpendicular to tangent
                tailVerts[j * _ringPoints + i] = ringCenter
                                                + radius * Mathf.Cos(angle) * normal
                                                + radius * Mathf.Sin(angle) * binormal;
                
                // Calculate normals for each vertex
                tailNormals[j * _ringPoints + i] = (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal).normalized;
            }
        }

        phi = Mathf.PI * -.5f;
        tailVerts[tailVerts.Length - 1] = center + tangent * lastRadius;
        tailNormals[tailVerts.Length - 1] = tangent;

        
        int t = 0; // index for triangle array

        for (int seg = 0; seg < 5; seg++) {
            if (seg == 5 - 1)
            {
                for (int i = 0; i < _ringPoints; i++)
                {
                    int a = seg * _ringPoints + i;
                    int b = seg * _ringPoints + (i + 1) % _ringPoints;
                    
                    tailTriangles[t++] = a;
                    tailTriangles[t++] = tailVerts.Length - 1;
                    tailTriangles[t++] = b;
                }

                break;
            }
            int nextSeg = seg + 1;

            for (int i = 0; i < _ringPoints; i++)
            {
                int a = seg * _ringPoints + i;
                int b = seg * _ringPoints + (i + 1) % _ringPoints;
                int aNext = nextSeg * _ringPoints + i;
                int bNext = nextSeg * _ringPoints + (i + 1) % _ringPoints;

                tailTriangles[t++] = a;
                tailTriangles[t++] = aNext;
                tailTriangles[t++] = b;

                tailTriangles[t++] = b;
                tailTriangles[t++] = aNext;
                tailTriangles[t++] = bNext;
            }
        }

        int oldVertCount = vertices.Length;
        int oldTriCount = triangles.Length;
        
        System.Array.Resize(ref vertices, oldVertCount + tailVerts.Length);
        System.Array.Resize(ref normals, normals.Length + tailNormals.Length);
        System.Array.Resize(ref triangles, oldTriCount + tailTriangles.Length);
        
        for (int i = 0; i < tailVerts.Length; i++)
        {
            vertices[oldVertCount + i] = tailVerts[i];
            normals[oldVertCount + i] = tailNormals[i];
        }
        
        for (int i = 0; i < tailTriangles.Length; i++)
        {
            triangles[oldTriCount + i] = tailTriangles[i] + oldVertCount;
        }
    }
}
