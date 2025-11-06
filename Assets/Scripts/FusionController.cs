using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FusionController : MonoBehaviour
{
    // ===== Scene objects =====
    [Header("Scene References")]
    [SerializeField] private Transform proton;    // 場景左側的 H-1（請拖場景中的實例）
    [SerializeField] private Transform boron11;   // 場景右側的 B-11（請拖場景中的實例）
    [SerializeField] private Transform meetPoint; // 反應發生點（中心空物件）

    // ===== UI =====
    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider energyBar;    // 可留空
    [SerializeField] private TMP_Text energyText; // 顯示 "8.70 MeV"

    // ===== FX =====
    [Header("FX")]
    [SerializeField] private ParticleSystem protonApproachFx; // 左能量流
    [SerializeField] private ParticleSystem boronApproachFx;  // 右能量流
    [SerializeField] private ParticleSystem flashFx;          // 中央閃光
    [SerializeField] private ParticleSystem energyBurstFx;    // 能量爆發粒子

    // ===== Nucleus prefabs (生成用) =====
    [Header("Nucleus Prefabs")]
    [SerializeField] private GameObject nucleusB11Prefab;
    [SerializeField] private GameObject nucleusC12Prefab;
    [SerializeField] private GameObject nucleusBe8Prefab;
    [SerializeField] private GameObject nucleusHe4Prefab;

    // ===== Motion tuning =====
    [Header("Motion")]
    [SerializeField] private float approachTime = 2.0f; // 靠近時間
    [SerializeField]
    private AnimationCurve approachCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ===== He-4 tuning =====
    [Header("He-4 Flight")]
    [SerializeField] private float he4Speed = 6f;
    [SerializeField] private float he4Life = 4f; // 最大壽命（秒）

    // ===== Energy display =====
    [Header("Energy")]
    [SerializeField] private float fusionMeV = 8.7f;

    // ===== Runtime =====
    private Vector3 protonStartPos, boronStartPos;
    private bool busy = false;

    // 生成的臨時核種與 He-4 清單（避免殘留）
    private GameObject lastB11, lastC12, lastBe8;
    private readonly List<GameObject> activeHe4 = new List<GameObject>();

    private void Awake()
    {
        // 快速檢查（避免未指派）
        if (!proton || !boron11 || !meetPoint)
        {
            Debug.LogError("FusionController: 請在 Inspector 指派 Proton / Boron11 / MeetPoint。");
        }

        protonStartPos = proton ? proton.position : Vector3.zero;
        boronStartPos = boron11 ? boron11.position : Vector3.zero;

        if (startButton) startButton.onClick.AddListener(OnStartClicked);

        SetStatus("Idle");
        if (energyBar) energyBar.value = 0f;
        if (energyText) energyText.text = "0.00 MeV";

        SafeStop(protonApproachFx);
        SafeStop(boronApproachFx);
        SafeStop(flashFx);
        SafeStop(energyBurstFx);
    }

    public void OnStartClicked()
    {
        if (busy) return;
        StartCoroutine(RunFusion());
    }

    private IEnumerator RunFusion()
    {
        busy = true;
        SetStatus("Particles accelerating and approaching...");

        SafePlay(protonApproachFx);
        SafePlay(boronApproachFx);

        // 兩球靠近
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, approachTime);
            float k = approachCurve.Evaluate(Mathf.Clamp01(t));

            if (proton)
                proton.position = Vector3.Lerp(protonStartPos,
                    meetPoint.position + Vector3.left * 0.20f, k);

            if (boron11)
                boron11.position = Vector3.Lerp(boronStartPos,
                    meetPoint.position + Vector3.right * 0.20f, k);

            yield return null;
        }

        SafeStop(protonApproachFx, true);
        SafeStop(boronApproachFx, true);

        // 閃光
        SetStatus("Quantum tunneling succeeded! Fusion triggered!");
        if (flashFx)
        {
            flashFx.transform.position = meetPoint.position;
            flashFx.Play();
        }

        // 隱藏原始兩球
        if (proton) proton.gameObject.SetActive(false);
        if (boron11) boron11.gameObject.SetActive(false);

        // --- 生成 B-11（展示外殼） ---
        lastB11 = Instantiate(nucleusB11Prefab, meetPoint.position, Quaternion.identity);
        var atomB11 = lastB11.GetComponent<Atom>();
        if (atomB11) StartCoroutine(atomB11.FadeInShell(0.5f));

        yield return new WaitForSeconds(0.5f);

        // --- 形成 C-12（短暫） ---
        SetStatus("Forming C-12 (transient)...");
        lastC12 = Instantiate(nucleusC12Prefab, meetPoint.position, Quaternion.identity);
        var atomC12 = lastC12.GetComponent<Atom>();
        if (atomC12) StartCoroutine(atomC12.FadeInShell(0.4f));

        if (atomB11) StartCoroutine(atomB11.FadeOutShell(0.3f));
        if (lastB11) Destroy(lastB11, 0.7f);

        yield return new WaitForSeconds(0.5f);

        // --- 衰變為 Be-8 ---
        SetStatus("Decaying to Be-8...");
        lastBe8 = Instantiate(nucleusBe8Prefab, meetPoint.position, Quaternion.identity);
        var atomBe8 = lastBe8.GetComponent<Atom>();
        if (atomBe8) StartCoroutine(atomBe8.FadeInShell(0.35f));

        if (lastC12) Destroy(lastC12, 0.7f);

        yield return new WaitForSeconds(0.5f);

        // --- Be-8 → 3 × He-4 ---
        SetStatus("Be-8 decays → 3 × He-4 (alpha)!");
        if (atomBe8) StartCoroutine(atomBe8.FadeOutShell(0.25f));

        SpawnHe4Triplet(meetPoint.position);
        if (lastBe8) Destroy(lastBe8, 1.0f);

        // 能量視覺
        ShowEnergyVisual(fusionMeV);

        yield return new WaitForSeconds(1.8f);

        // 重置
        ResetForNext();
        busy = false;
    }

    // 生成 3 顆 He-4，速度向外並加入集中管理
    private void SpawnHe4Triplet(Vector3 origin)
    {
        Vector3[] dirs =
        {
            Vector3.forward,
            Quaternion.Euler(0f, 120f, 0f) * Vector3.forward,
            Quaternion.Euler(0f, 240f, 0f) * Vector3.forward
        };

        for (int i = 0; i < 3; i++)
        {
            GameObject go = Instantiate(nucleusHe4Prefab, origin, Quaternion.identity);

            var rb = go.GetComponent<Rigidbody>();
            if (!rb) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 0.01f;
            rb.drag = 0.1f;

            Vector3 jitter = new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.2f, 0.2f)
            );
            rb.velocity = dirs[i].normalized * he4Speed + jitter;

            // 壽命：避免殘留，最多 2.5s
            float life = Mathf.Min(he4Life, 2.5f);
            Destroy(go, life);

            activeHe4.Add(go);
        }
    }

    // 能量數字、能量條與粒子
    private void ShowEnergyVisual(float mev)
    {
        if (energyBurstFx)
        {
            energyBurstFx.transform.position = meetPoint.position;
            energyBurstFx.Play();
        }

        if (energyText)
            StartCoroutine(CountUpEnergy(mev, 1.2f));

        if (energyBar)
            StartCoroutine(FillEnergyBar(1.2f));
    }

    private IEnumerator CountUpEnergy(float mev, float duration)
    {
        float t = 0f;
        float start = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            float val = Mathf.Lerp(start, mev, k);
            energyText.text = val.ToString("F2") + " MeV";
            yield return null;
        }
        energyText.text = mev.ToString("F2") + " MeV";
    }

    private IEnumerator FillEnergyBar(float duration)
    {
        float t = 0f;
        float v0 = energyBar.value;
        while (t < duration)
        {
            t += Time.deltaTime;
            energyBar.value = Mathf.Lerp(v0, 1f, t / duration);
            yield return null;
        }
        energyBar.value = 1f;
    }

    private void ResetForNext()
    {
        // 清理上一輪所有生成物
        if (lastB11) Destroy(lastB11);
        if (lastC12) Destroy(lastC12);
        if (lastBe8) Destroy(lastBe8);
        lastB11 = lastC12 = lastBe8 = null;

        foreach (var he in activeHe4)
            if (he) Destroy(he);
        activeHe4.Clear();

        // 重設 UI
        if (energyBar) energyBar.value = 0f;
        if (energyText) energyText.text = "0.00 MeV";

        // 把原始兩球放回原位
        if (proton)
        {
            proton.position = protonStartPos;
            proton.gameObject.SetActive(true);
        }
        if (boron11)
        {
            boron11.position = boronStartPos;
            boron11.gameObject.SetActive(true);
        }

        SetStatus("Idle");
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log(msg);
    }

    // 安全控制粒子
    private static void SafePlay(ParticleSystem ps)
    {
        if (ps) ps.Play();
    }
    private static void SafeStop(ParticleSystem ps, bool clear = true)
    {
        if (!ps) return;
        ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear
                            : ParticleSystemStopBehavior.StopEmitting);
    }
}
