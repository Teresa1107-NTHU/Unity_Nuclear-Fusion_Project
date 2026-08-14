using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

public class FusionController : MonoBehaviour
{
    private enum FusionPhase
    {
        Idle,       // 尚未開始
        Running,    // 反應中
        Finished    // 反應結束，停在最後畫面
    }

    // ===== Scene objects =====
    [Header("Scene References")]
    [SerializeField] private Transform proton;    // 場景中的 H-1
    [SerializeField] private Transform boron11;   // 場景中的 B-11
    [SerializeField] private Transform meetPoint; // 反應中心

    // ===== UI =====
    [Header("UI")]
    [SerializeField] private Button startButton;       // 單一按鈕：Start / Restart
    [SerializeField] private TMP_Text startButtonText;   // 按鈕上的字
    [SerializeField] private Button pauseButton;       // 可選：Pause / Resume
    [SerializeField] private TMP_Text pauseButtonText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider energyBar;
    [SerializeField] private TMP_Text energyText;

    // ===== Nucleus Prefabs =====
    [Header("Nucleus Prefabs")]
    [SerializeField] private GameObject nucleusC12Prefab;
    [SerializeField] private GameObject nucleusBe8Prefab;
    [SerializeField] private GameObject nucleusHe4Prefab;

    // ===== Timing =====
    [Header("Timing")]
    [SerializeField] private float approachTime = 2.0f; // H & B 靠近時間
    [SerializeField] private float c12ExcitedTime = 1.5f;  // C-12* 脈動時間
    [SerializeField] private float be8DisplayTime = 1.8f;  // Be-8 不穩定晃動時間
    [SerializeField] private float betweenStagesGap = 0.4f;  // 各階段之間的小停頓

    // ===== He-4 Flight & Parking =====
    [Header("He-4 Movement & Parking")]
    [SerializeField] private float he4BaseSpeed = 4f;   // 一開始飛出的速度
    [SerializeField] private float heFlyTime = 0.7f; // 從中心飛出的時間
    [SerializeField] private float heMoveToSlotTime = 0.8f; // 從飛行位置移動到右下角格子的時間

    [SerializeField] private Transform heParkingAnchor; // He 集中停靠的起始位置（右下角）
    [SerializeField] private int hePerRow = 4;   // 每列最多放幾顆 He
    [SerializeField] private float heSlotSpacingX = 0.6f; // He 格子在 x 方向的間距
    [SerializeField] private float heSlotSpacingY = 0.45f; // He 格子在 y 方向的間距

    // ===== Energy (MeV) =====
    [Header("Energy (MeV)")]
    [SerializeField] private float energyAlpha1 = 3.76f; // 第一顆 He
    [SerializeField] private float energyAlpha2 = 2.46f; // 第二、第三顆 He

    private float currentEnergy = 0f;

    // ===== Runtime =====
    private Vector3 protonStartPos;
    private Vector3 boronStartPos;

    private bool isPaused = false;
    private bool busy = false;
    private FusionPhase phase = FusionPhase.Idle;

    private GameObject currentBe8;                     // C → He + Be 時產生的 Be 實例
    private readonly List<GameObject> heProducts = new List<GameObject>(); // 場上所有 He

    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FusionSendStatus(string status);

    [DllImport("__Internal")]
    private static extern void FusionSendEnergy(float energy);
    #endif

    private void Awake()
    {
        protonStartPos = proton.position;
        boronStartPos = boron11.position;

        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (pauseButton) pauseButton.onClick.AddListener(OnPauseClicked);

        if (startButtonText) startButtonText.text = "Start";
        if (pauseButtonText) pauseButtonText.text = "Pause";
        if (statusText) statusText.text = "Idle";
        if (energyText) energyText.text = "0.00 MeV";
        if (energyBar) energyBar.value = 0f;

        if (pauseButton) pauseButton.interactable = false;

        Time.timeScale = 1f;
        phase = FusionPhase.Idle;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        isPaused = false;
    }

