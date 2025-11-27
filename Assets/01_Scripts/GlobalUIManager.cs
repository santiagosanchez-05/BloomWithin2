using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    private static GlobalUIManager instance;

    [Header("Prefabs Globales")]
    public GameObject pauseMenuPrefab;

    void Awake()
    {
        // Si ya existe uno, destruir el duplicado
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 🔹 persiste entre escenas

        // Si el menú no existe en la escena, lo instanciamos
        if (pauseMenuPrefab != null && FindObjectOfType<PauseMenu>() == null)
        {
            Instantiate(pauseMenuPrefab);
        }
    }
}