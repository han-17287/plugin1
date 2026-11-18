namespace Plugin.AncientCivilization
{
    // Pure playback state keeps hidden time out of both typing and the final reading delay.
    // Four pending slots are bounded; overflow joins the last pending message rather than dropping words.
    internal sealed class AncientRouteRadioPlayback
    {
        struct Message
        {
            public string Speaker,Text;
            public Message(string speaker,string text){Speaker=speaker;Text=text;}
        }
        const int Capacity=4;
        readonly Message[] pending=new Message[Capacity];int head,count;
        string speaker,text,visibleText="";int pageStart,pageEnd,revealed;
        float clock,nextLetter,completedAt=-1;bool pageReady;
        public bool Active {get{return text!=null;}}
        public string Speaker {get{return speaker;}}
        public string Text {get{return text;}}
        public string VisibleText {get{return visibleText;}}
        public int PageStart {get{return pageStart;}}
        public bool PageReady {get{return pageReady;}}
        public bool Complete {get{return pageReady&&revealed>=pageEnd;}}
        static string DisplayName(string value)
        {
            const string suffix=" · 무전";
            if(!string.IsNullOrEmpty(value)&&value.EndsWith(suffix,System.StringComparison.Ordinal))value=value.Substring(0,value.Length-suffix.Length);
            if(string.IsNullOrEmpty(value)||value=="통신관"||value=="사령관")value="사령관";
            return value.Replace("백명 경위","백 경위").Replace("백명","백 경위")+suffix;
        }
        public void Show(string who,string words)
        {
            if(string.IsNullOrWhiteSpace(words))return;
            var message=new Message(DisplayName(who),words.Replace("백명 경위","백 경위").Replace("백명","백 경위"));
            if(!Active){Begin(message);return;}
            if(count<Capacity){pending[(head+count)%Capacity]=message;count++;return;}
            int last=(head+count-1)%Capacity;var tail=pending[last];
            tail.Text+="\n"+(tail.Speaker==message.Speaker?"":message.Speaker+"\n")+message.Text;pending[last]=tail;
        }
        void Begin(Message message)
        {speaker=message.Speaker;text=message.Text;pageStart=revealed=0;BeginPage();}
        void BeginPage()
        {pageEnd=pageStart;pageReady=false;visibleText="";clock=0;nextLetter=.04f;completedAt=-1;}
        static bool SplitsPair(string value,int end)
        {return end>0&&end<value.Length&&char.IsHighSurrogate(value[end-1])&&char.IsLowSurrogate(value[end]);}
        public void SetPageEnd(int end)
        {
            if(!Active)return;
            end=System.Math.Max(pageStart+1,System.Math.Min(text.Length,end));
            if(SplitsPair(text,end))end--;
            if(end<=pageStart)end=System.Math.Min(text.Length,pageStart+(pageStart+1<text.Length&&char.IsHighSurrogate(text[pageStart])&&char.IsLowSurrogate(text[pageStart+1])?2:1));
            if(!pageReady||end!=pageEnd)
            {
                pageEnd=end;revealed=System.Math.Max(pageStart,System.Math.Min(pageEnd,revealed));
                visibleText=text.Substring(pageStart,revealed-pageStart);completedAt=-1;
            }
            pageReady=true;
        }
        public void Tick(float seconds,bool visible)
        {
            if(!visible||!Active||!pageReady||float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return;
            clock+=seconds;
            int before=revealed;
            if(clock-nextLetter>.3f)nextLetter=clock-.1f;
            for(int i=0;i<4&&revealed<pageEnd&&clock>=nextLetter;i++)
            {
                char letter=text[revealed++];
                if(char.IsHighSurrogate(letter)&&revealed<pageEnd&&char.IsLowSurrogate(text[revealed]))revealed++;
                nextLetter+=.04f+(letter=='.'||letter=='?'||letter=='!'||letter=='…'?.12f:letter==','?.06f:0);
            }
            if(before!=revealed)visibleText=text.Substring(pageStart,revealed-pageStart);
            if(revealed<pageEnd)return;
            if(completedAt<0)completedAt=clock;
            if(clock-completedAt<1.5f)return;
            if(pageEnd<text.Length){pageStart=revealed=pageEnd;BeginPage();return;}
            text=speaker=null;visibleText="";pageReady=false;
            if(count>0)
            {var message=pending[head];pending[head]=default(Message);head=(head+1)%Capacity;count--;Begin(message);}
        }
    }
}