    // =====================================================
    // Start / Restart 按鈕行為（關鍵）
    // =====================================================
    private void OnStartClicked()
    {
        // Debug 看現在狀態
        Debug.Log($"Start button clicked, phase = {phase}");

        if (phase == FusionPhase.Running)
        {
            // 反應進行中，忽略 Start
            return;
        }

        if (phase == FusionPhase.Finished)
        {
            // ★ 第一按：Restart → 只重設畫面到 Idle，不自動開始
            ResetSceneToIdle();
            phase = FusionPhase.Idle;

            if (startButtonText) startButtonText.text = "Start";
            SetStatus("Ready. Press Start to run again.");

            return;
        }

        // phase == Idle → 真正開始跑
        if (!busy)
        {
            StartCoroutine(RunFusion());
        }
    }

    // =====================================================
    // Pause / Resume
    // =====================================================
    private void OnPauseClicked()
    {
        if (phase != FusionPhase.Running) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseButtonText)
            pauseButtonText.text = isPaused ? "Resume" : "Pause";

        SetStatus(isPaused ? "Paused" : "Running...");
    }

    // =====================================================
    // Main Fusion Sequence
    // =====================================================
    private IEnumerator RunFusion()
    {
        busy = true;
        phase = FusionPhase.Running;
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseButton)
        {
            pauseButton.interactable = true;
            if (pauseButtonText) pauseButtonText.text = "Pause";
        }
        if (startButtonText) startButtonText.text = "Running...";

        SetStatus("Proton approaching B-11...");

        // --- 1. H & B 靠近中心 ---
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, approachTime);
            float k = Mathf.Clamp01(t);

            proton.position = Vector3.Lerp(
                protonStartPos,
                meetPoint.position + Vector3.left * 0.2f,
                k
            );
            boron11.position = Vector3.Lerp(
                boronStartPos,
                meetPoint.position + Vector3.right * 0.2f,
                k
            );

            yield return null;
        }

        // H & B 消失 → 代表已結合成 C
        proton.gameObject.SetActive(false);
        boron11.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        // --- 2. C-12* 激發態 + 破裂成 He + Be ---
        yield return ShowC12ExcitedAndBreak();
        yield return new WaitForSeconds(betweenStagesGap);

        // --- 3. Be-8 衰變到 2 He ---
        if (currentBe8 != null)
        {
            yield return ShowBe8Decay(currentBe8);
            currentBe8 = null;
        }

        // 反應完成：停在最後畫面，不重置
        yield return new WaitForSeconds(0.5f);

        phase = FusionPhase.Finished;
        busy = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseButton) pauseButton.interactable = false;
        if (startButtonText) startButtonText.text = "Restart";

        SetStatus("Fusion finished. Check products, then press Restart.");
    }

    // =====================================================
    // C-12*：脈動 → 收縮 → 破裂成 He + Be
    // =====================================================
    private IEnumerator ShowC12ExcitedAndBreak()
    {
        SetStatus("C-12* excited (unstable)");

        GameObject c12 = Instantiate(nucleusC12Prefab, meetPoint.position, Quaternion.identity);
        Transform tf = c12.transform;
        Atom atom = c12.GetComponent<Atom>();

        Vector3 baseScale = tf.localScale;

        if (atom) StartCoroutine(atom.FadeInShell(0.4f));

        // 輕微脈動
        float t = 0f;
        while (t < c12ExcitedTime)
        {
            t += Time.deltaTime;
            float k = t / c12ExcitedTime;

            float pulse = 1f + 0.06f * Mathf.Sin(k * Mathf.PI * 4f);
            tf.localScale = baseScale * pulse;

            yield return null;
        }

        // 爆之前收縮一下
        float shrinkTime = 0.12f;
        t = 0f;
        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            float k = t / shrinkTime;
            tf.localScale = Vector3.Lerp(baseScale, baseScale * 0.6f, k);
            yield return null;
        }

        // 這一瞬間：C → He + Be
        SetStatus("C-12 → He-4 + Be-8");

        // 第一顆 He（高能，往右上）
        SpawnHe_First(meetPoint.position);
        AddEnergy(energyAlpha1, 0.6f);

        // Be-8（留在中心，後續再衰變）
        currentBe8 = Instantiate(nucleusBe8Prefab, meetPoint.position, Quaternion.identity);
        Atom beAtom = currentBe8.GetComponent<Atom>();
        if (beAtom) StartCoroutine(beAtom.FadeInShell(0.15f));

        Destroy(c12);
    }

    // =====================================================
    // Be-8：晃動 → 收縮 → 2 He
    // =====================================================
    private IEnumerator ShowBe8Decay(GameObject be)
    {
        SetStatus("Be-8 unstable decay");

        Transform tf = be.transform;
        Atom atom = be.GetComponent<Atom>();

        Vector3 basePos = tf.position;
        Vector3 baseScale = tf.localScale;

        // 抖動階段
        float t = 0f;
        float shakeTime = be8DisplayTime * 0.8f;

        while (t < shakeTime)
        {
            t += Time.deltaTime;
            float k = t / shakeTime;

            float shakePosAmp = 0.03f;
            float shakeScaleAmp = 0.04f;

            Vector3 offset = new Vector3(
                Mathf.Sin(k * Mathf.PI * 10f) * shakePosAmp,
                Mathf.Cos(k * Mathf.PI * 11f) * shakePosAmp,
                0f
            );
            tf.position = basePos + offset;

            float sPulse = 1f + shakeScaleAmp * Mathf.Sin(k * Mathf.PI * 8f);
            tf.localScale = baseScale * sPulse;

            yield return null;
        }

        // 收縮段
        float squeezeTime = be8DisplayTime * 0.2f;
        t = 0f;
        while (t < squeezeTime)
        {
            t += Time.deltaTime;
            float k = t / squeezeTime;
            tf.position = basePos;
            tf.localScale = Vector3.Lerp(baseScale, baseScale * 0.5f, k);
            yield return null;
        }

        // Be 消失，同時產生兩顆 He
        SetStatus("Be-8 → He-4 + He-4");

        SpawnHe_Double(basePos);
        AddEnergy(energyAlpha2, 0.5f); // He #2
        AddEnergy(energyAlpha2, 0.5f); // He #3

        if (atom) StartCoroutine(atom.FadeOutShell(0.2f));
        Destroy(be);
    }

    // =====================================================
    // He emission：飛出去 → 收集到右下角
    // =====================================================
    private void SpawnHe_First(Vector3 origin)
    {
        Vector3 dir = new Vector3(0.5f, 0.7f, 0f).normalized;
        SpawnHe(origin, dir, he4BaseSpeed * 1.2f);
    }

    private void SpawnHe_Double(Vector3 origin)
    {
        Vector3 dir1 = new Vector3(0.85f, -0.2f, 0f).normalized;
        Vector3 dir2 = new Vector3(0.4f, -0.8f, 0f).normalized;

        SpawnHe(origin, dir1, he4BaseSpeed);
        SpawnHe(origin, dir2, he4BaseSpeed);
    }

    private void SpawnHe(Vector3 origin, Vector3 dir, float speed)
    {
        if (!nucleusHe4Prefab) return;

        GameObject go = Instantiate(nucleusHe4Prefab, origin, Quaternion.identity);
        heProducts.Add(go);

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (!rb) rb = go.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.mass = 0.01f;
        rb.drag = 0.0f;
        rb.velocity = dir.normalized * speed;

        int slotIndex = heProducts.Count - 1;
        StartCoroutine(HeLifeRoutine(go.transform, rb, slotIndex));
    }

    private IEnumerator HeLifeRoutine(Transform tf, Rigidbody rb, int slotIndex)
    {
        if (tf == null || rb == null) yield break;

        // 第一階段：從中心飛出一小段時間
        float t = 0f;
        while (t < heFlyTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 第二階段：停止物理，移動到右下角停靠格子
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        Vector3 startPos = tf.position;
        Vector3 targetPos = startPos;

        if (heParkingAnchor != null)
        {
            int col = slotIndex % hePerRow;
            int row = slotIndex / hePerRow;

            Vector3 offset = new Vector3(
                col * heSlotSpacingX,
                -row * heSlotSpacingY,
                0f
            );
            targetPos = heParkingAnchor.position + offset;
        }

        t = 0f;
        while (t < heMoveToSlotTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / heMoveToSlotTime);
            tf.position = Vector3.Lerp(startPos, targetPos, k);
            yield return null;
        }

        tf.position = targetPos;
    }

    // =====================================================
    // Energy UI
    // =====================================================
    private void AddEnergy(float deltaMeV, float duration)
    {
        StartCoroutine(EnergyRoutine(deltaMeV, duration));
    }

    private IEnumerator EnergyRoutine(float deltaMeV, float duration)
    {
        float start = currentEnergy;
        float target = currentEnergy + deltaMeV;
        currentEnergy = target;

        float t = 0f;
        float total = energyAlpha1 + 2f * energyAlpha2;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float v = Mathf.Lerp(start, target, k);

            if (energyText)
            {
                energyText.text =
                    v.ToString("F2") + " MeV";
            }

            if (energyBar && total > 0f)
            {
                energyBar.value =
                    v / total;
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            FusionSendEnergy(v);
            #endif

            yield return null;
        }

        if (energyText)
        {
            energyText.text =
                target.ToString("F2") + " MeV";
        }

        if (energyBar && total > 0f)
        {
            energyBar.value =
                target / total;
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        FusionSendEnergy(target);
        #endif
    }

    // =====================================================
    // Reset（按 Restart 時才會呼叫）
    // =====================================================
    private void ResetSceneToIdle()
    {
        Time.timeScale = 1f;
        isPaused = false;

        // 刪掉所有 He
        foreach (var he in heProducts)
        {
            if (he) Destroy(he);
        }
        heProducts.Clear();

        // 刪掉 Be
        if (currentBe8 != null)
        {
            Destroy(currentBe8);
            currentBe8 = null;
        }

        // 清能量 UI
        currentEnergy = 0f;

        if (energyText)
        {
            energyText.text = "0.00 MeV";
        }

        if (energyBar)
        {
            energyBar.value = 0f;
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        FusionSendEnergy(0f);
        #endif

        // H、B 回原位、顯示
        proton.position = protonStartPos;
        boron11.position = boronStartPos;
        proton.gameObject.SetActive(true);
        boron11.gameObject.SetActive(true);

        if (pauseButton) pauseButton.interactable = false;
        if (pauseButtonText) pauseButtonText.text = "Pause";

        SetStatus("Idle");
    }

    // =====================================================
    // Web Control API
    // 提供 FusionWebController 呼叫
    // =====================================================

    /// <summary>
    /// 從網頁開始核融合反應。
    /// Idle 狀態才會真正開始。
    /// </summary>
    public void WebStartFusion()
    {
        if (phase == FusionPhase.Idle && !busy)
        {
            OnStartClicked();
        }
    }


    /// <summary>
    /// 從網頁暫停反應。
    /// </summary>
    public void WebPauseFusion()
    {
        if (
            phase == FusionPhase.Running &&
            !isPaused
        )
        {
            OnPauseClicked();
        }
    }


    /// <summary>
    /// 從網頁繼續反應。
    /// </summary>
    public void WebResumeFusion()
    {
        if (
            phase == FusionPhase.Running &&
            isPaused
        )
        {
            OnPauseClicked();
        }
    }


    /// <summary>
    /// 從網頁重新設定核融合反應。
    /// 不會自動開始下一次反應。
    /// </summary>
    public void WebRestartFusion()
    {
        StopAllCoroutines();

        busy = false;
        isPaused = false;

        Time.timeScale = 1f;

        ResetSceneToIdle();

        phase = FusionPhase.Idle;

        if (startButtonText)
            startButtonText.text = "Start";

        SetStatus("Ready.");
    }

    /*
 * 更新反應狀態，
 * 並在 WebGL 環境下同步回傳給 HTML。
 */
    private void SetStatus(string msg)
    {
        if (statusText)
        {
            statusText.text = msg;
        }

        Debug.Log(msg);

#if UNITY_WEBGL && !UNITY_EDITOR
    FusionSendStatus(msg);
#endif
    }
}
