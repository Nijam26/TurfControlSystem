// Bridges the browser to /hubs/turftimer (see Infrastructure/Realtime/TurfTimerHub.cs).
// The background CountdownBackgroundService pushes "ReceiveTurfStatuses" once a second
// and "ReceiveAnnouncement" whenever a 10/5/1/0-minute threshold is crossed. Every page
// that shows live turf data (Dashboard, TvDisplay) calls turfTimer.start() once and
// turfTimer.stop() when it's disposed.
window.turfTimer = {
    connection: null,

    start: function (dotNetRef) {
        this.stop();

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/turftimer")
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveTurfStatuses", function (statuses) {
            dotNetRef.invokeMethodAsync("OnStatusesReceived", JSON.stringify(statuses));
        });

        this.connection.on("ReceiveAnnouncement", function (announcement) {
            dotNetRef.invokeMethodAsync("OnAnnouncementReceived", JSON.stringify(announcement));
            turfTimer.speak(announcement.message);
        });

        this.connection.start().catch(function (err) {
            console.error("turfTimer connection failed:", err);
        });
    },

    speak: function (text) {
        if ("speechSynthesis" in window && text) {
            window.speechSynthesis.cancel();
            var utterance = new SpeechSynthesisUtterance(text);
            utterance.rate = 0.95;
            window.speechSynthesis.speak(utterance);
        }
    },

    stop: function () {
        if (this.connection) {
            this.connection.stop();
            this.connection = null;
        }
    }
};
