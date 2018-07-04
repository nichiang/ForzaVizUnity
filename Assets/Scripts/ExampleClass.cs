using UnityEngine;
using System.Collections;

public class ExampleClass : MonoBehaviour
{
    public Vector3 stretchAxis;
    public float stretchFactor = 1.0F;
    private MeshFilter mf;
    private Vector3[] origVerts;
    private Vector3[] newVerts;
    private Vector3 basisA;
    private Vector3 basisB;
    private Vector3 basisC;
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        origVerts = mf.mesh.vertices;
        newVerts = new Vector3[origVerts.Length];
    }
    void Update()
    {
        basisA = stretchAxis;
        Vector3.OrthoNormalize(ref basisA, ref basisB, ref basisC);
        Matrix4x4 toNewSpace = new Matrix4x4();
        toNewSpace.SetRow(0, basisA);
        toNewSpace.SetRow(1, basisB);
        toNewSpace.SetRow(2, basisC);
        toNewSpace[3, 3] = 1.0F;
        Matrix4x4 scale = new Matrix4x4();
        scale[0, 0] = stretchFactor;
        scale[1, 1] = 1.0F / stretchFactor;
        scale[2, 2] = 1.0F / stretchFactor;
        scale[3, 3] = 1.0F;
        Matrix4x4 fromNewSpace = toNewSpace.transpose;
        Matrix4x4 trans = toNewSpace * scale * fromNewSpace;
        int i = 0;
        while (i < origVerts.Length)
        {
            newVerts[i] = trans.MultiplyPoint3x4(origVerts[i]);
            i++;
        }
        mf.mesh.vertices = newVerts;
    }
}