using UnityEngine;
using UnityEngine.UI;
namespace HumanEye
{
    [AddComponentMenu("UI/Observatory Icon")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudIcon : MaskableGraphic
    {
        public enum Symbol { Home, Chat, Play, Pause, Help, Review, Settings, Scan, Eye, Trace, Freeze, Replay, Book, Chevron, ZoomIn, ZoomOut, TurnLeft, TurnRight, Sound, SoundOff, List }   // append only: symbols are saved by index
        public Symbol symbol;
        [Range(1,4)] public float stroke = 2;
        VertexHelper mesh;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh; vh.Clear();
            switch(symbol)
            {
                case Symbol.Home: Path(.1f,.48f,.5f,.85f,.9f,.48f); Path(.23f,.52f,.23f,.15f,.42f,.15f,.42f,.42f,.6f,.42f,.6f,.15f,.78f,.15f,.78f,.52f); break;
                case Symbol.Chat: Path(.15f,.8f,.85f,.8f,.85f,.32f,.42f,.32f,.24f,.12f,.24f,.32f,.15f,.32f,.15f,.8f); Circle(.32f,.56f,.025f); Circle(.5f,.56f,.025f); Circle(.68f,.56f,.025f); break;
                case Symbol.Play: Path(.3f,.18f,.8f,.5f,.3f,.82f,.3f,.18f); break;
                case Symbol.Pause: Path(.33f,.18f,.33f,.82f); Path(.66f,.18f,.66f,.82f); break;
                case Symbol.Help: Path(.28f,.69f,.34f,.8f,.57f,.83f,.71f,.71f,.68f,.58f,.5f,.46f,.5f,.35f); Circle(.5f,.16f,.025f); break;
                case Symbol.Review: Path(.23f,.12f,.23f,.87f,.77f,.87f,.77f,.12f,.23f,.12f); Path(.35f,.69f,.65f,.69f); Path(.35f,.51f,.65f,.51f); Path(.35f,.33f,.62f,.33f); break;
                case Symbol.Settings: Circle(.5f,.5f,.17f); Circle(.5f,.5f,.34f); for(int k=0;k<8;k++) { float a=k*Mathf.PI/4; Line(new Vector2(.5f,.5f)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.33f,new Vector2(.5f,.5f)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.45f); } break;
                case Symbol.Scan: Path(.1f,.64f,.1f,.88f,.34f,.88f); Path(.66f,.88f,.9f,.88f,.9f,.64f); Path(.9f,.36f,.9f,.12f,.66f,.12f); Path(.34f,.12f,.1f,.12f,.1f,.36f); Circle(.5f,.5f,.18f); break;
                case Symbol.Eye: Path(.04f,.5f,.24f,.72f,.5f,.81f,.76f,.72f,.96f,.5f,.76f,.28f,.5f,.19f,.24f,.28f,.04f,.5f); Circle(.5f,.5f,.18f); break;
                case Symbol.Trace: Path(.2f,.2f,.42f,.49f,.65f,.27f,.8f,.76f,.48f,.84f,.42f,.49f); Circle(.2f,.2f,.08f); Circle(.65f,.27f,.07f); Circle(.8f,.76f,.08f); Circle(.48f,.84f,.06f); break;
                case Symbol.Freeze: for(int k=0;k<6;k++) { float a=k*Mathf.PI/3; var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)); var n=new Vector2(-d.y,d.x); var c=new Vector2(.5f,.5f); Line(c,c+d*.43f); Line(c+d*.28f,c+d*.15f+n*.12f); Line(c+d*.28f,c+d*.15f-n*.12f); } break;
                case Symbol.Replay: case Symbol.TurnLeft: Arc(.5f,.48f,.34f,55,360); Path(.54f,.91f,.33f,.79f,.55f,.66f); break;
                case Symbol.TurnRight: Arc(.5f,.48f,.34f,-180,125); Path(.45f,.91f,.67f,.79f,.45f,.66f); break;
                case Symbol.Book: Path(.1f,.18f,.1f,.82f,.3f,.88f,.5f,.78f,.7f,.88f,.9f,.82f,.9f,.18f,.7f,.24f,.5f,.14f,.3f,.24f,.1f,.18f); Path(.5f,.14f,.5f,.78f); break;
                case Symbol.Chevron: Path(.36f,.18f,.65f,.5f,.36f,.82f); break;
                case Symbol.Sound: case Symbol.SoundOff:
                    Path(.12f,.38f,.3f,.38f,.52f,.18f,.52f,.82f,.3f,.62f,.12f,.62f,.12f,.38f);
                    if (symbol == Symbol.Sound) { Arc(.52f,.5f,.17f,-50,50); Arc(.52f,.5f,.31f,-50,50); } else { Path(.66f,.37f,.9f,.63f); Path(.66f,.63f,.9f,.37f); }
                    break;
                case Symbol.List: for (int k = 0; k < 3; k++) { float y = .74f - k * .24f; Circle(.2f, y, .045f); Path(.36f, y, .86f, y); } break;
                case Symbol.ZoomIn: case Symbol.ZoomOut: Circle(.43f,.58f,.29f); Path(.65f,.36f,.9f,.1f); Path(.28f,.58f,.58f,.58f); if(symbol==Symbol.ZoomIn) Path(.43f,.43f,.43f,.73f); break;
            }
        }
        void Path(params float[] pts) { for(int i=2;i<pts.Length;i+=2) Line(new Vector2(pts[i-2],pts[i-1]),new Vector2(pts[i],pts[i+1])); }
        void Circle(float x,float y,float radius) => Arc(x,y,radius,0,360);
        void Arc(float x,float y,float radius,float from,float to)
        {
            int steps=Mathf.CeilToInt(Mathf.Abs(to-from)/12); var previous=new Vector2(x,y)+new Vector2(Mathf.Cos(from*Mathf.Deg2Rad),Mathf.Sin(from*Mathf.Deg2Rad))*radius;
            for(int k=1;k<=steps;k++) { float a=Mathf.Lerp(from,to,k/(float)steps)*Mathf.Deg2Rad; var p=new Vector2(x,y)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius; Line(previous,p); previous=p; }
        }
        void Line(Vector2 a,Vector2 b)
        {
            var r=rectTransform.rect; a=r.min+Vector2.Scale(a,r.size); b=r.min+Vector2.Scale(b,r.size); var d=b-a; if(d.sqrMagnitude<.0001f)return;
            var n=new Vector2(-d.y,d.x).normalized*stroke*.5f; int start=mesh.currentVertCount;
            foreach(var p in new[]{a-n,a+n,b+n,b-n}) { var v=UIVertex.simpleVert; v.position=p; v.color=color; mesh.AddVert(v); }
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start+2,start+3,start);
        }
    }
}
