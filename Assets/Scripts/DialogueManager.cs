using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 이거 추가

// 대화 한 줄을 담는 데이터 클래스 — 화자 이름과 대화 텍스트로 구성
[System.Serializable]
public class DialogueLine
{
    public string speaker; // 대화하는 화자의 이름
    public string text;    // 실제 대화 내용
}

// JSON 파일에서 불러온 대화 데이터 전체를 담는 래퍼 클래스
[System.Serializable]
public class DialogueData
{
    public List<DialogueLine> lines; // 대화 줄 목록 (순서대로 출력)
}

// 대화창 UI 흐름을 관리하는 클래스 — JSON 로드, 줄 전환, 종료 처리를 담당
public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;          // 대화창 전체 패널 오브젝트
    public TextMeshProUGUI nameText;          // 화자 이름을 표시하는 텍스트
    public TextMeshProUGUI conversationText;  // 대화 내용을 표시하는 텍스트

    [Header("Dialogue JSON")]
    public TextAsset dialogueJson; // Inspector에서 JSON 파일 드래그

    private List<DialogueLine> lines; // 파싱된 대화 줄 목록
    private int currentIndex = 0;    // 현재 표시 중인 대화 줄 인덱스

    // 시작 시 JSON 로드 후 대화 시작
    void Start()
    {
        LoadDialogue();
        dialoguePanel.SetActive(false);
        StartDialogue();
    }

    // JSON 배열 텍스트를 래퍼 오브젝트로 감싸서 파싱
    void LoadDialogue()
    {
        // JsonUtility는 최상위 배열을 직접 파싱하지 못해 {"lines":...} 형태로 감쌈
        string wrapped = "{\"lines\":" + dialogueJson.text + "}";
        DialogueData data = JsonUtility.FromJson<DialogueData>(wrapped);
        lines = data.lines;
    }

    // 대화를 처음부터 시작 — 인덱스 초기화 후 첫 줄 표시
    public void StartDialogue()
    {
        currentIndex = 0;
        dialoguePanel.SetActive(true);
        ShowLine();
    }

    // 다음 줄로 이동 — 마지막 줄이면 대화 종료
    public void NextLine()
    {
        currentIndex++;
        if (currentIndex < lines.Count)
        {
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    // 현재 인덱스의 화자·대화 텍스트를 UI에 반영
    void ShowLine()
    {
        nameText.text = lines[currentIndex].speaker;
        conversationText.text = lines[currentIndex].text;
    }

    // 대화 종료 — 대화창 패널 비활성화
    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // 마우스 왼쪽 클릭으로 다음 대화 줄로 이동
        if (dialoguePanel.activeSelf && Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextLine();
        }
    }
}