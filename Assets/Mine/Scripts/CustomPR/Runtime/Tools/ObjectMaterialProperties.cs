using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ObjectMaterialProperties : MonoBehaviour
{
    [SerializeField]
    Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f;

    static MaterialPropertyBlock block;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(CommonShaderPropertyID.baseColorId, baseColor);
        block.SetFloat(CommonShaderPropertyID.cutoffId, cutoff);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
