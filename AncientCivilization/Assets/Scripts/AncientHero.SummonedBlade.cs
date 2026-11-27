using Plugin.PlayerCore;
using UnityEngine;

namespace Plugin.AncientCivilization
{
    public sealed partial class AncientHero
    {
        public SummonedBlade Loadout { get; private set; }
        public int SummonParticles => Mathf.FloorToInt(Mathf.Max(0, particles) + Mathf.Max(0, CaptureEnergy));
        ElectricPlugCombat plugElectricity;
        bool autoEquipPending, caughtFalling;
        float equipTravelDistance;
        float lastStockSpent;
        PlayerMovementData campaignMovement;
        Collider2D ignoredPlatform;
        float dashUntil, dashReady, dashBufferedUntil, dashFacing, dropUntil;
        int usedAirDashes;

        void InitializeSummonedEquipment(AncientGame game)
        {
            campaignMovement = Resources.Load<PlayerMovementData>("PlayerCore/DefaultMovement");
            plugElectricity = ElectricPlugCombat.Attach(gameObject, plugSettings);
            Loadout = gameObject.AddComponent<SummonedBlade>();
            Loadout.Configure(aimCamera, () => SummonParticles, SpendSummonParticles,
                amount => AddCapturedEnergy(amount), BladeArea,
                seconds => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds),
                message => game.Notice(message, 1.5f), UnsafeInstallGround, RefundSummonParticles);
            plugPorts.BladeSupplies=Loadout.Supplies;
        }

        internal bool SpendSummonParticles(int amount)
        {
            if (amount < 0 || amount > SummonParticles) return false;
            // 마지막 정수 단위까지 소비하는 과부하는 양쪽 저장고의 소수 잔량도 비운다.
            lastStockSpent=Mathf.Min(particles,amount);
            if (amount > 0 && amount == SummonParticles) { particles = 0; CaptureEnergy = 0; return true; }
            float fromStock = Mathf.Min(particles, amount);
            particles -= fromStock;
            CaptureEnergy = Mathf.Max(0, CaptureEnergy - (amount - fromStock));
            return true;
        }

        internal float ReservedFromStock(int escrow) =>
            Mathf.Min(escrow,lastStockSpent,Mathf.Max(0,MaxParticles-particles));

        internal void RefundSummonParticles(int amount)
        {
            float toStock=ReservedFromStock(amount);
            particles+=toStock;
            CaptureEnergy=Mathf.Min(100,CaptureEnergy+amount-toStock);
        }

        bool UnsafeInstallGround(Vector2 ground)
        {
            var g = AncientGame.Instance;
            if (g.Production && g.Production.DeckCombat && !g.Production.IsDeckGround(ground)) return true;
            if (g.Orbital && g.Orbital.Arena != null && g.Orbital.Arena.UnsafeBladeGround(ground)) return true;
            if (g.Enforcer && g.Enforcer.UnsafeBladeGround(ground)) return true;
            if (g.Sniper && g.Sniper.UnsafeBladeGround(ground)) return true;
            return g.ArenaActive && g.boss && (ground.x < g.boss.ArenaLeft + .6f || ground.x > g.boss.ArenaRight - .6f);
        }

        void BladeArea(Vector2 centre, float radius, float damage, SummonedBladeHit kind)
        {
            var game = AncientGame.Instance;
            plugElectricity.Area(centre, radius, damage, kind);
            if (kind == SummonedBladeHit.Field || !game || game.won) return;
            if (game.Enforcer) game.Enforcer.BreakObjectsInRadius(centre, radius);
            if (game.Sniper) game.Sniper.BladeBurst(centre, radius, damage);
            if (game.Orbital && game.Orbital.Arena != null) game.Orbital.Arena.BreakSupplyInRadius(centre,radius);
        }

