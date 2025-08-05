using System.Collections.Generic;
using UnityEngine;

public class DragonBuilder : MonoBehaviour
{
    public MeshFilter meshFilter;
    
    [Header("Dragon Shape")]
    public float radius = 1f;
    public float spineWidth = .01f;
    public float spineHeight = 2f;
    public int dragonMeters = 10;
    
    [Header("Dragon Quality")]
    public int points = 10;
    public int segmentsPerMeter = 10;
    
    [Header("Dragon Path")]
    public float amplitude = .1f;
    public float frequency = .1f;
    
    // Private runtime fields
    private float _segmentSpacing;
    private int _segments = 10;
    private float _phase;
    private int _ringPoints;
    private Mesh _mesh;
    
    // Constants
    private const int TailSegments = 5;
    private const float Delta = 0.01f;
    readonly float _twoPi = Mathf.PI * 2;

    void EnsureMesh()
    {
        if (_mesh == null)
            _mesh = new Mesh();
        if (meshFilter != null && meshFilter.sharedMesh != _mesh)
            meshFilter.sharedMesh = _mesh;
    }

    void Start()
    {
        EnsureMesh();
    }

    void OnValidate() { if (!Application.isPlaying) EnsureMesh(); }

    void GenerateMesh()
    {
        _segmentSpacing = 1f / segmentsPerMeter;
        _segments = segmentsPerMeter * dragonMeters;
        
        _mesh.Clear();
        
        List<float> tubeAngles = new List<float>();

        for (int i = 0; i < points; i++)
        {
            float angle = _twoPi * i / points;

            if (Mathf.Approximately(angle, 0f)) continue;

            tubeAngles.Add(angle);
        }
        
        // Creates a point either side of angle zero to adjust the width of the spine
        tubeAngles.Insert(0, -spineWidth);
        tubeAngles.Insert(1, 0f);
        tubeAngles.Insert(2, spineWidth);

        _ringPoints = tubeAngles.Count;
        
        Vector3[] vertices = new Vector3[_ringPoints * _segments];
        
        Vector3[] normals = new Vector3[vertices.Length];
        
        int[] triangles = new int[_segments  * _ringPoints * 6];
        
        Vector3 lastCenter = Vector3.zero;
        Vector3 lastTangent = Vector3.forward;
        
        float lastRadius = radius;

        for (int segment = 0; segment < _segments; segment++)
        { 
            // Segment follows an animated sine wave
            
            GetFrame(segment * _segmentSpacing, out var center, out var normal, out var binormal, out var tangent);
            
            if (segment == _segments - 1)
            {
                lastCenter = center;
                lastTangent = tangent;
            }

            for (int pointIndex = 0; pointIndex < tubeAngles.Count; pointIndex++)
            {
                float currentRadius = (pointIndex == 1) ? radius * spineHeight : radius;
                currentRadius -= (currentRadius - 0.5f) / (_segments - 1) * segment;

                if (pointIndex == tubeAngles.Count - 1)
                {
                    lastRadius = currentRadius;
                }

                CreateRing(tubeAngles, vertices, normals, normal, binormal, center, currentRadius, pointIndex, segment);
            }
        }
        
        int triangleIndex = 0; // index for triangle array

        for (int segmentIndex = 0; segmentIndex < _segments; segmentIndex++) {
            if (segmentIndex == _segments - 1) break;
            
            CreateSegmentTriangles(triangles, segmentIndex, ref triangleIndex);
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
        
        for (int segment = 0; segment < TailSegments; segment++)
        {
            Vector3 normal = Vector3.up;
            Vector3 binormal = Vector3.Cross(normal, tangent).normalized;
            normal = Vector3.Cross(tangent, binormal).normalized;

            float halfPi = Mathf.PI * 0.5f;
            float phi = ((float)segment / TailSegments) * halfPi;

            float currentRadius = lastRadius * Mathf.Cos(phi);
            Vector3 ringCenter = center + tangent * (lastRadius * Mathf.Sin(phi));
            
            for (int pointIndex = 0; pointIndex < tubeAngles.Count; pointIndex++)
            {
                CreateRing(tubeAngles, tailVerts, tailNormals, normal, binormal, ringCenter, currentRadius, pointIndex, segment);
            }
        }
        
        tailVerts[tailVerts.Length - 1] = center + tangent * lastRadius;
        tailNormals[tailVerts.Length - 1] = tangent;

        
        int triangleIndex = 0; // index for triangle array

        for (int segmentIndex = 0; segmentIndex < TailSegments; segmentIndex++) {
            if (segmentIndex == TailSegments - 1)
            {
                for (int i = 0; i < _ringPoints; i++)
                {
                    int a = segmentIndex * _ringPoints + i;
                    int b = segmentIndex * _ringPoints + (i + 1) % _ringPoints;
                    
                    tailTriangles[triangleIndex++] = a;
                    tailTriangles[triangleIndex++] = tailVerts.Length - 1;
                    tailTriangles[triangleIndex++] = b;
                }

                break;
            }
            
            CreateSegmentTriangles(tailTriangles, segmentIndex, ref triangleIndex);
            
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

    private void CreateSegmentTriangles(int[] triangles, int segmentIndex, ref int triangleIndex)
    {
        int nextSeg = segmentIndex + 1;

        for (int pointIndex = 0; pointIndex < _ringPoints; pointIndex++)
        {
            int a = segmentIndex * _ringPoints + pointIndex;
            int b = segmentIndex * _ringPoints + (pointIndex + 1) % _ringPoints;
            int aNext = nextSeg * _ringPoints + pointIndex;
            int bNext = nextSeg * _ringPoints + (pointIndex + 1) % _ringPoints;

            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = aNext;
            triangles[triangleIndex++] = b;

            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = aNext;
            triangles[triangleIndex++] = bNext;
        }
    }

    private void CreateRing(List<float>tubeAngles, Vector3[] vertices, Vector3[] normals,
        Vector3 normal, Vector3 binormal, Vector3 center,
        float currentRadius, int pointIndex, int segment)
    {
        float angle = tubeAngles[pointIndex];
        // Position vertex on the circle, oriented perpendicular to tangent
        vertices[segment * _ringPoints + pointIndex] = center
                                        + currentRadius * Mathf.Cos(angle) * normal
                                        + currentRadius * Mathf.Sin(angle) * binormal;
                
        // Calculate normals for each vertex
        normals[segment * _ringPoints + pointIndex] = (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal).normalized;
    }
    
    private void GetFrame(float x, out Vector3 center, out Vector3 normal, out Vector3 binormal, out Vector3 tangent)
    {
        float y = amplitude * Mathf.Sin(frequency * x + _phase);
        float z = 0;
    
        center = new Vector3(x, y, z);
    
        // Tangent vector of the tsine wave at this segment
        
        float y1 = amplitude * Mathf.Sin(frequency * (x + Delta) + _phase);
        Vector3 pointAhead = new Vector3(x + Delta, y1, z);
    
        tangent = (pointAhead - center).normalized;
    
        // Normal and binormal vectors forming the circle's plane
        normal = Vector3.up;
        binormal = Vector3.Cross(normal, tangent).normalized;
        normal = Vector3.Cross(tangent, binormal).normalized;
    }
}