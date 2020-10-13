var VideoChat = VideoChat || {};

VideoChat.App = (function (webRTC) {
    var _attachLocalMedia = function (id, ref) {
        navigator.getUserMedia(
            {
                video: true,
                audio: true
            },
            function (stream) {
                console.log('App: Playing my local video feed');
                var videoElement = document.getElementById(id);

                videoElement.srcObject = stream;
                ref.invokeMethodAsync('AttachLocalMediaCallback', true);
            },
            function (error) {
                ref.invokeMethodAsync('AttachLocalMediaCallback', false);
            }
        );
    };

    var _detachLocalMedia = function (id) {
        var videoElement = document.getElementById(id);

        videoElement.srcObject.getTracks().forEach(function(track) {
            track.stop();
        });

        videoElement.srcObject = null;
    }
    
    return {
        attachLocalMedia: _attachLocalMedia,
        detachLocalMedia: _detachLocalMedia
    };
})(VideoChat.WebRTC);

