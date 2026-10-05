// Test doubles for texture identity and the two real RimWorld 1.6 icon API signatures.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public class Object
    {
        public static T Instantiate<T>(T source) where T : Object => (T)(object)((Texture2D)(object)source).Copy();
        public static void Destroy(Object value) { }
    }
    public class Texture : Object { public int width, height; public string name; }
    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Clamp }
    public enum FilterMode { Bilinear }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r=r; this.g=g; this.b=b; this.a=a; }
        public static Color white => new Color(1,1,1,1);
        public static Color operator *(Color x, Color y) => new Color(x.r*y.r,x.g*y.g,x.b*y.b,x.a*y.a);
        public static Color Lerp(Color x, Color y, float t) => new Color(x.r+(y.r-x.r)*t,x.g+(y.g-x.g)*t,x.b+(y.b-x.b)*t,x.a+(y.a-x.a)*t);
    }
    public struct Vector2 { public float x,y; public Vector2(float x,float y) { this.x=x; this.y=y; } }
    public class Texture2D : Texture
    {
        private Color[] pixels;
        public bool isReadable = true;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Texture2D(int w,int h,TextureFormat format=TextureFormat.RGBA32,bool mipChain=false)
        { width=w; height=h; pixels=Enumerable.Repeat(Color.white,w*h).ToArray(); }
        public Color[] GetPixels() => pixels.ToArray();
        public void SetPixels(Color[] value) => pixels=value.ToArray();
        public void Apply(bool mip=false,bool unreadable=false) { isReadable=!unreadable; }
        // Resolution mismatch tests use a constant mask, independent of the sampling algorithm.
        public Color GetPixelBilinear(float u,float v) => pixels[(int)(v*height)*width+(int)(u*width)];
        public Texture2D Copy() { var t=new Texture2D(width,height) { name=name,filterMode=filterMode };t.SetPixels(pixels);return t; }
    }
    public class Shader { }
    public class Material
    {
        public Texture mainTexture;
        public Color color, secondary;
        public Texture mask;
        public bool HasProperty(int id) => mask!=null;
        public Texture GetTexture(int id) => mask;
        public Color GetColor(int id) => secondary;
    }
}
namespace CharacterEditor
{
    internal static class TextureTool
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static UnityEngine.Texture2D GetTexture(Verse.ThingDef t,int stackCount=1,Verse.ThingStyleDef tsd=null,Verse.Rot4 rotation=default)
            => tsd?.UIIcon ?? (UnityEngine.Texture2D)t.graphic.MatSingle.mainTexture;
    }
}
namespace Verse
{
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Class)] public class StaticConstructorOnStartupAttribute : Attribute { }
    public static class Log { public static void Error(string text) => throw new Exception(text); }
    public static class ShaderPropertyIDs { public const int MaskTex=1,ColorTwo=2; }
    public enum TextureAtlasGroup { Item }
    public struct Rot4 { }
    public class GraphicData { public string texPath; }
    public struct GraphicRequest
    {
        public Texture2D texture; public string path,maskPath; public Shader shader;
        public Vector2 drawSize; public Color color,colorTwo; public GraphicData graphicData;
    }
    public class Graphic
    {
        public string path,maskPath; public Vector2 drawSize; public GraphicData data;
        public virtual Material MatSingle => null;
        public virtual void Init(GraphicRequest req) { }
        public virtual void TryInsertIntoAtlas(TextureAtlasGroup groupKey) { }
        public virtual Graphic GetColoredVersion(Shader s,Color c,Color c2) => null;
    }
    public class Graphic_Single : Graphic
    {
        protected Material mat;
        public override Material MatSingle => mat;
        public override void Init(GraphicRequest req)
        {
            path=req.path;maskPath=req.maskPath;drawSize=req.drawSize;data=req.graphicData;
            mat=new Material { mainTexture=req.texture??ContentFinder<Texture2D>.Get(path),
                mask=ContentFinder<Texture2D>.Get(maskPath),color=req.color,secondary=req.colorTwo };
        }
        public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey) => GlobalTextureAtlasManager.Insert(mat.mainTexture,mat.mask);
    }
    public static class GlobalTextureAtlasManager
    {
        public static Dictionary<Texture,Texture> Masks = new Dictionary<Texture,Texture>();
        public static void Insert(Texture texture,Texture mask)
        {
            if (Masks.TryGetValue(texture,out var previous)&&previous!=mask) throw new Exception("Same texture with 2 different masks");
            Masks[texture]=mask;
        }
    }
    public static class GraphicDatabase
    {
        public static T Get<T>(string path,Shader shader,Vector2 size,Color color,Color colorTwo,GraphicData data,string maskPath) where T:Graphic,new()
        { var g=new T();g.Init(new GraphicRequest { path=path,shader=shader,drawSize=size,color=color,colorTwo=colorTwo,graphicData=data,maskPath=maskPath });return g; }
    }
    public static class ContentFinder<T> { public static Dictionary<string,T> Values=new Dictionary<string,T>();public static T Get(string path)=>Values[path]; }
    public static class TextureAtlasHelper { public static Texture2D MakeReadableTextureInstance(Texture2D source)=>source.Copy(); }
    public static class DefDatabase<T> { public static List<T> AllDefsListForReading=new List<T>(); }
    public class ThingDef { public string defName,uiIconPath; public Graphic graphic;public GraphicData graphicData;public Texture2D uiIcon;public Color uiIconColor;public Material uiIconMaterial; }
    public class ThingStyleDef { public Texture2D UIIcon; }
    public class Thing { public ThingDef def;public Texture2D UIIconOverride;public ThingStyleDef StyleDef; }
    public static class LongEventHandler
    {
        public static List<Action> Queued=new List<Action>();
        public static void ExecuteWhenFinished(Action action)=>Queued.Add(action);
    }
    public static class Widgets
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Texture2D GetIconFor(ThingDef thingDef,out Material material,ThingDef stuffDef=null,ThingStyleDef thingStyleDef=null,int? graphicIndexOverride=null)
        { material=thingDef.uiIconMaterial;return thingStyleDef?.UIIcon??thingDef.uiIcon; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Texture2D GetIconFor(ThingDef thingDef)
        { return GetIconFor(thingDef,out var material); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Texture GetIconFor(Thing thing,Vector2 size,Rot4? rot,bool stackOfOne,out float scale,out float angle,out Vector2 iconProportions,out Color color,out Material material)
        {
            scale=1;angle=0;iconProportions=size;material=thing.def.graphic.MatSingle;color=material.color;
            return thing.UIIconOverride??thing.StyleDef?.UIIcon??material.mainTexture;
        }
    }
}
