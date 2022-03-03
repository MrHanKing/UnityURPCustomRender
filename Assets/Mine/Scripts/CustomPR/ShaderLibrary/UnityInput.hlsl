#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;

//投影矩阵 P
float4x4 glstate_matrix_projection;

// 本身不是有效类型，而是取决于目标平台的float4 or half4
real4 unity_WorldTransformParams;

#endif