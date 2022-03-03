using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ObjectMaterialProperties : MonoBehaviour
{
    [SerializeField]
    Color baseColor = Color.white;
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
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
