using UnityEngine;
using UnityEngine.UI;

namespace ArrowFlow
{
    // Native editable artwork for elements absent from the supplied slices.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Arrow Attack/Reference Decoration")]
    public sealed class ReferenceDecoration : MaskableGraphic
    {
        public enum Style { FrameTop, FrameBottom, FrameLeft, FrameRight, YellowButton, GreenButton, PlayIcon }
        public Style style;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect r = rectTransform.rect;
            if (style <= Style.FrameRight)
            {
                // Mitred corners keep their 720x1280 sizes (72 wide, 96 and 101 high) however long a frame side
                // grows on taller or wider screens, so the four sides still meet cleanly.
                float side = Mathf.Clamp01(72f / Mathf.Max(1, r.width)), top = Mathf.Clamp01(96f / Mathf.Max(1, r.height)), bottom = Mathf.Clamp01(101.12f / Mathf.Max(1, r.height));
                Vector2[] points;
                if (style == Style.FrameTop) points = new[] { new Vector2(0,1),new Vector2(1,1),new Vector2(1-side,0),new Vector2(side,0) };
                else if (style == Style.FrameBottom) points = new[] { new Vector2(side,1),new Vector2(1-side,1),new Vector2(1,0),new Vector2(0,0) };
                else if (style == Style.FrameLeft) points = new[] { new Vector2(0,1),new Vector2(1,1-top),new Vector2(1,bottom),new Vector2(0,0) };
                else points = new[] { new Vector2(0,1-top),new Vector2(1,1),new Vector2(1,0),new Vector2(0,bottom) };
                foreach (var p in points) mesh.AddVert(new Vector2(r.xMin+p.x*r.width,r.yMin+p.y*r.height),color,Vector2.zero);
                mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);return;
            }
            if(style == Style.PlayIcon)
            {
                Triangle(mesh,r,new Color32(105,24,0,255));
                Triangle(mesh,new Rect(r.x+2,r.y+2,r.width-5,r.height-4),new Color32(255,146,0,255));
                Triangle(mesh,new Rect(r.x+4,r.y+5,r.width-11,r.height-9),new Color32(255,232,47,255));return;
            }
            bool green=style==Style.GreenButton;
            Round(mesh,r,21,green?new Color32(53,126,0,255):new Color32(183,138,0,255));
            Round(mesh,new Rect(r.x+1.5f,r.y+2,r.width-3,r.height-3),20,green?new Color32(133,217,56,255):new Color32(255,235,156,255));
            Round(mesh,new Rect(r.x+3,r.y+4,r.width-6,r.height-6),18,green?new Color32(51,171,0,255):new Color32(248,198,0,255));
            Round(mesh,new Rect(r.x+3,r.y+r.height*.51f,r.width-6,r.height*.49f-2),18,green?new Color32(112,207,46,255):new Color32(255,226,144,255));
            Round(mesh,new Rect(r.x+10,r.yMax-22,20,13),7,new Color32(255,255,242,230));
            Round(mesh,new Rect(r.x+36,r.yMax-11,2,2),1,new Color32(255,255,242,230));
        }
        void Triangle(VertexHelper mesh,Rect r,Color tint)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(new Vector2(r.xMin,r.yMin),tint*color,Vector2.zero);mesh.AddVert(new Vector2(r.xMin,r.yMax),tint*color,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.center.y),tint*color,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);
        }
        void Round(VertexHelper mesh,Rect r,float radius,Color tint)
        {
            radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);int start=mesh.currentVertCount;
            mesh.AddVert(r.center,tint*color,Vector2.zero);int count=0;
            for(int corner=0;corner<4;corner++)
            {
                Vector2 center=new Vector2(corner<2?r.xMax-radius:r.xMin+radius,corner==0||corner==3?r.yMin+radius:r.yMax-radius);
                for(int i=0;i<=12;i++)
                {
                    float a=(-90+corner*90+i*7.5f)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint*color,Vector2.zero);
                    count++;if(count>1)mesh.AddTriangle(start,start+count-1,start+count);
                }
            }
            mesh.AddTriangle(start,start+count,start+1);
        }
    }
}


