/*
 * Fusion WebGL 與外層網頁的通訊橋接。
 * 將 Unity 的反應狀態與能量數值傳送給外層 Alpha-E 網頁。
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
    }

});