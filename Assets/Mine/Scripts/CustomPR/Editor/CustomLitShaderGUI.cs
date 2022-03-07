using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomLitShaderGUI : ShaderGUI
{
    #region 定义
    enum ShadowMode
    {
        On,// 透明度
        Clip,// 裁剪
        Dither,// 抖动
        Off,
    }
    #endregion

    #region 属性设置
    bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    bool PremultiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTI_ALPHA", value);
    }
    BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }
    BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }
    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }
    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in materials)
            {
                m.renderQueue = (int)value;
            }
        }
    }
    ShadowMode Shadows
    {
        set
        {
            if (SetProperty("_Shadows", (float)value))
            {
                SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
            }
        }
    }
    #endregion

    MaterialEditor editor;
    MaterialProperty[] properties;
    Object[] materials;

    bool showPresets;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUI.BeginChangeCheck();

        base.OnGUI(materialEditor, properties);
        this.editor = materialEditor;
        this.properties = properties;
        this.materials = materialEditor.targets;

        BakedEmission();

        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "预制模式", true);
        if (showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
        }
    }

    void SetProperty(string name, string keyword, bool value)
    {
        SetProperty(name, value ? 1f : 0f);
        SetKeyword(keyword, value);
    }

    // 属性
    private bool SetProperty(string name, float value)
    {
        MaterialProperty target = FindProperty(name, properties);
        var oldValue = target.floatValue;
        target.floatValue = value;

        return oldValue != value;
    }

    // 关键字
    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material m in materials)
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material m in materials)
            {
                m.DisableKeyword(keyword);
            }
        }
    }

    private bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }

    private void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
            Shadows = ShadowMode.On;
        }
    }

    private void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
            Shadows = ShadowMode.Clip;
        }
    }

    private void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
            Shadows = ShadowMode.Dither;
        }
    }

    private void TransparentPreset()
    {
        if (PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
            Shadows = ShadowMode.Clip;
        }
    }

    private void SetShadowCasterPass()
    {
        MaterialProperty shadows = FindProperty("_Shadows", properties, false);
        if (shadows == null || shadows.hasMixedValue)
        {
            return;
        }
        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach (Material m in materials)
        {
            m.SetShaderPassEnabled("ShadowCaster", enabled);
        }
    }
    // 自发光相关配置
    private void BakedEmission()
    {
        EditorGUI.BeginChangeCheck();
        editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in this.materials)
            {
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
}
