#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

// 表面数据结构 方便管理和可读 不影响运行效率 shader编译器是高度优化的 会recode我们的代码
struct Surface{
    float3 normal;
    float3 color;
    float alpha;
};

#endif