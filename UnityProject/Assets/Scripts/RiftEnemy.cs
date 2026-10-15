using UnityEngine;
public class RiftEnemy:MonoBehaviour {
 float hp=85,attackAt,windup,stun;bool dead;CharacterController body;Vector3 origin;
 void Start(){body=gameObject.AddComponent<CharacterController>();body.height=1.6f;body.center=Vector3.up*.8f;origin=transform.position;
 var mesh=RiftGame.Shape("Creature",PrimitiveType.Capsule,Vector3.up*.8f,new Vector3(.8f,.8f,.8f),new Color(.55f,.12f,.19f),transform);Destroy(mesh.GetComponent<Collider>());}
 void Update(){if(dead)return;var hero=RiftGame.Instance.hero;if(!hero||hero.hp<=0)return;
 Vector3 d=hero.transform.position-transform.position;d.y=0;float distance=d.magnitude;
 if(windup>0){if(Time.time>=windup){if(distance<2.6f)hero.Hit(12);windup=0;attackAt=Time.time+1.4f;}return;}
 if(Time.time<stun)return;
 Vector3 move=Vector3.zero;
 if(distance<18&&distance>1.9f)move=d.normalized*2.6f;
 else if(distance<=1.9f&&Time.time>attackAt){windup=Time.time+.65f;}
 else if(distance>=18){Vector3 target=origin+new Vector3(Mathf.Sin(Time.time*.3f),0,Mathf.Cos(Time.time*.3f))*4;move=(target-transform.position);move.y=0;move=move.normalized;}
 if(move.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(move);
 body.Move((move+Vector3.down*10)*Time.deltaTime);
 }
 public void Hit(float damage,Vector3 direction){if(dead)return;hp-=damage;windup=0;stun=Time.time+.35f;if(body)body.Move(direction*.5f);if(hp<=0){dead=true;if(body)body.enabled=false;RiftGame.Instance.Reward();Destroy(gameObject);}}
}
public class HarvestTree:MonoBehaviour {
 int hits;bool falling;float angle;Quaternion initial;
 void Start(){initial=transform.rotation;
 RiftGame.Shape("Trunk",PrimitiveType.Cylinder,new Vector3(0,2,0),new Vector3(.65f,2,.65f),new Color(.28f,.16f,.07f),transform);
 RiftGame.Shape("Crown",PrimitiveType.Sphere,new Vector3(0,5,0),new Vector3(3.6f,4.6f,3.6f),new Color(.1f,.28f,.12f),transform);
 }
 public void Hit(){if(falling)return;hits++;RiftGame.Instance.Message("Tree: "+hits+" / 9");if(hits>=9){falling=true;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;}}
 void Update(){if(!falling)return;angle=Mathf.Min(90,angle+Time.deltaTime*(20+angle));transform.rotation=initial*Quaternion.Euler(angle,0,0);if(angle>=90){RiftGame.Instance.wood+=3;RiftGame.Instance.Message("Wood +3 (Inventory → Materials)");Destroy(gameObject);}}
}
