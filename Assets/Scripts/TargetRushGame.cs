using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TargetRushGame : MonoBehaviour
{
    public static TargetRushGame Instance { get; private set; }

    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private TargetController[] targets;
    [SerializeField] private Text scoreLabel;
    [SerializeField] private Text timerLabel;
    [SerializeField] private Text streakLabel;
    [SerializeField] private Text statusLabel;
    [SerializeField] private Text helpLabel;

    private float timeRemaining;
    private int score;
    private int streak;
    private bool roundActive;
    private Coroutine statusRoutine;
    private TargetController restartGazeTarget;
    private float restartGazeTime;

    public bool RoundActive => roundActive;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RestartRound();
    }

    private void Update()
    {
        if (roundActive)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                EndRound();
            }
        }
        else if (InputSystemBridge.RestartPressed() || GazeRestartPressed())
        {
            RestartRound();
        }

        RefreshHud();
    }

    public void RestartRound()
    {
        score = 0;
        streak = 0;
        timeRemaining = roundDuration;
        roundActive = true;
        restartGazeTarget = null;
        restartGazeTime = 0f;

        if (statusRoutine != null)
        {
            StopCoroutine(statusRoutine);
        }

        statusLabel.text = "READY — FIND A TARGET";
        helpLabel.text = "LOOK AT A TARGET TO FIRE  •  TAP SCREEN TO FIRE";

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].ResetTarget(true);
            }
        }
    }

    public void RegisterHit(TargetController target)
    {
        if (!roundActive)
        {
            return;
        }

        streak++;
        int multiplier = Mathf.Clamp(1 + (streak - 1) / 3, 1, 5);
        int points = target.PointValue * multiplier;
        score += points;

        string message = string.Format("DIRECT HIT  +{0}   x{1}", points, multiplier);
        ShowStatus(message, Color.cyan);
        target.ResetTarget(false);
    }

    public void RegisterMiss()
    {
        if (!roundActive)
        {
            return;
        }

        streak = 0;
        ShowStatus("MISS — STREAK RESET", new Color(1f, 0.45f, 0.2f));
    }

    private void EndRound()
    {
        roundActive = false;
        ShowStatus(string.Format("TIME!  FINAL SCORE  {0}", score), Color.yellow);
        helpLabel.text = "LOOK AT A TARGET OR TAP SCREEN TO PLAY AGAIN";
    }

    private bool GazeRestartPressed()
    {
        Camera viewCamera = Camera.main;
        if (viewCamera == null)
        {
            return false;
        }

        RaycastHit hit;
        TargetController target = Physics.Raycast(
            viewCamera.transform.position, viewCamera.transform.forward, out hit, 20f)
            ? hit.collider.GetComponent<TargetController>()
            : null;

        if (target == null)
        {
            restartGazeTarget = null;
            restartGazeTime = 0f;
            return false;
        }

        if (target != restartGazeTarget)
        {
            restartGazeTarget = target;
            restartGazeTime = 0f;
        }

        restartGazeTime += Time.deltaTime;
        return restartGazeTime >= 0.75f;
    }

    private void ShowStatus(string message, Color color)
    {
        statusLabel.color = color;
        statusLabel.text = message;

        if (statusRoutine != null)
        {
            StopCoroutine(statusRoutine);
        }

        statusRoutine = StartCoroutine(ClearStatusAfterDelay(1.25f));
    }

    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (roundActive)
        {
            statusLabel.color = Color.white;
            statusLabel.text = "KEEP MOVING";
        }
    }

    private void RefreshHud()
    {
        scoreLabel.text = string.Format("SCORE  {0:0000}", score);
        timerLabel.text = string.Format("TIME  {0:00}", Mathf.CeilToInt(timeRemaining));
        streakLabel.text = streak > 0
            ? string.Format("STREAK  x{0}", Mathf.Clamp(1 + (streak - 1) / 3, 1, 5))
            : "STREAK  —";

        if (timeRemaining <= 10f && roundActive)
        {
            timerLabel.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 3f, 1f));
        }
        else
        {
            timerLabel.color = Color.white;
        }
    }
}
