using System;
using System.Linq;
using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        static int checks;
        static void Check(bool ok,string assertion)
        {
            checks++;
            if(!ok) throw new InvalidOperationException(assertion);
        }

        public static void Main()
        {
            for(int id=1;id<=50;id++)
            {
                var room=Open(id);
                Check(room.callbacks.Count>0 || room.interactions.Count>0,"No traversal or interaction in room "+id);
                Check(room.objects.Any(o=>o.Object.GetComponent<BoxCollider2D>()),"No playable surface in room "+id);
                room.Tick(610);
                Check(room.objects.All(o=>float.IsFinite(o.Object.transform.position.x)&&float.IsFinite(o.Object.transform.position.y)),
                    "Non-finite device geometry after a full cycle in room "+id);
                Check(room.interactions.All(i=>i.Action!=null || i.Destination>=1&&i.Destination<=50),
                    "Bad room destination in room "+id);
            }
            CheckProduction();
            CheckLiving();
            CheckAdministration();
            CheckResidence();
            CheckSummit();
            Console.WriteLine("PASS: "+checks+" authored patrol room checks (50 rooms).");
        }

        static void CheckProduction()
        {
            var dock=Open(1);
            dock.Point(20,1.18f);
            dock.Tick(12);
            Check(dock.Velocity.x>=4.39f,"Missed cargo hook leaves a moving lower belt under the player");
            dock.Zipping=true;
            dock.Point(20,1.18f);
            dock.Tick();
            Check(dock.Velocity.x==0,"Catching belt cannot redirect an active straight zip");
            dock.Zipping=false;
            dock.Grounded=false;
            dock.Point(24.65f,5.95f);
            dock.JumpHeld=true;
            dock.Tick();
            Check(dock.Velocity.y==0,"Ordinary jump cannot trigger the arrival pad");
            dock.JumpHeld=false;
            dock.Arrive("천장 크레인 · 화물 고리 3");
            dock.Tick();
            dock.JumpHeld=true;
            dock.Tick();
            Check(dock.Velocity.y>=14 && dock.Velocity.x>=16,"Third cargo hook arms a distinct post-arrival jump");
            dock.JumpHeld=false;
            dock.Velocity.y*=.5f;
            bool crossed=false;
            for(int i=0;i<45;i++)
            {
                // Conservative tapped-jump envelope with the P1 movement profile at 50Hz.
                dock.Velocity.y-=(dock.Velocity.y>0 ? 32f : 52.8f)*.02f;
                dock.Velocity.x=Mathf.MoveTowards(dock.Velocity.x,9,32*.02f);
                dock.Tick();
                dock.Hero+=dock.Velocity*.02f;
                if(dock.Hero.x>=33.8f && dock.Hero.y>=8) crossed=true;
            }
            Check(crossed,"Even from farthest hook position, the launch reaches the upper dock");

            var storage=Open(6);
            storage.Point(6,1.8f);
            storage.PrimaryHeld=true;
            storage.Tick();
            Check(storage.ObjectNamed("폐전구 1").activeSelf,"Hook mode cannot cut a bulb");
            storage.expedition.Player.Mode=PlayerMode.Attack;
            storage.Tick();
            storage.PrimaryHeld=false;storage.CollectHeld=true;
            storage.Tick(4);
            Check(storage.Charge==3,"Collecting a broken bulb gives three particles");
            storage.UseAction("리프트에 입자 3개 공급");
            Check(storage.Charge==0 && storage.Flag("prod.store.lift"),"Powering the lift actually spends particles");
        }

        static void CheckLiving()
        {
            var alley=Open(12);
            alley.Point(26,6.5f);
            alley.Arrive("긴 빨래 리프트 고리");
            alley.Tick();
            Check(alley.Velocity.x>=14,"The laundry hook carries an unbraked player toward the roof");
            alley.Axis=-1;
            alley.Tick();
            Check(alley.Velocity.y<0 && Math.Abs(alley.Velocity.x)<1,"Countersteering over the sill converts the launch into an accurate landing");
            Check(!alley.PortalOpen(11),"Laundry shortcut initially closed");
            alley.UseAction("세탁소 창을 열고 안으로 들어가기");
            alley.UseAction("세탁소 분실물 확인");
            Check(alley.PortalOpen(11),"The secret laundromat opens a road back to the market");
            var shop=Open(17,alley);
            shop.Point(31.5f,1.55f);
            Check(!shop.WantsRoomMusic(),"A silent accordion must not interrupt the score before repair");
            shop.UseAction("손풍금 수리공 은주에게 말 걸기");
            Check(shop.Flag("living.reedsFixed") && shop.PortalOpen(11),"Lost accordion from room 12 unlocks the shop shortcut in room 17");
            shop.Point(31.5f,1.55f);
            Check(shop.WantsRoomMusic(),"Approaching the repaired accordion must request the music bus");
            shop.TickSpeaker(false,false,false);
            Check(!shop.roomSpeaker.isPlaying,"Accordion must wait for the region fade");
            shop.Tick(10);
            Check(shop.roomSpeaker.isPlaying && shop.roomSpeaker.volume > 0,"Granted accordion must be audible");
            shop.TickSpeaker(true,false,true);
            Check(!shop.roomSpeaker.isPlaying && shop.speakerPaused,"Menu must pause the accordion");
            shop.Point(0,0);
            shop.Tick(20);
            Check(!shop.WantsRoomMusic() && !shop.roomSpeaker.isPlaying,"Leaving a muted room source must return control to the score");
        }

        static void CheckAdministration()
        {
            var lift=Open(26);
            var crate=lift.ObjectNamed("중량 기록 상자").GetComponent<Rigidbody2D>();
            Check(crate && crate.bodyType==RigidbodyType2D.Dynamic,"The archive crate has a dynamic physics body");
            crate.position=new Vector2(23.1f,1.91f);
            lift.Tick();
            Check(!lift.ObjectNamed("하단 기록 문").activeSelf && lift.ObjectNamed("상단 기록 문").activeSelf,
                "Loading the lower stop changes which door can be used");
            crate.position=new Vector2(23.1f,6.92f);
            lift.Tick();
            Check(!lift.ObjectNamed("상단 기록 문").activeSelf,"The upper stop can be chosen independently");
            var receiver=Open(28);
            receiver.Point(8,1);
            receiver.Shot("유리문 레이저 수광부");
            receiver.Tick();
            Check(receiver.ObjectNamed("광학 유리문").activeSelf,"A ground-level laser cannot bypass the side ledge");
            receiver.Point(24,8);
            receiver.Shot("유리문 레이저 수광부");
            receiver.Tick();
            Check(!receiver.ObjectNamed("광학 유리문").activeSelf,"A beam from the correct angle strikes the receiver");
        }

        static void CheckResidence()
        {
            var canal=Open(32);
            var raft=canal.ObjectNamed("부력 화물 · 서쪽 운반함");
            var hatch=canal.ObjectNamed("중간 수위 정비문");
            var valve=canal.ObjectNamed("수위 연동 배수문");
            canal.Tick();
            float ebb=raft.transform.position.y;
            Check(hatch.activeSelf&&!valve.activeSelf,"At low water the drain is accessible");
            canal.Tick(136);
            Check(!hatch.activeSelf && valve.activeSelf,"At medium water the maintenance door opens");
            canal.Tick(140);
            Check(hatch.activeSelf && valve.activeSelf && raft.transform.position.y>ebb+6,
                "At high water the floating cargo forms the upper route");
            canal.Point(25,5);
            canal.Tick();
            Check(!hatch.activeSelf,"A player inside the maintenance doorway can still get out");
        }

        static void CheckSummit()
        {
            var rooftops=Open(41);
            var vehicle=rooftops.ObjectNamed("뒤편 레일 정비차");
            var bridge=rooftops.ObjectNamed("정비차 전개교");
            rooftops.Point(7,1);
            bool opened=false,closed=false;
            var finalSocket=rooftops.ObjectNamed("순차 전개 전방 고리 5");
            rooftops.Tick();
            Check(!finalSocket.activeSelf,"The last relay socket starts folded");
            bool deployed=false;
            float start=vehicle.transform.position.x, travel=0;
            for(int i=0;i<900;i++)
            {
                rooftops.Tick();
                opened|=bridge.activeSelf;closed|=!bridge.activeSelf;
                deployed|=finalSocket.activeSelf;
                travel=Math.Max(travel,Math.Abs(vehicle.transform.position.x-start));
            }
            Check(travel>20 && opened && closed && deployed,"Driving the maintenance car extends the relay sockets and temporary bridge");
            var station=Open(49);
            station.UseAction("무전 수신 주파수 맞추기");
            Check(station.WantsRoomMusic(),"Tuning the accordion station must request focus");
            station.TickSpeaker(false,false,false);
            Check(!station.roomSpeaker.isPlaying,"The radio melody must wait for the score fade");
            station.Tick(2);
            Check(station.roomSpeaker.isPlaying,"The tuned radio melody was lost");
            station.TickSpeaker(true,false,true);
            Check(!station.roomSpeaker.isPlaying,"The summit radio kept playing while the room was paused");
            station.Tick();
            station.roomSpeaker.isPlaying=false;
            Check(!station.WantsRoomMusic(),"A completed radio melody still held the soundtrack");
            station.Tick();
            Check(!station.roomSpeaker.isPlaying,"A completed radio clue started looping");
            station.UseAction("무전 수신 주파수 맞추기");
            Check(!station.WantsRoomMusic(),"A metal sound effect should not stop the soundtrack");
            station.TickSpeaker(false,false,false);
            Check(station.roomSpeaker.isPlaying,"A radio sound effect incorrectly required a music grant");
            station.TrialActive=true;station.Grounded=false;
            station.Point(5,4,8,9);station.Tick();
            station.Point(46,8);station.Tick();
            Check(station.Flag("summit.airline"),"Finishing the trial without touching down awards the optional air record");
            var sunrise=Open(50);
            int[] destinations={1,11,30,38,41,49};
            Check(destinations.All(sunrise.PortalOpen),"The observatory connects to all five districts, including the broadcast room and return lift");
        }
    }
}
