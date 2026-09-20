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

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem energyBurst;

    // C-12 裂解效果
    [SerializeField] private ParticleSystem fusionShockwave;
    [SerializeField] private ParticleSystem cBreakEffect;

    // Be-8 裂解效果
    [SerializeField] private ParticleSystem beBreakEffect;

    [Header("Break Effect Scale")]
    [SerializeField] private float c12ShockwaveScale = 1.0f;
    [SerializeField] private float be8ShockwaveScale = 0.65f;

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

    // ===== Approach =====
    [Header("Approach")]
    [SerializeField] private float approachOffset = 0.4f;

    // 接觸後的反應動畫
    [SerializeField] private float impactTime = 0.18f;
    [SerializeField] private float impactShrink = 0.92f;
    [SerializeField] private float c12AppearTime = 0.30f;

    [Header("C-12 Excited Animation")]
    [SerializeField] private float c12PulseAmount = 0.045f;
    [SerializeField] private float c12ShakeAmount = 0.035f;
    [SerializeField] private float c12ShakeSpeed = 22f;

    [Header("C-12 Break Animation")]
    [SerializeField] private float c12BreakShrink = 0.72f;
    [SerializeField] private float c12BreakShrinkTime = 0.10f;

    // 裂解後 He / Be 分開的距離
    [SerializeField] private float c12BreakSeparation = 0.55f;

    // He、Be 從中心分離所需時間
    [SerializeField] private float c12BreakMoveTime = 0.20f;

    // 第一顆 He 的裂解方向
    [SerializeField]
    private Vector3 firstHeBreakDirection =
        new Vector3(-0.65f, 0.45f, 0.15f);

    // Be-8 的裂解方向
    [SerializeField]
    private Vector3 be8BreakDirection =
        new Vector3(0.65f, -0.20f, -0.10f);

    [Header("Be-8 Decay Animation")]
    [SerializeField] private float be8ShakeAmount = 0.035f;
    [SerializeField] private float be8ShakeSpeed = 25f;

    // 裂解前壓縮 / 拉伸
    [SerializeField] private float be8SqueezeTime = 0.16f;
    [SerializeField] private float be8StretchAmount = 1.15f;

    // 兩顆 He 剛裂開時的距離與時間
    [SerializeField] private float be8BreakSeparation = 0.65f;
    [SerializeField] private float be8BreakMoveTime = 0.18f;

    // 兩顆 He 的裂解方向
    [SerializeField]
    private Vector3 secondHeBreakDirection =
        new Vector3(0.85f, 0.30f, 0.18f);

    [SerializeField]
    private Vector3 thirdHeBreakDirection =
        new Vector3(-0.75f, -0.38f, -0.15f);

    // ===== He-4 Flight & Parking =====
    [Header("He-4 Movement & Parking")]
    [SerializeField] private float he4BaseSpeed = 4f;   // 一開始飛出的速度
    [SerializeField] private float heFlyTime = 0.7f; // 從中心飛出的時間
    [SerializeField] private float heMoveToSlotTime = 0.8f; // 從飛行位置移動到右下角格子的時間

    [SerializeField] private Transform heParkingAnchor; // He 集中停靠的起始位置（右下角）
    [SerializeField] private int hePerRow = 4;   // 每列最多放幾顆 He
    [SerializeField] private float heSlotSpacingX = 0.6f; // He 格子在 x 方向的間距
    [SerializeField] private float heSlotSpacingY = 0.45f; // He 格子在 y 方向的間距

    [Header("He-4 Parking Animation")]
    [SerializeField] private float heParkingArcHeight = 0.55f;
    [SerializeField] private float heParkingArcSide = 0.28f;
    [SerializeField] private float heParkingDepth = 0.22f;

    [SerializeField] private float heParkingOvershoot = 0.10f;
    [SerializeField] private float heParkingSettleTime = 0.18f;
    [SerializeField] private float heSlowDownTime = 0.16f;

    [Header("Individual He-4 Motion")]
    [SerializeField] private float he1FlyMultiplier = 1.10f;
    [SerializeField] private float he2FlyMultiplier = 0.82f;
    [SerializeField] private float he3FlyMultiplier = 0.95f;

    [SerializeField] private float he1ArcMultiplier = 1.20f;
    [SerializeField] private float he2ArcMultiplier = 0.70f;
    [SerializeField] private float he3ArcMultiplier = 0.95f;

    [Header("Individual He-4 Return Path")]
    [SerializeField] private float he1ReturnSide = -0.75f;
    [SerializeField] private float he1ReturnHeight = 0.55f;

    [SerializeField] private float he2ReturnSide = 0.85f;
    [SerializeField] private float he2ReturnHeight = 0.30f;

    [SerializeField] private float he3ReturnSide = -0.35f;
    [SerializeField] private float he3ReturnHeight = -0.25f;

    [SerializeField] private float parkingApproachDistance = 0.75f;

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

