using System;
using System.Linq;
using System.Runtime.CompilerServices;
using KoRimUtility.MedicalIcons;
using UnityEngine;
using Verse;

internal static class Program
{
    private static int checks;
    private static void Check(bool value,string message) { if(!value)throw new Exception(message);checks++; }
    private static void Near(float expected,float actual) => Check(Math.Abs(expected-actual)<0.00001f,$"Expected {expected}, got {actual}");
    private static Texture2D Texture(string name,Color color,int size=2)
    { var t=new Texture2D(size,size) { name=name };t.SetPixels(Enumerable.Repeat(color,size*size).ToArray());ContentFinder<Texture2D>.Values[name]=t;return t; }
    private static GraphicRequest Request(string path,string mask) => new GraphicRequest {
        path=path,maskPath=mask,shader=new Shader(),drawSize=new Vector2(1,1),color=Color.white,colorTwo=new Color(.25f,.25f,.25f),graphicData=new GraphicData { texPath=path } };
    private static ThingDef Def(string name,Graphic graphic,string path)
    { var d=new ThingDef { defName=name,graphic=graphic,graphicData=new GraphicData { texPath=path },uiIcon=(Texture2D)graphic.MatSingle.mainTexture,uiIconColor=graphic.MatSingle.color };DefDatabase<ThingDef>.AllDefsListForReading.Add(d);return d; }
    public static void Main()
    {
        var shaded=MedicalIconTexture.Shade(new Color(1,.8f,.4f,.25f),new Color(.5f,.5f,0),new Color(.2f,.4f,.6f),new Color(.1f,.2f,.3f));
        Near(.33f,shaded.r);Near(.336f,shaded.g);Near(.208f,shaded.b);Near(.25f,shaded.a);
        const string path="BionicIcons/Boxes/Default";
        var original=Texture(path,Color.white);
        Texture("male",new Color(0,1,0));Texture("female",new Color(.5f,.5f,0));Texture("lung",new Color(1,0,0));
        var male=new Graphic_MedicalIcon();male.Init(Request(path,"male"));
        var female=new Graphic_MedicalIcon();female.Init(Request(path,"female"));
        var repeated=new Graphic_MedicalIcon();repeated.Init(Request(path,"male"));
        Check(male.MatSingle.mainTexture!=original,"Do not reuse the upstream atlas key");
        Check(male.MatSingle.mainTexture!=female.MatSingle.mainTexture,"Different masks need different atlas keys");
        Check(male.MatSingle.mainTexture==repeated.MatSingle.mainTexture,"The same base/mask pair should be cached");
        male.TryInsertIntoAtlas(TextureAtlasGroup.Item);female.TryInsertIntoAtlas(TextureAtlasGroup.Item);
        var lung=new Graphic_Single();lung.Init(Request(path,"lung"));lung.TryInsertIntoAtlas(TextureAtlasGroup.Item);
        Check(GlobalTextureAtlasManager.Masks.Count==3,"Each mask must keep its own atlas entry");
        Check(lung.MatSingle.mainTexture==original,"Existing organ world graphics remain unchanged");
        Near(1,original.GetPixels()[0].r);
        var recolored=male.GetColoredVersion(new Shader(),new Color(.8f,.7f,.6f),new Color(.1f,.2f,.3f));
        Check(recolored is Graphic_MedicalIcon&&recolored.maskPath=="male","Recoloring must retain the mask and graphic class");
        Check(recolored.MatSingle.mainTexture==male.MatSingle.mainTexture,"Recoloring may share the same base/mask atlas key");
        Texture("HD",Color.white,4);
        var hd=new Graphic_MedicalIcon();hd.Init(Request("HD","male"));hd.TryInsertIntoAtlas(TextureAtlasGroup.Item);
        Check(GlobalTextureAtlasManager.Masks.Count==3,"Mismatched HD texture/mask sizes must not enter the atlas");
        Near(.25f,MedicalIconTexture.Bake(hd.MatSingle).GetPixels()[5].r);
        var maleDef=Def("Penis",male,path);var femaleDef=Def("Vagina",female,path);var lungDef=Def("Lung",lung,path);
        var other=Def("Unrelated",lung,"Other/Texture");
        var custom=Def("ExplicitIcon",lung,path);custom.uiIconPath="Custom/Path";custom.uiIcon=Texture("custom",new Color(.7f,.1f,.9f));
        RuntimeHelpers.RunClassConstructor(typeof(MedicalIconUI).TypeHandle);
        Check(maleDef.uiIcon==male.MatSingle.mainTexture,"UI refresh must wait for other startup graphics changes");
        foreach(var action in LongEventHandler.Queued)action();
        foreach(var d in new[]{maleDef,femaleDef,lungDef})
        {
            Check(d.uiIcon!=d.graphic.MatSingle.mainTexture,"UI needs the composed image, not the case texture");
            Check(d.uiIconMaterial==null,"A composed UI icon needs no mask shader");Near(1,d.uiIconColor.r);
            Check(Widgets.GetIconFor(d)==d.uiIcon,"Texture-only API must retain the pictogram");
            Check(Widgets.GetIconFor(d,out var material)==d.uiIcon&&material==null,"Definition icon API must avoid double masking");
            var instance=new Thing { def=d };
            var tex=Widgets.GetIconFor(instance,new Vector2(64,64),null,false,out var scale,out var angle,out var proportions,out var color,out material);
            Check(tex==d.uiIcon&&material==null,"Thing instance UI must use the composed image");Near(1,color.r);
        }
        Near(.25f,maleDef.uiIcon.GetPixels()[0].r);Near(.625f,femaleDef.uiIcon.GetPixels()[0].r);
        Check(other.uiIcon==original,"Unrelated graphics must not be changed");Check(custom.uiIcon==ContentFinder<Texture2D>.Get("custom"),"Explicit UI textures must be preserved");
        var styled=new ThingStyleDef { UIIcon=custom.uiIcon };
        Check(Widgets.GetIconFor(maleDef,out var ignored,null,styled)==custom.uiIcon,"Style UI overrides must be preserved");
        Check(CharacterEditor.TextureTool.GetTexture(maleDef)==maleDef.uiIcon,"CE's raw graphic path lookup must retain the pictogram");
        Check(CharacterEditor.TextureTool.GetTexture(lungDef)==lungDef.uiIcon,"CE must retain existing organ pictograms");
        Check(CharacterEditor.TextureTool.GetTexture(maleDef,1,styled)==custom.uiIcon,"CE style overrides must be preserved");
        var overridden=new Thing { def=maleDef,UIIconOverride=custom.uiIcon };
        Check(Widgets.GetIconFor(overridden,new Vector2(64,64),null,false,out _,out _,out _,out _,out _)==custom.uiIcon,"Instance UI overrides must be preserved");
        var saved=maleDef.uiIcon;MedicalIconUI.Refresh();Check(saved==maleDef.uiIcon,"Refresh must reuse cached composed icons");
        Console.WriteLine($"Medical icon compatibility: {checks} checks passed (atlas identity, shader colors and real Harmony UI hooks; Unity test doubles).");
    }
}
