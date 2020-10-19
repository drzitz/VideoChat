var VideoChat = VideoChat || {};

VideoChat.App = (function () {
    var _ref;
    var _mediaStream;
    var _connections = {};
    var _iceServers = [{ url: 'stun:stun.ekiga.net' }/*, { url: 'stun:74.125.142.127:19302' }*/];

    var _init = function (ref) {
        _ref = ref;
        navigator.getUserMedia(
            {
                video: {
                    width: { ideal: 1920 },
                    height: { ideal: 1080 }
                },
                audio: true
            },
            function (stream) {
                _mediaStream = stream;
                console.log('App: playing my local video feed');

                _attachMedia('my-video', _mediaStream, true);

                ref.invokeMethodAsync('AttachLocalMediaCallback', true);
            },
            function (error) {
                ref.invokeMethodAsync('AttachLocalMediaCallback', false);
            }
        );
    };

    var _attachMedia = function (id, stream, muted) {
        var videoElement = document.getElementById(id);
        videoElement.srcObject = stream;

        if (muted) {
            videoElement.muted = true;
        }
    };

    var _detachMedia = function (id) {
        var videoElement = document.getElementById(id);

        videoElement.srcObject.getTracks().forEach(function (track) {
            track.stop();
        });

        videoElement.srcObject = null;
    };

    var _createConnection = function (connectionId) {
        console.log('WebRTC: creating connection...');
        var connection = new RTCPeerConnection({ iceServers: _iceServers });

        connection.onicecandidate = function (event) {
            if (event.candidate) {
                console.log('WebRTC: new ICE candidate');
                _ref.invokeMethodAsync('SendSignalCallback', JSON.stringify({ "candidate": event.candidate }), connectionId);
            } else {
                console.log('WebRTC: ICE candidate gathering complete');
            }
        };

        connection.onconnectionstatechange = function () {
            console.log('WebRTC: connection state changed to "' + connection.connectionState + '"');
        };

        connection.oniceconnectionstatechange = function () {
            console.log('WebRTC: ice connection state changed to "' + connection.iceConnectionState + '"');
        };

        connection.onaddstream = function (event) {
            console.log('App: binding remote stream to partner window');
            _attachMedia('partner-video', event.stream)
        };

        connection.onremovestream = function (event) {
            console.log('App: removing remote stream from partner window');
            _detachMedia('partner-video');
        };

        _connections[connectionId] = connection;

        return connection;
    };

    var _initiateOffer = function (connectionId) {
        var connection = _getConnection(connectionId);

        console.log('App: adding local stream to connection');
        connection.addStream(_mediaStream);

        connection.createOffer(
            function (desc) {
                connection.setLocalDescription(
                    desc,
                    function () {
                        _ref.invokeMethodAsync('SendSignalCallback', JSON.stringify({ "sdp": connection.localDescription }), connectionId);
                    },
                    function (error) { console.log('Error: setting local description failed. ' + error) });
            },
            function (error) { console.log('Error: creating session description failed. ' + error); });
    };

    var _processSignal = function (connectionId, data) {
        var signal = JSON.parse(data);
        var connection = _getConnection(connectionId);

        console.log('WebRTC: received signal');

        if (signal.sdp) {
            _receivedSdpSignal(connection, connectionId, signal.sdp);
        } else if (signal.candidate) {
            _receivedCandidateSignal(connection, connectionId, signal.candidate);
        }
    };

    var _receivedSdpSignal = function (connection, connectionId, sdp) {
        console.log('WebRTC: processing sdp signal');

        connection.setRemoteDescription(new RTCSessionDescription(sdp),
            function () {
                if (connection.remoteDescription.type == "offer") {
                    console.log('WebRTC: received offer, sending response...');

                    connection.addStream(_mediaStream);

                    connection.createAnswer(
                        function (desc) {
                            connection.setLocalDescription(
                                desc,
                                function () { _ref.invokeMethodAsync('SendSignalCallback', JSON.stringify({ "sdp": connection.localDescription }), connectionId); },
                                function (error) { console.log('Error: setting local description failed. ' + error) });
                        },
                        function (error) { console.log('Error: creating session description failed. ' + error); });
                } else if (connection.remoteDescription.type == "answer") {
                    console.log('WebRTC: received answer');
                }
            },
            function () { console.log('setRemoteDescription error') });
    };

    var _receivedCandidateSignal = function (connection, connectionId, candidate) {
        console.log('WebRTC: processing candidate signal');
        connection.addIceCandidate(new RTCIceCandidate(candidate));
    };

    var _getConnection = function (connectionId) {
        var connection = _connections[connectionId] || _createConnection(connectionId);
        return connection;
    };

    var _closeConnection = function (connectionId) {
        var connection = _connections[connectionId];

        if (connection) {
            _detachMedia('partner-video');

            connection.close();
            delete _connections[connectionId];
        }
    };

    return {
        init: _init,
        initiateOffer: _initiateOffer,
        processSignal: _processSignal,
        closeConnection: _closeConnection,
        connections: _connections
    };
})();

