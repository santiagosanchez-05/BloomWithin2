using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Asignar el Canvas del menú
    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // 🔹 Reanuda el juego
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // 🔹 Detiene el tiempo
        isPaused = true;
    }

    public void ExitGame()
    {
        // Puedes regresar al menú principal o cerrar el juego
        Time.timeScale = 1f; // Asegura restaurar el tiempo antes de salir
        SceneManager.LoadScene("Menu"); // Cambia por tu escena de menú
        // Si es un build final, podrías usar:
        // Application.Quit();
    }
}