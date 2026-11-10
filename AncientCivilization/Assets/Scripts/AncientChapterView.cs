using UnityEngine;

namespace Plugin.AncientCivilization
{
    // Traversal objects remain active near the physical camera even after their
    // case has been completed. Graphics quality never changes their collisions.
    public static class AncientChapterView
    {
        public static bool NearCamera(AncientGame game,Vector2 point,int floor)
        {
            if(!game||!game.Story||!game.View)return false;
            var view=game.View;Vector2 camera=view.transform.position;
            return AncientChapterVisibility.PointVisible(floor,game.Story.CurrentFloor,point.x,point.y,camera.x,camera.y,
                view.orthographicSize*view.aspect,view.orthographicSize,AncientChapterVisibility.GimmickMargin);
        }
    }
}
