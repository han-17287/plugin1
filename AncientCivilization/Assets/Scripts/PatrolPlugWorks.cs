using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        PlugDistrictDevice plugWorks;
        BluePlugSocket[] machineSockets;
        int poweredSocket = -1;
        bool worksUsed, worksSubscribed;

        void InstallPlugWorks(int id)
        {
            if (!PatrolCatalog.TryPowerSite(id, out var origin, out bool secondSite)) return;
            int floor = (id - 1) / 10;
            plugWorks = PlugDistrictDevice.Spawn(transform, floor, origin, secondSite);
            machineSockets = new BluePlugSocket[plugWorks.SocketCount];
            for (int i = 0; i < machineSockets.Length; ++i)
            {
                var holder = new GameObject(plugWorks.Label + (i == 0 ? " · A" : " · B"));
                holder.transform.SetParent(transform, false);
                holder.transform.position = plugWorks.SocketPoint(i);
                var port = holder.AddComponent<BluePlugSocket>();
                port.MarkPowerOutlet();
                port.Configure(holder.transform.InverseTransformPoint(plugWorks.TowPoint(i)),
                    BluePlugSkill.Tow | BluePlugSkill.Swing, Vector2.right);
                machineSockets[i] = port;
                RegisterBluePlugSocket(port);
            }
            SyncWorksSockets();
            if (isActiveAndEnabled) SubscribeWorks();
        }

        void SubscribeWorks()
        {
            if (worksSubscribed) return;
            worksSubscribed = true;
            bluePlug.SeatedSocketChanged += ChangeWorksSocket;
            bluePlug.LinkReset += ResetWorks;
        }

        void OnEnable()
        {
            // AddComponent 직후에는 아직 방을 구성하지 않았다.
            if (plugWorks && bluePlug) SubscribeWorks();
        }

        void OnDisable()
        {
            if (!plugWorks || !bluePlug) return;
            worksSubscribed = false;
            bluePlug.SeatedSocketChanged -= ChangeWorksSocket;
            bluePlug.LinkReset -= ResetWorks;
            ResetWorks();
        }

        void ChangeWorksSocket(BluePlugSocket next)
        {
            if (poweredSocket >= 0)
            {
                plugWorks.Release(poweredSocket);
                poweredSocket = -1;
            }
            if (!next) return;
            for (int i = 0; i < machineSockets.Length; ++i)
            {
                if (next != machineSockets[i] || !plugWorks.SocketAvailable(i)) continue;
                poweredSocket = i;
                worksUsed = true;
                plugWorks.Dock(i);
                SyncWorksSockets();
                break;
            }
        }

        void StepPlugWorks(float dt)
        {
            if (!plugWorks || !worksSubscribed) return;
            // 손잡이를 설비 옆에 설치하면 장착하거나 방전될 때까지 급전한다.
            var selectedPort = bluePlug.SeatedSocket;
            bool selectedMachine = false;
            foreach (var port in machineSockets) if (port == selectedPort) selectedMachine = true;
            if (!selectedMachine)
            {
                selectedPort = null;
                foreach (var port in machineSockets)
                    if (port.Operational && bluePlug.SuppliesAt(port.Point)) { selectedPort = port; break; }
            }
            if ((poweredSocket >= 0 ? machineSockets[poweredSocket] : null) != selectedPort)
                ChangeWorksSocket(selectedPort);
            plugWorks.Step(dt, officer.position);
            SyncWorksSockets();
        }

        void SyncWorksSockets()
        {
            for (int i = 0; i < machineSockets.Length; ++i)
            {
                var holder = machineSockets[i].gameObject;
                bool available = plugWorks.SocketAvailable(i);
                if (holder.activeSelf != available) holder.SetActive(available);
            }
        }

        void ResetWorks()
        {
            if (!worksUsed) return;
            poweredSocket = -1;
            worksUsed = false;
            plugWorks.ResetDevice();
            SyncWorksSockets();
        }
    }
}
