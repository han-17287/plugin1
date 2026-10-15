using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// Runtime scene construction keeps the starter independent of third-party assets.
public class RiftGame : MonoBehaviour {
 public static RiftGame Instance;
 public Hero hero; public bool playing; public int slot, page;
 public string notice = "";
 public int wood, level = 1, xp;
 bool inventory, selecting; float noticeUntil;
 readonly string[] pages = {"Equipment", "Armor", "Materials", "Food"};
 [Serializable] public class SaveData { public int wood, level=1, xp; public float x,y,z,hp=100,energy=100; }
 string SavePath(int s) => Path.Combine(Application.persistentDataPath,"rift_slot_"+s+".json");
 void Awake() { Instance=this; }
 void Start() { Application.targetFrameRate=60; }
 public static GameObject Shape(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform parent=null) {
  var o=GameObject.CreatePrimitive(type); o.name=name; o.transform.SetParent(parent,false); o.transform.localPosition=pos; o.transform.localScale=scale;
  var m=new Material(Shader.Find("Standard")); m.color=color; o.GetComponent<Renderer>().material=m; return o;
 }
 public void Message(string s) {notice=s; noticeUntil=Time.time+4;}
 void Begin(bool load) {
  if(playing)return;
  SaveData data=null;
  if(load) {try {data=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath(slot)));} catch(Exception e){Message("Cannot load save: "+e.Message);return;}}
  wood=data==null?0:data.wood;level=data==null?1:Mathf.Max(1,data.level);xp=data==null?0:data.xp;
  var world=new GameObject("World");world.AddComponent<RiftWorld>();
  var p=new GameObject("Hero");hero=p.AddComponent<Hero>();
  hero.transform.position=data==null?new Vector3(0,RiftWorld.Height(0,0)+3,0):new Vector3(data.x,data.y+1,data.z);
  if(data!=null){hero.hp=Mathf.Clamp(data.hp,1,100);hero.energy=Mathf.Clamp(data.energy,0,100);}
  playing=true; Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
 }
 public void Save() {
  if(!playing || hero.hp<=0)return;
  var p=hero.transform.position;
  try {File.WriteAllText(SavePath(slot),JsonUtility.ToJson(new SaveData {wood=wood,level=level,xp=xp,x=p.x,y=p.y,z=p.z,hp=hero.hp,energy=hero.energy},true));Message("Saved slot "+(slot+1));}
  catch(Exception e){Message("Save failed: "+e.Message);}
 }
 public void Reward() {xp+=25;if(xp>=level*100){xp-=level*100;level++;hero.hp=100;Message("Level up!");}}
 void Update() {
  if(!playing)return;
  if(Input.GetKeyDown(KeyCode.I)){inventory=!inventory;Cursor.lockState=inventory?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=inventory;}
  if(Input.GetKeyDown(KeyCode.Escape)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  if(Input.GetKeyDown(KeyCode.F5))Save();
 }
 public bool CanControl => playing && !inventory && Cursor.lockState==CursorLockMode.Locked && hero.hp>0;
 void OnGUI() {
  GUI.skin.label.fontSize=18; GUI.skin.button.fontSize=18;
  if(!playing){
   GUI.Box(new Rect(Screen.width/2-240,Screen.height/2-240,480,480),"");
   GUILayout.BeginArea(new Rect(Screen.width/2-210,Screen.height/2-210,420,420));
   GUILayout.Label("PLUGIN — RIFT OF THE SKY");GUILayout.Space(28);
   if(!selecting){if(GUILayout.Button("NEW GAME / SELECT SLOT",GUILayout.Height(50)))selecting=true;}
   else {
    GUILayout.Label("Select a save slot (New game replaces it when saved)");
    for(int i=0;i<3;i++)if(GUILayout.Button((slot==i?"▶ ":"")+"Slot "+(i+1)+(File.Exists(SavePath(i))?" · Saved":" · Empty"),GUILayout.Height(40)))slot=i;
    if(GUILayout.Button("START NEW GAME",GUILayout.Height(45)))Begin(false);
    GUI.enabled=File.Exists(SavePath(slot));if(GUILayout.Button("CONTINUE",GUILayout.Height(45)))Begin(true);GUI.enabled=true;
   }
   GUILayout.EndArea();
  } else {
   GUI.Box(new Rect(20,20,310,130),"");GUI.Label(new Rect(30,30,290,28),"HP "+Mathf.CeilToInt(hero.hp)+" / 100");
   GUI.color=new Color(.15f,.65f,1);GUI.Box(new Rect(30,65,280*hero.energy/100,18),"");GUI.color=Color.white;
   GUI.Label(new Rect(30,90,290,50),"Energy "+(int)hero.energy+"   Level "+level+"   XP "+xp);
   GUI.Label(new Rect(20,Screen.height-65,Screen.width-40,60),"WASD Move · Shift Run · Space Jump · C Roll · Mouse Look / Wheel Zoom\nLMB Combo / Hold Charge · I Inventory · F5 Save · Esc Unlock · RMB Capture");
   if(hero.hp<=0){GUI.Box(new Rect(Screen.width/2-150,Screen.height/2-60,300,130),"YOU DIED");if(GUI.Button(new Rect(Screen.width/2-120,Screen.height/2,240,45),"Respawn")){hero.hp=100;hero.transform.position=new Vector3(0,RiftWorld.Height(0,0)+3,0);}}
   if(inventory){GUILayout.BeginArea(new Rect(Screen.width/2-240,180,480,360),GUI.skin.box);GUILayout.Label("INVENTORY");GUILayout.BeginHorizontal();if(GUILayout.Button("<"))page=(page+3)%4;GUILayout.Label(pages[page]);if(GUILayout.Button(">"))page=(page+1)%4;GUILayout.EndHorizontal();GUILayout.Space(24);GUILayout.Label(page==2?"Wood × "+wood:"No items yet");GUILayout.EndArea();}
  }
  if(Time.time<noticeUntil)GUI.Label(new Rect(30,160,Screen.width-60,60),notice);
 }
}
