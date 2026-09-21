using UnityEngine;

// 게임에 등록된 모든 주식 데이터를 들고 있는 저장소
//
// 원래는 종족/산업 서브탭 UI(드워프 탭·광업 탭 …)를 관리하던 컴포넌트였으나,
// 종목 목록 화면이 CurrentMarketScreen + MarketScreenBinder로 교체되면서
// 탭 UI 로직은 전부 제거했다. 지금은 allstock 배열을 보관하는 역할만 한다.
//
// GameManager가 종목을 찾을 때 항상 여기를 거치므로 씬에서 이 컴포넌트를 지우면 안 된다.
public class TabManager : MonoBehaviour
{
    public Stock[] allstock; // 게임에 등록된 모든 주식 데이터

    void Start()
    {
        // 새 게임을 시작할 때 이전 플레이에서 남은 종목별 이슈 표시를 비운다
        // (Stock은 ScriptableObject라 플레이 모드를 나가도 값이 남는다)
        foreach (Stock stock in allstock)
            stock.runtimeIssues.Clear();
    }
}