        bool TickSummonedInput(AncientGame game)
        {
            Loadout.ConfinedDeckFallback=game.Production && game.Production.DeckCombat;
            // 이동·피격·탑승 중 장비 버튼을 다음 상태로 넘기지 않는다.
            if (bike || wired || !body.simulated || MeleeInterrupted || !Application.isFocused ||
                (game.Production && game.Production.Active && !game.Production.DeckCombat)) return false;
            if (AttackMode && Input.GetKeyDown(KeyCode.E))
            {
                // 조종석 바로 옆에서는 전투용 승하차가 우선이다.
                if (game.Sniper && game.Sniper.TryInteract()) return true;
                Combat.CancelAttacks();
                Loadout.TryDetach();
                CancelCampaignPlug();
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Loadout.TrySummon();
                return true;
            }
            if (AttackMode && Input.GetKeyDown(KeyCode.R))
            {
                if (Loadout.TryOverload()) Combat.CancelAttacks();
                return true;
            }
            return false;
        }

        void ResetSummonedEquipment()
        {
            if (Loadout) Loadout.ResetEncounter();
            if (plugElectricity) plugElectricity.ResetCombat();
            autoEquipPending = caughtFalling = false;
            dashUntil = dashReady = dashBufferedUntil = 0;
            usedAirDashes = 0;
            RestoreThinPlatform();
        }

        void PresentEquippedBlade()
        {
            if (!AttackMode || plugCharacterMotion == null) return;
            if (Combat && Combat.BladeAnimating && !Combat.LegacyFinale)
            {
                int step=Combat.BladeStep, frame=Combat.DrawnCutFrame;
                Loadout.SetEquippedPose(AncientBladeMotion.World(art,AncientBladeAtlas.ReelLocal(step,frame)),
                    AncientBladeMotion.World(art,AncientBladeAtlas.HiltLocal(step,frame)),Combat.StrikeDirection,false);
                return;
            }
            Vector2 right = plugCharacterMotion.RightGripWorld;
            Vector2 pointing = new Vector2(direction, .48f);
            if (Combat && Combat.Charging && aimCamera)
                pointing = (Vector2)aimCamera.ScreenToWorldPoint(Input.mousePosition) - right;
            Loadout.SetEquippedPose(plugCharacterMotion.LeftGripWorld, right, pointing, !Combat || !Combat.BladeAnimating);
        }

        void RestoreThinPlatform()
        {
            if (ignoredPlatform && heroCollider) Physics2D.IgnoreCollision(heroCollider, ignoredPlatform, false);
            ignoredPlatform = null;
        }

        bool TryDropThinPlatform()
        {
            if (!campaignMovement || bike || wired || !Grounded()) return false;
            var hit = Physics2D.Raycast(body.position, Vector2.down, heroCollider.size.y * .5f + .18f, 1 << 8);
            if (!hit.collider || !hit.collider.usedByEffector ||
                !hit.collider.TryGetComponent<PlatformEffector2D>(out var effector) || !effector.useOneWay) return false;
            RestoreThinPlatform();
            ignoredPlatform = hit.collider;
            Physics2D.IgnoreCollision(heroCollider, ignoredPlatform, true);
            dropUntil = Time.time + campaignMovement.dropDuration;
            lastGrounded = jumpBufferedUntil = -10;
            body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y,-3));
            return true;
        }

        bool StepShortDash(bool grounded, ref Vector2 velocity)
        {
            if (!campaignMovement || bike || wired || !body.simulated || MeleeInterrupted)
            { dashUntil = dashBufferedUntil = 0; return false; }
            if (grounded) usedAirDashes = 0;
            if (dashBufferedUntil > Time.time && Time.time >= dashReady && (grounded || usedAirDashes < campaignMovement.airDashes))
            {
                dashBufferedUntil = 0;
                dashUntil = Time.time + campaignMovement.dashDuration;
                dashReady = dashUntil + campaignMovement.dashCooldown;
                dashFacing = Mathf.Abs(axis) > .1f ? Mathf.Sign(axis) : direction;
                if (!grounded) usedAirDashes++;
            }
            if (Time.time >= dashUntil) return false;
            float distance = campaignMovement.dashSpeed * Time.fixedDeltaTime;
            Vector2 heading = Vector2.right * dashFacing;
            float clear = CampaignBodyTravel(heading,distance,false);
            velocity = heading * (clear / Time.fixedDeltaTime);
            if (clear < distance - .001f) dashUntil = 0;
            return true;
        }
    }
}
