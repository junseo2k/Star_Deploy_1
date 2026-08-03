using TMPro;
using UnityEngine;

// 대출/상환 패널을 관리하는 클래스
public class LoanPanel : MonoBehaviour
{
    public TextMeshProUGUI loanAmountText;   // 현재 대출금 표시
    public TextMeshProUGUI maxLoanText;      // 최대 대출 가능 금액 표시
    public TextMeshProUGUI inputAmountText;  // 현재 입력 금액 표시
    public TextMeshProUGUI noticeText;       // 오류/안내 메시지 표시
    public Transform loanPanel;              // 이 패널 루트 오브젝트
    public TextMeshProUGUI interestText; // 턴당 이자 표시

    private int inputAmount = 0;   // 현재 입력된 금액
    private int stepAmount = 100;  // +/- 버튼 1회 클릭 시 변경 단위

    // 패널이 열릴 때 UI 초기화
    void OnEnable()
    {
        inputAmount = 0;
        RefreshUI();
    }

    // UI 텍스트 전체 갱신
    void RefreshUI()
    {
        int maxLoan = GameManager.Instance.MaxLoan;
        loanAmountText.text = $"현재 대출금: {GameManager.Instance.loanAmount} G";
        maxLoanText.text = $"대출 가능 금액: {maxLoan} G";
        inputAmountText.text = $"입력 금액: {inputAmount} G";
        noticeText.text = "";
        interestText.text = $"턴당 이자: {GameManager.Instance.TurnInterest} G";
    }

    // + 버튼 클릭 시 입력 금액 증가
    public void OnPlusClick()
    {
        int maxLoan = GameManager.Instance.MaxLoan;
        if (inputAmount + stepAmount > maxLoan)
        {
            noticeText.text = "대출 한도를 초과했습니다.";
            return;
        }
        inputAmount += stepAmount;
        inputAmountText.text = $"입력 금액: {inputAmount} G";
        noticeText.text = "";
    }

    // - 버튼 클릭 시 입력 금액 감소
    public void OnMinusClick()
    {
        if (inputAmount - stepAmount < 0)
        {
            noticeText.text = "0보다 작을 수 없습니다.";
            return;
        }
        inputAmount -= stepAmount;
        inputAmountText.text = $"입력 금액: {inputAmount} G";
        noticeText.text = "";
    }

    // 대출 확인 버튼 클릭
    public void OnLoanClick()
    {
        if (inputAmount <= 0)
        {
            noticeText.text = "금액을 입력해주세요.";
            return;
        }
        bool success = GameManager.Instance.TakeLoan(inputAmount);
        if (success)
        {
            noticeText.text = $"{inputAmount} G 대출 완료!";
            inputAmount = 0;
            RefreshUI();
        }
        else
        {
            noticeText.text = "대출 한도를 초과했습니다.";
        }
    }

    // 상환 버튼 클릭
    public void OnRepayClick()
    {
        if (inputAmount <= 0)
        {
            noticeText.text = "금액을 입력해주세요.";
            return;
        }
        bool success = GameManager.Instance.RepayLoan(inputAmount);
        if (success)
        {
            noticeText.text = $"{inputAmount} G 상환 완료!";
            inputAmount = 0;
            RefreshUI();
        }
        else
        {
            noticeText.text = "골드가 부족합니다.";
        }
    }

    // 닫기 버튼 클릭
    public void OnCloseClick()
    {
        UIManager.instance.ReturnToEntire(loanPanel.gameObject);
    }
}