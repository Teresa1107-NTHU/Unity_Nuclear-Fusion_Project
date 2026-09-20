/*
 * Fusion WebGL 與外層網頁的通訊橋接。
 * 將 Unity 的反應狀態、能量數值與反應階段
 * 傳送給外層 Alpha-E 網頁。
 */

mergeInto(LibraryManager.library, {

    FusionSendStatus: function (statusPtr) {

        const status = UTF8ToString(statusPtr);

        window.parent.postMessage(
            {
                source: "fusion-unity",
                type: "FusionStatus",
                status: status
            },
            "*"
        );
    },


    FusionSendEnergy: function (energy) {

        window.parent.postMessage(
            {
                source: "fusion-unity",
                type: "FusionEnergy",
                energy: energy
            },
            "*"
        );
    },


    FusionSendStage: function (stagePtr) {

        const stage = UTF8ToString(stagePtr);

        window.parent.postMessage(
            {
                source: "fusion-unity",
                type: "FusionStage",
                stage: stage
            },
            "*"
        );
    }

});