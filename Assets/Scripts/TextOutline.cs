﻿using UnityEngine;
using System.Collections;

/*
 * Text outline
 * 
 * To outline your 3d textmesh, simply add this script to it:
 * 
 * Script by unity3d.com user: 
 * TheGering
 * 
 * Source:
 * http://answers.unity3d.com/questions/542646/3d-text-strokeoutline.html
 * Mar 21, 2014
 */
public class TextOutline : MonoBehaviour
{
    public float pixelSize = 1;
    public Color outlineColor = Color.black;
    public bool resolutionDependant = false;
    public int doubleResolution = 1024;
    private TextMesh textMesh;
    private MeshRenderer meshRenderer;
    public string SortingLayerName = "Foreground";
    public int SortingOrder = 100;

    void Start()
    {
        textMesh = GetComponent<TextMesh>();
        meshRenderer = GetComponent<MeshRenderer>();
        for (int i = 0; i < 8; i++)
        {
            GameObject outline = new GameObject("outline", typeof(TextMesh));
            outline.transform.parent = transform;
            outline.transform.localScale = new Vector3(1, 1, 1);
            MeshRenderer otherMeshRenderer = outline.GetComponent<MeshRenderer>();
            otherMeshRenderer.material = new Material(meshRenderer.material);
            otherMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            otherMeshRenderer.receiveShadows = false;
            otherMeshRenderer.sortingLayerID = meshRenderer.sortingLayerID;
            otherMeshRenderer.sortingLayerName = meshRenderer.sortingLayerName;
        }
    }

    void Awake()
    {
        this.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName;
        this.GetComponent<MeshRenderer>().sortingOrder = SortingOrder;
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            outlineColor.a = textMesh.color.a * textMesh.color.a;
            // copy attributes
            for (int i = 0; i < transform.childCount; i++)
            {
                TextMesh other = transform.GetChild(i).GetComponent<TextMesh>();
                other.color = outlineColor;
                other.text = textMesh.text;
                other.alignment = textMesh.alignment;
                other.anchor = textMesh.anchor;
                other.characterSize = textMesh.characterSize;
                other.font = textMesh.font;
                other.fontSize = textMesh.fontSize;
                other.fontStyle = textMesh.fontStyle;
                other.richText = textMesh.richText;
                other.tabSize = textMesh.tabSize;
                other.lineSpacing = textMesh.lineSpacing;
                other.offsetZ = textMesh.offsetZ;
                bool doublePixel = resolutionDependant && (Screen.width > doubleResolution || Screen.height > doubleResolution);
                Vector3 pixelOffset = GetOffset(i) * (doublePixel ? 2.0f * pixelSize : pixelSize);
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPoint + pixelOffset);
                other.transform.position = worldPoint;
                MeshRenderer otherMeshRenderer = transform.GetChild(i).GetComponent<MeshRenderer>();
                otherMeshRenderer.sortingLayerID = meshRenderer.sortingLayerID;
                otherMeshRenderer.sortingLayerName = meshRenderer.sortingLayerName;
                otherMeshRenderer.sortingOrder = meshRenderer.sortingOrder;
            }
        }
    }

    Vector3 GetOffset(int i)
    {
        switch (i % 8)
        {
            case 0: return new Vector3(0, 1, 0);
            case 1: return new Vector3(1, 1, 0);
            case 2: return new Vector3(1, 0, 0);
            case 3: return new Vector3(1, -1, 0);
            case 4: return new Vector3(0, -1, 0);
            case 5: return new Vector3(-1, -1, 0);
            case 6: return new Vector3(-1, 0, 0);
            case 7: return new Vector3(-1, 1, 0);
            default: return Vector3.zero;
        }
    }
}
