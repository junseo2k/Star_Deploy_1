// 불확정 뉴스의 대기 상태를 나타내는 클래스
// 불확정 뉴스는 발생 즉시 가격에 영향을 주지 않고, daysLeft가 0이 되는 시점에
// GameManager.AddActiveNews()로 넘어가 그때부터 진짜 ActiveCase로 동작함
public class PendingNews
{
    public Case caseData; // 대기 중인 뉴스 데이터
    public int daysLeft;  // 실제 효과가 나타나기까지 남은 턴 수
}