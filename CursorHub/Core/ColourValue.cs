namespace CursorHub.Core;
internal static class ColourValue
{
    public static uint? Valid(uint? rgb)=>rgb is <=0xFFFFFF?rgb:null;
    public static uint ToColorRef(uint rgb)=>((rgb&255)<<16)|(rgb&0xFF00)|((rgb>>16)&255);
    public static uint FromColorRef(uint bgr)=>ToColorRef(bgr);
}
internal static class PresetPagination
{
    public const int PageSize=24,Limit=128;
    public static int PageCount(int count)=>Math.Max(1,(count+(count<Limit?1:0)+PageSize-1)/PageSize);
    public static int ClampPage(int count,int page)=>Math.Clamp(page,0,PageCount(count)-1);
    public static bool HasAdd(int count,int page)=>count<Limit&&page==count/PageSize;
}
