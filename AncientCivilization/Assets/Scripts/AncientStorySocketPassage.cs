using System.Collections.Generic;
using Plugin.PlayerCore;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugin.AncientCivilization
{
    // 본편을 그대로 보관한 채 걷던 도로 사이에 실제 콘센트 방을 실행한다.
    // 메인 씬을 다시 만들면 재고 보고, 적 상태와 대화 트리거가 되감기므로 보관된 루트만 되살린다.
    public static class AncientStorySocketPassage
    {
        static readonly List<GameObject> suspended = new List<GameObject>();
        static AncientGame game;
        static SummonedBlade streetWeapon;
        static SummonedBladeCarry carry;
        static Scene home, street;
        static int group, serial;
        static bool enteredFromWest, opening;
        static string prefix;
        public static bool Active => game;
        public static bool FromRight { get; private set; }
        public static bool Revisiting { get; private set; }
        public static string Prefix => prefix;
        public static int Group => group;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            suspended.Clear(); game = null; streetWeapon = null; carry = default; home = street = default; group = -1; serial = 0; prefix = null;
        }
        public static string SavePrefix() => "storySocket." + AncientGame.Saved("patrol.generation") + ".";
        public static void Begin(AncientGame owner, int id, bool fromRight, bool resume, string savePrefix)
        {
            if (Active || !owner || !owner.hero || AncientStoryPassageRules.Group(id) < 0) return;
            prefix = savePrefix;
            group = AncientStoryPassageRules.Group(id);
            enteredFromWest = resume ? AncientGame.Saved(prefix + "west") != 0 : !fromRight;
            opening = AncientStoryPassageRules.Opening(owner.Story.CurrentFloor, owner.Story.Stage);
            FromRight = fromRight;
            Revisiting = (AncientGame.Saved(prefix + "complete") & (1 << group)) != 0;
            game = owner;
            home = game.gameObject.scene;
            // 분리 전에 발판·플러그 입력을 비운다. 영웅 자원과 본편 전투 상태는 동일 인스턴스에 남는다.
            carry = game.hero.Loadout ? game.hero.Loadout.LeaveRoom() : default;
            game.hero.TravelTo(game.hero.transform.position);
            if (resume)
            {
                carry = RestoreKit(game.hero, prefix);
            }
            game.Save();
            try
            {
                street = SceneManager.CreateScene("StorySocketStreet_" + ++serial);
                foreach (var root in home.GetRootGameObjects())
                {
                    if (!root.activeSelf) continue;
                    suspended.Add(root);
                    root.SetActive(false);
                }
                if (!SceneManager.SetActiveScene(street)) throw new System.InvalidOperationException("탐험 구간을 열 수 없습니다.");
                PatrolEntry.PrepareStoryRoom(id);
                var expedition = new GameObject("본편 탐험로").AddComponent<PatrolExpedition>();
                if (!expedition.StreetReady) throw new System.InvalidOperationException("탐험로 방 구성을 완료하지 못했습니다.");
                streetWeapon = expedition.Player.GetComponent<SummonedBlade>();
                streetWeapon.EnterRoom(carry);
                StoreVitals();
            }
            catch
            {
                Exit(!enteredFromWest, false);
                throw;
            }
        }
        public static void Stamp(int roomId, bool fromRight)
        {
            AncientGame.SetSaved(prefix + "room", fromRight ? -roomId : roomId);
            AncientGame.SetSaved(prefix + "west", enteredFromWest ? 1 : 0);
            AncientGame.SetSaved(prefix + "opening", opening ? 1 : 0);
            StoreVitals();
            PlayerPrefs.Save();
        }
        public static void StoreVitals()
        {
            if (!Active) return;
            // 낙하 전 예약분은 저장 사본에만 돌려준다. 씬 안의 실제 보유량과 설치물은 바꾸지 않는다.
            StoreKit(game.hero, prefix, streetWeapon ? streetWeapon.CarrySnapshot : carry,
                streetWeapon ? streetWeapon.ReservedParticles : 0);
        }
        static void StoreKit(AncientHero hero, string key, SummonedBladeCarry equipment, int escrow)
        {
            float toStock = hero.ReservedFromStock(escrow);
            AncientGame.SetSaved(key + "equipped", equipment.equipped ? 1 : 0);
            AncientGame.SetSaved(key + "cooldownMs", Mathf.CeilToInt(equipment.cooldown * 1000));
            AncientGame.SetSaved(key + "particles", Mathf.RoundToInt((hero.particles + toStock) * 100));
            AncientGame.SetSaved(key + "captured", Mathf.RoundToInt(Mathf.Min(100,hero.CaptureEnergy + escrow - toStock) * 100));
            AncientGame.SetSaved(key + "hp", Mathf.RoundToInt(hero.hp * 100));
        }
        static SummonedBladeCarry RestoreKit(AncientHero hero, string key)
        {
            hero.particles = Mathf.Clamp(AncientGame.Saved(key + "particles", Mathf.RoundToInt(hero.particles * 100)) / 100f, 0, hero.MaxParticles);
            hero.RestoreStreetEnergy(AncientGame.Saved(key + "captured", Mathf.RoundToInt(hero.CaptureEnergy * 100)) / 100f);
            hero.hp = Mathf.Clamp(AncientGame.Saved(key + "hp", Mathf.RoundToInt(hero.hp * 100)) / 100f, .01f, hero.MaxHP);
            return new SummonedBladeCarry {
                equipped = AncientGame.Saved(key + "equipped") != 0,
                cooldown = Mathf.Max(0, AncientGame.Saved(key + "cooldownMs")) / 1000f
            };
        }
        static bool FieldReady(AncientGame owner)
        {
            return owner.Story && AncientStoryPassageRules.Eligible(owner.Story.CurrentFloor, owner.Story.Stage) &&
                !owner.won && !owner.hero.bike && !owner.ArenaActive && !(owner.City && owner.City.ArenaActive) &&
                !(owner.Production && owner.Production.Active) && !(owner.Finale && owner.Finale.Active);
        }
        public static void SaveField(AncientGame owner)
        {
            if (Active || !owner.HasSession || !owner.hero || !owner.hero.Loadout || AncientDeveloperTravel.IsVisiting) return;
            string key = SavePrefix() + "field.";
            bool ready = FieldReady(owner);
            AncientGame.SetSaved(key + "valid", ready ? 1 : 0);
            if (ready) StoreKit(owner.hero, key, owner.hero.Loadout.CarrySnapshot, owner.hero.Loadout.ReservedParticles);
        }
        public static void RestoreField(AncientGame owner)
        {
            if (AncientDeveloperTravel.IsVisiting || !FieldReady(owner)) return;
            string key = SavePrefix() + "field.";
            if (AncientGame.Saved(key + "valid") != 0)
                owner.hero.Loadout.EnterRoom(RestoreKit(owner.hero, key));
        }
        public static void ForgetField() => AncientGame.SetSaved(SavePrefix() + "field.valid", 0);

        public static void Exit(bool east, bool reachedEdge = true)
        {
            if (!Active) return;
            AncientGame owner = game;
            if (streetWeapon) carry = streetWeapon.LeaveRoom();
            streetWeapon = null;
            float x = AncientStoryPassageRules.Seam(group, opening) + (east ? 1.75f : -1.75f);
            float ground = opening ? AncientProductionLayout.GroundHeight(x) : 0;
            if (reachedEdge && east == enteredFromWest)
                AncientGame.SetSaved(prefix + "complete", AncientGame.Saved(prefix + "complete") | (1 << group));
            AncientGame.SetSaved(prefix + "room", 0);
            if (home.IsValid() && home.isLoaded) SceneManager.SetActiveScene(home);
            if (street.IsValid() && street.isLoaded)
                foreach (var root in street.GetRootGameObjects()) root.SetActive(false);
            PatrolEntry.EndStoryRoom();
            game = null;
            foreach (var root in suspended) if (root) root.SetActive(true);
            suspended.Clear();
            owner.hero.TravelTo(new Vector2(x, ground + 1.2f));
            if (owner.hero.Loadout) owner.hero.Loadout.EnterRoom(carry);
            owner.BlockGameplayInput();
            owner.SnapCameraToHero();
            owner.Save();
            if (street.IsValid() && street.isLoaded) SceneManager.UnloadSceneAsync(street);
            street = home = default;
            Time.timeScale = 1;
        }
    }
}
