namespace Plugin.AncientCivilization
{
    public sealed class AncientEvidenceQuestion
    {
        public readonly int Mission,Answer;public readonly string Prompt,Feedback;public readonly string[] Options;
        public AncientEvidenceQuestion(int mission,int answer,string prompt,string feedback,params string[] options)
        {Mission=mission;Answer=answer;Prompt=prompt;Feedback=feedback;Options=options;}
    }
    public static class AncientEvidenceQuestions
    {
        public static readonly AncientEvidenceQuestion[] All={
            new AncientEvidenceQuestion(3,1,"인계표에서 먼저 확인해야 할 모순은?","수량은 일치한다. 문제는 별도 이송 승인이 검거보다 먼저 내려왔다는 시각이다.","수량이 맞으니 인계 과정에도 문제가 없다","검거 이전에 이미 별도 이송이 승인됐다","실물과 전표의 코어 수량이 다르다"),
            new AncientEvidenceQuestion(7,2,"빈 수령처를 추적하려면 어떤 기록이 필요한가?","지역 장부는 일부 거래만 보여 준다. 수령처를 수정한 행정구 기록을 추적해야 한다.","공란인 물량을 모두 지역 조직의 절도로 확정한다","목적지가 없으니 더 이상 확인할 수 없다","수정 이력이 가리키는 행정구 원본을 확인한다"),
            new AncientEvidenceQuestion(10,0,"원본과 사본의 시간 순서가 보여 주는 것은?","사본은 수정 전에 전송됐다. 이후 바뀐 원본이 과거 사본의 내용을 지울 수는 없다.","사본 전송 뒤 원본의 수령처가 지워졌다","사본이 원본 수정 뒤 새로 작성됐다","수량이 달라 두 기록을 비교할 수 없다"),
            new AncientEvidenceQuestion(13,2,"설비 계약과 첨부 명단을 함께 보면 무엇이 드러나는가?","계약은 설비 이동이라지만 실제 첨부 대상은 주민과 주소다. 두 문서를 분리하면 이동 대상이 감춰진다.","설비 계약이므로 주민 명단은 별개로 처리한다","주소가 같으니 주민 서명도 자동으로 유효하다","설비 재배치 명목으로 주민을 이송하고 있다"),
            new AncientEvidenceQuestion(15,1,"점수 승인과 이동 보류가 동시에 가능한 이유는?","기존 층의 권한은 유지된다. 최상층은 시민 점수와 별도의 직할 승인으로 통제된다.","점수를 더 모으면 최상층 직할 승인을 대체할 수 있다","최상층은 포인트 외의 직할 승인이 필요하다","이미 얻었던 모든 층의 통행권이 사라졌다"),
            new AncientEvidenceQuestion(17,0,"접수 대기열과 삭제 시각이 말해 주는 것은?","신청 번호는 남았지만 본문은 개별 심사 전에 삭제됐다. 신청이 없었던 것도, 심사 결과가 나온 것도 아니다.","신청자들은 개별 심사 기회조차 받지 못했다","신청한 사람이 아무도 없었다","신청이 전부 승인돼 자동으로 삭제됐다"),
            new AncientEvidenceQuestion(19,1,"연구 색인과 오늘의 승인서를 연결하는 근거는?","매몰 문명 연구와 코어 회수 계획이 같은 승인번호를 쓴다. 오래된 연구가 오늘의 물량 이송과 연결돼 있다.","노트가 오래됐으므로 오늘 작전과 무관하다","오늘의 코어가 매몰 문명 회수 작전에 쓰인다","코어가 이미 도착했으므로 작전은 끝났다")
        };
        public static AncientEvidenceQuestion Find(int mission)
        {foreach(var question in All)if(question.Mission==mission)return question;return null;}
        public static bool CanCompare(int mission,int tasks,int node)
        {return node==2&&(tasks&3)==3&&Find(mission)!=null;}
    }
}
