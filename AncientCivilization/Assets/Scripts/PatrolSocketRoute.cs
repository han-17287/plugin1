using UnityEngine;

namespace Plugin.PlayerCore
{
    public sealed partial class PatrolRoom
    {
        bool socketRoute;
        bool RestingAnywhere => bluePlug.WallResting;
        bool SocketSwinging => bluePlug.Phase == BluePlugPhase.Swinging;
        bool SocketTowing => bluePlug.Phase == BluePlugPhase.Towing;

        bool PluggedInto(GameObject socket) => socket && bluePlug.SeatedSocket && bluePlug.SeatedSocket.gameObject == socket;
        bool Powered(GameObject socket) => PluggedInto(socket) || socket && bluePlug.SuppliesAt(socket.transform.position);
        bool Resting(GameObject socket) => socket && bluePlug.RestingSocket && bluePlug.RestingSocket.gameObject == socket;

        GameObject WallOutlet(string label, float x, float y, int outwardSign, float height = 1.6f)
        {
            int outward = outwardSign < 0 ? -1 : 1;
            var holder = Hook(label, x, y);
            // 투척 핀이 벽보다 먼저 닿는다. 견인 도착점은 경위의 몸통까지 벽 밖에 남긴다.
            const float recess = .17f, depth = .48f;
            var rail = Box(label + " 고정대", x - outward * (recess + depth * .5f), y,
                depth, height, new Color(.12f, .17f, .21f), true, 3);
            rail.transform.SetParent(holder.transform, true);
            var grip = PlugDistrictArt.Part(rail.transform, "벽 손잡이", (room - 1) / 10,
                PlugDistrictPiece.Fixture, Vector2.zero, new Vector2(depth, height), 4);
            grip.flipX = outward > 0;
            var socket = holder.GetComponent<BluePlugSocket>();
            socket.ConfigureWallRest(rail.GetComponent<BoxCollider2D>(), outward);
            socket.Configure(new Vector2(outward * .255f, 0), BluePlugSkill.Tow | BluePlugSkill.Swing,
                new Vector2(-outward, 0));
            socket.MarkPowerOutlet();
            return holder;
        }

        void BuildSocketStreet(int id)
        {
            if (id == 1) BuildTowBench(40);
            if (id == 11)
            {
                BuildTowBench(4);
                BuildSwingBench(7);
            }
            if (id <= 20) BuildSocketLower(id);
            else if (id <= 30) BuildSocketAdmin(id);
            else if (id <= 40) BuildSocketResidence(id);
            else BuildSocketSummit(id);
        }
    }
}
