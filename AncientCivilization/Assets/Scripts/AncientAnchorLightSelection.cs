using System;

namespace Plugin.AncientCivilization
{
    // A bounded max heap retains the nearest visible anchor indices. Runtime owns
    // their positions: selected moving hooks are read from game.anchors each frame.
    // Begin/Consider allocate nothing and never sort the full world anchor list.
    public sealed class AncientAnchorLightSelection
    {
        readonly int[] slots;readonly double[] distances;int count;
        public int Capacity {get{return slots.Length;}}
        public int Count {get{return count;}}
        public AncientAnchorLightSelection(int capacity)
        {if(capacity<1)throw new ArgumentOutOfRangeException("capacity");slots=new int[capacity];distances=new double[capacity];}
        public void Begin(){count=0;}
        public int SlotAt(int index){return index>=0&&index<count?slots[index]:-1;}
        static bool Worse(double distance,int slot,double otherDistance,int otherSlot)
        {return distance>otherDistance||distance==otherDistance&&slot>otherSlot;}
        void Swap(int a,int b)
        {int slot=slots[a];slots[a]=slots[b];slots[b]=slot;double distance=distances[a];distances[a]=distances[b];distances[b]=distance;}
        public void Consider(int slot,int objectFloor,int currentFloor,float x,float y,float cameraX,float cameraY,float halfWidth,float halfHeight,float margin)
        {
            if(slot<0||objectFloor<0||currentFloor<0||!AncientChapterVisibility.PointVisible(objectFloor,currentFloor,x,y,cameraX,cameraY,halfWidth,halfHeight,margin))return;
            double dx=(double)x-cameraX,dy=(double)y-cameraY,distance=dx*dx+dy*dy;
            if(count<slots.Length)
            {
                int child=count++;slots[child]=slot;distances[child]=distance;
                while(child>0)
                {int parent=(child-1)/2;if(!Worse(distances[child],slots[child],distances[parent],slots[parent]))break;Swap(child,parent);child=parent;}
                return;
            }
            if(!Worse(distances[0],slots[0],distance,slot))return;
            slots[0]=slot;distances[0]=distance;int at=0;
            while(true)
            {
                int left=at*2+1;if(left>=count)break;int right=left+1,worst=left;
                if(right<count&&Worse(distances[right],slots[right],distances[left],slots[left]))worst=right;
                if(!Worse(distances[worst],slots[worst],distances[at],slots[at]))break;
                Swap(at,worst);at=worst;
            }
        }
    }
}
