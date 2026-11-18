using System;
using System.Collections.Generic;
using UnityEngine;
namespace Plugin.AncientCivilization
{
    // One rendered resident per named speaker. Repeated conversations never create visible duplicates.
    public sealed class AncientRouteResidents
    {
        sealed class Resident {public SpriteRenderer view;public int spot,wanted,revisit;public float distance;}
        readonly Dictionary<string,Resident> speakers=new Dictionary<string,Resident>();
        readonly List<Resident> people=new List<Resident>();readonly Resident[] byStop=new Resident[AncientLowerRouteRules.StopCount];
        readonly AncientGame game;
        static Vector2 Position(int id){return AncientLowerRoute.BasePoint(id)+new Vector2(-6,-1.3f);}
        static bool HasRole(string name,params string[] roles)
        {foreach(string role in roles)if(name.IndexOf(role,StringComparison.Ordinal)>=0)return true;return false;}
        static int Artwork(int id)
        {
            string name=AncientLowerRouteData.Stops[id].Speaker;
            if(name=="주민 아린")return 2;
            if(HasRole(name,"약제사","의료","진료"))return 5;
            if(HasRole(name,"배달","우편"))return 6;
            if(HasRole(name,"식권","도시락","급식","휴게실","세탁"))return id%2==0?1:9;
            if(HasRole(name,"기록","계량","송신","인계","인쇄"))return id%2==0?10:14;
            // Child art is reserved for the explicitly written child. The old
            // sector-index rotation could give an adult cargo worker that body.
            int[] workers={0,4,11,15,20};return workers[id%workers.Length];
        }
        public AncientRouteResidents(AncientGame owner,Transform parent,Func<int,bool> known)
        {
            game=owner;
            for(int id=0;id<AncientLowerRouteRules.StopCount;id++)
            {
                var stop=AncientLowerRouteData.Stops[id];Resident resident;
                if(!speakers.TryGetValue(stop.Speaker,out resident))
                {
                    resident=new Resident{spot=id,wanted=id};resident.view=AncientArt.Display(stop.Speaker,AncientCitizenArt.ForCitizen(Artwork(id)),Position(id),9,parent);
                    resident.view.transform.localScale=Vector3.one*.9f;resident.view.enabled=false;people.Add(resident);speakers.Add(stop.Speaker,resident);
                }
                byStop[id]=resident;if(known(id))resident.spot=resident.wanted=id;
            }
            foreach(var resident in people)resident.view.transform.position=Position(resident.spot);
        }
        public SpriteRenderer At(int id){return id>=0&&id<byStop.Length?byStop[id].view:null;}
        public bool InPlace(int id){return id>=0&&id<byStop.Length&&byStop[id].spot==id;}
        public void Focus(int id){if(id>=0&&id<byStop.Length)byStop[id].wanted=id;}
        public void Revisit(int mission,float playerX,Func<int,bool> available)
        {
            if(!AncientLowerRouteRules.Supports(mission))return;
            foreach(var person in people){person.revisit=-1;person.distance=float.PositiveInfinity;}
            for(int id=mission*9;id<(mission+1)*9;id++)
            {
                if(!available(id))continue;var person=byStop[id];float distance=Mathf.Abs(Position(id).x-playerX);
                if(distance<person.distance){person.distance=distance;person.revisit=id;}
            }
            foreach(var person in people)if(person.revisit>=0)person.wanted=person.revisit;
        }
        public bool TryActor(string speaker,out Transform actor,out float offset)
        {
            actor=null;offset=1.9f;Resident resident;
            if(!speakers.TryGetValue(speaker,out resident)||!resident.view.enabled)return false;
            actor=resident.view.transform;return true;
        }
        public int NearestKnown(Func<int,bool> known)
        {
            if(!game.hero)return -1;int nearest=-1;float best=3.2f*3.2f;
            foreach(var person in people)
            {
                if(!person.view.enabled||!known(person.spot))continue;
                Vector2 delta=(Vector2)person.view.transform.position+Vector2.up-(Vector2)game.hero.transform.position;
                if(delta.sqrMagnitude<best){best=delta.sqrMagnitude;nearest=person.spot;}
            }
            return nearest;
        }
        public void Tick()
        {
            if(!game||!game.Story||!game.View)return;
            var camera=game.View;float width=camera.orthographicSize*camera.aspect,height=camera.orthographicSize;
            Vector2 centre=camera.transform.position;
            foreach(var person in people)
            {
                Vector2 point=person.view.transform.position;int floor=person.spot/36;
                bool visible=AncientChapterVisibility.PointVisible(floor,game.Story.CurrentFloor,point.x,point.y+1,centre.x,centre.y,width,height,6);
                if(person.spot!=person.wanted&&!visible&&!game.Story.DialogueOpen)
                {person.spot=person.wanted;point=Position(person.spot);person.view.transform.position=point;floor=person.spot/36;}
                person.view.enabled=game.HasSession&&!game.MainMenu&&AncientChapterVisibility.PointVisible(floor,game.Story.CurrentFloor,point.x,point.y+1,centre.x,centre.y,width,height,4);
                if(person.view.enabled&&game.hero)person.view.flipX=game.hero.transform.position.x<point.x;
            }
        }
    }
}
