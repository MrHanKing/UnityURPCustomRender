using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ObjectMaterialProperties : MonoBehaviour
{
    [SerializeField]
    Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f, metallic = 0f, smoothness = 0f;
    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.black;

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
        block.SetFloat(CommonShaderPropertyID.metallicId, metallic);
        block.SetFloat(CommonShaderPropertyID.smoothnessId, smoothness);
        block.SetColor(CommonShaderPropertyID.emissionColorId, emissionColor);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
