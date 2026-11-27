using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientPatrolGate : MonoBehaviour
    {
        AncientGame game;
        readonly Vector2[] gates=new Vector2[6];
        GUIStyle label;
        int near=-1;
        string storyPrefix;
        int sampleFloor = -1;
        float lastX;
        bool sampled, checkedResume;

        public void Initialize(AncientGame owner)
        {
            game=owner;
            storyPrefix = AncientStorySocketPassage.SavePrefix();
            checkedResume = false;
            float tutorial=AncientProductionLayout.TutorialCheckpointX(0)+6;
            gates[0]=new Vector2(tutorial,AncientProductionLayout.GroundHeight(tutorial));
            for(int f=0;f<5;f++)gates[f+1]=new Vector2(AncientCityLayout.Entry(f)+8,0);
            // 도로 목에 동일한 정비 표식을 둔다. 통과만으로 길이 이어진다는 안내다.
            for (int group = 0; group < AncientStoryPassageRules.PassageCount; group++)
            {
                InstallStreetSign(AncientStoryPassageRules.Seam(group, false));
                if (group < 2) InstallStreetSign(AncientStoryPassageRules.Seam(group, true));
            }
            for(int i=0;i<gates.Length;i++)
            {
                Vector2 feet=gates[i];
                game.Box("도시 순찰 안내소",feet+Vector2.up*1.0f,new Vector2(1.05f,1.9f),new Color(.1f,.17f,.22f),3).transform.SetParent(transform,true);
                game.Box("순찰소 안내등",feet+Vector2.up*1.54f,new Vector2(.75f,.17f),AncientGame.Cyan,4).transform.SetParent(transform,true);
            }
        }
        void InstallStreetSign(float x)
        {
            Color frame = new Color(.13f,.17f,.20f), metal = new Color(.39f,.44f,.45f);
            var bracket = game.Box("탐험로 건축 이음 · 지주", new Vector2(x,2.32f), new Vector2(.26f,4.64f), frame,-3).transform;
            bracket.SetParent(transform,true);
            game.Box("탐험로 건축 이음 · 들보",new Vector2(x,4.57f),new Vector2(2.15f,.24f),metal,-2).transform.SetParent(transform,true);
            game.Box("탐험로 통행등",new Vector2(x,4.36f),new Vector2(1.18f,.07f),new Color(.98f,.79f,.52f),-1).transform.SetParent(transform,true);
        }

        void OnEnable()
        {
            sampled = false;
        }

        bool TryWalkThrough()
        {
            if (!game.HasSession || !game.Story || game.won || game.hero.bike || AncientDeveloperTravel.IsVisiting ||
                (game.City && game.City.ArenaActive))
            {
                sampled = false;
                return false;
            }
            int floor = game.Story.CurrentFloor, stage = game.Story.Stage;
            if (!checkedResume)
            {
                checkedResume = true;
                int signedRoom = AncientGame.Saved(storyPrefix + "room");
                if (signedRoom >= -50 && signedRoom <= 50 && signedRoom != 0 &&
                    AncientStoryPassageRules.ResumeValid(Mathf.Abs(signedRoom), floor, stage, AncientGame.Saved(storyPrefix + "opening") != 0))
                {
                    AncientStorySocketPassage.Begin(game, Mathf.Abs(signedRoom), signedRoom < 0, true, storyPrefix);
                    return true;
                }
                if (signedRoom != 0) AncientGame.SetSaved(storyPrefix + "room", 0);
            }
            float x = game.hero.transform.position.x;
            int group = sampled && floor == sampleFloor ? AncientStoryPassageRules.Crossed(lastX, x, floor, stage) : -1;
            bool fromRight = x < lastX;
            sampled = true;
            sampleFloor = floor;
            lastX = x;
            if (group < 0) return false;
            AncientStorySocketPassage.Begin(game, fromRight ? AncientStoryPassageRules.Last(group) : AncientStoryPassageRules.First(group), fromRight, false, storyPrefix);
            return true;
        }

        void Update()
        {
            near=-1;
            if(!game || game.Paused || !game.GameplayInputReady || !game.hero || game.ArenaActive ||
                game.Production && game.Production.Active || game.Finale && game.Finale.Active){sampled = false; return;}
            if (TryWalkThrough()) return;
            for(int i=0;i<gates.Length;i++)
            {
                if(((Vector2)game.hero.transform.position-gates[i]-Vector2.up).sqrMagnitude>8f)continue;
                near=i;break;
            }
            if (near < 0) return;
            int entry = 1 + 10 * Mathf.Max(0, near - 1);
            if (Input.GetKeyDown(KeyCode.P)) PatrolEntry.Request(entry);
            else if (Input.GetKeyDown(KeyCode.O)) PatrolEntry.Request(entry, wallSockets:true);
        }
        void OnGUI()
        {
            if(!game || !game.hero || !game.Story || game.Paused || Event.current.type!=EventType.Repaint)return;
            int floor = game.Story.CurrentFloor, stage = game.Story.Stage;
            if (near < 0 && game.HasSession && AncientStoryPassageRules.Eligible(floor,stage))
            {
                for (int group = floor * 2; group < floor * 2 + 2; group++)
                {
                    float x = AncientStoryPassageRules.Seam(group, AncientStoryPassageRules.Opening(floor,stage));
                    float delta = game.hero.transform.position.x - x;
                    if (Mathf.Abs(delta) > 6.5f) continue;
                    int room = delta <= 0 ? AncientStoryPassageRules.First(group) : AncientStoryPassageRules.Last(group);
                    DrawSign(new Vector2(x,5.1f),delta <= 0 ? PatrolSocketCatalog.Name(room) + "  ▶" : "◀  " + PatrolSocketCatalog.Name(room));
                }
            }
            if(near>=0)DrawSign(gates[near]+Vector2.up*2.6f,"P  도시 자유 순찰     O  벽 콘센트 길");
        }

        void DrawSign(Vector2 position,string message)
        {
            Vector3 point=game.View.WorldToScreenPoint(position);
            if(point.z<=0)return;
            if(label==null)label=new GUIStyle(GUI.skin.box){font=AncientKoreanUI.Font,fontSize=17,alignment=TextAnchor.MiddleCenter};
            GUI.Box(new Rect(point.x-145,Screen.height-point.y-26,290,28),message,label);
        }
    }
}
