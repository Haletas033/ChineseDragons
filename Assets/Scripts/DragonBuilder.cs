using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class DragonBuilder : MonoBehaviour
{
    
    public MeshFilter meshFilter;

    public float r = 1f; //Radius of circle
    public int points = 10;
    public int segments = 10;
    
    public float amplitude = .1f;
    public float frequency = .1f;

    public float segmentSpacing = 1f;
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
            float tubeAngle = 2 * Mathf.PI * i / points;
            tubeAngles.Add(tubeAngle);
        }
            
        tubeAngles.Insert(0, 359 * Mathf.Deg2Rad); 
        tubeAngles.Insert(2, 1 * Mathf.Deg2Rad);   

        _ringPoints = tubeAngles.Count;
        
        Vector3[] vertices = new Vector3[_ringPoints * segments];
        
        
        int[] triangles = new int[segments  * _ringPoints * 6];

        for (int j = 0; j < segments; j++)
        { 
            //Segment follow an animated sine wave
            
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
            Vector3 normal = Vector3.forward; // Fixed axis perpendicular to the sine wave plane
            Vector3 binormal = Vector3.Cross(tangent, normal);
 

            for (int i = 0; i < tubeAngles.Count; i++)
            {
                float angle = tubeAngles[i];
                // Position vertex on the circle, oriented perpendicular to tangent
                vertices[j * _ringPoints + i] = center
                                           + r * Mathf.Cos(angle) * normal
                                           + r * Mathf.Sin(angle) * binormal;
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
                triangles[t++] = b;
                triangles[t++] = aNext;

                triangles[t++] = b;
                triangles[t++] = bNext;
                triangles[t++] = aNext;
            }
        }

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    void Update()
    {
        _phase += Time.deltaTime;
        GenerateMesh();
    }
}