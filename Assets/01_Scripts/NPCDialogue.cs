using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class NPCDialogue : MonoBehaviour
{
    [Header("Configuración del diálogo")]
    [TextArea(2, 5)]
    public string dialogueText = "Hola viajero, ten cuidado más adelante...";
    public float textSpeed = 0.03f;
    public float displayTime = 3f;

    [Header("Referencias UI")]
    public GameObject dialoguePanel; // panel de diálogo (Canvas hijo del NPC o global)
    public TMP_Text dialogueTMP;     // si usas TextMeshPro
    // public Text dialogueUI;       // usa esta si prefieres el Text normal

    private bool isPlayerNearby = false;
    private bool isShowingDialogue = false;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerNearby && !isShowingDialogue)
        {
            StartCoroutine(ShowDialogue());
        }
    }

    private IEnumerator ShowDialogue()
    {
        isShowingDialogue = true;
        dialoguePanel.SetActive(true);

        // limpia texto
        dialogueTMP.text = "";

        foreach (char c in dialogueText)
        {
            dialogueTMP.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        yield return new WaitForSeconds(displayTime);
        dialoguePanel.SetActive(false);
        isShowingDialogue = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            isPlayerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            isPlayerNearby = false;
    }
}