[DllImport("__Internal")]
private static extern void FusionSendStage(string stage);
#endif

    private void SetStage(string stage)
    {
        Debug.Log("Fusion Stage: " + stage);

#if UNITY_WEBGL && !UNITY_EDITOR
    FusionSendStage(stage);
#endif
    }

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
        SetStage("APPROACH");

        // --- 1. H & B 靠近中心 ---
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, approachTime);

            float k = Mathf.Clamp01(t);

            // SmoothStep：起步慢 → 中間快 → 接觸前減速
            k = k * k * (3f - 2f * k);

            proton.position = Vector3.Lerp(
                protonStartPos,
                meetPoint.position + Vector3.left * approachOffset,
                k
            );

            boron11.position = Vector3.Lerp(
                boronStartPos,
                meetPoint.position + Vector3.right * approachOffset,
                k
            );

            yield return null;
        }

        // --- 接觸瞬間：兩個原子向反應中心擠壓 ---
        SetStatus("Proton captured by B-11...");
        SetStage("CAPTURE");

        Vector3 protonImpactStart = proton.position;
        Vector3 boronImpactStart = boron11.position;

        Vector3 protonBaseScale = proton.localScale;
        Vector3 boronBaseScale = boron11.localScale;

        t = 0f;

        while (t < impactTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / impactTime);

            // 接觸後再往中心壓進去一些
            proton.position = Vector3.Lerp(
                protonImpactStart,
                meetPoint.position + Vector3.left * 0.08f,
                k
            );

            boron11.position = Vector3.Lerp(
                boronImpactStart,
                meetPoint.position + Vector3.right * 0.08f,
                k
            );

            // 越接近中心稍微壓縮
            float scale = Mathf.Lerp(
                1f,
                impactShrink,
                Mathf.Sin(k * Mathf.PI)
            );

            proton.localScale = protonBaseScale * scale;
            boron11.localScale = boronBaseScale * scale;

            yield return null;
        }

        // 確保 scale 恢復
        proton.localScale = protonBaseScale;
        boron11.localScale = boronBaseScale;

        // 到這一刻才真正發生融合
        proton.gameObject.SetActive(false);
        boron11.gameObject.SetActive(false);

        // 接觸中心產生短暫能量爆發
        if (energyBurst != null)
        {
            energyBurst.transform.position = meetPoint.position;
            energyBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            energyBurst.Play();
        }

        yield return new WaitForSeconds(0.12f);

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
        SetStage("FINISHED");
    }

    // =====================================================
    // C-12*：脈動 → 收縮 → 破裂成 He + Be
    // =====================================================
    private IEnumerator ShowC12ExcitedAndBreak()
    {
        SetStatus("C-12* excited (unstable)");
        SetStage("C12_EXCITED");

        GameObject c12 = Instantiate(
    nucleusC12Prefab,
    meetPoint.position,
    Quaternion.identity
);

        Transform tf = c12.transform;
        Atom atom = c12.GetComponent<Atom>();

        Vector3 baseScale = tf.localScale;

        // 一開始縮小
        tf.localScale = baseScale * 0.15f;

        if (atom)
            StartCoroutine(atom.FadeInShell(c12AppearTime));

        // ===== C-12 形成動畫 =====
        float t = 0f;

        while (t < c12AppearTime)
        {
            t += Time.deltaTime;

            float k = Mathf.Clamp01(t / c12AppearTime);

            // SmoothStep
            float smooth = k * k * (3f - 2f * k);

            // 先快速膨脹到 1.12，再回到 1.0
            float scale;

            if (smooth < 0.75f)
            {
                scale = Mathf.Lerp(
                    0.15f,
                    1.12f,
                    smooth / 0.75f
                );
            }
            else
            {
                scale = Mathf.Lerp(
                    1.12f,
                    1f,
                    (smooth - 0.75f) / 0.25f
                );
            }

            tf.localScale = baseScale * scale;

            yield return null;
        }

        tf.localScale = baseScale;

        // ===== C-12* 激發態：脈動 + 越來越不穩定 =====
        Vector3 c12BasePos = tf.position;

        t = 0f;

        while (t < c12ExcitedTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / c12ExcitedTime);

            // -----------------------------
            // 1. 脈動
            // -----------------------------
            // 後期稍微加強
            float pulseStrength = Mathf.Lerp(
                c12PulseAmount,
                c12PulseAmount * 1.8f,
                k
            );

            float pulse =
                1f +
                pulseStrength *
                Mathf.Sin(t * 10f);

            tf.localScale = baseScale * pulse;


            // -----------------------------
            // 2. 不穩定震動
            // -----------------------------
            // 前面很穩，越接近裂解震動越強
            float shakeStrength =
                c12ShakeAmount *
                k * k;

            float shakeX =
                Mathf.Sin(t * c12ShakeSpeed) *
                shakeStrength;

            float shakeY =
                Mathf.Sin(
                    t * c12ShakeSpeed * 1.37f
                ) * shakeStrength;

            float shakeZ =
                Mathf.Sin(
                    t * c12ShakeSpeed * 0.73f
                ) * shakeStrength * 0.6f;

            tf.position =
                c12BasePos +
                new Vector3(
                    shakeX,
                    shakeY,
                    shakeZ
                );

            yield return null;
        }

        // 回到反應中心
        tf.position = c12BasePos;
        tf.localScale = baseScale;

        // =====================================================
        // C-12 裂解前：快速收縮蓄力
        // =====================================================
        t = 0f;

        while (t < c12BreakShrinkTime)
        {
            t += Time.deltaTime;

            float k = Mathf.Clamp01(
                t / Mathf.Max(0.01f, c12BreakShrinkTime)
            );

            // 前面收得慢，最後突然縮進去
            float smooth = k * k;

            tf.localScale = Vector3.Lerp(
                baseScale,
                baseScale * c12BreakShrink,
                smooth
            );

            yield return null;
        }


        // =====================================================
        // 裂解瞬間
        // =====================================================
        SetStatus("C-12 → He-4 + Be-8");
        SetStage("C12_BREAK");

        Vector3 breakPoint = tf.position;


        // 大型 shockwave
        PlayParticleAt(
            fusionShockwave,
            breakPoint,
            c12ShockwaveScale
        );

        // C-12 裂解粒子
        PlayParticleAt(
            cBreakEffect,
            breakPoint,
            1f
        );


        // C-12 消失
        Destroy(c12);


        // =====================================================
        // 產生 He-4 與 Be-8
        // =====================================================

        // ---------- 第一顆 He ----------
        GameObject firstHe = Instantiate(
            nucleusHe4Prefab,
            breakPoint,
            Quaternion.identity
        );

        heProducts.Add(firstHe);


        // ---------- Be-8 ----------
        currentBe8 = Instantiate(
            nucleusBe8Prefab,
            breakPoint,
            Quaternion.identity
        );

        Atom beAtom =
            currentBe8.GetComponent<Atom>();

        if (beAtom)
        {
            StartCoroutine(
                beAtom.FadeInShell(0.15f)
            );
        }


        // =====================================================
        // 計算裂解方向
        // =====================================================

        Vector3 heDir =
            firstHeBreakDirection.normalized;

        Vector3 beDir =
            be8BreakDirection.normalized;


        // 讓兩者大致朝相反方向
        Vector3 heTarget =
            breakPoint +
            heDir * c12BreakSeparation;

        Vector3 beTarget =
            breakPoint +
            beDir * c12BreakSeparation;


        // =====================================================
        // He / Be 從中心彈開
        // =====================================================

        Vector3 heStart =
            firstHe.transform.position;

        Vector3 beStart =
            currentBe8.transform.position;

        t = 0f;

        while (t < c12BreakMoveTime)
        {
            t += Time.deltaTime;

            float k = Mathf.Clamp01(
                t / Mathf.Max(0.01f, c12BreakMoveTime)
            );

            // Ease-out
            // 一開始裂得很快，後面減速
            float smooth =
                1f - Mathf.Pow(1f - k, 3f);


            if (firstHe != null)
            {
                firstHe.transform.position =
                    Vector3.Lerp(
                        heStart,
                        heTarget,
                        smooth
                    );
            }


            if (currentBe8 != null)
            {
                currentBe8.transform.position =
                    Vector3.Lerp(
                        beStart,
                        beTarget,
                        smooth
                    );
            }

            yield return null;
        }


        // =====================================================
        // 第一顆 He 接著飛出去
        // =====================================================

        if (firstHe != null)
        {
            Rigidbody rb =
                firstHe.GetComponent<Rigidbody>();

            if (!rb)
            {
                rb =
                    firstHe.AddComponent<Rigidbody>();
            }

            rb.useGravity = false;
            rb.mass = 0.01f;
            rb.drag = 0f;

            // 接續裂解方向繼續飛
            rb.velocity =
                heDir *
                he4BaseSpeed *
                1.2f;

            int slotIndex =
                heProducts.Count - 1;

            StartCoroutine(
                HeLifeRoutine(
                    firstHe.transform,
                    rb,
                    slotIndex
                )
            );
        }


        // 第一顆 alpha 能量
        AddEnergy(
            energyAlpha1,
            0.6f
        );
    }

    // =====================================================
    // Be-8：晃動 → 收縮 → 2 He
    // =====================================================
    private IEnumerator ShowBe8Decay(GameObject be)
    {
        SetStatus("Be-8 unstable decay");
        SetStage("BE8_UNSTABLE");

        Transform tf = be.transform;

        Vector3 basePos = tf.position;
        Vector3 baseScale = tf.localScale;

        // =====================================================
        // Phase 1：Be-8 越來越不穩定
        // =====================================================

        float shakeTime = be8DisplayTime * 0.72f;
        float t = 0f;

        while (t < shakeTime)
        {
            t += Time.deltaTime;

            float k = Mathf.Clamp01(
                t / Mathf.Max(0.01f, shakeTime)
            );

            // 前面穩定，後面震動快速增加
            float strength =
                be8ShakeAmount *
                Mathf.Lerp(0.25f, 1.8f, k * k);

            float shakeX =
                Mathf.Sin(
                    t * be8ShakeSpeed
                ) * strength;

            float shakeY =
                Mathf.Sin(
                    t * be8ShakeSpeed * 1.31f
                ) * strength;

            float shakeZ =
                Mathf.Sin(
                    t * be8ShakeSpeed * 0.77f
                ) * strength * 0.8f;

            tf.position =
                basePos +
                new Vector3(
                    shakeX,
                    shakeY,
                    shakeZ
                );


            // 同時有輕微脈動
            float pulse =
                1f +
                Mathf.Sin(t * 11f) *
                0.025f *
                (0.4f + k);

            tf.localScale =
                baseScale * pulse;

            yield return null;
        }


        // =====================================================
        // Phase 2：裂解前拉伸 / 壓縮
        // =====================================================

        tf.position = basePos;

        Vector3 stretchScale =
            new Vector3(
                baseScale.x * be8StretchAmount,
                baseScale.y * 0.78f,
                baseScale.z * 0.90f
            );

        t = 0f;

        while (t < be8SqueezeTime)
        {
            t += Time.deltaTime;

            float k = Mathf.Clamp01(
                t / Mathf.Max(0.01f, be8SqueezeTime)
            );

            // 越到最後變形越明顯
            float smooth = k * k;

            tf.localScale =
                Vector3.Lerp(
                    baseScale,
                    stretchScale,
                    smooth
                );

            // 最後再加一點快速震動
            float finalShake =
                be8ShakeAmount *
                0.6f *
                k;

            tf.position =
                basePos +
                new Vector3(
                    Mathf.Sin(t * 45f) * finalShake,
                    Mathf.Cos(t * 39f) * finalShake,
                    Mathf.Sin(t * 31f) * finalShake
                );

            yield return null;
        }


        // =====================================================
        // Phase 3：Be-8 裂解
        // =====================================================

        SetStatus("Be-8 → He-4 + He-4");
        SetStage("BE8_BREAK");

        Vector3 breakPoint = basePos;

        // Be-8 使用比較小的 shockwave
        PlayParticleAt(
            fusionShockwave,
            breakPoint,
            be8ShockwaveScale
        );

        // Be-8 裂解粒子
        PlayParticleAt(
            beBreakEffect,
            breakPoint,
            0.75f
        );


        // Be-8 消失
        Destroy(be);

        // currentBe8 也清掉，避免 Reset 再處理
        currentBe8 = null;


        // =====================================================
        // Phase 4：產生兩顆 He-4
        // =====================================================

        GameObject he2 = Instantiate(
            nucleusHe4Prefab,
            breakPoint,
            Quaternion.identity
        );

        GameObject he3 = Instantiate(
            nucleusHe4Prefab,
            breakPoint,
            Quaternion.identity
        );

        heProducts.Add(he2);
        heProducts.Add(he3);


        // =====================================================
        // 裂解方向
        // =====================================================

        Vector3 dir2 =
            secondHeBreakDirection.normalized;

        Vector3 dir3 =
            thirdHeBreakDirection.normalized;


        Vector3 he2Start =
            breakPoint;

        Vector3 he3Start =
            breakPoint;


        Vector3 he2Target =
            breakPoint +
            dir2 * be8BreakSeparation;

        Vector3 he3Target =
            breakPoint +
            dir3 * be8BreakSeparation;


        // =====================================================
        // Phase 5：兩顆 He 瞬間彈開
        // =====================================================

        t = 0f;

        while (t < be8BreakMoveTime)
        {
            t += Time.deltaTime;

            float k = Mathf.Clamp01(
                t / Mathf.Max(0.01f, be8BreakMoveTime)
            );

            // 強烈 ease-out
            // 一開始非常快，後面稍微減速
            float smooth =
                1f - Mathf.Pow(1f - k, 3f);


            if (he2 != null)
            {
                he2.transform.position =
                    Vector3.Lerp(
                        he2Start,
                        he2Target,
                        smooth
                    );
            }


            if (he3 != null)
            {
                he3.transform.position =
                    Vector3.Lerp(
                        he3Start,
                        he3Target,
                        smooth
                    );
            }

            yield return null;
        }


        // =====================================================
        // Phase 6：接續飛行
        // =====================================================

        if (he2 != null)
        {
            Rigidbody rb2 =
                he2.GetComponent<Rigidbody>();

            if (!rb2)
                rb2 = he2.AddComponent<Rigidbody>();

            rb2.useGravity = false;
            rb2.mass = 0.01f;
            rb2.drag = 0f;

            rb2.velocity =
                dir2 * he4BaseSpeed;

            // 第一顆 He 已經是 index 0
            // he2 通常會是 index 1
            int slotIndex2 =
                heProducts.IndexOf(he2);

            StartCoroutine(
                HeLifeRoutine(
                    he2.transform,
                    rb2,
                    slotIndex2
                )
            );
        }


        if (he3 != null)
        {
            Rigidbody rb3 =
                he3.GetComponent<Rigidbody>();

            if (!rb3)
                rb3 = he3.AddComponent<Rigidbody>();

            rb3.useGravity = false;
            rb3.mass = 0.01f;
            rb3.drag = 0f;

            rb3.velocity =
                dir3 * he4BaseSpeed;

            int slotIndex3 =
                heProducts.IndexOf(he3);

            StartCoroutine(
                HeLifeRoutine(
                    he3.transform,
                    rb3,
                    slotIndex3
                )
            );
        }


        // =====================================================
        // Energy
        // =====================================================

        AddEnergy(
            energyAlpha2,
            0.5f
        );

        AddEnergy(
            energyAlpha2,
            0.5f
        );
    }

    private void PlayParticleAt(
    ParticleSystem ps,
    Vector3 position,
    float scaleMultiplier = 1f
)
    {
        if (ps == null)
            return;

        ps.transform.position = position;

        ps.transform.localScale =
            Vector3.one * scaleMultiplier;

        ps.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        ps.Play();
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

    private IEnumerator HeLifeRoutine(
    Transform tf,
    Rigidbody rb,
    int slotIndex
)
    {
        if (tf == null || rb == null)
            yield break;


        // =====================================================
        // 每顆 He 各自的參數
        // =====================================================

        float flyMultiplier = 1f;
        float arcMultiplier = 1f;

        float returnSide = 0f;
        float returnHeight = 0f;


        if (slotIndex == 0)
        {
            flyMultiplier = he1FlyMultiplier;
            arcMultiplier = he1ArcMultiplier;

            returnSide = he1ReturnSide;
            returnHeight = he1ReturnHeight;
        }
        else if (slotIndex == 1)
        {
            flyMultiplier = he2FlyMultiplier;
            arcMultiplier = he2ArcMultiplier;

            returnSide = he2ReturnSide;
            returnHeight = he2ReturnHeight;
        }
        else if (slotIndex == 2)
        {
            flyMultiplier = he3FlyMultiplier;
            arcMultiplier = he3ArcMultiplier;

            returnSide = he3ReturnSide;
            returnHeight = he3ReturnHeight;
        }


        // =====================================================
        // Phase 1：裂解後自由飛行
        // =====================================================

        float actualFlyTime =
            heFlyTime * flyMultiplier;

        float t = 0f;

        while (t < actualFlyTime)
        {
            if (tf == null || rb == null)
                yield break;

            t += Time.deltaTime;

            yield return null;
        }


        // =====================================================
        // 記錄原本的飛行慣性
        // =====================================================

        Vector3 incomingVelocity =
            rb.velocity;

        Vector3 incomingDirection =
            incomingVelocity.sqrMagnitude > 0.0001f
            ? incomingVelocity.normalized
            : Vector3.right;

        float incomingSpeed =
            incomingVelocity.magnitude;


        // =====================================================
        // Phase 2：稍微減速
        //
        // 不停下來。
        // 保留一部分原本的速度，避免「急煞車」。
        // =====================================================

        t = 0f;

        while (t < heSlowDownTime)
        {
            if (tf == null || rb == null)
                yield break;

            t += Time.deltaTime;

            float k =
                Mathf.Clamp01(
                    t /
                    Mathf.Max(
                        0.01f,
                        heSlowDownTime
                    )
                );

            float smooth =
                k * k *
                (3f - 2f * k);

            float speedScale =
                Mathf.Lerp(
                    1f,
                    0.42f,
                    smooth
                );

            rb.velocity =
                incomingVelocity *
                speedScale;

            yield return null;
        }


        // =====================================================
        // Phase 3：交給曲線動畫
        // =====================================================

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.isKinematic =
            true;


        Vector3 startPos =
            tf.position;


        // =====================================================
        // 最終 Parking Slot
        // =====================================================

        Vector3 targetPos =
            startPos;

        if (heParkingAnchor != null)
        {
            int col =
                slotIndex %
                hePerRow;

            int row =
                slotIndex /
                hePerRow;

            Vector3 offset =
                new Vector3(
                    col * heSlotSpacingX,
                    -row * heSlotSpacingY,
                    0f
                );

            targetPos =
                heParkingAnchor.position +
                offset;
        }


        // =====================================================
        // Cubic Bezier
        //
        // P0 = startPos
        //
        // P1 = 延續原本爆炸方向
        //
        // P2 = 每顆 He 自己的「回收航線」
        //
        // P3 = Parking Slot
        // =====================================================


        // -----------------------------------------------------
        // Control Point 1
        //
        // 最重要：
        // 一開始絕對不要立刻朝 Parking。
        // 先沿原本的裂解方向繼續飛。
        // -----------------------------------------------------

        float momentumDistance =
            Mathf.Clamp(
                incomingSpeed * 0.38f,
                0.45f,
                1.10f
            );


        Vector3 control1 =
            startPos +
            incomingDirection *
            momentumDistance;


        // -----------------------------------------------------
        // 計算 Parking 的進場方向
        // -----------------------------------------------------

        Vector3 parkingDirection =
            targetPos -
            startPos;

        if (parkingDirection.sqrMagnitude < 0.0001f)
        {
            parkingDirection =
                Vector3.right;
        }

        parkingDirection.Normalize();


        // -----------------------------------------------------
        // Control Point 2
        //
        // 三顆 He 不再共用同一種曲線。
        //
        // returnSide：
        //     控制左右繞行
        //
        // returnHeight：
        //     控制上下繞行
        // -----------------------------------------------------

        Vector3 control2 =
            targetPos
            -
            parkingDirection *
            parkingApproachDistance;


        control2 +=
            Vector3.right *
            returnSide;


        control2 +=
            Vector3.up *
            returnHeight *
            arcMultiplier;


        // -----------------------------------------------------
        // Z 軸深度
        //
        // 讓三顆 He 不完全在同一平面交叉。
        // -----------------------------------------------------

        float depthDirection = 0f;

        if (slotIndex == 0)
            depthDirection = 1f;

        else if (slotIndex == 1)
            depthDirection = -1f;

        else if (slotIndex == 2)
            depthDirection = 0.45f;


        control2 +=
            Vector3.forward *
            heParkingDepth *
            depthDirection;


        // =====================================================
        // Phase 4：沿 Cubic Bezier 回到 Parking
        // =====================================================

        t = 0f;

        while (t < heMoveToSlotTime)
        {
            if (tf == null)
                yield break;

            t +=
                Time.deltaTime;


            float k =
                Mathf.Clamp01(
                    t /
                    Mathf.Max(
                        0.01f,
                        heMoveToSlotTime
                    )
                );


            // -------------------------------------------------
            // 時間曲線
            //
            // 前面稍微快，
            // 最後才慢慢進 Parking。
            // -------------------------------------------------

            float smooth =
                1f -
                Mathf.Pow(
                    1f - k,
                    2.2f
                );


            float u =
                1f -
                smooth;


            // =================================================
            // Cubic Bezier
            // =================================================

            Vector3 position =

                u * u * u *
                startPos

                +

                3f *
                u * u *
                smooth *
                control1

                +

                3f *
                u *
                smooth * smooth *
                control2

                +

                smooth *
                smooth *
                smooth *
                targetPos;


            tf.position =
                position;


            yield return null;
        }


        // =====================================================
        // Phase 5：很小的 Overshoot
        //
        // 這裡只做很小的「落位感」，
        // 不再讓 He 明顯飛過頭。
        // =====================================================

        Vector3 arrivalDirection =
            targetPos -
            control2;

        if (arrivalDirection.sqrMagnitude < 0.0001f)
        {
            arrivalDirection =
                Vector3.right;
        }

        arrivalDirection.Normalize();


        Vector3 overshootPos =
            targetPos +
            arrivalDirection *
            heParkingOvershoot;


        Vector3 settleStart =
            tf.position;


        float halfSettle =
            heParkingSettleTime *
            0.5f;


        // =====================================================
        // Phase 6：稍微超過
        // =====================================================

        t = 0f;

        while (t < halfSettle)
        {
            if (tf == null)
                yield break;

            t +=
                Time.deltaTime;


            float k =
                Mathf.Clamp01(
                    t /
                    Mathf.Max(
                        0.01f,
                        halfSettle
                    )
                );


            float smooth =
                1f -
                Mathf.Pow(
                    1f - k,
                    2f
                );


            tf.position =
                Vector3.Lerp(
                    settleStart,
                    overshootPos,
                    smooth
                );


            yield return null;
        }


        // =====================================================
        // Phase 7：回到真正 Parking Slot
        // =====================================================

        Vector3 returnStart =
            tf.position;

        t = 0f;


        while (t < halfSettle)
        {
            if (tf == null)
                yield break;

            t +=
                Time.deltaTime;


            float k =
                Mathf.Clamp01(
                    t /
                    Mathf.Max(
                        0.01f,
                        halfSettle
                    )
                );


            float smooth =
                1f -
                Mathf.Pow(
                    1f - k,
                    3f
                );


            tf.position =
                Vector3.Lerp(
                    returnStart,
                    targetPos,
                    smooth
                );


            yield return null;
        }


        // =====================================================
        // 最終位置
        // =====================================================

        tf.position =
            targetPos;
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
        SetStage("IDLE");
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
        SetStage("IDLE");

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
