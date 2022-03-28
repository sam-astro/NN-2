using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class TextureDrawer : MonoBehaviour
{
    public Camera cam;
    public int SIZE = 2;
    public Material mat;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            return;

        RaycastHit hit;
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
            return;


        Renderer rend = hit.transform.GetComponent<Renderer>();
        MeshCollider meshCollider = hit.collider as MeshCollider;

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
            return;
        
        //tex = rend.material.mainTexture as Texture2D;
        Texture2D tex = mat.mainTexture as Texture2D;
        Vector2 pixelUV = hit.textureCoord;

        pixelUV.x *= tex.width;
        pixelUV.y *= tex.height;

        //Expand where to draw on both direction
        //for (int i = 0; i < SIZE; i++)
        //{
            int x = (int)pixelUV.x;
            int y = (int)pixelUV.y;

            Color setcolor = Color.black;

            if (Input.GetMouseButton(0))
                setcolor = Color.white;

            tex.SetPixel(x, y, setcolor);
            tex.SetPixel(x, y + 1, setcolor);
            tex.SetPixel(x + 1, y + 1, setcolor);
            tex.SetPixel(x + 1, y, setcolor);
            tex.SetPixel(x + 1, y - 1, setcolor);
            tex.SetPixel(x, y - 1, setcolor);
            tex.SetPixel(x - 1, y - 1, setcolor);
            tex.SetPixel(x - 1, y, setcolor);
            tex.SetPixel(x - 1, y + 1, setcolor);
        //}
        tex.Apply();
    }

    public void ClearImage()
    {
        Texture2D tex = mat.mainTexture as Texture2D;
        for (int i = 0; i < tex.width; i++)
        {
            for (int j = 0; j < tex.height; j++)
            {
                tex.SetPixel(i, j, Color.black);
            }
        }

        tex.Apply();
    }

    public static Texture2D Clear(Texture2D t)
    {
        for (int i = 0; i < t.width; i++)
        {
            for (int j = 0; j < t.height; j++)
            {
                t.SetPixel(i, j, Color.black);
            }
        }

        t.Apply();
        return t;
    }
}
