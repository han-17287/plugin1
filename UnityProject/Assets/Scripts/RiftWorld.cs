using UnityEngine;
public class RiftWorld : MonoBehaviour {
 public static float Height(float x,float z)=>Mathf.PerlinNoise((x+512)*.018f,(z+512)*.018f)*7;
 void Start(){
  Random.InitState(17287);
  RenderSettings.ambientLight=new Color(.5f,.6f,.7f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.55f,.7f,.8f);RenderSettings.fogDensity=.004f;
  var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(50,-30,0);
  var data=new TerrainData();data.heightmapResolution=257;data.size=new Vector3(256,12,256);var heights=new float[257,257];
  for(int z=0;z<=256;z++)for(int x=0;x<=256;x++)heights[z,x]=Height(x-128,z-128)/12;
  data.SetHeights(0,0,heights);var terrain=Terrain.CreateTerrainGameObject(data);terrain.name="256m rolling landscape";terrain.transform.position=new Vector3(-128,0,-128);
  var tex=new Texture2D(32,32);for(int y=0;y<32;y++)for(int x=0;x<32;x++)tex.SetPixel(x,y,Color.Lerp(new Color(.16f,.26f,.07f),new Color(.36f,.43f,.18f),Random.value));tex.Apply();
  var layer=new TerrainLayer();layer.diffuseTexture=tex;layer.tileSize=new Vector2(6,6);data.terrainLayers=new[]{layer};
  for(int i=0;i<160;i++){float x=Random.Range(-120f,120f),z=Random.Range(-120f,120f);if(new Vector2(x,z).magnitude<10)continue;var root=new GameObject("Harvestable tree");root.transform.position=new Vector3(x,Height(x,z),z);root.AddComponent<HarvestTree>();}
  for(int i=0;i<1000;i++){float x=Random.Range(-125f,125f),z=Random.Range(-125f,125f);var grass=RiftGame.Shape("Wind grass",PrimitiveType.Cube,new Vector3(x,Height(x,z)+.35f,z),new Vector3(.08f,.7f,.18f),new Color(.28f,.43f,.12f),transform);Destroy(grass.GetComponent<Collider>());grass.AddComponent<WindGrass>();}
  for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var e=new GameObject("Rift creature");e.transform.position=new Vector3(Mathf.Cos(a)*25,8,Mathf.Sin(a)*25);e.AddComponent<RiftEnemy>();}
  for(int i=0;i<4;i++){var b=RiftGame.Shape("Boundary",PrimitiveType.Cube,new Vector3(i<2?(i==0?-129:129):0,20,i>=2?(i==2?-129:129):0),i<2?new Vector3(2,50,260):new Vector3(260,50,2),Color.gray);b.GetComponent<Renderer>().enabled=false;}
 }
}
public class WindGrass:MonoBehaviour {void Update(){transform.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*2+transform.position.x)*12,0,Mathf.Sin(Time.time*1.5f+transform.position.z)*8);}}
public class RiftEffect:MonoBehaviour {public float size=3;float age;void Update(){age+=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(.1f,size,age/.22f);if(age>.22f)Destroy(gameObject);}}
