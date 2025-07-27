using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    
    void Start()
    {
        _mesh = new Mesh();
        meshFilter.mesh = _mesh;
    }
    
    void OnValidate() {
        if (Application.isPlaying) return;
        GenerateMesh();
    }

    void GenerateMesh()
    {
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


            // Normal and binormal vectors forming the circle's plane
            Vector3 normal = Vector3.up;
            Vector3 binormal = Vector3.Cross(normal, tangent).normalized;
            normal = Vector3.Cross(tangent, binormal).normalized;

 

            for (int i = 0; i < tubeAngles.Count; i++)
            {
                float radius = (i == 1) ? r * 2 : r;

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

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.normals = normals;
    }

    void LateUpdate()
    {
        _phase += Time.deltaTime;
        GenerateMesh();
    }
}