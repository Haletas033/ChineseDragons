using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class LóngBuilder : MonoBehaviour
{
    
    public MeshFilter meshFilter;

    public float r = 1f; //Radius of circle
    public float R = 2f; //Radius of torus
    public int points = 10;
    public int segments = 10;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;
        
        Vector3[] vertices = new Vector3[points * segments];
        
        
        int[] triangles = new int[segments  * points * 6];

        for (int j = 0; j < segments; j++)
        {
            // Angle around the torus
            float segmentAngle = 2 * Mathf.PI * j / segments;
            
            //Segment follow a circular path making a torus
            
            // Center of this circle on the torus
            Vector3 center = new Vector3(
                R * Mathf.Cos(segmentAngle),
                R * Mathf.Sin(segmentAngle),
                0);

            // Tangent vector of the torus at this segment
            Vector3 tangent = new Vector3(
                -Mathf.Sin(segmentAngle),
                Mathf.Cos(segmentAngle),
                0);

            // Normal and binormal vectors forming the circle's plane
            Vector3 normal = Vector3.forward; // Fixed axis perpendicular to the torus plane
            Vector3 binormal = Vector3.Cross(tangent, normal);

            for (int i = 0; i < points; i++)
            {
                float tubeAngle = 2 * Mathf.PI * i / points;

                // Position vertex on the circle, oriented perpendicular to tangent
                vertices[j * points + i] = center
                                           + r * Mathf.Cos(tubeAngle) * normal
                                           + r * Mathf.Sin(tubeAngle) * binormal;
            }
        }
        
        
        int t = 0; // index for triangle array

        for (int seg = 0; seg < segments; seg++) {
            int nextSeg = (seg + 1) % segments; // Wrap around

            for (int i = 0; i < points; i++)
            {
                int a = seg * points + i;
                int b = seg * points + (i + 1) % points;
                int aNext = nextSeg * points + i;
                int bNext = nextSeg * points + (i + 1) % points;

                triangles[t++] = a;
                triangles[t++] = b;
                triangles[t++] = aNext;

                triangles[t++] = b;
                triangles[t++] = bNext;
                triangles[t++] = aNext;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}