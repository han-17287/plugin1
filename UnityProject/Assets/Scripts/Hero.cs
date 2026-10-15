using UnityEngine;
using System.Collections.Generic;
public class Hero : MonoBehaviour {
 public float hp=100,energy=100;
 CharacterController controller; Camera view; Transform arm; float yaw,pitch=20,distance=6,vertical,rollUntil,attackReady,pressed=-1,comboUntil; int combo; Vector3 rollDirection;
 void Start(){
  controller=gameObject.AddComponent<CharacterController>();controller.height=1.8f;controller.center=Vector3.up*.9f;controller.radius=.35f;
  Visual("Shirt",PrimitiveType.Capsule,new Vector3(0,1.1f,0),new Vector3(.65f,.5f,.4f),Color.white);
  Visual("Head",PrimitiveType.Sphere,new Vector3(0,1.85f,0),Vector3.one*.42f,new Color(.85f,.66f,.48f));
  Visual("Hair",PrimitiveType.Sphere,new Vector3(0,2.04f,-.03f),new Vector3(.45f,.22f,.42f),new Color(.07f,.06f,.1f));
  Visual("Leg L",PrimitiveType.Cube,new Vector3(-.2f,.4f,0),new Vector3(.25f,.8f,.28f),new Color(.12f,.15f,.2f));
  Visual("Leg R",PrimitiveType.Cube,new Vector3(.2f,.4f,0),new Vector3(.25f,.8f,.28f),new Color(.12f,.15f,.2f));
  Visual("Cape",PrimitiveType.Cube,new Vector3(0,1,-.28f),new Vector3(.7f,1.1f,.06f),new Color(.03f,.18f,.65f));
  arm=Visual("Blue Gauntlet",PrimitiveType.Cube,new Vector3(.5f,1.1f,.15f),new Vector3(.3f,.45f,.35f),Color.cyan).transform;
  view=new GameObject("Third Person Camera").AddComponent<Camera>();view.gameObject.AddComponent<AudioListener>();view.farClipPlane=400;view.fieldOfView=65;
 }
 GameObject Visual(string n,PrimitiveType t,Vector3 p,Vector3 s,Color c){var o=RiftGame.Shape(n,t,p,s,c,transform);Destroy(o.GetComponent<Collider>());return o;}
 void Update(){
  var game=RiftGame.Instance;
  if(Input.GetMouseButtonDown(1)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
  if(!game.CanControl){pressed=-1;return;}
  yaw+=Input.GetAxis("Mouse X")*2.5f;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2, -15,70);distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y,2.5f,12);
  Vector3 input=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));input=Quaternion.Euler(0,yaw,0)*Vector3.ClampMagnitude(input,1);
  if(input.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(input),Time.deltaTime*14);
  if(controller.isGrounded){vertical=-2;if(Input.GetKeyDown(KeyCode.Space))vertical=7;}
  vertical-=20*Time.deltaTime;
  if(Input.GetKeyDown(KeyCode.C)&&Time.time>rollUntil+.4f){rollUntil=Time.time+.4f;rollDirection=input.sqrMagnitude>.01f?input:transform.forward;pressed=-1;}
  var movement=Time.time<rollUntil?rollDirection*12:input*(Input.GetKey(KeyCode.LeftShift)?8:4.5f);
  controller.Move((movement+Vector3.up*vertical)*Time.deltaTime);
  if(Input.GetMouseButtonDown(0)&&Time.time>=attackReady)pressed=Time.time;
  if(pressed>=0){float held=Time.time-pressed;if(held>.25f)energy=Mathf.Max(0,energy-22*Time.deltaTime);arm.localScale=Vector3.one*(.35f+Mathf.Min(held,1.6f)*.12f);
   if(Input.GetMouseButtonUp(0)||held>=1.85f||energy<=0){if(held>.25f){float power=Mathf.Clamp01((held-.25f)/1.6f);Strike(25+power*65,3+power*7,true);}else{combo=Time.time>comboUntil?1:combo%3+1;comboUntil=Time.time+1;Strike(combo==3?32:18,combo==3?3:2.4f,combo==3);}pressed=-1;attackReady=Time.time+.32f;}
  } else {energy=Mathf.Min(100,energy+15*Time.deltaTime);arm.localScale=new Vector3(.3f,.45f,.35f);}
 }
 void LateUpdate(){if(!view)return;Vector3 target=transform.position+Vector3.up*1.6f;Vector3 offset=Quaternion.Euler(pitch,yaw,0)*Vector3.back*distance;
  float d=distance;if(Physics.SphereCast(target,.2f,offset.normalized,out var hit,distance,~0,QueryTriggerInteraction.Ignore)&&hit.collider.gameObject!=gameObject)d=Mathf.Max(.3f,hit.distance-.15f);
  view.transform.position=target+offset.normalized*d;view.transform.LookAt(target);
 }
 void Strike(float damage,float range,bool area){
  var seen=new HashSet<int>();Vector3 center=transform.position+Vector3.up;
  foreach(var c in Physics.OverlapSphere(center,range)){
   Vector3 delta=c.transform.position-transform.position;delta.y=0;
   if(!area&&Vector3.Dot(transform.forward,delta.normalized)<.2f)continue;
   var enemy=c.GetComponentInParent<RiftEnemy>();if(enemy&&seen.Add(enemy.GetInstanceID()))enemy.Hit(damage+RiftGame.Instance.level*2,transform.forward);
   var tree=c.GetComponentInParent<HarvestTree>();if(tree&&seen.Add(tree.GetInstanceID()))tree.Hit();
  }
  var fx=RiftGame.Shape("Blue impact",PrimitiveType.Sphere,center+transform.forward,Vector3.one*.2f,new Color(0,.7f,1));Destroy(fx.GetComponent<Collider>());var effect=fx.AddComponent<RiftEffect>();effect.size=range;
 }
 public void Hit(float damage){if(hp<=0||Time.time<rollUntil)return;hp=Mathf.Max(0,hp-damage);pressed=-1;}
}
