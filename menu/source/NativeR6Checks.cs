using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Drawing.Drawing2D;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeR6Checks(string[] args)
    {
        Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;
        Frame(EventType.Repaint);Check(!Fields.ContainsKey("MzWebId")&&!Fields.ContainsKey("MzMemoryPaused"),"no WebView state and no feature pause flag in final bytecode");
        for(int i=0;i<12;i++){
            Click(Get<float>("MzBubbleX")+44,Get<float>("MzBubbleY")+22);Check(Get<bool>("MzOpen"),"repeat badge opens "+i);
            Click(Get<float>("MzPanelX")+349,Get<float>("MzPanelY")+34);Check(!Get<bool>("MzOpen")&&GUIUtility.hotControl==0,"repeat close releases capture "+i);
        }
        float bx=Get<float>("MzBubbleX"),by=Get<float>("MzBubbleY");
        Frame(EventType.MouseDown,bx+44,by+22);Frame(EventType.MouseDrag,bx+144,by+122);Frame(EventType.MouseUp,bx+144,by+122);
        Check(!Get<bool>("MzOpen")&&Math.Abs(Get<float>("MzBubbleX")-bx-100)<.01,"badge drag moves without toggling");
        bx=Get<float>("MzBubbleX");by=Get<float>("MzBubbleY");Frame(EventType.TouchDown,bx+44,by+22);Frame(EventType.TouchUp,bx+44,by+22);Check(Get<bool>("MzOpen")&&GUIUtility.hotControl==0,"touch events map to owned menu input");
        Set("MzPanelX",200f);Set("MzPanelY",20f);Frame(EventType.Repaint);
        var sw=Get<bool[]>("MzSwitch");License(Record(Get<string>("MzDeviceHash")));Set("MzTab",0);bool was=sw[0];
        Frame(EventType.MouseUp,240,152);Check(sw[0]==was,"stray up cannot toggle a row");
        Frame(EventType.MouseDown,2,2);Frame(EventType.MouseUp,240,152);Check(sw[0]==was,"outside game press cannot activate menu on release");
        Click(240,152);Check(sw[0]!=was,"owned row down/up toggles once");
        Frame(EventType.MouseDown,230,44);Frame(EventType.MouseDrag,480,144);Frame(EventType.MouseUp,480,144);Check(Get<float>("MzPanelX")>200&&GUIUtility.hotControl==0,"header drag captures and releases outside header");
        Frame(EventType.MouseDown,Get<float>("MzPanelX")+30,Get<float>("MzPanelY")+20);Frame(EventType.Ignore);Check(!Get<bool>("MzPointerCaptured")&&Get<int>("MzDragMode")==0&&GUIUtility.hotControl==0,"cancel releases drag and hot control");
        GUIUtility.hotControl=777;bool opened=Get<bool>("MzOpen");Click(bx+44,by+22);Check(Get<bool>("MzOpen")==opened&&GUIUtility.hotControl==777,"never takes or releases another GUI owner's capture");GUIUtility.hotControl=0;
        var roster=new ArrayList{Enemy(),Enemy()};var bot=Enemy();bot.IsClientBot=true;roster.Add(bot);
        NativeVisibleCountChecks(args);
        Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[5]=true;sw[10]=true;sw[11]=true;
        Camera.main=new Camera {gameObject=new GameObject("r6-camera")};var local=Enemy();local.Local=true;local.Firing=true;COW.GameFacade.Local=local;COW.GameFacade.Match=new JMAGGLCNGIG {Players=roster};Set("MzFov",350f);Time.Frame++;Render();
        var mesh=Get<Mesh>("MzMesh");Run("MzHeapObserve",134217728L);Run("MzHeapObserve",218103808L);Time.Frame++;Run("MzRunAim");Render();Check(Get<int>("MzMemoryPressure")==2&&Get<int>("MzQuadCount")>0&&local.AimCalls>0,"heap pressure retains working ESP and aim");
        Check(Get<float>("MzMemoryEspDelay")>=.09f,"pressure bounds sampling cost without dropping features");
        Run("MzIdleMemory",false,0f);Run("MzIdleMemory",false,5.1f);Check(mesh.Destroyed&&Get<Mesh>("MzMesh")==null&&!Texture2D.whiteTexture.Destroyed,"idle cleanup frees only owned native resources");
        Run("MzIdleMemory",true,6f);Time.Now=6;Render();Check(Get<Mesh>("MzMesh")!=null&&!System.Object.ReferenceEquals(mesh,Get<Mesh>("MzMesh")),"re-enable rebuilds released resources");
        Set("MzHeapFloor",0L);Set("MzHeapCollectBase",0L);Set("MzHeapNext",0f);Set("MzHeapCollectAfter",0f);Set("MzHeapCollections",0);
        UnityEngine.Profiling.Profiler.ReportedHeap=1073741824;Time.Now=10;Run("MzMemoryTick");Time.Now=21;Run("MzMemoryTick");Check(Get<int>("MzHeapCollections")==0,"stable large game heap does not force useless full collections");
        UnityEngine.Profiling.Profiler.ReportedHeap=1107296256;Time.Now=22;Run("MzMemoryTick");Check(Get<int>("MzHeapCollections")==1,"fresh managed growth triggers bounded collection");Time.Now=40;Run("MzMemoryTick");Check(Get<int>("MzHeapCollections")==1,"unchanged post-collection heap does not repeatedly collect");UnityEngine.Profiling.Profiler.ReportedHeap=0;
        Set("MzPanelX",130f);Set("MzPanelY",18f);Set("MzBubbleX",24f);Set("MzBubbleY",96f);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",0);Set("MzMemoryPressure",0);Set("MzMemoryEspDelay",.04f);Time.Now=41;License(Record(Get<string>("MzDeviceHash")));Set("MzTab",0);Set("MzTabAnim",0f);for(int settle=0;settle<18;settle++)Frame(EventType.Repaint);
        Check(GUI.Calls.All(d=>d.Text==null||(!d.Text.Contains("HTML")&&!d.Text.Contains("Bộ nhớ cao"))),"native UI contains no HTML fallback or memory pause banner");
        string output=Path.GetDirectoryName(args[0]);Snapshot(Path.Combine(output,"native-r6-menu.svg"));NativePng(Path.Combine(output,"native-r6-menu.png"));
        Set("MzOpen",false);Set("MzOpenAnim",0f);COW.GameFacade.Match.Players.Clear();Run("MzResetCounts");Frame(EventType.Repaint);Set("MzNextEsp",0f);Frame(EventType.Repaint);NativePng(Path.Combine(output,"native-r6-clear.png"));
        NativeR6Profile(output);
        Console.WriteLine("PASS: "+Checks+" checks; native touch/capture/drag/cancel, Clear/count caching, no feature pause, adaptive collection and resource lifecycle.");
    }
    static void NativeR6Profile(string output)
    {
        var results=new System.Collections.Generic.List<object>();var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);for(int i=0;i<11;i++)sw[i]=true;sw[13]=true;
        COW.GameFacade.Match=new JMAGGLCNGIG();for(int i=0;i<16;i++)COW.GameFacade.Match.Players.Add(Enemy((i%8-4)*.1f));GUI.Record=false;AppDomain.MonitoringIsEnabled=true;long previous=0;
        for(int mode=0;mode<2;mode++){
            Set("MzMemoryEspDelay",mode==0?.04f:.10f);Time.Now=100;Render();GC.Collect();long start=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;int projections=Camera.ProjectionCalls;
            for(int frame=0;frame<1200;frame++){Time.Now=100+(frame+1)/60f;Run("MzRender");}
            long allocated=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize-start;results.Add(new {mode=mode==0?"normal":"adaptive-pressure",frames=1200,simulatedSeconds=20,allocatedBytes=allocated,bytesPerSecond=allocated/20,projections=Camera.ProjectionCalls-projections,deviceVerified=false});
            if(mode==0)previous=allocated;else Check(allocated<previous,"adaptive pressure reduces dense ESP allocation without disabling its features");
        }
        GUI.Record=true;File.WriteAllText(Path.Combine(output,"native-r6-profile.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(results));
    }
    static System.Drawing.Drawing2D.GraphicsPath NativeRound(System.Drawing.RectangleF r,float radius)
    {
        var p=new System.Drawing.Drawing2D.GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
        if(d<=0){p.AddRectangle(r);return p;}p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    static void NativePng(string file)
    {
        // Standalone visualization of VM draw commands, not a device screenshot.
        using(var bmp=new System.Drawing.Bitmap(Screen.width,Screen.height))using(var g=System.Drawing.Graphics.FromImage(bmp)){
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using(var bg=new System.Drawing.Drawing2D.LinearGradientBrush(new System.Drawing.Rectangle(0,0,Screen.width,Screen.height),System.Drawing.Color.FromArgb(28,42,48),System.Drawing.Color.FromArgb(12,20,29),30))g.FillRectangle(bg,0,0,Screen.width,Screen.height);
            foreach(var d in GUI.Calls){var r=d.Rect;if(r.width<=0||r.height<=0)continue;var saved=g.Save();float sx=d.Matrix.Scale.x==0?1:d.Matrix.Scale.x,sy=d.Matrix.Scale.y==0?1:d.Matrix.Scale.y;g.TranslateTransform(d.Matrix.Position.x,d.Matrix.Position.y);g.RotateTransform(d.Matrix.Angle);g.ScaleTransform(sx,sy);var c=d.Color;
                using(var brush=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((int)(Math.Max(0,Math.Min(1,c.a))*255),(int)(Math.Max(0,Math.Min(1,c.r))*255),(int)(Math.Max(0,Math.Min(1,c.g))*255),(int)(Math.Max(0,Math.Min(1,c.b))*255)))){
                    var rect=new System.Drawing.RectangleF(r.x,r.y,r.width,r.height);
                    if(d.Text!=null){using(var font=new System.Drawing.Font("Segoe UI",d.FontSize>0?d.FontSize:12,d.FontName!=null&&d.FontName.Contains("Demi")?System.Drawing.FontStyle.Bold:System.Drawing.FontStyle.Regular,System.Drawing.GraphicsUnit.Pixel))using(var fmt=new System.Drawing.StringFormat()){
                        fmt.Alignment=d.Align==TextAnchor.MiddleCenter?System.Drawing.StringAlignment.Center:System.Drawing.StringAlignment.Near;fmt.LineAlignment=d.Align==TextAnchor.UpperLeft?System.Drawing.StringAlignment.Near:System.Drawing.StringAlignment.Center;if(!d.Wrap)fmt.FormatFlags=System.Drawing.StringFormatFlags.NoWrap;g.DrawString(d.Text,font,brush,rect,fmt);
                    }}else if(d.Texture==null||d.Texture==Texture2D.whiteTexture){using(var path=NativeRound(rect,d.Radius))g.FillPath(brush,path);}
                    else if(d.Texture is Texture2D){if(d.Radius>0){using(var clip=NativeRound(rect,d.Radius))g.SetClip(clip,System.Drawing.Drawing2D.CombineMode.Intersect);}NativeTexture(g,d,rect);}
                }g.Restore(saved);
            }
            using(var f=new System.Drawing.Font("Segoe UI",11,System.Drawing.GraphicsUnit.Pixel))using(var b=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110,150,160)))g.DrawString("MIZZE  /  OFFLINE C# BYTECODE PREVIEW",f,b,22,Screen.height-28);
            bmp.Save(file,System.Drawing.Imaging.ImageFormat.Png);
        }
    }
    static void NativeTexture(System.Drawing.Graphics graphics,Draw draw,System.Drawing.RectangleF rectangle)
    {
        var texture=draw.Texture as Texture2D;if(texture==null||texture.Pixels==null)return;int width=texture.Width,height=texture.Height;if(width<=0||height<=0||texture.Pixels.Length<width*height)return;
        using(var image=new System.Drawing.Bitmap(width,height))using(var attrs=new System.Drawing.Imaging.ImageAttributes()){
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){var p=texture.Pixels[(height-1-y)*width+x];image.SetPixel(x,y,System.Drawing.Color.FromArgb((int)(Math.Max(0,Math.Min(1,p.a))*255),(int)(Math.Max(0,Math.Min(1,p.r))*255),(int)(Math.Max(0,Math.Min(1,p.g))*255),(int)(Math.Max(0,Math.Min(1,p.b))*255)));}
            var c=draw.Color;var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix00=c.r;matrix.Matrix11=c.g;matrix.Matrix22=c.b;matrix.Matrix33=c.a;attrs.SetColorMatrix(matrix);
            var uv=draw.Uv;if(uv.width<=0||uv.height<=0)uv=new Rect(0,0,1,1);
            graphics.DrawImage(image,System.Drawing.Rectangle.Round(rectangle),uv.x*width,(1-uv.y-uv.height)*height,uv.width*width,uv.height*height,System.Drawing.GraphicsUnit.Pixel,attrs);
        }
    }
}
