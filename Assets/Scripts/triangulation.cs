// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class triangulation : MonoBehaviour
// {
//     public MeshFilter meshFilter;
//     List<Vector3> originalVertices = new List<Vector3>();
//     public GameObject cubePrefab;
//     HashSet<Vector3> mySet = new HashSet<Vector3>();
//     void Start()
//     {
//         getPointsOfmodel(gameObject);
//     }
//     public void getPointsOfmodel(GameObject gameObject)
//     {
//         MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
//         bool firstMesh = true; 
//         foreach (MeshFilter meshFilter in meshFilters)
//         {
//             if (firstMesh)
//             {
//                 firstMesh = false;
//                 continue;   
//             }
//             Mesh mesh = meshFilter.mesh;
//             scanTriangles(mesh);
//         }
//     }
//     public void scanTriangles(Mesh mesh)
//     {
//         Bounds bounds = mesh.bounds;
//         Vector3 modelSize = bounds.size;

//         float maxDimension = Mathf.Max(modelSize.x, modelSize.y, modelSize.z);

//         float scaleFactor = 79f / maxDimension;

//         Vector3 scale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

//         float minVertex = float.MaxValue;

//         for (int i = 0; i < mesh.vertices.Length; i++)
//         {
//             Vector3 scaledVertex = Vector3.Scale(mesh.vertices[i], scale);
//             mesh.vertices[i] = scaledVertex;
//         }

//   mesh.RecalculateNormals();

//         for (int i = 0; i < mesh.triangles.Length; i += 3)
//         {
//             int index1 = mesh.triangles[i];
//             int index2 = mesh.triangles[i + 1];
//             int index3 = mesh.triangles[i + 2];
//             Vector3 vertex1 = mesh.vertices[index1];
//             Vector3 vertex2 = mesh.vertices[index2];
//             Vector3 vertex3 = mesh.vertices[index3];

//             ScanPointsInsideTriangle(vertex1*scaleFactor, vertex2*scaleFactor, vertex3*scaleFactor);
//         }

//         for (int i = 0; i < originalVertices.Count; i++)
//         {
//             minVertex = Mathf.Min(originalVertices[i].x, originalVertices[i].y, originalVertices[i].z, minVertex);
//         }

//         if( minVertex >0 )
//         minVertex = 0;


//         for (int i = 0; i < originalVertices.Count; i++)
//         {
//             Debug.Log("before:" + originalVertices[i]);
//             Vector3 temp = new Vector3(
//                originalVertices[i].x - minVertex,
//               originalVertices[i].y - minVertex,
//               originalVertices[i].z - minVertex);
//             //Vector3 temp = new Vector3(
//             //    Mathf.Round((originalVertices[i].x - minVertex) * scaleFactor),
//             //    Mathf.Round((originalVertices[i].y - minVertex) * scaleFactor),
//             //   Mathf.Round((originalVertices[i].z - minVertex) * scaleFactor));
//             originalVertices[i] = temp;
//             Debug.Log(originalVertices[i]);
//             mySet.Add(originalVertices[i]);
//         }
//         originalVertices.RemoveRange(0, originalVertices.Count);
//         // foreach (Vector3 i in mySet)
//         // {
//         //     GameObject cube = Instantiate(cubePrefab, i, Quaternion.identity);
//         //     Material material = new Material(Shader.Find("Standard"));
//         //     material.color = new Color(255, 0, 0, 255);
//         //     cube.GetComponent<Renderer>().material = material;
//         // }

//         // Debug.Log(minVertex);
//         // Debug.Log(mySet.Count);
//         // Debug.Log("points:" + originalVertices.Count);
//         // Debug.Log("triangle" + mesh.triangles.Length);
//     }
//     void ScanPointsInsideTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
//     {

//         float minX = Mathf.Min(vertex1.x, vertex2.x, vertex3.x);
//         float minY = Mathf.Min(vertex1.y, vertex2.y, vertex3.y);
//         float minZ = Mathf.Min(vertex1.z, vertex2.z, vertex3.z);

//         float maxX = Mathf.Max(vertex1.x, vertex2.x, vertex3.x);
//         float maxY = Mathf.Max(vertex1.y, vertex2.y, vertex3.y);
//         float maxZ = Mathf.Max(vertex1.z, vertex2.z, vertex3.z);

//         for (float x = minX; x <= maxX; x += 10f)
//         {
//             for (float y = minY; y <= maxY; y += 10f)
//             {
//                 for (float z = minZ; z <= maxZ; z +=10f)
//                 {
//                     Vector3 point = new Vector3(x, y, z);

//                     if (IsPointInsideTriangle(point, vertex1, vertex2, vertex3))
//                     {
//                         originalVertices.Add(point);
//                     }
//                 }
//             }
//         }
//     }

//     bool IsPointInsideTriangle(Vector3 point, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
//     {

//         // Calculate vectors
//         // Calculate vectors
//         Vector3 vector12 = vertex2 - vertex1;
//         Vector3 vector13 = vertex3 - vertex1;
//         Vector3 vectorP1 = point - vertex1;

//         // Calculate dot products
//         float dot123 = Vector3.Dot(vector12, vector13);
//         float dotP112 = Vector3.Dot(vectorP1, vector12);
//         float dotP113 = Vector3.Dot(vectorP1, vector13);

//         // Calculate barycentric coordinates
//         float u = (dot123 * dotP113 - vector13.y * dotP112) / (dot123 * dot123);
//         float v = (dot123 * dotP112 - vector12.y * dotP113) / (dot123 * dot123);

//         // Check if coordinates are non-negative and their sum is less than or equal to 1
//         return (u >= 0f) && (v >= 0f) && (u + v <= 1f);
//     }
// }

