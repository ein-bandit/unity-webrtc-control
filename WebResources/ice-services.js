
function parseIce(candidateString) {
    // token                  =  1*(alphanum / "-" / "." / "!" / "%" / "*"
    //                              / "_" / "+" / "`" / "'" / "~" )
    var token_re = '[0-9a-zA-Z\\-\\.!\\%\\*_\\+\\`\\\'\\~]+';

    // ice-char               = ALPHA / DIGIT / "+" / "/"
    var ice_char_re = '[a-zA-Z0-9\\+\\/]+';

    // foundation             = 1*32ice-char
    var foundation_re = ice_char_re;

    // component-id           = 1*5DIGIT
    var component_id_re = '[0-9]{1,5}';

    // transport             = "UDP" / transport-extension
    // transport-extension   = token      ; from RFC 3261
    var transport_re = token_re;

    // priority              = 1*10DIGIT
    var priority_re = '[0-9]{1,10}';

    // connection-address SP      ; from RFC 4566
    var connection_address_v4_re = '[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}';
    var connection_address_v6_re = '\\:?(?:[0-9a-fA-F]{0,4}\\:?)+'; // fde8:cd2d:634c:6b00:6deb:9894:734:f75f

    var connection_address_re = '(?:' + connection_address_v4_re + ')|(?:' + connection_address_v6_re + ')';

    // port                      ; port from RFC 4566
    var port_re = '[0-9]{1,5}';

    //  cand-type             = "typ" SP candidate-types
    //  candidate-types       = "host" / "srflx" / "prflx" / "relay" / token
    var cand_type_re = token_re;

    var ICE_RE = '(?:a=)?candidate:(' + foundation_re + ')' + // candidate:599991555 // 'a=' not passed for Firefox (and now for Chrome too)
      '\\s' + '(' + component_id_re + ')' +                 // 2
      '\\s' + '(' + transport_re + ')' +                 // udp
      '\\s' + '(' + priority_re + ')' +                 // 2122260222
      '\\s' + '(' + connection_address_re + ')' +                 // 192.168.1.32 || fde8:cd2d:634c:6b00:6deb:9894:734:f75f
      '\\s' + '(' + port_re + ')' +                 // 49827
      '\\s' + 'typ' +                       // typ
      '\\s' + '(' + cand_type_re + ')' +                 // host
      '(?:' +
      '\\s' + 'raddr' +
      '\\s' + '(' + connection_address_re + ')' +
      '\\s' + 'rport' +
      '\\s' + '(' + port_re + ')' +
      ')?' +
      '(?:' +
      '\\s' + 'generation' +                       // generation
      '\\s' + '(' + '\\d+' + ')' +                 // 0
      ')?' +
      '(?:' +
      '\\s' + 'ufrag' +                       // ufrag
      '\\s' + '(' + ice_char_re + ')' +      // WreAYwhmkiw6SPvs
      ')?';

    var pattern = new RegExp(ICE_RE);
    var parsed = candidateString.match(pattern);

    //console.log('parseIceCandidate(): candidateString:', candidateString);
    //console.log('parseIceCandidate(): pattern:', pattern);
    //console.log('parseIceCandidate(): parsed:', parsed);

    // Check if the string was successfully parsed
    if (!parsed) {
        console.warn('parseIceCandidate(): parsed is empty: \'' + parsed + '\'');
        return null;
    }

    var propNames = [
      'foundation',
      'component_id',
      'transport',
      'priority',
      'localIP',
      'localPort',
      'type',
      'remoteIP',
      'remotePort',
      'generation',
      'ufrag'
    ];

    var candObj = {};
    for (var i = 0; i < propNames.length; i++) {
        candObj[propNames[i]] = parsed[i + 1];
    }
    return candObj;
}

function stringifyIce(iceCandObj) {
    var s = 'candidate:' + iceCandObj.foundation + '' +
          ' ' + iceCandObj.component_id + '' +
          ' ' + iceCandObj.transport + '' +
          ' ' + iceCandObj.priority + '' +
          ' ' + iceCandObj.localIP + '' +
          ' ' + iceCandObj.localPort + '' +
          ' typ ' + iceCandObj.type + '' +
          (iceCandObj.remoteIP ? ' raddr ' + iceCandObj.remoteIP + '' : '') +
          (iceCandObj.remotePort ? ' rport ' + iceCandObj.remotePort + '' : '') +
          (iceCandObj.generation ? ' generation ' + iceCandObj.generation + '' : '') +
          (iceCandObj.ufrag ? ' ufrag ' + iceCandObj.ufrag + '' : '');
    return s;
}


function dumpStat(o, b) {

    var s = "";

    s += formatStat(o);

    if (b != undefined) {
        s += "<br> <br>";
        s += formatStat(b);
    }

    feedbackmsg = feedbackmsgrecv + feedbackmsgsend;

    return s;
}


function formatStat(o) {
    var s = "";
    if (o != undefined) {
        s += o.type + ": " + new Date(o.timestamp).toISOString() + "<br>";
        if (o.ssrc) s += "SSRC: " + o.ssrc + " ";
        if (o.packetsReceived !== undefined) {
            s += "Recvd: " + o.packetsReceived + " packets (" +
                 (o.bytesReceived / 1000000).toFixed(2) + " MB)" + " Lost: " + o.packetsLost;

            feedbackmsgrecv = s;

        } else if (o.packetsSent !== undefined) {
            s += "Sent: " + o.packetsSent + " packets (" + (o.bytesSent / 1000000).toFixed(2) + " MB)";
            feedbackmsgsend = s;
        }

        if (o.bitrateMean !== undefined) {
            s += "<br>Avg. bitrate: " + (o.bitrateMean / 1000000).toFixed(2) + " Mbps (" +
                 (o.bitrateStdDev / 1000000).toFixed(2) + " StdDev)";
            if (o.discardedPackets !== undefined) {
                s += " Discarded packts: " + o.discardedPackets;
            }
        }
        if (o.framerateMean !== undefined) {
            s += "<br>Avg. framerate: " + (o.framerateMean).toFixed(2) + " fps (" +
                 o.framerateStdDev.toFixed(2) + " StdDev)";
            if (o.droppedFrames !== undefined) s += " Dropped frames: " + o.droppedFrames;
            if (o.jitter !== undefined) s += " Jitter: " + o.jitter;
        }
        if (o.googFrameRateReceived !== undefined) {
            s += "<br>googFrameRateReceived: " + o.googFrameRateReceived + " fps";
            s += " googJitterBufferMs: " + o.googJitterBufferMs;
            s += "<br>googFrameReceived: " + o.googFrameWidthReceived + "x" + o.googFrameHeightReceived;
            s += "<br>googCurrentDelayMs: " + o.googCurrentDelayMs;
            s += " googDecodeMs: " + o.googDecodeMs;
        }

        if (o.googFrameRateSent !== undefined) {
            s += "<br>googFrameRateSent: " + o.googFrameRateSent + " fps";
            s += " googEncodeUsagePercent: " + o.googEncodeUsagePercent + "%";
            s += "<br>googFrameSent: " + o.googFrameWidthSent + "x" + o.googFrameHeightSent;
            s += " googAvgEncodeMs: " + o.googAvgEncodeMs;
        }
    }
    return s;
}