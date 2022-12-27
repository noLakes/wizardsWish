using System.Collections;
using UnityEngine;

public class DayAndNightCycler : MonoBehaviour
{
    public Transform starsTransform;

    private float starsRefreshRate;
    private float rotationAngleStep;
    private Vector3 rotationAxis;
    private Coroutine starsCoroutine;

    private void Start()
    {
        // apply initial rotation on stars
        starsTransform.rotation = Quaternion.Euler(
            Game.Instance.gameGlobalParameters.dayInitialRatio * 360f,
            -30f,
            0f
        );
        // compute relevant calculation parameters
        starsRefreshRate = 0.1f;
        rotationAxis = starsTransform.right;
        rotationAngleStep = 360f * starsRefreshRate / Game.Instance.gameGlobalParameters.dayLengthInSeconds;

        if(!Game.Instance.gameIsPaused)
        {
            starsCoroutine = StartCoroutine("UpdateStars");
        }
    }

    private IEnumerator UpdateStars()
    {
        while (true)
        {
            starsTransform.Rotate(rotationAxis, rotationAngleStep, Space.World);
            yield return new WaitForSeconds(starsRefreshRate);
        }
    }

    private void OnPauseGame()
    {
        if(starsCoroutine != null)
        {
            StopCoroutine(starsCoroutine);
            starsCoroutine = null;
        }
    }

    private void OnResumeGame()
    {
        if(starsCoroutine == null)
        {
            starsCoroutine = StartCoroutine(UpdateStars());
        }
    }

    private void OnEnable()
    {
        EventManager.AddListener("PauseGame", OnPauseGame);
        EventManager.AddListener("ResumeGame", OnResumeGame);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("PauseGame", OnPauseGame);
        EventManager.RemoveListener("ResumeGame", OnResumeGame);
    }
}