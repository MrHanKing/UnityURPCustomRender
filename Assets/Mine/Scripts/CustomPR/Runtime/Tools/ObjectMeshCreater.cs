using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjectMeshCreater : MonoBehaviour
{
    [SerializeField]
    Mesh mesh = default;
    [SerializeField]
    Material material = default;
    [SerializeField]
    LightProbeProxyVolume lightProbeProxyVolume = null;

    // 生成1024个渲染网格
    // 变换矩阵
    Matrix4x4[] matrices = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    float[] metallic = new float[1023], smoothness = new float[1023];
    MaterialPropertyBlock block;

    private void Awake()
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f, Quaternion.identity, Vector3.one
            );
            baseColors[i] = new Vector4(Random.value, Random.value, Random.value, 1f);
            metallic[i] = Random.value < 0.25f ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(CommonShaderPropertyID.baseColorId, baseColors);
            block.SetFloatArray(CommonShaderPropertyID.metallicId, metallic);
            block.SetFloatArray(CommonShaderPropertyID.smoothnessId, smoothness);

            if (!lightProbeProxyVolume)
            {
                // 生成光照探针 并计算和传递数据
                var positions = new Vector3[1023];
                for (int i = 0; i < matrices.Length; i++)
                {
                    positions[i] = matrices[i].GetColumn(3);
                }
                var lightProbes = new SphericalHarmonicsL2[1023];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(
                    positions, lightProbes, null
                );

                block.CopySHCoefficientArraysFrom(lightProbes);
            }
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, 1023, block,
        ShadowCastingMode.On, true, 0, null,
        lightProbeProxyVolume ? LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided,
        lightProbeProxyVolume);
    }
}
