using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed class AncientPerformance : MonoBehaviour
    {
        static readonly int[] targets={60,120,144,240,-1};
        readonly float[] samples=new float[240],sorted=new float[240];
        readonly int[] sampleFloor=new int[240],sampleTarget=new int[240],sampleQuality=new int[240],samplePhase=new int[240],sampleRoom=new int[240],sampleSniper=new int[240],sampleSniperObjects=new int[240];
        readonly float[] sampleX=new float[240],sampleY=new float[240],sampleSpeed=new float[240],sampleEnergy=new float[240];
        readonly int[] sampleMode=new int[240],sampleWire=new int[240],sampleLaser=new int[240];
        readonly int[] sampleProductionPhase=new int[240],sampleProductionRound=new int[240],sampleProductionMounts=new int[240],sampleStoryStage=new int[240];
        AncientGame game;int cursor,count,targetIndex;float updateAt;bool details;
        float mean,p95,peak;string summary="",diagnostics="";GUIStyle style;
        ProfilerRecorder allocations;
        public int Target {get{return targets[targetIndex];}}
        public string TargetLabel {get{return Target<0?"무제한":Target.ToString(CultureInfo.InvariantCulture);}}
        public string Summary {get{return summary;}}
        public void Initialize(AncientGame owner)
        {
            game=owner;int saved=AncientGame.Saved("frameTarget",240);targetIndex=Array.IndexOf(targets,saved);
            if(targetIndex<0)targetIndex=3;ApplyTarget();
        }
        void ApplyTarget()
        {QualitySettings.vSyncCount=0;QualitySettings.maxQueuedFrames=2;Application.targetFrameRate=Target;}
        public void CycleTarget()
        {targetIndex=(targetIndex+1)%targets.Length;ApplyTarget();AncientGame.SetSaved("frameTarget",Target);PlayerPrefs.Save();Refresh();game.Notice("프레임 목표 "+TargetLabel+" / 실제 성능은 기기에 따라 다릅니다.",2);}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F8))CycleTarget();
            if(Input.GetKeyDown(KeyCode.F9))
            {
                details=!details;
                if(details&&!allocations.Valid)
                {try{allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);}catch(Exception){allocations=default(ProfilerRecorder);}}
                if(!details&&allocations.Valid)allocations.Dispose();
                Refresh();
            }
            if(details&&Input.GetKeyDown(KeyCode.F10))Export();
            if(!game.HasSession||game.Paused||!Application.isFocused)return;
            float dt=Time.unscaledDeltaTime;if(!AncientFrameRules.ValidSample(dt))return;
            samples[cursor]=dt*1000;sampleFloor[cursor]=game.Story.CurrentFloor;sampleTarget[cursor]=Target;
            sampleQuality[cursor]=AncientVisuals.HighQuality?1:0;samplePhase[cursor]=game.Finale?game.Finale.Phase:0;sampleRoom[cursor]=game.Campaign?game.Campaign.CurrentRoom:-1;sampleSniper[cursor]=game.Sniper?game.Sniper.Phase:0;sampleSniperObjects[cursor]=game.Sniper?game.Sniper.LiveObjects:0;
            Vector2 position=game.hero.transform.position;sampleX[cursor]=position.x;sampleY[cursor]=position.y;sampleSpeed[cursor]=game.hero.HorizontalSpeed;
            sampleEnergy[cursor]=game.hero.CaptureEnergy;sampleMode[cursor]=game.hero.AttackMode?1:0;sampleWire[cursor]=game.hero.Wired?1:0;sampleLaser[cursor]=game.hero.Combat&&game.hero.Combat.Firing?1:0;
            var pursuit=game.Production;
            sampleProductionPhase[cursor]=pursuit?(int)pursuit.Sequence.Phase:0;sampleProductionRound[cursor]=pursuit?pursuit.Sequence.Round:0;
            sampleProductionMounts[cursor]=pursuit?pursuit.Sequence.MountCount:0;sampleStoryStage[cursor]=game.Story.Stage;
            cursor=(cursor+1)%samples.Length;count=Mathf.Min(count+1,samples.Length);
            if(Time.unscaledTime<updateAt)return;updateAt=Time.unscaledTime+.5f;Refresh();
        }
        void Refresh()
        {
            float total=0;peak=0;
            for(int i=0;i<count;i++){float value=samples[i];total+=value;peak=Mathf.Max(peak,value);sorted[i]=value;}
            mean=count>0?total/count:0;Array.Sort(sorted,0,count);p95=count>0?sorted[AncientFrameRules.P95Index(count)]:0;
            summary="목표 "+TargetLabel+" / "+(mean>0?(1000/mean).ToString("F0"):"—")+" FPS";
            if(!details)return;
            long bytes=allocations.Valid?allocations.LastValue:-1;
            diagnostics="프레임 평균 "+mean.ToString("F2")+"ms · P95 "+p95.ToString("F2")+"ms\n최대 "+peak.ToString("F2")+"ms / 최근 GC "+(bytes>=0?(bytes/1024f).ToString("F1")+"KB":"미지원")+
                "\n정적 장식 "+game.WorldBatch.QuadCount+"개 → 묶음 "+game.WorldBatch.RendererCount+"개 / 현재 "+game.WorldBatch.ActiveRendererCount+"개\n활성 적 "+game.enemies.Count+" / 투척물 "+game.shots.Count+" / 풀 "+game.Projectiles.Capacity+" (누락 "+game.Projectiles.DroppedSpawns+")"+(game.Sniper?"\n저격수 물체 "+game.Sniper.LiveObjects+" / 풀 "+game.Sniper.ObjectCapacity+" · 누락 "+game.Sniper.DroppedSpawns:"")+"\nF8 프레임 목표 · F9 닫기 · F10 CSV 저장";
        }
        void Export()
        {
            if(count==0){game.Notice("플레이 중 프레임을 수집한 뒤 F10으로 저장하세요.",3);return;}
            try
            {
                var text=new StringBuilder("sample,frame_ms,floor,target_fps,enhanced,finale_phase,campaign_room,sniper_phase,sniper_objects,hero_x,hero_y,horizontal_speed,capture_energy,attack_mode,wired,laser_active,production_phase,production_round,production_mounts,story_stage\n");
                for(int i=0;i<count;i++)
                {
                    int at=count==samples.Length?(cursor+i)%samples.Length:i;
                    text.Append(i).Append(',').Append(samples[at].ToString("F4",CultureInfo.InvariantCulture)).Append(',')
                        .Append(sampleFloor[at]).Append(',').Append(sampleTarget[at]).Append(',').Append(sampleQuality[at]).Append(',').Append(samplePhase[at]).Append(',').Append(sampleRoom[at]).Append(',').Append(sampleSniper[at]).Append(',').Append(sampleSniperObjects[at]).Append(',')
                        .Append(sampleX[at].ToString("F3",CultureInfo.InvariantCulture)).Append(',').Append(sampleY[at].ToString("F3",CultureInfo.InvariantCulture)).Append(',')
                        .Append(sampleSpeed[at].ToString("F3",CultureInfo.InvariantCulture)).Append(',').Append(sampleEnergy[at].ToString("F3",CultureInfo.InvariantCulture)).Append(',')
                        .Append(sampleMode[at]).Append(',').Append(sampleWire[at]).Append(',').Append(sampleLaser[at]).Append(',')
                        .Append(sampleProductionPhase[at]).Append(',').Append(sampleProductionRound[at]).Append(',').Append(sampleProductionMounts[at]).Append(',').Append(sampleStoryStage[at]).Append('\n');
                }
                string path=Path.Combine(Application.persistentDataPath,"performance-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".csv");
                File.WriteAllText(path,text.ToString());game.Notice("성능 기록 저장: "+path,6);
            }
            catch(Exception e){game.Notice("성능 기록 저장 실패: "+e.Message,4);}
        }
        void OnGUI()
        {
            if(!details||game.MainMenu||Event.current.type!=EventType.Repaint)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){font=AncientKoreanUI.Font,fontSize=14,wordWrap=true};
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            float x=Mathf.Max(12,Screen.width-430);AncientHUD.Panel(new Rect(x,220,415,170),new Color(.015f,.035f,.05f,.94f));
            GUI.Label(new Rect(x+12,228,390,157),diagnostics,style);GUI.matrix=matrix;
        }
        void OnDestroy(){if(allocations.Valid)allocations.Dispose();}
    }
}
