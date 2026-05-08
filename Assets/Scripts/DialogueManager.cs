using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 이거 추가
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
}

[System.Serializable]
public class DialogueData
{
    public List<DialogueLine> lines;
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI conversationText;

    [Header("Dialogue JSON")]
    public TextAsset dialogueJson; // Inspector에서 JSON 파일 드래그

    private List<DialogueLine> lines;
    private int currentIndex = 0;

    void Start()
    {
        LoadDialogue();
        dialoguePanel.SetActive(false);
        StartDialogue(); // 추가
    }

    void LoadDialogue()
    {
        // JSON 배열을 감싸서 파싱
        string wrapped = "{\"lines\":" + dialogueJson.text + "}";
        DialogueData data = JsonUtility.FromJson<DialogueData>(wrapped);
        lines = data.lines;
    }

    public void StartDialogue()
    {
        currentIndex = 0;
        dialoguePanel.SetActive(true);
        ShowLine();
    }

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

    void ShowLine()
    {
        nameText.text = lines[currentIndex].speaker;
        conversationText.text = lines[currentIndex].text;
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // 클릭 또는 스페이스바로 다음 대화
        if (dialoguePanel.activeSelf && Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextLine();
        }
    }
}