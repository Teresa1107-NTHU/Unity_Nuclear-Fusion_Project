/*
 * Fusion Web 控制器
 *
 * 用途：
 * 接收 HTML / JavaScript 傳入的控制指令，
 * 再呼叫 FusionController 控制 p–11B 核融合動畫。
 *
 * HTML
 *   ↓
 * WebGL index.html
 *   ↓
 * FusionWebController
 *   ↓
 * FusionController
 */

using UnityEngine;

public class FusionWebController : MonoBehaviour
{
    [Header("Fusion Controller")]
    [SerializeField]
    private FusionController fusionController;


    // =====================================================
    // Start
    // =====================================================

    public void StartFusion(string value)
    {
        Debug.Log(
            "Web → Fusion：Start"
        );

        if (fusionController == null)
        {
            Debug.LogWarning(
                "FusionController 尚未指定。"
            );

            return;
        }

        fusionController.WebStartFusion();
    }


    // =====================================================
    // Pause
    // =====================================================

    public void PauseFusion(string value)
    {
        Debug.Log(
            "Web → Fusion：Pause"
        );

        if (fusionController == null)
            return;

        fusionController.WebPauseFusion();
    }


    // =====================================================
    // Resume
    // =====================================================

    public void ResumeFusion(string value)
    {
        Debug.Log(
            "Web → Fusion：Resume"
        );

        if (fusionController == null)
            return;

        fusionController.WebResumeFusion();
    }


    // =====================================================
    // Restart
    // =====================================================

    public void RestartFusion(string value)
    {
        Debug.Log(
            "Web → Fusion：Restart"
        );

        if (fusionController == null)
            return;

        fusionController.WebRestartFusion();
    }

    // =====================================================
    // Unity Editor 測試
    // =====================================================

    [ContextMenu("TEST - Start Fusion")]
    private void TestStartFusion()
    {
        StartFusion("start");
    }


    [ContextMenu("TEST - Pause Fusion")]
    private void TestPauseFusion()
    {
        PauseFusion("pause");
    }


    [ContextMenu("TEST - Resume Fusion")]
    private void TestResumeFusion()
    {
        ResumeFusion("resume");
    }


    [ContextMenu("TEST - Restart Fusion")]
    private void TestRestartFusion()
    {
        RestartFusion("restart");
    }
}