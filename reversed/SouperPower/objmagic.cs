using UnityEngine;
using UnityEngine.Rendering;

public partial class Soup
{
    public class ObjMajic
    {
        public static void DrawBlockedLine(Vector3 start, Vector3 hit, Vector3 end, Color color, Vector3 disp = new(), float width = 0.02f)
        {
            DrawLine(start, hit, color, disp, width);
            // Color blockedColor = new(1 - color.r, 1 - color.g, 1 - color.b, color.a/4); 
            Color blockedColor = Color.red; 
            DrawLine(hit, end, blockedColor, disp, width);
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color, Vector3 disp = new(), float width = 0.02f)
        {
            GameObject line = new("ray");
            line.transform.SetParent(Game.currentLevel.gameObject.transform);
            LineRenderer render = line.AddComponent<LineRenderer>();
            render.startWidth = width;
            render.endWidth = width;
            render.positionCount = 2;
            render.SetPosition(0, start + disp);
            render.SetPosition(1, end + disp);
            Material material = render.material ?? new Material(Shader.Find("Standard"));
            MakeTransluscent(material);
            material.renderQueue = 3001;
            render.material.color = color;
        }

        public static void ShadePureColor(Renderer render, Color color)
        {
            Material material = render.material ?? new Material(Shader.Find("Standard"));
            MakeTransluscent(material);
            material.color = color;
        }

        public static void CreateDummySphere(string name, float radius, Vector3 pos, Color color)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.SetActive(true);
            sphere.transform.SetParent(Game.currentLevel.gameObject.transform, false);
            sphere.transform.localScale = Vector3.one * radius * 2;
            sphere.transform.position = pos;
            sphere.GetComponent<Collider>().isTrigger = true;
            ShadePureColor(sphere.GetComponent<Renderer>(), color);
        }

        public static void MakeTransluscent(Material material)
        {
            material.shader = Shader.Find("Standard");
            material.SetFloat("_Mode", 2);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }
}